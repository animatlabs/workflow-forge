# Docs Site (GitHub Pages)

<a href="https://github.com/animatlabs/workflow-forge/actions/workflows/build-test.yml"><img src="https://github.com/animatlabs/workflow-forge/actions/workflows/build-test.yml/badge.svg?branch=main" alt="Build and Test" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status" alt="Quality Gate Status" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage" alt="Coverage" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating" alt="Reliability Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating" alt="Security Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating" alt="Maintainability Rating" /></a>

This folder powers the GitHub Pages site for WorkflowForge ([animatlabs.com/workflow-forge](https://animatlabs.com/workflow-forge/)).

## Local preview

From the repository root (requires Docker, Python, .NET SDK):

```bash
python scripts/serve_docs_docker.py
```

Open **http://127.0.0.1:8080/workflow-forge/** (API: **…/workflow-forge/api/WorkflowForge.html**).

Skip rebuild if `docs/_site` is already current:

```bash
python scripts/serve_docs_docker.py --skip-build
```

## Pre-deploy verification

Same pipeline as [`.github/workflows/pages.yml`](../.github/workflows/pages.yml):

```bash
python scripts/verify_docs_ci.py
```

See [RELEASING.md](RELEASING.md#documentation-github-pages) before deploying docs.

## Build only

```bash
python scripts/build_docs.py --baseurl /workflow-forge
```

DocFX output is written to `docs/api/` (gitignored). Jekyll output is `docs/_site/`.

Details: [docfx/README.md](../docfx/README.md).

## Optional: Ruby-only Jekyll (guides only)

If you have Ruby/Bundler and **do not** need generated API pages:

```bash
cd docs
bundle install
bundle exec jekyll serve --baseurl /workflow-forge
```

For full site parity including `.NET API`, use `serve_docs_docker.py` or `build_docs.py` above.

After API/doc pipeline changes, smoke-check locally: left sidebar lists namespaces/types, search finds `WorkflowBuilder`, logo links to guides home (`../`).

## Public copy (guides)

The site documents **current** WorkflowForge only (not versioned doc sets). Do not put release semver labels in nav or hub tables (for example `(2.2+)`). Upgrade history belongs in [CHANGELOG](../CHANGELOG.md) on GitHub.

## GitHub Pages

1. **Settings → Pages** → Source: **GitHub Actions** (required for **Deploy Docs**; branch `/docs` only runs Jekyll and skips DocFX).
2. **Actions → Deploy Docs (GitHub Pages)** — [`.github/workflows/pages.yml`](../.github/workflows/pages.yml) — `workflow_dispatch` on `main`. Approve **`github-pages`** if your environment requires it.

Pushes to `main` run **Build and Test** only (build/test), not docs deploy.
