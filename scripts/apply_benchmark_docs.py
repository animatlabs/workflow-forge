#!/usr/bin/env python3
"""Apply benchmark_extract.json to workflow-forge documentation."""

from __future__ import annotations

import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
JSON_PATH = ROOT / "scripts" / "benchmark_extract.json"

# Re-use formatters from extract script
import extract_benchmark_docs as ex  # noqa: E402


def fmt_us(v: float | None, delay: bool = False) -> str:
    return ex.format_time_us(v, delay)


def fmt_b(b: int | None) -> str:
    return ex.format_bytes(b)


def ratio_str(a: float | None, b: float | None) -> str:
    if not a or not b or a <= 0:
        return "N/A"
    return f"**{b / a:.1f}x faster**"


BENCH_RUN_ALL_TFMS = "dotnet run -c Release -f net48 -f net8.0 -f net10.0"


def _chart_time_label(us: float | None) -> str:
    if us is None:
        return "N/A"
    if us >= 1000:
        return f"{us / 1000:.1f}ms"
    return fmt_us(us)


def _bar_heights(values: list[float], floor: float = 15.0) -> list[float]:
    # Execution times here span 3+ orders of magnitude (WorkflowForge in the tens of
    # microseconds vs. competitors in the tens of milliseconds), so a linear scale would
    # render every non-Elsa bar as an invisible sliver. Use a log scale with a visibility
    # floor instead, and derive it from the actual data rather than a fixed placeholder.
    logs = [math.log10(max(v, 1.0)) for v in values]
    lo, hi = min(logs), max(logs)
    if hi == lo:
        return [100.0 for _ in values]
    return [floor + (l - lo) / (hi - lo) * (100 - floor) for l in logs]


def _state_machine_vchart_inner(s9: dict) -> str:
    groups: list[str] = []
    for rt_key, label, three in [
        ("net10", ".NET 10.0", True),
        ("net8", ".NET 8.0", True),
        ("net48", ".NET FX 4.8", False),
    ]:
        c = s9[rt_key]
        wf = _chart_time_label(c["wf_us"])
        wc = _chart_time_label(c["wc_us"])
        has_elsa = three and c.get("elsa_us")
        values = [c["wf_us"] or 1.0, c["wc_us"] or 1.0]
        if has_elsa:
            values.append(c["elsa_us"])
        heights = _bar_heights(values)
        bars = [
            f'        <div class="perf-vchart-bar"><div class="perf-vchart-val">{wf}</div>'
            f'<div class="perf-vchart-fill wf" style="height: {heights[0]:.0f}%;"></div></div>',
            f'        <div class="perf-vchart-bar"><div class="perf-vchart-val">{wc}</div>'
            f'<div class="perf-vchart-fill wc" style="height: {heights[1]:.0f}%;"></div></div>',
        ]
        if has_elsa:
            el = _chart_time_label(c["elsa_us"])
            bars.append(
                f'        <div class="perf-vchart-bar"><div class="perf-vchart-val">{el}</div>'
                f'<div class="perf-vchart-fill elsa" style="height: {heights[2]:.0f}%;"></div></div>'
            )
        inner = "\n".join(bars)
        groups.append(
            f'    <div class="perf-vchart-group">\n'
            f"      <div class=\"perf-vchart-bars\">\n{inner}\n"
            f"      </div>\n"
            f'      <div class="perf-vchart-group-label">{label}</div>\n'
            f"    </div>"
        )
    divider = '    <div class="perf-vchart-divider"></div>\n'
    return divider.join(groups)


def patch_state_machine_vcharts(text: str, s9: dict) -> str:
    inner = _state_machine_vchart_inner(s9)
    return re.sub(
        r'(<div class="perf-vchart-title">State Machine[^<]*</div>\s*'
        r'<div class="perf-vchart-subtitle">[^<]*</div>\s*'
        r'<div class="perf-vchart-container">)\s*.*?\s*(</div>\s*'
        r'<div class="perf-vchart-legend">)',
        lambda m: m.group(1) + "\n" + inner + "\n  " + m.group(2),
        text,
        count=1,
        flags=re.DOTALL,
    )


def fix_performance_reproduction_block(text: str) -> str:
    """Replace reproduction bash blocks idempotently (avoids duplicated -f flags)."""
    internal = (
        "**Internal benchmarks**:\n\n```bash\n"
        "cd src/benchmarks/WorkflowForge.Benchmarks\n"
        f"{BENCH_RUN_ALL_TFMS}\n```"
    )
    comparative = (
        "**Competitive benchmarks**:\n\n```bash\n"
        "cd src/benchmarks/WorkflowForge.Benchmarks.Comparative\n"
        f"{BENCH_RUN_ALL_TFMS}\n```"
    )
    text = re.sub(
        r"\*\*Internal benchmarks\*\*:\s*\n+```bash\n.*?```",
        internal,
        text,
        flags=re.DOTALL,
    )
    text = re.sub(
        r"\*\*Competitive benchmarks\*\*:\s*\n+```bash\n.*?```",
        comparative,
        text,
        flags=re.DOTALL,
    )
    return text


def multi_runtime_table(canon: dict, delay: bool = False) -> str:
    lines = [
        "| Runtime | WorkflowForge | Workflow Core | Elsa |",
        "|---------|---------------|---------------|------|",
    ]
    for label, key in [(".NET 10.0", "net10"), (".NET 8.0", "net8"), (".NET FX 4.8", "net48")]:
        row = canon[key]
        wf = fmt_us(row["wf_us"], delay)
        wc = fmt_us(row["wc_us"], delay) if row.get("wc_us") else "N/A"
        elsa = fmt_us(row["elsa_us"], delay) if row.get("elsa_us") else "N/A†"
        if key == "net48":
            lines.append(f"| {label} | {wf} | {wc} | {elsa} |")
        else:
            lines.append(f"| {label} | {wf} | {wc} | {elsa} |")
    return "\n".join(lines)


def mem_runtime_table(canon: dict) -> str:
    lines = [
        "| Runtime | WorkflowForge | Workflow Core | Elsa |",
        "|---------|---------------|---------------|------|",
    ]
    for label, key in [(".NET 10.0", "net10"), (".NET 8.0", "net8"), (".NET FX 4.8", "net48")]:
        row = canon[key]
        wf = fmt_b(row.get("wf_alloc"))
        wc = fmt_b(row.get("wc_alloc")) if row.get("wc_alloc") is not None else "N/A"
        elsa = fmt_b(row.get("elsa_alloc")) if row.get("elsa_alloc") is not None else "N/A†"
        lines.append(f"| {label} | {wf} | {wc} | {elsa} |")
    return "\n".join(lines)


