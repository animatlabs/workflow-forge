#!/usr/bin/env python3
"""
WorkflowForge NuGet Package Publisher
Cross-platform publish script for building, packing, and publishing NuGet packages.
Usage: python scripts/publish-packages.py --version 2.1.0 [--publish] [--api-key KEY] [--sign] [--cert-path PATH] [--cert-password PWD]
"""

import argparse
import subprocess
import sys
import zipfile
from pathlib import Path

PACKAGES = [
    "src/core/WorkflowForge/WorkflowForge.csproj",
    "src/core/WorkflowForge.Testing/WorkflowForge.Testing.csproj",
    "src/extensions/WorkflowForge.Extensions.DependencyInjection/WorkflowForge.Extensions.DependencyInjection.csproj",
    "src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj",
    "src/extensions/WorkflowForge.Extensions.Resilience/WorkflowForge.Extensions.Resilience.csproj",
    "src/extensions/WorkflowForge.Extensions.Resilience.Polly/WorkflowForge.Extensions.Resilience.Polly.csproj",
    "src/extensions/WorkflowForge.Extensions.Validation/WorkflowForge.Extensions.Validation.csproj",
    "src/extensions/WorkflowForge.Extensions.Audit/WorkflowForge.Extensions.Audit.csproj",
    "src/extensions/WorkflowForge.Extensions.Persistence/WorkflowForge.Extensions.Persistence.csproj",
    "src/extensions/WorkflowForge.Extensions.Persistence.Recovery/WorkflowForge.Extensions.Persistence.Recovery.csproj",
    "src/extensions/WorkflowForge.Extensions.Observability.Performance/WorkflowForge.Extensions.Observability.Performance.csproj",
    "src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/WorkflowForge.Extensions.Observability.HealthChecks.csproj",
    "src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/WorkflowForge.Extensions.Observability.OpenTelemetry.csproj",
]

def run(cmd, cwd=None, check=True):
    """Run a command and return the result."""
    print(f"  > {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    result = subprocess.run(cmd, shell=isinstance(cmd, str), cwd=cwd, capture_output=True, text=True)
    if check and result.returncode != 0:
        print(f"  ERROR: {result.stderr.strip()}")
        sys.exit(1)
    return result

def main():
    parser = argparse.ArgumentParser(description="WorkflowForge NuGet Package Publisher")
    parser.add_argument("--version", required=True, help="Package version (e.g. 2.1.0)")
    parser.add_argument("--api-key", help="NuGet API key")
    parser.add_argument("--publish", action="store_true", help="Actually publish (dry-run by default)")
    parser.add_argument("--sign", action="store_true", help="Sign packages before publishing")
    parser.add_argument("--cert-path", help="Path to signing certificate (.pfx)")
    parser.add_argument("--cert-password", help="Certificate password")
    args = parser.parse_args()

    root = Path(__file__).parent.parent
    output_dir = root / "packages"
    output_dir.mkdir(exist_ok=True)

    print(f"\n{'='*60}")
    print(f"WorkflowForge Package Publisher v{args.version}")
    print(f"Mode: {'PUBLISH' if args.publish else 'DRY RUN'}")
    print(f"Sign: {'Yes' if args.sign else 'No'}")
    print(f"{'='*60}\n")

    # Step 1: Restore
    print("[1/5] Restoring dependencies...")
    run(["dotnet", "restore", "WorkflowForge.sln"], cwd=root)

    # Step 2: Build
    print("\n[2/5] Building solution...")
    run(["dotnet", "build", "WorkflowForge.sln", "--configuration", "Release", "--no-restore"], cwd=root)

    # Step 3: Pack
    print("\n[3/5] Packing NuGet packages...")
    results = []
    for project in PACKAGES:
        project_path = root / project
        name = project_path.stem
        print(f"  Packing {name}...")
        run([
            "dotnet", "pack", str(project_path),
            "--configuration", "Release",
            "--no-build",
            f"-p:PackageVersion={args.version}",
            f"--output", str(output_dir),
        ], cwd=root)
        results.append({"name": name, "packed": True, "signed": False, "published": False})

    # Step 4: Verify
    print("\n[4/5] Verifying packages...")
    for nupkg in sorted(output_dir.glob("*.nupkg")):
        with zipfile.ZipFile(nupkg) as zf:
            names = zf.namelist()
            has_readme = any("README" in n for n in names)
            print(f"  {nupkg.name}: {'OK' if has_readme else 'WARNING: no README'} ({len(names)} files)")

    # Step 5: Sign (optional)
    if args.sign:
        print("\n[4.5/5] Signing packages...")
        if not args.cert_path:
            print("  ERROR: --cert-path required for signing")
            sys.exit(1)
        for nupkg in sorted(output_dir.glob("*.*nupkg")):
            cmd = [
                "dotnet", "nuget", "sign", str(nupkg),
                "--certificate-path", args.cert_path,
                "--timestamper", "http://timestamp.digicert.com",
            ]
            if args.cert_password:
                cmd.extend(["--certificate-password", args.cert_password])
            run(cmd, cwd=root)
            for r in results:
                if nupkg.name.startswith(r["name"] + "."):
                    r["signed"] = True

    # Step 6: Publish (optional)
    if args.publish:
        print("\n[5/5] Publishing to NuGet...")
        if not args.api_key:
            print("  ERROR: --api-key required for publishing")
            sys.exit(1)
        for nupkg in sorted(output_dir.glob("*.*nupkg")):
            print(f"  Publishing {nupkg.name}...")
            run([
                "dotnet", "nuget", "push", str(nupkg),
                "--api-key", args.api_key,
                "--source", "https://api.nuget.org/v3/index.json",
                "--skip-duplicate",
            ], cwd=root)
            for r in results:
                if nupkg.name.startswith(r["name"] + "."):
                    r["published"] = True
    else:
        print("\n[5/5] Skipping publish (dry-run mode). Use --publish to push to NuGet.")

    # Summary
    print(f"\n{'='*60}")
    print("SUMMARY")
    print(f"{'='*60}")
    print(f"{'Package':<55} {'Pack':>5} {'Sign':>5} {'Push':>5}")
    print("-" * 75)
    for r in results:
        pack = "OK" if r["packed"] else "-"
        sign = "OK" if r["signed"] else "-"
        push = "OK" if r["published"] else "-"
        print(f"{r['name']:<55} {pack:>5} {sign:>5} {push:>5}")
    print()

if __name__ == "__main__":
    main()
