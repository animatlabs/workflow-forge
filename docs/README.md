# Docs Site (GitHub Pages)

<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=alert_status" alt="Quality Gate Status" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=coverage" alt="Coverage" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=reliability_rating" alt="Reliability Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=security_rating" alt="Security Rating" /></a>
<a href="https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge"><img src="https://sonarcloud.io/api/project_badges/measure?project=animatlabs_workflow-forge&metric=sqale_rating" alt="Maintainability Rating" /></a>

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
