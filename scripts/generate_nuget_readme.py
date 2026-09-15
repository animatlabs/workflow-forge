#!/usr/bin/env python3
"""Expand build/nuget-readme-template.md into per-package nuget-readme.md files."""

from __future__ import annotations

import argparse
import re
from pathlib import Path


def _parse_release_notes_by_package(path: Path) -> dict[str, list[str]]:
    sections: dict[str, list[str]] = {}
    current_key = "_default_"
    sections[current_key] = []

    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("# "):
            current_key = line[2:].strip()
            sections.setdefault(current_key, [])
            continue
        stripped = line.strip()
        if stripped.startswith("- "):
            sections.setdefault(current_key, []).append(stripped)

    return sections


def _release_notes_for_package(notes_path: Path, package_id: str) -> str:
    sections = _parse_release_notes_by_package(notes_path)
    bullets: list[str] = []
    bullets.extend(sections.get("_default_", []))
    bullets.extend(sections.get(package_id, []))
    if not bullets:
        return "- See CHANGELOG on GitHub for this release."
    return "\n".join(bullets)


def generate_readme(
    *,
    template: Path,
    output: Path,
    package_title: str,
    description: str,
    package_id: str,
    version: str,
    long_readme_url: str,
    release_notes_path: Path,
    include_third_party_notices: bool,
) -> None:
    third_party_section = ""
    if include_third_party_notices:
        third_party_section = (
            "## Third-party notices\n\n"
            "This package IL-merges third-party libraries. "
            "See `THIRD-PARTY-NOTICES.txt` in the nupkg."
        )

    release_notes = _release_notes_for_package(release_notes_path, package_id)

    template_text = template.read_text(encoding="utf-8-sig")
    content = (
        template_text.replace("{PackageTitle}", package_title)
        .replace("{OneLineDescription}", description.strip())
        .replace("{PackageId}", package_id)
        .replace("{Version}", version)
        .replace("{LongReadmeUrl}", long_readme_url)
        .replace("{PackageReleaseNotes}", release_notes)
        .replace("{ThirdPartySection}", third_party_section)
    )

    while "\n\n\n" in content:
        content = content.replace("\n\n\n", "\n\n")

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(content.rstrip() + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--template", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--release-notes", type=Path, required=True)
    parser.add_argument("--package-title", required=True)
    parser.add_argument("--one-line-description", default="")
    parser.add_argument("--description-file", type=Path, default=None)
    parser.add_argument("--package-id", required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--long-readme-url", required=True)
    parser.add_argument("--include-third-party-notices", action="store_true")
    args = parser.parse_args()

    description = args.one_line_description.strip()
    if args.description_file is not None:
        # utf-8-sig so a BOM written by MSBuild does not survive into the rendered readme.
        description = args.description_file.read_text(encoding="utf-8-sig").strip()

    if not description:
        raise SystemExit("Package description is required.")

    generate_readme(
        template=args.template,
        output=args.output,
        package_title=args.package_title,
        description=description,
        package_id=args.package_id,
        version=args.version,
        long_readme_url=args.long_readme_url,
        release_notes_path=args.release_notes,
        include_third_party_notices=args.include_third_party_notices,
    )


if __name__ == "__main__":
    main()