def scenario_advantage_ranges(scen: dict, n: int) -> tuple[str, str]:
    speed: list[float] = []
    mem: list[float] = []
    for key in ("net10", "net8", "net48"):
        c = scen["canonical"][key]
        wf, wc, el = c["wf_us"], c["wc_us"], c["elsa_us"]
        if n in (10, 12):
            if wf and el and el > 100:
                speed.append(el / wf)
            if wf and wc and wc > 100:
                speed.append(wc / wf)
        else:
            if wf and wf > 5:
                if wc and wc > 10:
                    speed.append(wc / wf)
                if el and el > 10:
                    speed.append(el / wf)
        wa, wca, ea = c.get("wf_alloc"), c.get("wc_alloc"), c.get("elsa_alloc")
        if wa and wa > 0:
            if wca:
                mem.append(wca / wa)
            if ea:
                mem.append(ea / wa)

    if n == 10:
        sp = "~1x (delay-bound)" if not speed or max(speed) < 1.5 else f"{min(speed):.1f}-{max(speed):.1f}x"
    elif n == 12:
        sp = f"{min(speed):.1f}-{max(speed):.1f}x" if speed else "N/A"
    elif speed:
        lo, hi = int(min(speed)), int(max(speed))
        sp = f"{lo}x" if lo == hi else f"{lo}-{hi}x"
    else:
        sp = "N/A"

    if mem:
        lo, hi = int(min(mem)), int(max(mem))
        mp = f"{lo}x" if lo == hi else f"{lo}-{hi}x"
    else:
        mp = "N/A"
    return sp, mp


def generate_scenario_breakdown(comp: dict) -> str:
    parts: list[str] = ["## Scenario Breakdown", ""]

    configs = [
        (1, "Simple Sequential Workflow", "Execute operations sequentially (1, 5, 10, 25, 50 operations)", "10 ops", "Operations"),
        (2, "Data Passing Workflow", "Pass data between operations (5, 10, 25 operations)", "10 ops", "Operations"),
        (3, "Conditional Branching", "Conditional execution paths (10, 25, 50 operations)", "10 ops", "Operations"),
        (4, "Loop/ForEach Processing", "Process collections with ForEach (10, 50, 100 items)", "50 items", "Items"),
        (5, "Concurrent Execution", "Run multiple workflows concurrently (1, 4, 8 workers)", "8 workers", "Workers"),
        (6, "Error Handling", "Exception handling and error propagation", None, None),
        (7, "Creation Overhead", "Workflow instantiation cost only", None, None),
        (8, "Complete Lifecycle", "Create, execute, and dispose workflow", None, None),
        (9, "State Machine", "State machine with conditional transitions (5, 10, 25)", "25 transitions", "Transitions"),
        (10, "Long Running", "Long-running operations with delays (delay-bound scenario)", "5 ops, 5ms delay", "Ops/Delay"),
        (11, "Parallel Execution", "Parallel operations within workflow (4, 8, 16 ops)", "16 ops", "Operations"),
        (12, "Event-Driven", "Event-driven workflow execution with delays", "1ms delay", "Delay"),
    ]

    for n, title, desc, canon_label, sweep_col in configs:
        scen = comp["scenarios"][str(n)]
        canon = scen["canonical"]
        delay = n in (10, 12)
        parts.append(f"### Scenario {n}: {title}")
        parts.append("")
        parts.append(f"**Description**: {desc}")
        parts.append("")
        if canon_label:
            parts.append(f"#### Multi-Runtime Performance (Median, {canon_label})")
            parts.append("")
        else:
            parts.append("#### Multi-Runtime Performance (Median)")
            parts.append("")
        parts.append(multi_runtime_table(canon, delay))
        parts.append("")

        if scen.get("sweeps"):
            parts.append("#### Parameter Sweep (.NET 8.0)")
            parts.append("")
            parts.append(
                f"| {sweep_col} | WorkflowForge | Workflow Core | Elsa | WF vs WC | WF vs Elsa |"
            )
            parts.append("|------------|---------------|---------------|------|----------|------------|")
            for label, sw in scen["sweeps"].items():
                wf, wc, el = sw["wf"]["us"], sw["wc"]["us"], sw["elsa"]["us"]
                parts.append(
                    f"| {label} | {fmt_us(wf, delay)} | {fmt_us(wc, delay)} | {fmt_us(el, delay)} | "
                    f"{ratio_str(wf, wc)} | {ratio_str(wf, el)} |"
                )
            parts.append("")

        parts.append("#### Memory Allocation (by Runtime)" if n != 1 else "#### Memory Allocation (10 ops, by Runtime)")
        parts.append("")
        parts.append(mem_runtime_table(canon))
        parts.append("")

        if scen.get("sweeps") and n in (1, 2, 3, 4, 9):
            parts.append("#### Memory Allocation - Parameter Sweep (.NET 8.0)")
            parts.append("")
            parts.append(f"| {sweep_col} | WorkflowForge | Workflow Core | Elsa |")
            parts.append("|------------|---------------|---------------|------|")
            for label, sw in scen["sweeps"].items():
                parts.append(
                    f"| {label} | {fmt_b(sw['wf']['alloc'])} | {fmt_b(sw['wc']['alloc'])} | {fmt_b(sw['elsa']['alloc'])} |"
                )
            parts.append("")

        parts.append("---")
        parts.append("")

    return "\n".join(parts)


def generate_advantage_summary(comp: dict) -> str:
    bands = comp["bands"]
    smin, smax = int(bands["speed_min"]), int(bands["speed_max"])
    mmin, mmax = int(bands["mem_min"]), int(bands["mem_max"])

    lines = [
        "## Performance Advantage Summary",
        "",
        "### By Scenario Type (12 Scenarios)",
        "",
        "| # | Scenario | Speed Advantage | Memory Advantage |",
        "|---|----------|-----------------|------------------|",
    ]
    names = ex.SCENARIO_NAMES
    for n in range(1, 13):
        sp, mp = scenario_advantage_ranges(comp["scenarios"][str(n)], n)
        nm = names[n].replace("*", "")
        bold = "**" if n == 9 else ""
        end = "**" if n == 9 else ""
        lines.append(f"| {n} | {nm} | {bold}{sp}{end} | {mp} |")

    lines.extend(
        [
            "",
            "Ranges include all three runtimes (.NET 10.0, .NET 8.0, .NET Framework 4.8). Elsa is excluded from .NET Framework 4.8 comparisons.",
            "",
            f"**Overall Speed Range**: **{smin}-{smax}x faster execution** (compute-bound scenarios)  ",
            f"**Overall Memory Range**: **{mmin}-{mmax}x less memory allocation**",
            "",
            "### Reading the summary table",
            "",
            f"1. **State machine** carries the widest execution spread we recorded (**{scenario_advantage_ranges(comp['scenarios']['9'], 9)[0]}** in the sweep).",
            "2. **Concurrent** work stays in a high multiple band vs Elsa on the runtimes listed.",
            "3. **Long running** and **event-driven** rows are delay-heavy; the standout delta there is allocation.",
            "4. On .NET 10.0 and 8.0, WorkflowForge reported less allocated memory in every row we logged (Elsa omitted on .NET Framework 4.8).",
            "",
            "---",
            "",
        ]
    )
    return "\n".join(lines)


