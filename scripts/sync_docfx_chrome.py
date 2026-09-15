#!/usr/bin/env python3
"""Generate Jekyll site header include from docs/_chrome/nav.json."""

from __future__ import annotations

import json
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def _load_nav(root: Path) -> dict:
    path = root / "docs" / "_chrome" / "nav.json"
    with path.open(encoding="utf-8") as f:
        return json.load(f)


def _jekyll_header(nav: dict) -> str:
    icon = nav["iconUrl"]
    title = nav["siteTitle"]
    gh = nav["githubUrl"]
    links = "\n".join(
        f'        <a href="{{{{ \'/{item["path"]}\' | relative_url }}}}">{item["label"]}</a>'
        for item in nav["links"]
    )
    return f"""<header class="site-header">
  <div class="header-container">
    <a href="{{{{ '/' | relative_url }}}}" class="site-logo">
      <img src="{icon}" alt="{title}" width="32" height="32">
      <span>{{{{ site.title }}}}</span>
    </a>
    <nav class="site-nav">
{links}
      <button class="dark-mode-toggle" id="dark-mode-toggle" aria-label="Toggle dark mode" title="Toggle dark mode">
        <svg class="icon-sun" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/><line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/><line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/><line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/></svg>
        <svg class="icon-moon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="display:none;"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
      </button>
      <a href="{gh}" class="github-link" target="_blank" rel="noopener">
        <svg viewBox="0 0 16 16" width="20" height="20" fill="currentColor"><path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.013 8.013 0 0016 8c0-4.42-3.58-8-8-8z"/></svg>
      </a>
    </nav>
    <button class="mobile-menu-btn" aria-label="Toggle menu">
      <svg viewBox="0 0 24 24" width="24" height="24" stroke="currentColor" stroke-width="2" fill="none"><path d="M3 12h18M3 6h18M3 18h18"/></svg>
    </button>
  </div>
</header>
"""


def sync(root: Path) -> None:
    nav = _load_nav(root)
    includes = root / "docs" / "_includes"
    includes.mkdir(parents=True, exist_ok=True)
    (includes / "site-chrome-header.html").write_text(_jekyll_header(nav), encoding="utf-8")


def main() -> int:
    root = _repo_root()
    sync(root)
    print("Synced Jekyll header from docs/_chrome/nav.json", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
