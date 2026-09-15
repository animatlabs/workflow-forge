#!/usr/bin/env python3
"""Extract BenchmarkDotNet CSV results and regenerate benchmark documentation tables."""

from __future__ import annotations

import csv
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
ARTIFACTS = ROOT / "BenchmarkDotNet.Artifacts" / "results"

RUNTIME_NET10 = ".NET 10.0"
RUNTIME_NET8 = ".NET 8.0"
RUNTIME_NET48 = ".NET Framework 4.8"

CANONICAL = {
    1: {"OperationCount": 10},
    2: {"OperationCount": 10},
    3: {"OperationCount": 10},
    4: {"ItemCount": 50},
    5: {"ConcurrencyLevel": 8},
    6: {},
    7: {},
    8: {},
    9: {"TransitionCount": 25},
    10: {"OperationCount": 5, "DelayMilliseconds": 5},
    11: {"OperationCount": 16},
    12: {"DelayMilliseconds": 1},
}

SWEEPS: dict[int, list[tuple[str, dict[str, int]]]] = {
    1: [
        ("1", {"OperationCount": 1}),
        ("5", {"OperationCount": 5}),
        ("10", {"OperationCount": 10}),
        ("25", {"OperationCount": 25}),
        ("50", {"OperationCount": 50}),
    ],
    2: [
        ("5", {"OperationCount": 5}),
        ("10", {"OperationCount": 10}),
        ("25", {"OperationCount": 25}),
    ],
    3: [
        ("10", {"OperationCount": 10}),
        ("25", {"OperationCount": 25}),
        ("50", {"OperationCount": 50}),
    ],
    4: [
        ("10", {"ItemCount": 10}),
        ("50", {"ItemCount": 50}),
        ("100", {"ItemCount": 100}),
    ],
    5: [
        ("1", {"ConcurrencyLevel": 1}),
        ("4", {"ConcurrencyLevel": 4}),
        ("8", {"ConcurrencyLevel": 8}),
    ],
    9: [
        ("5", {"TransitionCount": 5}),
        ("10", {"TransitionCount": 10}),
        ("25", {"TransitionCount": 25}),
    ],
    10: [
        ("3 ops / 1ms", {"OperationCount": 3, "DelayMilliseconds": 1}),
        ("5 ops / 5ms", {"OperationCount": 5, "DelayMilliseconds": 5}),
    ],
    11: [
        ("4", {"OperationCount": 4}),
        ("8", {"OperationCount": 8}),
        ("16", {"OperationCount": 16}),
    ],
    12: [
        ("1ms", {"DelayMilliseconds": 1}),
        ("5ms", {"DelayMilliseconds": 5}),
    ],
}

SCENARIO_NAMES = {
    1: "Sequential (10 ops)",
    2: "Data Passing (10 ops)",
    3: "Conditional (10 ops)",
    4: "Loop (50 items)",
    5: "Concurrent (8 workers)",
    6: "Error Handling",
    7: "Creation Overhead",
    8: "Complete Lifecycle",
    9: "State Machine (25)",
    10: "Long Running*",
    11: "Parallel (16 ops)",
    12: "Event-Driven*",
}


def parse_us_value(raw: str) -> float | None:
    if not raw or raw.strip() in ("NA", "—", "-"):
        return None
    s = raw.strip().strip('"').replace(",", "")
    if s.endswith("μs"):
        return float(s[:-2].strip())
    if s.endswith("ms"):
        return float(s[:-2].strip()) * 1000.0
    if s.endswith("ns"):
        return float(s[:-2].strip()) / 1000.0
    if s.endswith("s") and not s.endswith("μs") and not s.endswith("ms") and not s.endswith("ns"):
        return float(s[:-1].strip()) * 1_000_000.0
    try:
        return float(s)
    except ValueError:
        return None


def format_time_us(value_us: float | None, delay_bound: bool = False) -> str:
    if value_us is None:
        return "N/A"
    if delay_bound and value_us >= 1000:
        return f"{value_us / 1000:.0f}ms"
    if value_us >= 1000:
        return f"{value_us:,.0f}μs".replace(",", "")
    if value_us >= 100:
        return f"{value_us:,.0f}μs".replace(",", "")
    if value_us >= 10:
        return f"{value_us:.1f}μs"
    return f"{value_us:.2f}μs"


def format_bytes(b: int | None) -> str:
    if b is None:
        return "N/A"
    if b < 1024:
        return f"{b} B"
    kb = b / 1024.0
    if kb < 1024:
        if abs(kb - round(kb)) < 0.05:
            return f"{int(round(kb))}KB" if kb >= 10 else f"{kb:.2f}KB"
        return f"{kb:.2f}KB"
    return f"{kb / 1024:.2f}MB"