def patch_competitive_analysis(comp: dict) -> None:
    path = ROOT / "docs" / "performance" / "competitive-analysis.md"
    text = path.read_text(encoding="utf-8")

    bands = comp["bands"]
    smax = int(bands["speed_max"])
    mmax = int(bands["mem_max"])
    s9 = comp["scenarios"]["9"]["canonical"]["net10"]
    max_state = int(s9["elsa_us"] / s9["wf_us"]) if s9["elsa_us"] and s9["wf_us"] else smax
    s7 = comp["scenarios"]["7"]["canonical"]["net10"]
    min_exec = fmt_us(s7["wf_us"])

    text = re.sub(
        r"description:.*\n",
        "description: Detailed benchmark comparison of WorkflowForge vs Workflow Core and Elsa Workflows across 12 real-world scenarios with 10 iterations.\n",
        text,
        count=1,
    )
    text = re.sub(r"\*\*Version\*\*: 2\.1\.1", "**Version**: 2.2.0", text)
    text = re.sub(r"\*\*Analysis Date\*\*: March 2026", "**Analysis Date**: September 2026", text)
    text = re.sub(r"- WorkflowForge 2\.1\.1", "- WorkflowForge 2.2.0", text)
    text = re.sub(
        r"\*\*BenchmarkDotNet\*\*: v0\.15\.8 \(50 iterations, 5 warmup\)",
        "**BenchmarkDotNet**: v0.15.8 (10 iterations per job)",
        text,
    )
    text = re.sub(r"\*\*Benchmark Run\*\*: March 7, 2026", "**Benchmark Run**: September 15, 2026", text)

    text = re.sub(
        r"Across twelve benchmark scenarios[^\n]+\n",
        f"Across twelve benchmark scenarios on .NET 10.0, .NET 8.0, and .NET Framework 4.8 (10 iterations per job), "
        f"WorkflowForge measured **{int(bands['speed_min'])}–{smax}x faster execution** and "
        f"**{int(bands['mem_min'])}–{mmax}x less allocation** than Workflow Core and Elsa for the same scripted logic.\n",
        text,
        count=1,
    )

    text = re.sub(
        r"\| \*\*Max Speed Advantage\*\* \| 511x[^\|]+\|",
        f"| **Max Speed Advantage** | {max_state}x faster (State Machine 25 transitions, .NET 10.0) |",
        text,
    )
    text = re.sub(
        r"\| \*\*Max Memory Advantage\*\* \| 575x[^\|]+\|",
        f"| **Max Memory Advantage** | {mmax}x less allocation (Parallel 16 ops, .NET 10.0) |",
        text,
    )
    text = re.sub(
        r"\| \*\*Min Execution Time\*\* \| 11μs[^\|]+\|",
        f"| **Min Execution Time** | {min_exec} (Creation Overhead, .NET 10.0) |",
        text,
    )

    text = re.sub(
        r'<div class="perf-stat-value">511x</div>',
        f'<div class="perf-stat-value">{max_state}x</div>',
        text,
    )
    text = re.sub(
        r'<div class="perf-stat-value">575x</div>',
        f'<div class="perf-stat-value">{mmax}x</div>',
        text,
    )
    text = re.sub(
        r'<div class="perf-stat-value">11μs</div>',
        f'<div class="perf-stat-value">{min_exec}</div>',
        text,
        count=1,
    )

    # Executive comparison table (state machine, concurrent, sequential)
    def exec_row(rt_key, rt_label, scen_n, param_label):
        c = comp["scenarios"][str(scen_n)]["canonical"][rt_key]
        adv = ex.speed_range(c["wf_us"], c["wc_us"], c["elsa_us"])
        el = fmt_us(c["elsa_us"]) if c.get("elsa_us") else "N/A†"
        wc = fmt_us(c["wc_us"]) if c.get("wc_us") else "N/A"
        return (
            f"| {rt_label} | {param_label} | {fmt_us(c['wf_us'])} | {wc} | {el} | {adv} |"
        )

    exec_table = "\n".join(
        [
            "| Runtime | Scenario | WorkflowForge | Workflow Core | Elsa | WF Advantage |",
            "|---------|----------|---------------|---------------|------|--------------|",
            exec_row("net10", ".NET 10.0", 9, "State Machine (25)"),
            exec_row("net8", ".NET 8.0", 9, "State Machine (25)"),
            exec_row("net48", ".NET FX 4.8", 9, "State Machine (25)"),
            exec_row("net10", ".NET 10.0", 5, "Concurrent (8 wf)"),
            exec_row("net8", ".NET 8.0", 5, "Concurrent (8 wf)"),
            exec_row("net48", ".NET FX 4.8", 5, "Concurrent (8 wf)"),
            exec_row("net10", ".NET 10.0", 1, "Sequential (10 ops)"),
            exec_row("net8", ".NET 8.0", 1, "Sequential (10 ops)"),
            exec_row("net48", ".NET FX 4.8", 1, "Sequential (10 ops)"),
        ]
    )

    text = re.sub(
        r"\| Runtime \| Scenario \| WorkflowForge \| Workflow Core \| Elsa \| WF Advantage \|\n\|[-\| ]+\n(?:\|[^\n]+\n){9}",
        exec_table + "\n",
        text,
        count=1,
    )

    def mem_row(rt_key: str, rt_label: str, scen_n: int, scen_label: str) -> str:
        c = comp["scenarios"][str(scen_n)]["canonical"][rt_key]
        wf = fmt_b(c["wf_alloc"])
        wc = fmt_b(c["wc_alloc"]) if c.get("wc_alloc") is not None else "N/A"
        el = fmt_b(c["elsa_alloc"]) if c.get("elsa_alloc") is not None else "N/A†"
        adv = ex.mem_range(c["wf_alloc"], c["wc_alloc"], c["elsa_alloc"])
        return f"| {rt_label} | {scen_label} | {wf} | {wc} | {el} | {adv} |"

    mem_summary = "\n".join(
        [
            "| Runtime | Scenario | WorkflowForge | Workflow Core | Elsa | WF Advantage |",
            "|---------|----------|---------------|---------------|------|--------------|",
            mem_row("net10", ".NET 10.0", 5, "Concurrent (8 wf)"),
            mem_row("net8", ".NET 8.0", 5, "Concurrent (8 wf)"),
            mem_row("net48", ".NET FX 4.8", 5, "Concurrent (8 wf)"),
            mem_row("net10", ".NET 10.0", 11, "Parallel (16 ops)"),
            mem_row("net8", ".NET 8.0", 11, "Parallel (16 ops)"),
        ]
    )
    text = re.sub(
        r"(#### Memory Allocation \(Lower is Better\)\n\n)"
        r"\| Runtime \| Scenario \| WorkflowForge \| Workflow Core \| Elsa \| WF Advantage \|\n"
        r"\|[-\| ]+\n"
        r"(?:\|[^\n]+\n)+",
        r"\1" + mem_summary + "\n",
        text,
        count=1,
    )

    s9 = comp["scenarios"]["9"]["canonical"]
    text = patch_state_machine_vcharts(text, s9)

    middle = generate_scenario_breakdown(comp) + "\n" + generate_advantage_summary(comp)
    text = re.sub(
        r"## Scenario Breakdown\n.*?## Architectural Differences",
        middle + "## Architectural Differences",
        text,
        flags=re.DOTALL,
    )

    # Methodology
    text = re.sub(
        r"- \*\*Iterations\*\*: 50 per benchmark",
        "- **Iterations**: 10 per benchmark job",
        text,
    )
    text = text.replace("50 iterations", "10 iterations")
    text = text.replace("5 warmup", "default BenchmarkDotNet warmup")
    text = text.replace("at 50 iterations", "at 10 iterations")
    smin = int(bands["speed_min"])
    smax_r = int(bands["speed_max"])
    mmin = int(bands["mem_min"])
    mmax_r = int(bands["mem_max"])
    text = re.sub(
        r"Across these twelve scenarios, the harness logged \*\*[^*]+\*\* and \*\*[^*]+\*\*",
        f"Across these twelve scenarios, the harness logged **{smin}–{smax_r}x faster execution** and **{mmin}–{mmax_r}x lower allocation**",
        text,
    )
    text = re.sub(
        r"- Run via `dotnet run -c Release`",
        f"- Run via `{BENCH_RUN_ALL_TFMS}`",
        text,
    )

    path.write_text(text, encoding="utf-8")
    print(f"Patched {path}")


