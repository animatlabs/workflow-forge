# Docs Site (GitHub Pages)

This folder is the GitHub Pages source for WorkflowForge documentation.

## Local Preview

From the `docs/` directory:

```bash
bundle install
bundle exec jekyll serve --config _config.yml
```

The site will be available at `http://localhost:4000/`.

## GitHub Pages Setup

1. In GitHub, go to **Settings → Pages**.
2. Set **Source** to **GitHub Actions**.
3. The workflow at `.github/workflows/pages.yml` will build and deploy the site on each push to `main`.
