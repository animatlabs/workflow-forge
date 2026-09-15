#!/usr/bin/env python3
"""Verify benchmark docs match BenchmarkDotNet CSV artifacts and extract logic."""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

import extract_benchmark_docs as ex  # noqa: E402

BENCHMARK_DATA = ROOT / "docs" / "_includes" / "benchmark-data.md"
JSON_PATH = ROOT / "scripts" / "benchmark_extract.json"

US_TOL = 0.02

FORBIDDEN_PATTERNS: list[tuple[str, str]] = [
    (r"50 iterations per benchmark", "stale iteration count"),
    (r"13[–-]511", "stale speed band"),
    (r"9[–-]583", "stale speed band (use 2–583 from CSV bands)"),
    (r"6[–-]575", "stale memory band"),
    (r"511x", "stale peak ratio"),
    (r"-f net10\.0 -f net10\.0", "duplicated -f net10.0 in reproduction command"),
]

ALLOWLIST_SUBSTRINGS = [
    "584 B",  # internal ActionOperationExecution allocation
]


def band_speed_str(bands: dict) -> str:
    return f"{int(bands['speed_min'])}–{int(bands['speed_max'])}"


def band_mem_str(bands: dict) -> str:
    return f"{int(bands['mem_min'])}–{int(bands['mem_max'])}"


def peak_state_machine_elsa(comp: dict) -> int:
    s9 = comp["scenarios"]["9"]["canonical"]["net10"]
    wf, el = s9["wf_us"], s9["elsa_us"]
    if not wf or not el:
        raise ValueError("missing scenario 9 net10 medians")
    return int(round(el / wf))


def verify_csv_matches_extract(comp: dict) -> list[str]:
    errors: list[str] = []
    fresh = ex.extract_comparative()
    for n in range(1, 13):
        for rt_key in ("net10", "net8", "net48"):
            a = comp["scenarios"][str(n)]["canonical"][rt_key]
            b = fresh["scenarios"][str(n)]["canonical"][rt_key]
            for field in ("wf_us", "wc_us", "elsa_us"):
                av, bv = a.get(field), b.get(field)
                if av is None and bv is None:
                    continue
                if av is None or bv is None:
                    errors.append(f"scenario {n} {rt_key} {field}: json={av} extract={bv}")
                    continue
                if abs(av - bv) > US_TOL:
                    errors.append(
                        f"scenario {n} {rt_key} {field}: json={av} != fresh extract={bv}"
                    )
    return errors


def verify_benchmark_data_include(comp: dict) -> list[str]:
    errors: list[str] = []
    expected = ex.render_benchmark_data_md(comp)
    if not BENCHMARK_DATA.is_file():
        return [f"missing {BENCHMARK_DATA}"]
    actual = BENCHMARK_DATA.read_text(encoding="utf-8")
    if actual.strip() != expected.strip():
        # Show first differing line for debugging
        exp_lines = expected.strip().splitlines()
        act_lines = actual.strip().splitlines()
        for i, (e, a) in enumerate(zip(exp_lines, act_lines)):
            if e != a:
                errors.append(
                    f"benchmark-data.md line {i + 1} mismatch:\n  expected: {e}\n  actual:   {a}"
                )
                break
        else:
            if len(exp_lines) != len(act_lines):
                errors.append(
                    f"benchmark-data.md line count {len(act_lines)} != expected {len(exp_lines)}"
                )
            else:
                errors.append("benchmark-data.md content differs from render_benchmark_data_md")
    return errors


def verify_speed_advantage_columns(comp: dict) -> list[str]:
    errors: list[str] = []
    content = BENCHMARK_DATA.read_text(encoding="utf-8")
    for n in range(1, 13):
        c8 = comp["scenarios"][str(n)]["canonical"]["net8"]
        expected = ex.speed_range(c8["wf_us"], c8["wc_us"], c8["elsa_us"])
        row_re = re.compile(
            rf"\| {n} \| [^|]+\| [^|]+\| [^|]+\| [^|]+\| {re.escape(expected)} \|"
        )
        if not row_re.search(content):
            errors.append(f"scenario {n} .NET 8 speed advantage expected '{expected}' in include")
    return errors


def verify_headlines(comp: dict) -> list[str]:
    errors: list[str] = []
    bands = comp["bands"]
    speed_band = band_speed_str(bands)
    mem_band = band_mem_str(bands)
    peak = peak_state_machine_elsa(comp)

    checks: list[tuple[Path, list[str]]] = [
        (ROOT / "README.md", [speed_band.replace("–", "-"), mem_band.replace("–", "-")]),
        (ROOT / "docs/performance/performance.md", [speed_band, mem_band, f"{peak}x"]),
        (ROOT / "docs/performance/competitive-analysis.md", [speed_band]),
        (BENCHMARK_DATA, [speed_band, mem_band]),
    ]

    for path, needles in checks:
        if not path.is_file():
            errors.append(f"missing file for headline check: {path}")
            continue
        text = path.read_text(encoding="utf-8")
        for needle in needles:
            alt = needle.replace("–", "-")
            if needle not in text and alt not in text:
                errors.append(f"{path.relative_to(ROOT)}: expected to contain '{needle}'")

    # Peak state machine: README should use rounded Elsa ratio, not truncated band max
    readme = (ROOT / "README.md").read_text(encoding="utf-8")
    if f"reaches **{peak}×**" not in readme and f"reaches **{peak}x**" not in readme:
        errors.append(f"README.md: expected 'reaches **{peak}×**' for state machine vs Elsa")

    perf = (ROOT / "docs/performance/performance.md").read_text(encoding="utf-8")
    if f"**{peak}x** wall time" not in perf:
        errors.append(f"performance.md: expected peak **{peak}x** wall time (state machine vs Elsa)")

    return errors