INTERNAL_METHOD_MAP = {
    "LoggingOperationExecution": "LoggingOperationExecution",
    "ConditionalOperationFalse": "ConditionalOperationFalse",
    "ConditionalOperationTrue": "ConditionalOperationTrue",
    "CustomOperationExecution": "CustomOperationExecution",
    "DelegateOperationExecution": "DelegateOperationExecution",
    "ActionOperationExecution": "ActionOperationExecution",
    "ForEachOperationSmallCollection": "ForEachSmallCollection",
    "ForEachOperationLargeCollection": "ForEachLargeCollection",
    "OperationWithRestoration": "WithRestoration",
    "OperationDataManipulation": "DataManipulation",
    "ChainedOperationsExecution": "ChainedOperations",
    "OperationExceptionHandling": "ExceptionHandling",
    "DelayOperationExecution": "DelayOperationExecution",
    "DelegateOperationCreation": "DelegateCreation",
    "ActionOperationCreation": "ActionCreation",
    "CustomOperationCreation": "CustomCreation",
}


def fmt_internal_us(v: float | None) -> str:
    if v is None:
        return "—"
    if v >= 1000:
        return f"{v / 1000:.1f}ms"
    if v >= 1:
        return f"{v:.1f}μs"
    return f"{v * 1000:.1f}ns" if v < 1 else f"{v:.2f}μs"


def iget(bench: dict, method: str, rt: str, **params: int) -> dict:
    key = method
    if params:
        key += "|" + "|".join(f"{k}={v}" for k, v in params.items())
    return bench[key][rt]


def op_row(bench: dict, label: str, method: str) -> str:
    m8, m10, m48 = iget(bench, method, "net8"), iget(bench, method, "net10"), iget(bench, method, "net48")
    alloc = m8.get("alloc")
    alloc_s = f"{alloc:,} B" if alloc is not None else "—"
    return (
        f"| {label} | {fmt_internal_us(m8['median_us'])} | {fmt_internal_us(m10['median_us'])} | "
        f"{fmt_internal_us(m48['median_us'])} | {alloc_s} |"
    )


