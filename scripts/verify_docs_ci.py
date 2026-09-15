#!/usr/bin/env python3
"""
Pre-flight checks mirroring .github/workflows/pages.yml (build only, no deploy).

Exits non-zero on first failure.
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path


def _root() -> Path:
    return Path(__file__).resolve().parent.parent


def _count_toc_items(data: dict) -> int:
    count = 0
    for item in data.get("items", []):
        count += 1
        count += _count_toc_items(item)
    return count


def _regression_checks(root: Path) -> list[str]:
    errors: list[str] = []
    api_dir = root / "docs" / "api"
    nested = api_dir / "api"
    if nested.is_dir() and (any(nested.glob("*.html")) or (nested / "toc.json").is_file()):
        errors.append(f"nested api path still present: {nested}")

    sample = api_dir / "WorkflowForge.html"
    if sample.is_file():
        text = sample.read_text(encoding="utf-8", errors="replace")
        if "logo.svg" in text:
            errors.append("sample API page still references generic logo.svg")
        if "assets/css/style.css" in text:
            errors.append("sample API page must not load Jekyll style.css")
        if "docfx-wf-brand.css" in text or "docfx-wf-overrides.css" in text:
            errors.append("sample API page must use DocFX stock styles only")
        if "wf-docfx-chrome" in text or 'class="site-header"' in text:
            errors.append("sample API page must not use Jekyll site header chrome")
        if "icon-navbar.png" not in text and "icon.png" not in text:
            errors.append("sample API page missing WorkflowForge logo branding")
        if 'href="../"' not in text and "animatlabs.com/workflow-forge" not in text:
            errors.append("sample API page missing link to documentation home")
        if "public/docfx.min.css" not in text and "styles/docfx.css" not in text:
            errors.append("sample API page missing stock DocFX stylesheet")
        if "branding.css" not in text:
            errors.append("sample API page missing branding.css")
        if 'content="nav-toc.html"' not in text:
            errors.append("sample API page must use nav-toc.html for navbar rel")
        if 'content="toc.html"' not in text or text.count('content="toc.html"') < 1:
            errors.append("sample API page must use toc.html for sidebar rel")
        if "dotnet.github.io/docfx" not in text:
            errors.append("sample API page missing DocFX attribution in footer")
    else:
        errors.append(f"missing flat API page: {sample}")

    nav_toc = api_dir / "nav-toc.json"
    if not nav_toc.is_file():
        errors.append("missing nav-toc.json for API navbar")
    else:
        nav_data = json.loads(nav_toc.read_text(encoding="utf-8"))
        names = [i.get("name") for i in nav_data.get("items", [])]
        if names != ["Documentation", "API"]:
            errors.append(f"nav-toc.json unexpected items: {names}")

    toc_json = api_dir / "toc.json"
    if toc_json.is_file():
        toc_data = json.loads(toc_json.read_text(encoding="utf-8"))
        toc_text = toc_json.read_text(encoding="utf-8", errors="replace")
        if "api/WorkflowForge" in toc_text or "api/toc.html" in toc_text:
            errors.append("toc.json still contains nested api/ paths (breaks sidebar links)")
        if _count_toc_items(toc_data) < 20:
            errors.append("toc.json type tree too small (sidebar likely broken)")
        if '"href":"../"' in toc_text or '"href": "../"' in toc_text:
            errors.append("toc.json must be type tree only, not navbar (../ link)")
    else:
        errors.append("missing toc.json for API sidebar")

    search_index = api_dir / "index.json"
    if search_index.is_file():
        search_text = search_index.read_text(encoding="utf-8", errors="replace")
        if '"api/WorkflowForge' in search_text or '"href": "api/' in search_text:
            errors.append("index.json still contains nested api/ search paths")
        if "README.html" in search_text:
            errors.append("index.json must not index maintainer README.html")
    else:
        errors.append("missing index.json for API search")

    site_api = root / "docs" / "_site" / "api" / "WorkflowForge.html"
    if not site_api.is_file():
        errors.append(f"missing in _site: {site_api}")

    return errors


def main() -> int:
    root = _root()
    py = sys.executable

    steps: list[tuple[str, list[str]]] = [
        ("dotnet tool restore", ["dotnet", "tool", "restore"]),
        (
            "build_docs (api + jekyll)",
            [py, "scripts/build_docs.py", "--baseurl", "/workflow-forge"],
        ),
    ]

    api_index = root / "docs" / "api" / "index.html"
    site_index = root / "docs" / "_site" / "index.html"
    api_flat = root / "docs" / "api" / "WorkflowForge.html"

    for label, cmd in steps:
        print(f"\n=== {label} ===", flush=True)
        subprocess.run(cmd, cwd=root, check=True)

    missing = [p for p in (api_index, site_index, api_flat) if not p.is_file()]
    if missing:
        print("error: expected outputs missing:", file=sys.stderr)
        for p in missing:
            print(f"  - {p}", file=sys.stderr)
        return 1

    regressions = _regression_checks(root)
    if regressions:
        print("error: doc regression checks failed:", file=sys.stderr)
        for msg in regressions:
            print(f"  - {msg}", file=sys.stderr)
        return 1

    print("\nAll doc CI checks passed.", flush=True)
    print(f"  API index:  {api_index}", flush=True)
    print(f"  API type:   {api_flat}", flush=True)
    print(f"  Site:       {site_index}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