def verify_forbidden_tokens() -> list[str]:
    errors: list[str] = []
    md_roots = [
        ROOT / "docs",
        ROOT / "README.md",
        ROOT / "src/benchmarks",
        ROOT / "src/core/WorkflowForge/README.md",
    ]
    for root in md_roots:
        paths = [root] if root.is_file() else root.rglob("*.md")
        for path in paths:
            try:
                text = path.read_text(encoding="utf-8")
            except OSError:
                continue
            for pattern, reason in FORBIDDEN_PATTERNS:
                for match in re.finditer(pattern, text, re.IGNORECASE):
                    snippet = text[max(0, match.start() - 20) : match.end() + 20]
                    if any(a in snippet for a in ALLOWLIST_SUBSTRINGS):
                        continue
                    errors.append(
                        f"{path.relative_to(ROOT)}: {reason} ({match.group()!r})"
                    )
    return errors


def verify_state_machine_vcharts(comp: dict) -> list[str]:
    errors: list[str] = []
    s9 = comp["scenarios"]["9"]["canonical"]
    net10 = s9["net10"]
    wc_label = f"{net10['wc_us'] / 1000:.1f}ms"
    el_label = f"{net10['elsa_us'] / 1000:.1f}ms"
    wf_label = ex.format_time_us(net10["wf_us"])
    for rel in (
        "docs/performance/competitive-analysis.md",
        "docs/index.md",
        "docs/performance/performance.md",
    ):
        path = ROOT / rel
        if not path.is_file():
            errors.append(f"missing {rel} for vchart check")
            continue
        text = path.read_text(encoding="utf-8")
        if wf_label not in text or wc_label not in text or el_label not in text:
            errors.append(
                f"{rel}: state-machine vchart missing .NET 10 labels "
                f"({wf_label}, {wc_label}, {el_label})"
            )
    return errors


def verify_memory_summary_table(comp: dict) -> list[str]:
    errors: list[str] = []
    path = ROOT / "docs/performance/competitive-analysis.md"
    if not path.is_file():
        return [f"missing {path}"]
    text = path.read_text(encoding="utf-8")
    c5 = comp["scenarios"]["5"]["canonical"]["net10"]
    wf = ex.format_bytes(c5["wf_alloc"])
    if wf not in text:
        errors.append(
            f"competitive-analysis.md: memory summary missing concurrent WF alloc {wf}"
        )
    return errors


def verify_reproduction_commands() -> list[str]:
    errors: list[str] = []
    perf = ROOT / "docs/performance/performance.md"
    text = perf.read_text(encoding="utf-8")
    good = "dotnet run -c Release -f net48 -f net8.0 -f net10.0"
    if text.count(good) < 2:
        errors.append(f"performance.md: expected two reproduction blocks with:\n  {good}")
    return errors


def main() -> int:
    errors: list[str] = []

    if not JSON_PATH.is_file():
        errors.append(f"Run extract first: missing {JSON_PATH}")
        print_report(errors)
        return 1

    import json

    payload = json.loads(JSON_PATH.read_text(encoding="utf-8"))
    comp = payload["comparative"]

    errors.extend(verify_csv_matches_extract(comp))
    errors.extend(verify_benchmark_data_include(comp))
    errors.extend(verify_speed_advantage_columns(comp))
    errors.extend(verify_headlines(comp))
    errors.extend(verify_forbidden_tokens())
    errors.extend(verify_state_machine_vcharts(comp))
    errors.extend(verify_memory_summary_table(comp))
    errors.extend(verify_reproduction_commands())

    bands = comp["bands"]
    print(
        f"Bands: speed {band_speed_str(bands)}x, mem {band_mem_str(bands)}x; "
        f"peak SM vs Elsa (.NET 10): {peak_state_machine_elsa(comp)}x"
    )
    print_report(errors)
    return 1 if errors else 0


def print_report(errors: list[str]) -> None:
    if not errors:
        print("verify_benchmark_docs: OK")
        return
    print(f"verify_benchmark_docs: {len(errors)} issue(s):", file=sys.stderr)
    for e in errors:
        print(f"  - {e}", file=sys.stderr)


if __name__ == "__main__":
    sys.exit(main())