def patch_internal_benchmarks(internal: dict) -> None:
    path = ROOT / "docs" / "performance" / "internal-benchmarks.md"
    text = path.read_text(encoding="utf-8")

    text = re.sub(r"\*\*Version\*\*: 2\.1\.1", "**Version**: 2.2.0", text)
    text = re.sub(
        r"\*\*Methodology\*\*: 50 iterations per benchmark, 5 warmup iterations",
        "**Methodology**: 10 iterations per benchmark job",
        text,
    )
    text = re.sub(r"\*\*Last Updated\*\*: March 6, 2026", "**Last Updated**: September 15, 2026", text)
    text = re.sub(r"Median numbers \(50 iterations\):", "Median numbers (10 iterations per job):", text)
    text = text.replace("50 iterations", "10 iterations")

    op = internal["OperationPerformanceBenchmark"]
    tp = internal["WorkflowThroughputBenchmark"]
    mem = internal["MemoryAllocationBenchmark"]
    conc = internal["ConcurrencyBenchmark"]

    exec_table = "\n".join(
        [
            "| Operation Type | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Allocated (.NET 8) |",
            "|----------------|----------|-----------|-------------|--------------------|",
            op_row(op, "LoggingOperationExecution", "LoggingOperationExecution"),
            op_row(op, "ConditionalOperationFalse", "ConditionalOperationFalse"),
            op_row(op, "ConditionalOperationTrue", "ConditionalOperationTrue"),
            op_row(op, "CustomOperationExecution", "CustomOperationExecution"),
            op_row(op, "DelegateOperationExecution", "DelegateOperationExecution"),
            op_row(op, "ActionOperationExecution", "ActionOperationExecution"),
            op_row(op, "ForEachSmallCollection", "ForEachOperationSmallCollection"),
            op_row(op, "ForEachLargeCollection", "ForEachOperationLargeCollection"),
            op_row(op, "WithRestoration", "OperationWithRestoration"),
            op_row(op, "DataManipulation", "OperationDataManipulation"),
            op_row(op, "ChainedOperations", "ChainedOperationsExecution"),
            op_row(op, "ExceptionHandling", "OperationExceptionHandling"),
            op_row(op, "DelayOperationExecution", "DelayOperationExecution"),
        ]
    )

    creation_table = "\n".join(
        [
            "| Operation Type | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Allocated (.NET 8) |",
            "|----------------|----------|-----------|-------------|--------------------|",
            op_row(op, "DelegateCreation", "DelegateOperationCreation"),
            op_row(op, "ActionCreation", "ActionOperationCreation"),
            op_row(op, "CustomCreation", "CustomOperationCreation"),
        ]
    )

    def tp_row(pattern: str, oc: int = 1, note: str = "") -> str:
        m8 = iget(tp, pattern, "net8", OperationCount=oc)
        m10 = iget(tp, pattern, "net10", OperationCount=oc)
        m48 = iget(tp, pattern, "net48", OperationCount=oc)
        alloc = m8.get("alloc")
        alloc_s = f"{alloc:,} B" if alloc is not None else "—"
        note_col = f" | {note}" if note else ""
        if note:
            return (
                f"| {pattern} | {fmt_internal_us(m8['median_us'])} | {fmt_internal_us(m10['median_us'])} | "
                f"{fmt_internal_us(m48['median_us'])} | {alloc_s} | {note} |"
            )
        return (
            f"| {pattern} | {fmt_internal_us(m8['median_us'])} | {fmt_internal_us(m10['median_us'])} | "
            f"{fmt_internal_us(m48['median_us'])} | {alloc_s} |"
        )

    tp_patterns = [
        ("SequentialCustomOperations", "CPU-bound"),
        ("HighPerformanceConfiguration", "CPU-bound"),
        ("ForEachLoopWorkflow", "CPU-bound"),
        ("SequentialDelegateOperations", "Delay-bound"),
        ("DataPassingWorkflow", "Delay-bound"),
        ("ConditionalOperationsWorkflow", "Delay-bound"),
        ("LoggingOperationsWorkflow", "Delay-bound"),
        ("MemoryIntensiveWorkflow", "Delay-bound"),
    ]
    tp_table = "\n".join(
        [
            "| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | Notes |",
            "|---------|----------|-----------|-------------|-----------------|-------|",
            *[tp_row(p, 1, note) for p, note in tp_patterns],
        ]
    )

    scale_rows = []
    for oc in (1, 5, 10, 25, 50):
        m8 = iget(tp, "SequentialCustomOperations", "net8", OperationCount=oc)
        m10 = iget(tp, "SequentialCustomOperations", "net10", OperationCount=oc)
        m48 = iget(tp, "SequentialCustomOperations", "net48", OperationCount=oc)
        alloc = m8.get("alloc")
        scale_rows.append(
            f"| {oc} | {fmt_internal_us(m8['median_us'])} | {fmt_internal_us(m10['median_us'])} | "
            f"{fmt_internal_us(m48['median_us'])} | {alloc:,} B |"
        )
    scale_table = "\n".join(
        [
            "| Operations | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) |",
            "|------------|----------|-----------|-------------|-----------------|",
            *scale_rows,
        ]
    )

    mem_patterns = [
        "MinimalAllocationWorkflow",
        "SmallObjectAllocation",
        "StringConcatenationAllocation",
        "StringBuilderOptimization",
        "CollectionAllocation",
        "ObjectPoolingSimulation",
        "ArrayReuseOptimization",
        "MemoryPressureScenario",
        "LargeObjectAllocation",
        "DisposableResourceManagement",
    ]
    mem_rows = []
    for pat in mem_patterns:
        m8 = iget(mem, pat, "net8", AllocationCount=10)
        m10 = iget(mem, pat, "net10", AllocationCount=10)
        m48 = iget(mem, pat, "net48", AllocationCount=10)
        alloc = m8.get("alloc")
        mem_rows.append(
            f"| {pat} | {fmt_internal_us(m8['median_us'])} | {fmt_internal_us(m10['median_us'])} | "
            f"{fmt_internal_us(m48['median_us'])} | {alloc:,} B | — |"
        )
    mem_table = "\n".join(
        [
            "| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 | Memory (.NET 8) | GC |",
            "|---------|----------|-----------|-------------|-----------------|-----|",
            *mem_rows,
        ]
    )

    # MinimalAllocationWorkflow ignores AllocationCount entirely (it allocates one foundry, one
    # operation, regardless of the Params value), so every scaling row is the same measurement —
    # pull it once per AllocationCount rather than assuming it stays in sync with the row above.
    mem_scale_rows = []
    for ac in (10, 50, 100, 500):
        m8 = iget(mem, "MinimalAllocationWorkflow", "net8", AllocationCount=ac)
        m10 = iget(mem, "MinimalAllocationWorkflow", "net10", AllocationCount=ac)
        mem_scale_rows.append(f"| {ac} | {m8['alloc']:,} B | {m10['alloc']:,} B |")
    mem_scale_table = "\n".join(
        [
            "| Allocations | .NET 8.0 Memory | .NET 10.0 Memory |",
            "|-------------|----------------|-----------------|",
            *mem_scale_rows,
        ]
    )

    def conc_row(method: str, cw: int, opw: int) -> str:
        m8 = iget(conc, method, "net8", ConcurrentWorkflowCount=cw, OperationsPerWorkflow=opw)
        m10 = iget(conc, method, "net10", ConcurrentWorkflowCount=cw, OperationsPerWorkflow=opw)
        m48 = iget(conc, method, "net48", ConcurrentWorkflowCount=cw, OperationsPerWorkflow=opw)
        return (
            f"| {method} | {fmt_internal_us(m8['median_us'])} | {fmt_internal_us(m10['median_us'])} | "
            f"{fmt_internal_us(m48['median_us'])} |"
        )

    conc_table_8 = "\n".join(
        [
            "| Pattern | .NET 8.0 | .NET 10.0 | .NET FX 4.8 |",
            "|---------|----------|-----------|-------------|",
            conc_row("SequentialWorkflows", 8, 5),
            conc_row("ConcurrentWorkflows", 8, 5),
            conc_row("ParallelWorkflows", 8, 5),
        ]
    )

    text = re.sub(
        r"\| Operation Type \| \.NET 8\.0 \| \.NET 10\.0 \| \.NET FX 4\.8 \| Allocated \(\.NET 8\) \|\n\|[-\| ]+\n(?:\|[^\n]+\n)+",
        exec_table + "\n",
        text,
        count=1,
    )
    text = re.sub(
        r"### Operation Creation \(Median Times\)\n\n\| Operation Type \|.*?\n(?:\|[^\n]+\n)+",
        "### Operation Creation (Median Times)\n\n" + creation_table + "\n",
        text,
        flags=re.DOTALL,
        count=1,
    )
    text = re.sub(
        r"\| Pattern \| \.NET 8\.0 \| \.NET 10\.0 \| \.NET FX 4\.8 \| Memory \(\.NET 8\) \| Notes \|\n\|[-\| ]+\n(?:\|[^\n]+\n)+",
        tp_table + "\n",
        text,
        count=1,
    )
    text = re.sub(
        r"\| Operations \| \.NET 8\.0 \| \.NET 10\.0 \| \.NET FX 4\.8 \| Memory \(\.NET 8\) \|\n\|[-\| ]+\n(?:\|[^\n]+\n)+",
        scale_table + "\n",
        text,
        count=1,
    )
    text = re.sub(
        r"\| Pattern \| \.NET 8\.0 \| \.NET 10\.0 \| \.NET FX 4\.8 \| Memory \(\.NET 8\) \| GC \|\n\|[-\| ]+\n(?:\|[^\n]+\n)+",
        mem_table + "\n",
        text,
        count=1,
    )
    text = re.sub(
        r"\| Allocations \| \.NET 8\.0 Memory \| \.NET 10\.0 Memory \|\n\|[-\| ]+\n(?:\|[^\n]+\n)+",
        mem_scale_table + "\n",
        text,
        count=1,
    )
    mem_scale_alloc8 = iget(mem, "MinimalAllocationWorkflow", "net8", AllocationCount=10)["alloc"]
    text = re.sub(
        r"flatlines at [\d,]+ B from 10 through 500 iterations",
        f"flatlines at {mem_scale_alloc8:,} B from 10 through 500 iterations",
        text,
        count=1,
    )

    chained = iget(op, "ChainedOperationsExecution", "net10")["median_us"] or 0
    logging = iget(op, "LoggingOperationExecution", "net10")["median_us"] or 0
    text = re.sub(
        r"\| \*\*Operation Execution\*\* \| [^\|]+ \|",
        f"| **Operation Execution** | {fmt_internal_us(logging)}–{fmt_internal_us(chained)} median (excluding delays) |",
        text,
        count=1,
    )
    custom_c = iget(op, "CustomOperationCreation", "net10")["median_us"]
    text = re.sub(
        r"\| \*\*Operation Creation\*\* \| [^\|]+ \|",
        f"| **Operation Creation** | {fmt_internal_us(custom_c)} median |",
        text,
        count=1,
    )

    path.write_text(text, encoding="utf-8")
    print(f"Patched {path}")


