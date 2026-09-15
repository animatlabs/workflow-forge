#!/usr/bin/env python3
"""
Pack all WorkflowForge NuGet projects (Release) and verify package contents.

Checks:
  - nuget-readme.md present (thin shell with GitHub blob link)
  - ILRepack packages: THIRD-PARTY-NOTICES.txt, no loose merged DLLs under lib/
  - Optional: diff .nuspec dependency ids against a previously packed baseline directory

Assembly reference closure is verified separately, in
tests/WorkflowForge.Packaging.Smoke.Tests/PackageClosureShould.cs.

Usage:
  python scripts/verify_nuget_packages.py
  python scripts/verify_nuget_packages.py --output artifacts/pack-verify
  python scripts/verify_nuget_packages.py --output artifacts/pack-verify --baseline-dir artifacts/pack-2.1.2
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

PACKABLE_PROJECTS = [
    "src/core/WorkflowForge/WorkflowForge.csproj",
    "src/core/WorkflowForge.Testing/WorkflowForge.Testing.csproj",
    "src/extensions/WorkflowForge.Extensions.Audit/WorkflowForge.Extensions.Audit.csproj",
    "src/extensions/WorkflowForge.Extensions.DependencyInjection/WorkflowForge.Extensions.DependencyInjection.csproj",
    "src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj",
    "src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/WorkflowForge.Extensions.Observability.HealthChecks.csproj",
    "src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/WorkflowForge.Extensions.Observability.OpenTelemetry.csproj",
    "src/extensions/WorkflowForge.Extensions.Observability.Performance/WorkflowForge.Extensions.Observability.Performance.csproj",
    "src/extensions/WorkflowForge.Extensions.Persistence/WorkflowForge.Extensions.Persistence.csproj",
    "src/extensions/WorkflowForge.Extensions.Persistence.Recovery/WorkflowForge.Extensions.Persistence.Recovery.csproj",
    "src/extensions/WorkflowForge.Extensions.Resilience/WorkflowForge.Extensions.Resilience.csproj",
    "src/extensions/WorkflowForge.Extensions.Resilience.Polly/WorkflowForge.Extensions.Resilience.Polly.csproj",
    "src/extensions/WorkflowForge.Extensions.Validation/WorkflowForge.Extensions.Validation.csproj",
]

ILREPACK_PACKAGE_IDS = {
    "WorkflowForge.Extensions.Logging.Serilog",
    "WorkflowForge.Extensions.Resilience.Polly",
}

FORBIDDEN_LOOSE_DLLS = (
    "Serilog.dll",
    "Serilog.Sinks.Console.dll",
    "Polly.dll",
    "Polly.Core.dll",
)


def _root() -> Path:
    return Path(__file__).resolve().parent.parent


def _run(cmd: list[str], cwd: Path) -> None:
    result = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True)
    if result.returncode != 0:
        raise RuntimeError(
            f"Command failed ({result.returncode}): {' '.join(cmd)}\n"
            f"{result.stdout}\n{result.stderr}"
        )


def _package_id_from_nupkg_name(name: str) -> str:
    match = re.match(r"^(.*)\.\d+\.\d+\.\d+(-[\w.]+)?$", name)
    return match.group(1) if match else name


def _nuspec_dependencies(nupkg: zipfile.ZipFile) -> dict[str, str]:
    nuspec_name = next((n for n in nupkg.namelist() if n.endswith(".nuspec")), None)
    if not nuspec_name:
        return {}
    root = ET.fromstring(nupkg.read(nuspec_name))
    ns = {"n": "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"}
    deps: dict[str, str] = {}
    for dep in root.findall(".//n:dependency", ns):
        dep_id = dep.get("id")
        version = dep.get("version", "")
        if dep_id:
            deps[dep_id] = version
    return deps


def _verify_nupkg(path: Path, errors: list[str]) -> str:
    package_id = _package_id_from_nupkg_name(path.stem)
    with zipfile.ZipFile(path) as archive:
        names = [n.replace("\\", "/") for n in archive.namelist()]
        readme = next((n for n in names if n.endswith("/nuget-readme.md") or n == "nuget-readme.md"), None)
        if readme is None:
            errors.append(f"{package_id}: missing nuget-readme.md")
        else:
            text = archive.read(readme).decode("utf-8")
            if "github.com/animatlabs/workflow-forge/blob" not in text.lower():
                errors.append(f"{package_id}: readme missing GitHub blob link")
            if "dotnet add package" not in text:
                errors.append(f"{package_id}: readme missing install command")
            if "What's new in" not in text:
                errors.append(f"{package_id}: readme missing 2.x release notes section")
            if "## Quick start" in text:
                errors.append(f"{package_id}: readme looks like long README (Quick start section)")
            if len([line for line in text.splitlines() if line.strip()]) > 30:
                errors.append(f"{package_id}: readme exceeds thin shell line budget")

        has_tpn = any(n.endswith("THIRD-PARTY-NOTICES.txt") for n in names)
        if package_id in ILREPACK_PACKAGE_IDS:
            if not has_tpn:
                errors.append(f"{package_id}: missing THIRD-PARTY-NOTICES.txt")
            if readme and "Third-party notices" not in archive.read(readme).decode("utf-8"):
                errors.append(f"{package_id}: readme missing third-party section")
            lib_paths = [n for n in names if n.startswith("lib/")]
            for dll in FORBIDDEN_LOOSE_DLLS:
                if any(n.endswith("/" + dll) for n in lib_paths):
                    errors.append(f"{package_id}: loose merged DLL in lib/: {dll}")
        elif has_tpn:
            errors.append(f"{package_id}: unexpected THIRD-PARTY-NOTICES.txt (non-ILRepack)")

        return package_id


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Pack output directory (default: artifacts/pack-verify under repo root)",
    )
    parser.add_argument(
        "--baseline-dir",
        type=Path,
        default=None,
        help="Directory with baseline .nupkg files (e.g. pack v2.1.2 into artifacts/pack-2.1.2)",
    )
    parser.add_argument("--skip-pack", action="store_true", help="Only verify existing .nupkg in --output")
    args = parser.parse_args()

    root = _root()
    out_dir = args.output or (root / "artifacts" / "pack-verify")
    out_dir.mkdir(parents=True, exist_ok=True)

    errors: list[str] = []

    if not args.skip_pack:
        for rel in PACKABLE_PROJECTS:
            project = root / rel
            print(f"build+pack {rel}")
            _run(["dotnet", "build", str(project), "-c", "Release"], root)
            _run(
                ["dotnet", "pack", str(project), "-c", "Release", "--no-build", "-o", str(out_dir)],
                root,
            )

    packages = sorted(out_dir.glob("*.nupkg"))
    if len(packages) != len(PACKABLE_PROJECTS):
        errors.append(f"expected {len(PACKABLE_PROJECTS)} packages, found {len(packages)} in {out_dir}")

    current_deps: dict[str, dict[str, str]] = {}
    for pkg in packages:
        pid = _verify_nupkg(pkg, errors)
        with zipfile.ZipFile(pkg) as archive:
            current_deps[pid] = _nuspec_dependencies(archive)

    if args.baseline_dir is not None:
        print(f"\nDependency diff vs baseline in {args.baseline_dir}:")
        baseline_deps: dict[str, dict[str, str]] = {}
        for pkg in sorted(args.baseline_dir.glob("*.nupkg")):
            pid = _package_id_from_nupkg_name(pkg.stem)
            with zipfile.ZipFile(pkg) as archive:
                baseline_deps[pid] = _nuspec_dependencies(archive)

        for pid in sorted(set(current_deps) | set(baseline_deps)):
            cur = current_deps.get(pid, {})
            base = baseline_deps.get(pid, {})
            if not base:
                print(f"  {pid}: (no baseline package)")
                continue
            added = set(cur) - set(base)
            removed = set(base) - set(cur)
            changed = {k for k in cur if k in base and cur[k] != base[k]}
            if not added and not removed and not changed:
                print(f"  {pid}: dependencies unchanged ({len(cur)} ids)")
                continue
            print(f"  {pid}:")
            for dep_id in sorted(removed):
                print(f"    - removed {dep_id} {base[dep_id]}")
            for dep_id in sorted(added):
                print(f"    + added {dep_id} {cur[dep_id]}")
            for dep_id in sorted(changed):
                print(f"    ~ {dep_id}: {base[dep_id]} -> {cur[dep_id]}")

    if errors:
        print("\nFAILED:", file=sys.stderr)
        for err in errors:
            print(f"  - {err}", file=sys.stderr)
        return 1

    print(f"\nOK: {len(packages)} packages verified in {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
