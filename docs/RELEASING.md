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
- A green `main` build: **`build-linux`** (net8 + net10 + Sonar) and **`build-windows-net48`**.

## Dependency governance

- Package versions are centralized in **`src/Directory.Packages.props`** (CPM). Projects outside
  `src/` pick it up through the solution-root `Directory.Packages.props`, which re-exports it;
  MSBuild's directory walk-up does the resolution, not an explicit import.
- Shipped libraries target **.NET Standard 2.0** (core and extensions). Test projects multi-target
  `net48`, `net8.0`, and `net10.0` to exercise consumer runtimes in CI.

## Sample smoke (manual, before publish)

```powershell
dotnet run --project src/samples/WorkflowForge.Samples.BasicConsole -f net8.0
```

Confirm persistence, validation (stage 2 amount rule), and health-check `using` disposal paths.

## Local pack smoke

```powershell
dotnet pack WorkflowForge.sln -c Release -o artifacts/pack-hygiene
```

Inspect ILRepack packages: no loose Serilog/Polly/OpenTelemetry DLLs under `lib/`, and
`THIRD-PARTY-NOTICES.txt` present. NuGet.org shows a **generated thin readme** (`nuget-readme.md`);
long package docs remain in each project’s `README.md` on GitHub.

**NuGet readme files (committed, regenerated before pack):**

| Artifact | Location |
|----------|----------|
| Template | `build/nuget-readme-template.md` |
| Per-package 2.2.0 bullets | `build/nuget-package-release-notes.md` |
| Generated thin readme (one per package) | `src/**/nuget-readme.md` next to each packable `.csproj` |
| MSBuild wiring | `src/build/Packaging/NuGetReadme.targets` |

Regenerate all `nuget-readme.md` files after editing the template or release-notes file:

```powershell
python scripts/regenerate_nuget_readmes.py
```

`dotnet pack` also regenerates `nuget-readme.md` in each project folder before packing.

Automated pack + content check (all 13 packages):

```powershell
python scripts/verify_nuget_packages.py
```

Optional output folder and dependency listing for manual compare to a prior release:

```powershell
# Pack v2.1.2 into a separate folder first, then:
python scripts/verify_nuget_packages.py --output artifacts/pack-hygiene --baseline-dir artifacts/pack-2.1.2
```

`verify_nuget_packages.py` checks package *contents* (readme, third-party notices, dependency
diffs). Assembly reference closure — that every externally referenced assembly is either merged
in or declared as a NuGet dependency — is verified separately by
`tests/WorkflowForge.Packaging.Smoke.Tests/PackageClosureShould.cs`, and an end-to-end consumer
install/build/run is verified by `PackagingSmokeShould.cs` in the same project. Both run as part
of the normal test suite (net8.0/net10.0 only) and pack the solution themselves — no separate
invocation needed.

## Extension matrix (packable)

| Package | Ship TFM | ILRepack | Notes |
|---------|----------|----------|-------|
| WorkflowForge | netstandard2.0 | no | Core |
| WorkflowForge.Testing | netstandard2.0 | no | Test helpers |
| Extensions.* (11) | netstandard2.0 | see below | Extension packages |

ILRepack: `Resilience.Polly`, `Logging.Serilog`.

Compare packed `.nuspec` dependency floors against the previous NuGet release before publish.

## Release checklist

1. Decide the new version (SemVer).
2. Update `<Version>` in `src/Directory.Build.props`.
3. Update `CHANGELOG.md` (move items from *Unreleased* into the new version section).
4. Update the version line in root `README.md` if present.
5. Commit and open a PR; merge once **`build-linux`** and **`build-windows-net48`** are green.
6. Trigger the release: **Actions → Build and Test → Run workflow** on `main` (leave `version` empty
   to use the committed version, or set it to override).
7. Approve the `nuget-publish` environment when prompted (`publish=true`).
8. Verify the packages on NuGet.org and the attached build artifacts (below).

## Pipeline walkthrough

### Every PR / push to `main`

[`.github/workflows/build-test.yml`](../.github/workflows/build-test.yml):

1. **`build-linux`** (`ubuntu-latest`) — Restore, Release build, `dotnet test` on **net8.0** and **net10.0**
   with OpenCover when Sonar runs; `sonarscanner begin` / `end` when `SONAR_TOKEN` is set. Uploads TRX
   and coverage artifacts.
2. **`build-windows-net48`** (`windows-latest`) — Release build, `dotnet test` on **net48** only.

Documentation deploy is separate: [`.github/workflows/pages.yml`](../.github/workflows/pages.yml)
(manual `workflow_dispatch` only).

### Release (`workflow_dispatch`)

Same workflow file:

1. **`release-pack`** (`ubuntu-latest`) — Release build and pack to `./packages`, package verify
   (`verify_nuget_packages.py --skip-pack`), CycloneDX SBOM to `bom.json` (`dotnet tool restore` then
   `dotnet tool run dotnet-CycloneDX`, pinned in `.config/dotnet-tools.json`), upload `nuget-packages`.
2. **`publish`** (`windows-latest`, `needs: release-pack`) — when `publish=true`: download artifact,
   optional signing, provenance attestations, push to NuGet.org.

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

## Documentation (GitHub Pages)

Before dispatching **Deploy Docs (GitHub Pages)** on `main` or a release branch:

```bash
python scripts/verify_docs_ci.py
```

This runs the same DocFX + Jekyll pipeline as CI and checks API URL layout and branding regressions.
See `docs/README.md` for local preview (`python scripts/serve_docs_docker.py`) and `docfx/README.md` for DocFX-only details.
