#!/usr/bin/env python3
"""Flatten docs/api/api/*.html to docs/api/*.html and fix internal links."""

from __future__ import annotations

import json
import re
import shutil
import sys
from pathlib import Path

NESTED = Path("api")
NAV_TOC_NAME = "nav-toc.json"
SEARCH_SKIP = frozenset({"README.html"})
REDIRECT_INDEX = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta http-equiv="refresh" content="0; url=WorkflowForge.html">
  <link rel="canonical" href="WorkflowForge.html">
  <title>WorkflowForge API</title>
</head>
<body>
  <p><a href="WorkflowForge.html">WorkflowForge API</a></p>
</body>
</html>
"""

_HREF_API = re.compile(r"""href=(["'])api/([^"']+)\1""")
_SRC_API = re.compile(r"""src=(["'])api/([^"']+)\1""")


def _nav_toc_document() -> dict:
    return {
        "items": [
            {"name": "Documentation", "href": "../", "topicHref": "../"},
            {
                "name": "API",
                "href": "WorkflowForge.html",
                "tocHref": "toc.html",
                "topicHref": "WorkflowForge.html",
                "topicUid": "WorkflowForge",
            },
        ]
    }


def _rewrite_text(text: str) -> str:
    text = _HREF_API.sub(r"href=\1\2\1", text)
    text = _SRC_API.sub(r"src=\1\2\1", text)
    text = text.replace('href="api/', 'href="')
    text = text.replace("href='api/", "href='")
    text = text.replace('"/api/api/', '"/api/')
    text = text.replace("'api/api/", "'api/")
    text = text.replace('href="../styles/', 'href="styles/')
    text = text.replace("href='../styles/", "href='styles/")
    text = text.replace('src="../styles/', 'src="styles/')
    text = text.replace('href="../toc.html', 'href="toc.html')
    text = text.replace('name="docfx:navrel" content="../toc.html"', 'name="docfx:navrel" content="nav-toc.html"')
    text = text.replace('name="docfx:tocrel" content="../toc.html"', 'name="docfx:tocrel" content="toc.html"')
    text = text.replace('name="docfx:navrel" content="toc.html"', 'name="docfx:navrel" content="nav-toc.html"')
    text = text.replace('property="docfx:navrel" content="../toc.html"', 'property="docfx:navrel" content="nav-toc.html"')
    text = text.replace('property="docfx:tocrel" content="../toc.html"', 'property="docfx:tocrel" content="toc.html"')
    text = text.replace('property="docfx:navrel" content="toc.html"', 'property="docfx:navrel" content="nav-toc.html"')
    text = text.replace("../../assets/", "../assets/")
    text = text.replace('property="docfx:rel" content="../"', 'property="docfx:rel" content=""')
    text = text.replace('name="docfx:rel" content="../"', 'name="docfx:rel" content=""')
    text = text.replace('href="../public/', 'href="public/')
    text = text.replace("href='../public/", "href='public/")
    text = text.replace('src="./../public/', 'src="public/')
    text = text.replace('src="../public/', 'src="public/')
    text = text.replace('href="../icon.png"', 'href="icon.png"')
    text = text.replace("href='../icon.png'", "href='icon.png'")
    text = text.replace('href="../icon-navbar.png"', 'href="icon-navbar.png"')
    text = text.replace('src="../icon.png"', 'src="icon.png"')
    text = text.replace("src='../icon.png'", "src='icon.png'")
    text = text.replace('src="../icon-navbar.png"', 'src="icon-navbar.png"')
    text = text.replace("src='../icon-navbar.png'", "src='icon-navbar.png'")
    text = text.replace('href="../index.html"', 'href="index.html"')
    text = text.replace('content="../toc.html"', 'content="toc.html"')
    text = text.replace('content="../nav-toc.html"', 'content="nav-toc.html"')
    if 'href="branding.css"' not in text and 'href="public/main.css"' in text:
        text = text.replace(
            '<link rel="stylesheet" href="public/main.css">',
            '<link rel="stylesheet" href="public/main.css">\n'
            '      <link rel="stylesheet" href="branding.css">',
        )
    return text


def _promote_type_toc(api_dir: Path) -> None:
    """Navbar uses nav-toc.json; sidebar uses toc.json (full type tree)."""
    nested_toc = api_dir / NESTED / "toc.json"
    root_toc = api_dir / "toc.json"
    nav_path = api_dir / NAV_TOC_NAME

    nav_path.write_text(
        json.dumps(_nav_toc_document(), separators=(",", ":")) + "\n",
        encoding="utf-8",
    )

    if nested_toc.is_file():
        shutil.move(str(nested_toc), str(root_toc))
    elif root_toc.is_file():
        data = json.loads(root_toc.read_text(encoding="utf-8"))
        items = data.get("items", [])
        nav_only = (
            len(items) <= 3
            and items
            and all(i.get("name") in ("Home", "Documentation", "API") for i in items)
        )
        if nav_only:
            raise RuntimeError(
                "toc.json still looks like navbar-only; missing nested api/toc.json to promote"
            )


def _rewrite_search_index(api_dir: Path) -> None:
    index_path = api_dir / "index.json"
    if not index_path.is_file():
        return
    raw = json.loads(index_path.read_text(encoding="utf-8"))
    updated: dict = {}
    for key, entry in raw.items():
        flat_key = key[4:] if key.startswith("api/") else key
        if flat_key in SEARCH_SKIP or key in SEARCH_SKIP:
            continue
        if isinstance(entry, dict) and "href" in entry:
            href = entry["href"]
            if isinstance(href, str) and href.startswith("api/"):
                entry = {**entry, "href": href[4:]}
        updated[flat_key] = entry
    index_path.write_text(json.dumps(updated, indent=2) + "\n", encoding="utf-8")


def _rewrite_all_pages(api_dir: Path) -> None:
    skip_json = {NAV_TOC_NAME, "toc.json", "index.json", "manifest.json", "xrefmap.yml"}
    for path in api_dir.rglob("*"):
        if not path.is_file():
            continue
        if path.parent.name == NESTED and path.parent.parent == api_dir:
            continue
        if path.suffix == ".json" and path.name in skip_json:
            continue
        if path.suffix not in (".html", ".json") and path.name != "toc.html":
            continue
        raw = path.read_text(encoding="utf-8")
        updated = _rewrite_text(raw)
        if updated != raw:
            path.write_text(updated, encoding="utf-8")

    (api_dir / "index.html").write_text(REDIRECT_INDEX, encoding="utf-8")


def _remove_nested_api_dir(api_dir: Path) -> None:
    nested = api_dir / NESTED
    if nested.is_dir():
        shutil.rmtree(nested)


def flatten(api_dir: Path) -> None:
    nested = api_dir / NESTED
    if nested.is_dir():
        for html in nested.glob("*.html"):
            target = api_dir / html.name
            if target.exists() and target.name != html.name:
                raise RuntimeError(f"flatten collision: {target}")
            shutil.move(str(html), str(target))

    _promote_type_toc(api_dir)
    _remove_nested_api_dir(api_dir)
    _rewrite_all_pages(api_dir)
    _rewrite_search_index(api_dir)


def main() -> int:
    root = Path(__file__).resolve().parent.parent
    api_dir = root / "docs" / "api"
    if not api_dir.is_dir():
        print(f"error: missing {api_dir}", file=sys.stderr)
        return 1
    flatten(api_dir)
    nested = api_dir / NESTED
    if nested.is_dir() and any(nested.glob("*.html")):
        print("error: nested api html remains after flatten", file=sys.stderr)
        return 1
    print(f"Flattened API output under {api_dir}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