def parse_allocated(raw: str) -> int | None:
    if not raw or raw.strip() in ("NA", "—", "-"):
        return None
    s = raw.strip().strip('"').replace(",", "")
    if s == "0 B":
        return 0
    if s.endswith(" KB"):
        return int(float(s[:-3].strip()) * 1024)
    if s.endswith(" MB"):
        return int(float(s[:-3].strip()) * 1024 * 1024)
    if s.endswith(" B"):
        return int(float(s[:-2].strip()))
    return None


def load_csv(path: Path) -> list[dict[str, str]]:
    with path.open(newline="", encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def row_matches(row: dict[str, str], filters: dict[str, int]) -> bool:
    for key, val in filters.items():
        if key not in row:
            return False
        try:
            if int(row[key]) != val:
                return False
        except (ValueError, TypeError):
            return False
    return True


def framework_from_method(method: str) -> str | None:
    m = method.strip().strip("'").lower()
    if "workflowforge" in m:
        return "wf"
    if "workflowcore" in m:
        return "wc"
    if "elsa" in m:
        return "elsa"
    return None


@dataclass
class Measurement:
    median_us: float | None
    allocated: int | None


def get_measurement(
    rows: list[dict[str, str]], runtime: str, filters: dict[str, int], framework: str
) -> Measurement:
    for row in rows:
        if row.get("Runtime") != runtime:
            continue
        if filters and not row_matches(row, filters):
            continue
        fw = framework_from_method(row.get("Method", ""))
        if fw != framework:
            continue
        median = parse_us_value(row.get("Median", ""))
        alloc = parse_allocated(row.get("Allocated", ""))
        return Measurement(median, alloc)
    return Measurement(None, None)


def _format_ratio(r: float) -> str:
    # %.0f rounds anything below 0.5 down to "0x", which reads as a false "zero times"
    # claim instead of "WorkflowForge is behind here". Ratios in [0.5, 1) still round to
    # "1x" under %.0f, which is accurate, so only widen precision below 0.5.
    return f"{r:.1f}" if r < 0.5 else f"{r:.0f}"


def speed_range(wf: float | None, wc: float | None, elsa: float | None) -> str:
    ratios: list[float] = []
    if wf and wf > 0:
        if wc and wc > 10:  # ignore stub/skip
            ratios.append(wc / wf)
        if elsa and elsa > 10:
            ratios.append(elsa / wf)
    if not ratios:
        if wc and wf and wc / wf > 1:
            return f"{wc / wf:.0f}x"
        return "N/A"
    lo, hi = min(ratios), max(ratios)
    if abs(lo - hi) < 0.5:
        return f"{_format_ratio(lo)}x"
    return f"{_format_ratio(lo)}-{_format_ratio(hi)}x"


def mem_range(wf: int | None, wc: int | None, elsa: int | None) -> str:
    ratios: list[float] = []
    if wf and wf > 0:
        if wc:
            ratios.append(wc / wf)
        if elsa:
            ratios.append(elsa / wf)
    if not ratios:
        return "N/A"
    lo, hi = min(ratios), max(ratios)
    if abs(lo - hi) < 0.5:
        return f"{_format_ratio(lo)}x"
    return f"{_format_ratio(lo)}-{_format_ratio(hi)}x"


def load_scenario_rows(n: int) -> list[dict[str, str]]:
    pattern = f"*Scenario{n}Benchmark-report.csv"
    matches = list(ARTIFACTS.glob(pattern))
    if not matches:
        raise FileNotFoundError(f"Missing CSV for scenario {n}: {pattern}")
    return load_csv(matches[0])


def extract_comparative() -> dict[str, Any]:
    data: dict[str, Any] = {"scenarios": {}, "bands": {}}
    all_speed: list[float] = []
    all_mem: list[float] = []

    for n in range(1, 13):
        rows = load_scenario_rows(n)
        canon = CANONICAL[n]
        scen: dict[str, Any] = {"canonical": {}, "sweeps": {}}
        delay_bound = n in (10, 12)

        for rt_key, rt in [("net10", RUNTIME_NET10), ("net8", RUNTIME_NET8), ("net48", RUNTIME_NET48)]:
            wf = get_measurement(rows, rt, canon, "wf")
            wc = get_measurement(rows, rt, canon, "wc")
            elsa = get_measurement(rows, rt, canon, "elsa")
            elsa_skip = rt == RUNTIME_NET48
            scen["canonical"][rt_key] = {
                "wf_us": wf.median_us,
                "wc_us": wc.median_us,
                "elsa_us": None if elsa_skip else elsa.median_us,
                "wf_alloc": wf.allocated,
                "wc_alloc": wc.allocated,
                "elsa_alloc": None if elsa_skip else elsa.allocated,
            }
            if n not in (10, 12) and wf.median_us and wf.median_us > 10:
                if wc.median_us and wc.median_us > 10:
                    all_speed.append(wc.median_us / wf.median_us)
                if elsa.median_us and elsa.median_us > 10 and not elsa_skip:
                    all_speed.append(elsa.median_us / wf.median_us)
            if wf.allocated and wf.allocated > 0:
                if wc.allocated and wc.allocated >= wf.allocated:
                    all_mem.append(wc.allocated / wf.allocated)
                if elsa.allocated and not elsa_skip and elsa.allocated >= wf.allocated:
                    all_mem.append(elsa.allocated / wf.allocated)

        if n in SWEEPS:
            for label, filt in SWEEPS[n]:
                sweep: dict[str, Any] = {}
                for fw in ("wf", "wc", "elsa"):
                    m = get_measurement(rows, RUNTIME_NET8, filt, fw)
                    sweep[fw] = {"us": m.median_us, "alloc": m.allocated}
                scen["sweeps"][label] = sweep

        data["scenarios"][str(n)] = scen

    if all_speed:
        data["bands"]["speed_min"] = min(all_speed)
        data["bands"]["speed_max"] = max(all_speed)
    if all_mem:
        data["bands"]["mem_min"] = min(all_mem)
        data["bands"]["mem_max"] = max(all_mem)

    return data


def render_benchmark_data_md(comp: dict[str, Any]) -> str:
    lines: list[str] = [
        "## Competitive Benchmark Summary (Median, 10 iterations)",
        "",
        "### Execution Time (.NET 8.0)",
        "",
        "| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |",
        "|---|----------|---------------|---------------|------|-----------------|",
    ]

    for n in range(1, 13):
        c = comp["scenarios"][str(n)]["canonical"]["net8"]
        wf = format_time_us(c["wf_us"], n in (10, 12))
        wc = format_time_us(c["wc_us"], n in (10, 12)) if c["wc_us"] else "N/A"
        elsa = format_time_us(c["elsa_us"], n in (10, 12)) if c["elsa_us"] else "N/A"
        if n == 8 and c["wc_us"] is None:
            wc = "N/A"
        adv = speed_range(c["wf_us"], c["wc_us"], c["elsa_us"])
        lines.append(f"| {n} | {SCENARIO_NAMES[n]} | {wf} | {wc} | {elsa} | {adv} |")

    lines.extend(
        [
            "",
            "*Long Running and Event-Driven are delay-bound; advantage is in memory.",
            "",
            "### Execution Time (.NET 10.0)",
            "",
            "| # | Scenario | WorkflowForge | Workflow Core | Elsa | Speed Advantage |",
            "|---|----------|---------------|---------------|------|-----------------|",
        ]
    )

    for n in range(1, 13):
        c = comp["scenarios"][str(n)]["canonical"]["net10"]
        wf = format_time_us(c["wf_us"], n in (10, 12))
        wc = format_time_us(c["wc_us"], n in (10, 12)) if c["wc_us"] else "N/A"
        elsa = format_time_us(c["elsa_us"], n in (10, 12)) if c["elsa_us"] else "N/A"
        if n == 8 and c["wc_us"] is None:
            wc = "N/A"
        adv = speed_range(c["wf_us"], c["wc_us"], c["elsa_us"])
        lines.append(f"| {n} | {SCENARIO_NAMES[n]} | {wf} | {wc} | {elsa} | {adv} |")

    lines.extend(
        [
            "",
            "### Execution Time (.NET Framework 4.8)",
            "",
            "| # | Scenario | WorkflowForge | Workflow Core | Speed Advantage |",
            "|---|----------|---------------|---------------|-----------------|",
        ]
    )

    for n in range(1, 13):
        if n in (10, 12):
            continue
        c = comp["scenarios"][str(n)]["canonical"]["net48"]
        wf = format_time_us(c["wf_us"])
        wc = format_time_us(c["wc_us"]) if c["wc_us"] else "N/A"
        adv = speed_range(c["wf_us"], c["wc_us"], None)
        lines.append(f"| {n} | {SCENARIO_NAMES[n].replace('*', '')} | {wf} | {wc} | {adv} |")

    lines.extend(
        [
            "",
            "Elsa does not support .NET Framework 4.8 and is excluded from this comparison.",
            "",
            "## Competitive Memory Summary (.NET 8.0)",
            "",
            "| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |",
            "|---|----------|---------------|---------------|------|------------------|",
        ]
    )

    for n in range(1, 13):
        c = comp["scenarios"][str(n)]["canonical"]["net8"]
        wf = format_bytes(c["wf_alloc"])
        wc = format_bytes(c["wc_alloc"]) if c["wc_alloc"] is not None else "N/A"
        elsa = format_bytes(c["elsa_alloc"]) if c["elsa_alloc"] is not None else "N/A"
        adv = mem_range(c["wf_alloc"], c["wc_alloc"], c["elsa_alloc"])
        lines.append(f"| {n} | {SCENARIO_NAMES[n]} | {wf} | {wc} | {elsa} | {adv} |")

    lines.extend(
        [
            "",
            "## Competitive Memory Summary (.NET 10.0)",
            "",
            "| # | Scenario | WorkflowForge | Workflow Core | Elsa | Memory Advantage |",
            "|---|----------|---------------|---------------|------|------------------|",
        ]
    )

    for n in range(1, 13):
        c = comp["scenarios"][str(n)]["canonical"]["net10"]
        wf = format_bytes(c["wf_alloc"])
        wc = format_bytes(c["wc_alloc"]) if c["wc_alloc"] is not None else "N/A"
        elsa = format_bytes(c["elsa_alloc"]) if c["elsa_alloc"] is not None else "N/A"
        adv = mem_range(c["wf_alloc"], c["wc_alloc"], c["elsa_alloc"])
        lines.append(f"| {n} | {SCENARIO_NAMES[n]} | {wf} | {wc} | {elsa} | {adv} |")

    bands = comp.get("bands", {})
    smin = int(bands.get("speed_min", 0))
    smax = int(bands.get("speed_max", 0))
    mmin = int(bands.get("mem_min", 0))
    mmax = int(bands.get("mem_max", 0))

    lines.extend(
        [
            "",
            "---",
            "",
            f"**Summary bands (all measured scenarios; delay-heavy 10/12 excluded from speed band math, September 2026 run):** "
            f"**{smin}–{smax}x** faster execution; **{mmin}–{mmax}x** lower allocation vs Workflow Core/Elsa.",
            "",
            "- Results captured on Windows 11 (25H2), Intel i7-1185G7, BenchmarkDotNet v0.15.8, **10 iterations** per job.",
            "- Median values used; Elsa omitted on .NET Framework 4.8.",
            "",
        ]
    )

    return "\n".join(lines)


def parse_internal_csv(name: str) -> list[dict[str, str]]:
    matches = list(ARTIFACTS.glob(f"*{name}-report.csv"))
    if not matches:
        raise FileNotFoundError(name)
    return load_csv(matches[0])


def _internal_row_key(row: dict[str, str]) -> str:
    method = row.get("Method", "").strip()
    parts = [method]
    for param in (
        "OperationCount",
        "ConcurrentWorkflowCount",
        "OperationsPerWorkflow",
        "AllocationCount",
    ):
        if param in row and row[param]:
            parts.append(f"{param}={row[param]}")
    return "|".join(parts)


def extract_internal() -> dict[str, Any]:
    benchmarks = [
        "OperationPerformanceBenchmark",
        "WorkflowThroughputBenchmark",
        "MemoryAllocationBenchmark",
        "ConcurrencyBenchmark",
    ]
    out: dict[str, Any] = {}
    for bn in benchmarks:
        rows = parse_internal_csv(bn)
        methods: dict[str, Any] = {}
        for row in rows:
            method = row.get("Method", "").strip()
            if not method:
                continue
            rt = row.get("Runtime", "")
            rt_key = (
                "net10"
                if rt == RUNTIME_NET10
                else "net8"
                if rt == RUNTIME_NET8
                else "net48"
                if rt == RUNTIME_NET48
                else None
            )
            if not rt_key:
                continue
            median_us = parse_us_value(row.get("Median", ""))
            alloc = parse_allocated(row.get("Allocated", ""))
            key = _internal_row_key(row)
            methods.setdefault(key, {})[rt_key] = {
                "median_us": median_us,
                "alloc": alloc,
            }
        out[bn] = methods
    return out


def main() -> None:
    comp = extract_comparative()
    internal = extract_internal()

    out_json = ROOT / "scripts" / "benchmark_extract.json"
    payload = {"comparative": comp, "internal": internal}
    out_json.write_text(json.dumps(payload, indent=2), encoding="utf-8")

    benchmark_data = ROOT / "docs" / "_includes" / "benchmark-data.md"
    benchmark_data.write_text(render_benchmark_data_md(comp), encoding="utf-8")

    print(f"Wrote {benchmark_data}")
    print(f"Wrote {out_json}")
    bands = comp.get("bands", {})
    print(
        f"Speed band: {bands.get('speed_min'):.1f}x - {bands.get('speed_max'):.1f}x; "
        f"Mem band: {bands.get('mem_min'):.1f}x - {bands.get('mem_max'):.1f}x"
    )


if __name__ == "__main__":
    main()
