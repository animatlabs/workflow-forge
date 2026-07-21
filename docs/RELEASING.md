# Releasing WorkflowForge

All packages (core + `WorkflowForge.Testing` + the 11 extensions) release together under a single
version number. This document covers the prerequisites, the release checklist, the CI/CD pipeline
walkthrough, artifact/attestation verification, and rollback.

## Versioning model

The version is centralized in **`src/Directory.Build.props`** (`<Version>`). Every packable project
inherits it — there are no per-project `<Version>`/`<PackageVersion>` entries. `PackageVersion` is
derived from `Version` unless explicitly overridden.

The release workflow also accepts an optional `version` input (a `workflow_dispatch` parameter). When
supplied it overrides **both** `Version` and `PackageVersion` at pack time (and rebuilds so the
assembly version matches the package version). When omitted, the packages are packed at the version
committed in `src/Directory.Build.props`.

> Prefer bumping `src/Directory.Build.props` in a committed change and leaving the workflow input
> empty — that keeps the source of truth in git. Use the input only for ad-hoc/out-of-band packs.

## Prerequisites

- Maintainer access to run the `Build and Test` workflow via **Run workflow** (`workflow_dispatch`).
- Approval rights on the `nuget-publish` GitHub Environment (publishing is gated on human approval).
- `NUGET_API_KEY` and `SONAR_TOKEN` configured as repository secrets.
- A green `main` build (tests pass on `net48`, `net8.0`, `net10.0`).

## Release checklist

1. Decide the new version (SemVer).
2. Update `<Version>` in `src/Directory.Build.props`.
3. Update `CHANGELOG.md` (move items from *Unreleased* into the new version section).
4. Update the version badge/number references in `README.md` if present.
5. Commit and open a PR; merge once CI is green.
6. Trigger the release: **Actions → Build and Test → Run workflow** on `main` (leave `version` empty
   to use the committed version, or set it to override).
7. Approve the `nuget-publish` environment when prompted.
8. Verify the packages on NuGet.org and the attached build artifacts (below).

## Pipeline walkthrough

The `.github/workflows/build-test.yml` workflow, on `workflow_dispatch`:

1. **Restore + Build + Test** in `Release` across `net8.0`, `net10.0`, and `net48`, with SonarCloud
   analysis and coverage collection.
2. **Pack** — `dotnet pack WorkflowForge.sln --configuration Release` into `./packages`. The three
   ILRepack extensions (`Resilience.Polly`, `Observability.OpenTelemetry`, `Logging.Serilog`) merge
   their third-party libraries during this Release build. (Packing in a non-Release configuration is
   blocked by a guard target, because ILRepack only runs in Release.)
3. **SBOM** — CycloneDX generates a JSON SBOM alongside the packages.
4. **Provenance** — Sigstore build-provenance attestation is produced for the `.nupkg`, `.snupkg`,
   and SBOM artifacts.
5. **Publish** — gated by the `nuget-publish` environment (manual approval), then pushed to NuGet.org.

## Verifying artifacts and attestation

- Confirm every expected package is present in `./packages` (core + Testing + 11 extensions), all at
  the same version, each with a matching `.snupkg`.
- For the ILRepack packages, confirm the merged third-party assemblies are **not** shipped as separate
  files and that required transitive dependencies (e.g. `Microsoft.Bcl.TimeProvider` for Polly) are
  listed in the `.nuspec` `<dependencies>`.
- Verify provenance with the GitHub CLI:

  ```bash
  gh attestation verify <package>.nupkg --repo animatlabs/workflow-forge
  ```

## Rollback

NuGet packages cannot be deleted once published; they can only be **unlisted**. To roll back:

1. Unlist the affected versions on NuGet.org (they remain restorable by exact version but disappear
   from search/latest resolution).
2. Fix forward: bump `<Version>` to a new patch, correct the issue, and run the release again.

## Future signing options

Author (certificate) signing of the `.nupkg` (in addition to the current Sigstore build-provenance
attestation) can be added to the publish step when a code-signing certificate is available.
