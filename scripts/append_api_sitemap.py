#!/usr/bin/env python3
"""Append generated API HTML URLs to docs/_site/sitemap.xml."""

from __future__ import annotations

import sys
from datetime import date
from pathlib import Path
from xml.etree import ElementTree as ET

SITE = "https://animatlabs.com"
BASE = "/workflow-forge"


def main() -> int:
    root = Path(__file__).resolve().parent.parent
    site_dir = root / "docs" / "_site"
    sitemap_path = site_dir / "sitemap.xml"
    api_dir = site_dir / "api"

    if not sitemap_path.is_file():
        print(f"warning: no sitemap at {sitemap_path}", flush=True)
        return 0
    if not api_dir.is_dir():
        print(f"warning: no api dir at {api_dir}", flush=True)
        return 0

    tree = ET.parse(sitemap_path)
    urlset = tree.getroot()
    ns = {"sm": "http://www.sitemaps.org/schemas/sitemap/0.9"}
    if urlset.tag.endswith("urlset"):
        existing = {
            loc.text
            for loc in urlset.findall("sm:url/sm:loc", ns) or urlset.findall("url/loc")
        }
    else:
        existing = set()

    today = date.today().isoformat()
    added = 0
    for html in sorted(api_dir.glob("*.html")):
        if html.name == "index.html":
            continue
        loc = f"{SITE}{BASE}/api/{html.name}"
        if loc in existing:
            continue
        url_el = ET.Element("url")
        loc_el = ET.SubElement(url_el, "loc")
        loc_el.text = loc
        lastmod = ET.SubElement(url_el, "lastmod")
        lastmod.text = today
        urlset.append(url_el)
        added += 1

    tree.write(sitemap_path, encoding="utf-8", xml_declaration=True)
    print(f"Appended {added} API URLs to sitemap", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
