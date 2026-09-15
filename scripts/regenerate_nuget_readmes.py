#!/usr/bin/env python3
"""Regenerate all src/**/nuget-readme.md from template and build/nuget-package-release-notes.md."""

from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

from generate_nuget_readme import generate_readme

PACKABLE = [
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


def _root() -> Path:
    return Path(__file__).resolve().parent.parent


def _read_version(root: Path) -> str:
    props = root / "src" / "Directory.Build.props"
    text = props.read_text(encoding="utf-8")
    match = re.search(r"<Version>([^<]+)</Version>", text)
    if not match:
        raise RuntimeError("Could not read <Version> from src/Directory.Build.props")
    return match.group(1).strip()


def _project_metadata(project_path: Path) -> dict[str, str]:
    tree = ET.parse(project_path)
    xml_root = tree.getroot()

    def _find(name: str) -> str:
        for el in xml_root.iter():
            if el.tag.endswith(name) and el.text:
                return el.text.strip()
        return ""

    package_id = _find("PackageId") or project_path.stem
    title = _find("Title") or _find("Product") or package_id
    description = _find("Description")
    repo_url = (_find("RepositoryUrl") or "https://github.com/animatlabs/workflow-forge").rstrip("/")
    branch = _find("RepositoryBranch") or "main"
    repo_root = _root()
    rel_dir = project_path.parent.relative_to(repo_root).as_posix()
    long_url = f"{repo_url}/blob/{branch}/{rel_dir}/README.md"

    il_repack = any(
        el.tag.endswith("ILRepackTargetsFile") and (el.text or "").strip()
        for el in xml_root.iter()
    )

    return {
        "package_id": package_id,
        "title": title,
        "description": description,
        "long_url": long_url,
        "il_repack": il_repack,
    }


def main() -> int:
    root = _root()
    version = _read_version(root)
    template = root / "build" / "nuget-readme-template.md"
    release_notes = root / "build" / "nuget-package-release-notes.md"

    for rel in PACKABLE:
        project = root / rel
        meta = _project_metadata(project)
        output = project.parent / "nuget-readme.md"
        generate_readme(
            template=template,
            output=output,
            package_title=meta["title"],
            description=meta["description"],
            package_id=meta["package_id"],
            version=version,
            long_readme_url=meta["long_url"],
            release_notes_path=release_notes,
            include_third_party_notices=meta["il_repack"],
        )
        print(f"wrote {output.relative_to(root)}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