def sync_summaries(comp: dict) -> None:
    bands = comp["bands"]
    smin = int(bands["speed_min"])
    smax = int(bands["speed_max"])
    mmin = int(bands["mem_min"])
    mmax = int(bands["mem_max"])

    s1 = comp["scenarios"]["1"]["canonical"]
    s9 = comp["scenarios"]["9"]["canonical"]

    def patch_file(rel: str, replacements: list[tuple[str, str]]) -> None:
        p = ROOT / rel
        t = p.read_text(encoding="utf-8")
        for old, new in replacements:
            t = t.replace(old, new)
        p.write_text(t, encoding="utf-8")
        print(f"Synced {rel}")

    ratio_10_seq = ex.speed_range(
        s1["net10"]["wf_us"], s1["net10"]["wc_us"], s1["net10"]["elsa_us"]
    )
    ratio_8_seq = ex.speed_range(
        s1["net8"]["wf_us"], s1["net8"]["wc_us"], s1["net8"]["elsa_us"]
    )

    readme_table = (
        f"| .NET 10.0 | {fmt_us(s9['net10']['wf_us'])} | "
        f"{fmt_us(s9['net10']['wc_us'])} ({int(s9['net10']['wc_us']/s9['net10']['wf_us'])}x) | "
        f"{fmt_us(s9['net10']['elsa_us'])} ({int(s9['net10']['elsa_us']/s9['net10']['wf_us'])}x) |"
    )

    seq_row = (
        f"| .NET 10.0 | {fmt_us(s1['net10']['wf_us'])} | "
        f"{fmt_us(s1['net10']['wc_us'])} ({ratio_10_seq.replace('x','')}) | "
        f"{fmt_us(s1['net10']['elsa_us'])} |"
    )

    band_str = f"{smin}–{smax}"
    mem_band = f"{mmin}–{mmax}"
    band_speed = band_str
    band_mem = mem_band

    common_old_new = [
        ("13-511×", f"{band_speed}×"),
        ("13–511×", f"{band_speed}×"),
        ("13-511x", f"{band_speed.replace('–', '-')}x"),
        ("13–511x", f"{band_speed.replace('–', '-')}x"),
        ("9–583×", f"{band_speed}×"),
        ("9-583×", f"{band_speed.replace('–', '-')}×"),
        ("9–583x", f"{band_speed.replace('–', '-')}x"),
        ("9-583x", f"{band_speed.replace('–', '-')}x"),
        ("13x–583x", f"{band_speed.replace('–', '-')}x"),
        ("13x-583x", f"{band_speed.replace('–', '-')}x"),
        ("6-575×", f"{band_mem}×"),
        ("6–575×", f"{band_mem}×"),
        ("6-575x", f"{band_mem.replace('–', '-')}x"),
        ("6–575x", f"{band_mem.replace('–', '-')}x"),
        ("6–533×", f"{band_mem}×"),
        ("6-533×", f"{band_mem.replace('–', '-')}×"),
        ("6x–575x", f"{band_mem.replace('–', '-')}x"),
        ("6x-575x", f"{band_mem.replace('–', '-')}x"),
        ("2–584×", f"{band_speed}×"),
        ("2-584×", f"{band_speed.replace('–', '-')}×"),
        ("2–584x", f"{band_speed.replace('–', '-')}x"),
        ("2-584x", f"{band_speed.replace('–', '-')}x"),
        ("511×", f"{int(round(smax))}×"),
        ("511x", f"{int(round(smax))}x"),
        ("50 iterations", "10 iterations per job"),
        ("50 iteration", "10 iteration"),
        ("(50 iterations each)", "(10 iterations per job)"),
        ("305–511×", f"{int(s9['net8']['wc_us']/s9['net8']['wf_us'])}–{int(s9['net10']['elsa_us']/s9['net10']['wf_us'])}×"),
    ]

    for rel in [
        "README.md",
        "docs/performance/performance.md",
        "docs/index.md",
        "docs/architecture/overview.md",
        "src/core/WorkflowForge/README.md",
        "src/benchmarks/WorkflowForge.Benchmarks/README.md",
        "src/benchmarks/WorkflowForge.Benchmarks.Comparative/README.md",
        "src/benchmarks/WorkflowForge.Benchmarks.Comparative/BENCHMARK_EXCLUSIONS.md",
        "docs/performance/competitive-analysis.md",
    ]:
        patch_file(rel, common_old_new)

    # README benchmark tables (state machine + sequential)
    readme = (ROOT / "README.md").read_text(encoding="utf-8")
    readme = re.sub(
        r"\| \.NET 10\.0 \| [^|]+μs \| [^|]+μs \([^)]+\) \| [^|]+μs \([^)]+\) \|",
        f"| .NET 10.0 | {fmt_us(s9['net10']['wf_us'])} | "
        f"{fmt_us(s9['net10']['wc_us'])} ({int(s9['net10']['wc_us']/s9['net10']['wf_us'])}x) | "
        f"{fmt_us(s9['net10']['elsa_us'])} ({int(s9['net10']['elsa_us']/s9['net10']['wf_us'])}x) |",
        readme,
    )
    readme = re.sub(
        r"\| \.NET 8\.0 \| 71μs \| 21,683μs \(305x\) \| 34,426μs \(485x\) \|",
        f"| .NET 8.0 | {fmt_us(s9['net8']['wf_us'])} | "
        f"{fmt_us(s9['net8']['wc_us'])} ({int(s9['net8']['wc_us']/s9['net8']['wf_us'])}x) | "
        f"{fmt_us(s9['net8']['elsa_us'])} ({int(s9['net8']['elsa_us']/s9['net8']['wf_us'])}x) |",
        readme,
    )
    readme = re.sub(
        r"\| \.NET FX 4\.8 \| 61μs \| 18,486μs \(303x\) \| N/A \|",
        f"| .NET FX 4.8 | {fmt_us(s9['net48']['wf_us'])} | "
        f"{fmt_us(s9['net48']['wc_us'])} ({int(s9['net48']['wc_us']/s9['net48']['wf_us'])}x) | N/A |",
        readme,
    )
    readme = re.sub(
        r"\| \.NET 10\.0 \| 422μs \| 13,828μs \(33x\) \| 18,676μs \(44x\) \|",
        f"| .NET 10.0 | {fmt_us(s1['net10']['wf_us'])} | "
        f"{fmt_us(s1['net10']['wc_us'])} ({ratio_10_seq}) | "
        f"{fmt_us(s1['net10']['elsa_us'])} |",
        readme,
    )
    readme = re.sub(
        r"\| \.NET 8\.0 \| 377μs \| 9,879μs \(26x\) \| 19,168μs \(51x\) \|",
        f"| .NET 8.0 | {fmt_us(s1['net8']['wf_us'])} | "
        f"{fmt_us(s1['net8']['wc_us'])} ({ratio_8_seq}) | "
        f"{fmt_us(s1['net8']['elsa_us'])} |",
        readme,
    )
    readme = re.sub(
        r"\| \.NET FX 4\.8 \| 122μs \| 6,743μs \(55x\) \| N/A \|",
        f"| .NET FX 4.8 | {fmt_us(s1['net48']['wf_us'])} | "
        f"{fmt_us(s1['net48']['wc_us'])} ({int(s1['net48']['wc_us']/s1['net48']['wf_us'])}x) | N/A |",
        readme,
    )
    peak_elsa_sm = int(round(s9["net10"]["elsa_us"] / s9["net10"]["wf_us"]))
    readme = re.sub(
        r"On \*\*\.NET 10\.0\*\*, state machine vs\. Elsa reaches \*\*\d+×\*\*\.",
        f"On **.NET 10.0**, state machine vs. Elsa reaches **{peak_elsa_sm}×**.",
        readme,
    )
    (ROOT / "README.md").write_text(readme, encoding="utf-8")

    perf = ROOT / "docs/performance/performance.md"
    pt = perf.read_text(encoding="utf-8")
    s7_net48 = comp["scenarios"]["7"]["canonical"]["net48"]["wf_us"]
    pt = re.sub(
        r"Shortest median: \*\*[^*]+\*\* \([^)]+\)",
        f"Shortest median: **{fmt_us(s7_net48)}** (creation overhead, .NET FX 4.8)",
        pt,
    )
    if "### Version 2.2.0" not in pt:
        pt = pt.replace(
            "### Version 2.1.2 (July 2026)",
            "### Version 2.2.0 (September 2026)\n\n"
            "- Refreshed competitive and internal benchmark documentation from September 2026 BenchmarkDotNet runs (10 iterations per job).\n\n"
            "### Version 2.1.2 (July 2026)",
        )
    pt = fix_performance_reproduction_block(pt)
    pt = re.sub(
        r"- \*\*Iterations\*\*: 50 per benchmark",
        "- **Iterations**: 10 per benchmark job",
        pt,
    )
    pt = re.sub(
        r"- \*\*Warmup\*\*: 5 iterations",
        "- **Warmup**: per BenchmarkDotNet default job",
        pt,
    )
    peak_sm = peak_elsa_sm
    pt = re.sub(
        r"Peak ratio in our log: \*\*\d+x\*\* wall time \(state machine vs Elsa, \.NET 10\.0\)",
        f"Peak ratio in our log: **{peak_sm}x** wall time (state machine vs Elsa, .NET 10.0)",
        pt,
    )
    pt = re.sub(
        r"Aggregate bands from the tables: \*\*[^*]+\*\* time, \*\*[^*]+\*\* allocation\.",
        f"Aggregate bands from the tables: **{band_speed}x** time, **{band_mem}x** allocation.",
        pt,
    )
    s7_net10 = comp["scenarios"]["7"]["canonical"]["net10"]["wf_us"]
    pt = re.sub(
        r'<div class="perf-stat-value">\d+x</div>\s*\n\s*<div class="perf-stat-label">Faster \(State Machine',
        f'<div class="perf-stat-value">{peak_sm}x</div>\n    <div class="perf-stat-label">Faster (State Machine',
        pt,
        count=1,
    )
    pt = re.sub(
        r'<div class="perf-stat-value">575x</div>',
        f'<div class="perf-stat-value">{mmax}x</div>',
        pt,
        count=1,
    )
    pt = re.sub(
        r'<div class="perf-stat-value">11μs</div>',
        f'<div class="perf-stat-value">{fmt_us(s7_net10)}</div>',
        pt,
        count=1,
    )
    pt = patch_state_machine_vcharts(pt, s9)
    perf.write_text(pt, encoding="utf-8")
    print("Synced docs/performance/performance.md")

    s5 = comp["scenarios"]["5"]["canonical"]
    core = (ROOT / "src/core/WorkflowForge/README.md").read_text(encoding="utf-8")
    core = re.sub(
        r"\| Sequential \(10 ops\) \| [^\n]+ \|",
        f"| Sequential (10 ops) | {fmt_us(s1['net8']['wf_us'])} | {fmt_us(s1['net8']['wc_us'])} | "
        f"{fmt_us(s1['net8']['elsa_us'])} | {ratio_8_seq} |",
        core,
    )
    core = re.sub(
        r"\| State machine \(25\) \| [^\n]+ \|",
        f"| State machine (25) | {fmt_us(s9['net8']['wf_us'])} | {fmt_us(s9['net8']['wc_us'])} | "
        f"{fmt_us(s9['net8']['elsa_us'])} | "
        f"{int(s9['net8']['wc_us']/s9['net8']['wf_us'])}–{int(s9['net8']['elsa_us']/s9['net8']['wf_us'])}× |",
        core,
    )
    core = re.sub(
        r"\| Concurrent \(8 workers\) \| [^\n]+ \|",
        f"| Concurrent (8 workers) | {fmt_us(s5['net8']['wf_us'])} | {fmt_us(s5['net8']['wc_us'])} | "
        f"{fmt_us(s5['net8']['elsa_us'])} | {ex.speed_range(s5['net8']['wf_us'], s5['net8']['wc_us'], s5['net8']['elsa_us'])} |",
        core,
    )
    core = re.sub(
        r"up to \*\*\d+×\*\* \(\.NET 10\.0 state machine\)",
        f"up to **{peak_elsa_sm}×** (.NET 10.0 state machine vs Elsa)",
        core,
    )
    core = re.sub(
        r"up to \d+× on \.NET 10\.0 \(state machine\)",
        f"up to {peak_elsa_sm}× on .NET 10.0 (state machine vs Elsa)",
        core,
    )
    (ROOT / "src/core/WorkflowForge/README.md").write_text(core, encoding="utf-8")

    idx = (ROOT / "docs/index.md").read_text(encoding="utf-8")
    idx = re.sub(
        r'<div class="perf-stat-value">575x</div>',
        f'<div class="perf-stat-value">{mmax}x</div>',
        idx,
    )
    idx = re.sub(
        r'<div class="perf-stat-value">8\.75μs</div>',
        f'<div class="perf-stat-value">{fmt_us(comp["scenarios"]["7"]["canonical"]["net10"]["wf_us"])}</div>',
        idx,
    )
    for rt_key, rt_label in [("net10", ".NET 10.0"), ("net8", ".NET 8.0"), ("net48", ".NET FX 4.8")]:
        c9 = s9[rt_key]
        wc_r = int(c9["wc_us"] / c9["wf_us"]) if c9["wc_us"] else 0
        el_r = int(c9["elsa_us"] / c9["wf_us"]) if c9.get("elsa_us") else 0
        mem_r = ex.mem_range(c9["wf_alloc"], c9["wc_alloc"], c9.get("elsa_alloc"))
        old = (
            f"| {rt_label} | **State Machine** | 455x faster | 583x faster | 46-249x less |"
            if rt_key == "net10"
            else f"| {rt_label} | **State Machine** | 305x faster | 485x faster | 46-248x less |"
            if rt_key == "net8"
            else f"| {rt_label} | **State Machine** | 303x faster | N/A† | 57x less |"
        )
        new = (
            f"| {rt_label} | **State Machine** | {wc_r}x faster | {el_r}x faster | {mem_r} less |"
            if rt_key != "net48"
            else f"| {rt_label} | **State Machine** | {wc_r}x faster | N/A† | {mem_r} less |"
        )
        if rt_key == "net48":
            new = f"| {rt_label} | **State Machine** | {wc_r}x faster | N/A† | {mem_r} less |"
        idx = idx.replace(old, new)

    c5_10 = s5["net10"]
    idx = re.sub(
        r"\| \.NET 10\.0 \| \*\*Concurrent \(8 wf\)\*\* \| [^\n]+ \|",
        f"| .NET 10.0 | **Concurrent (8 wf)** | {int(c5_10['wc_us']/c5_10['wf_us'])}x faster | "
        f"{int(c5_10['elsa_us']/c5_10['wf_us'])}x faster | {ex.mem_range(c5_10['wf_alloc'], c5_10['wc_alloc'], c5_10['elsa_alloc'])} less |",
        idx,
    )
    c5_8 = s5["net8"]
    idx = re.sub(
        r"\| \.NET 8\.0 \| \*\*Concurrent \(8 wf\)\*\* \| [^\n]+ \|",
        f"| .NET 8.0 | **Concurrent (8 wf)** | {int(c5_8['wc_us']/c5_8['wf_us'])}x faster | "
        f"{int(c5_8['elsa_us']/c5_8['wf_us'])}x faster | {ex.mem_range(c5_8['wf_alloc'], c5_8['wc_alloc'], c5_8['elsa_alloc'])} less |",
        idx,
    )
    c5_48 = s5["net48"]
    idx = re.sub(
        r"\| \.NET FX 4\.8 \| \*\*Concurrent \(8 wf\)\*\* \| [^\n]+ \|",
        f"| .NET FX 4.8 | **Concurrent (8 wf)** | {int(c5_48['wc_us']/c5_48['wf_us'])}x faster | N/A† | "
        f"{ex.mem_range(c5_48['wf_alloc'], c5_48['wc_alloc'], None)} less |",
        idx,
    )

    wf10 = fmt_us(s9["net10"]["wf_us"])
    wc10 = f"{s9['net10']['wc_us']/1000:.1f}ms"
    el10 = f"{s9['net10']['elsa_us']/1000:.1f}ms"
    idx = re.sub(
        r'<div class="perf-vchart-val">65μs</div>',
        f'<div class="perf-vchart-val">{wf10}</div>',
        idx,
        count=1,
    )
    idx = re.sub(
        r'<div class="perf-vchart-val">29\.5ms</div>',
        f'<div class="perf-vchart-val">{wc10}</div>',
        idx,
        count=1,
    )
    idx = patch_state_machine_vcharts(idx, s9)
    (ROOT / "docs/index.md").write_text(idx, encoding="utf-8")
    print("Synced docs/index.md and core README tables")


def main() -> None:
    ex.main()
    payload = json.loads(JSON_PATH.read_text(encoding="utf-8"))
    patch_competitive_analysis(payload["comparative"])
    patch_internal_benchmarks(payload["internal"])
    sync_summaries(payload["comparative"])


if __name__ == "__main__":
    main()
