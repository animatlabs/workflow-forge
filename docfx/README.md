# DocFX API reference

Generate API documentation for all packable WorkflowForge assemblies.

**Recommended (same as CI):** from the repository root:

```bash
python scripts/build_docs.py
```

Preview locally:

```bash
cd docs && bundle install
python scripts/build_docs.py --serve
```

Output is written to `docs/api/` (gitignored). Jekyll site output is `docs/_site/`. GitHub Pages runs `scripts/build_docs.py` in `.github/workflows/pages.yml`.

API-only (no Jekyll):

```bash
python scripts/build_docs.py --api-only
```

## Branding

API pages use the stock **DocFX `modern`** template only (`docfx.json` → `template: ["default", "modern"]`). No custom CSS or Jekyll header on `/api/*`.

- Logo: `docfx/icon-navbar.png` (32×32; regenerate from root `icon.png` if the icon changes) via `_appLogoPath`. Favicon uses full `icon.png`.
- Logo link: `_appLogoUrl` is `"../"` in `docfx.json`, which resolves to the guides home from any `/api/*` page. Nothing rewrites it at publish time.
- Footer: `_appFooter` in `docfx.json` (DocFX attribution + link back to guides).
- Guides nav: edit `docs/_chrome/nav.json`, then `python scripts/sync_docfx_chrome.py` (Jekyll header only).

## Upgrading DocFX

1. Bump the version in `.config/dotnet-tools.json`.
2. Run `dotnet tool restore` then `python scripts/verify_docs_ci.py`.
3. Re-check `_appFooter` / metadata if the modern template changes.

## Pre-deploy check

```bash
python scripts/verify_docs_ci.py
```
