# Releasing WorkflowForge

This guide documents how to prepare and run a release of all WorkflowForge NuGet packages using the GitHub Actions pipeline.

---

## Table of Contents

1. [Packages Released](#packages-released)
2. [Prerequisites](#prerequisites)
3. [Pre-Release Checklist](#pre-release-checklist)
4. [Triggering the Release](#triggering-the-release)
5. [What the Pipeline Does](#what-the-pipeline-does)
6. [Post-Release Verification](#post-release-verification)
7. [Rollback Procedure](#rollback-procedure)
8. [Trust Stack for Consumers](#trust-stack-for-consumers)
9. [Future: NuGet Code Signing](#future-nuget-code-signing)
10. [Future: EnablePackageValidation](#future-enablepackagevalidation)

---

## Packages Released

All 13 packages are released simultaneously at the same version:

| Package | Path |
|---|---|
| `WorkflowForge` | `src/core/WorkflowForge/` |
| `WorkflowForge.Testing` | `src/core/WorkflowForge.Testing/` |
| `WorkflowForge.Extensions.Audit` | `src/extensions/WorkflowForge.Extensions.Audit/` |
| `WorkflowForge.Extensions.DependencyInjection` | `src/extensions/WorkflowForge.Extensions.DependencyInjection/` |
| `WorkflowForge.Extensions.Logging.Serilog` | `src/extensions/WorkflowForge.Extensions.Logging.Serilog/` |
| `WorkflowForge.Extensions.Observability.HealthChecks` | `src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/` |
| `WorkflowForge.Extensions.Observability.OpenTelemetry` | `src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/` |
| `WorkflowForge.Extensions.Observability.Performance` | `src/extensions/WorkflowForge.Extensions.Observability.Performance/` |
| `WorkflowForge.Extensions.Persistence` | `src/extensions/WorkflowForge.Extensions.Persistence/` |
| `WorkflowForge.Extensions.Persistence.Recovery` | `src/extensions/WorkflowForge.Extensions.Persistence.Recovery/` |
| `WorkflowForge.Extensions.Resilience` | `src/extensions/WorkflowForge.Extensions.Resilience/` |
| `WorkflowForge.Extensions.Resilience.Polly` | `src/extensions/WorkflowForge.Extensions.Resilience.Polly/` |
| `WorkflowForge.Extensions.Validation` | `src/extensions/WorkflowForge.Extensions.Validation/` |

---

## Prerequisites

### GitHub Secrets

Configure these in **Settings → Secrets and variables → Actions**:

| Secret | Required | Purpose |
|---|---|---|
| `NUGET_API_KEY` | **Yes** | API key from nuget.org with push rights for the `AnimatLabs` org packages |
| `SONAR_TOKEN` | Recommended | SonarCloud authentication; analysis is skipped gracefully when absent |
| `SIGNING_CERT_BASE64` | Optional | Base64-encoded PFX certificate for NuGet code signing (see [Future: NuGet Code Signing](#future-nuget-code-signing)) |
| `SIGNING_CERT_PASSWORD` | Optional | Password for the PFX certificate above |

### GitHub Environment

Create a protected environment named **`nuget-publish`**:

1. Go to **Settings → Environments → New environment**
2. Name it exactly `nuget-publish`
3. Under **Deployment protection rules**, add at least one **Required reviewer** (yourself and/or another maintainer)
4. Optionally restrict to the `main` branch only

This environment gate means the publish job will **pause and wait for manual approval** before pushing anything to NuGet.org. It prevents accidental or unauthorized publishes.

---

## Pre-Release Checklist

Complete these steps **before** triggering the workflow:

- [ ] **Bump the version** in all 13 `.csproj` files:
  ```xml
  <Version>2.x.x</Version>
  <PackageVersion>2.x.x</PackageVersion>
  ```
  Also update `<PackageReleaseNotes>` in each file with a brief summary of changes.

- [ ] **Update `CHANGELOG.md`** — add a new `[x.x.x] - YYYY-MM-DD` entry documenting all changes.

- [ ] **Run full tests locally** across all target frameworks:
  ```powershell
  dotnet test WorkflowForge.sln -c Release --framework net8.0
  dotnet test WorkflowForge.sln -c Release --framework net10.0
  dotnet test WorkflowForge.sln -c Release --framework net48
  ```

- [ ] **Verify clean build** with no warnings-as-errors:
  ```powershell
  dotnet build WorkflowForge.sln -c Release
  ```

- [ ] **Verify NuGet audit passes** (runs automatically on restore; no vulnerable deps):
  ```powershell
  dotnet restore WorkflowForge.sln
  ```

- [ ] **Commit and push** all changes to `main` (or merge your PR). Wait for the automated `Build and Test` CI run to go green — do **not** trigger a publish from a failing commit.

---

## Triggering the Release

The release is triggered with a **manual `workflow_dispatch`** from GitHub Actions.

1. Go to your repository on GitHub: `https://github.com/animatlabs/workflow-forge`
2. Click the **Actions** tab
3. In the left sidebar, select **Build and Test**
4. Click the **Run workflow** dropdown button (top-right of the run list)
5. Fill in the three inputs:

   | Input | Value | Notes |
   |---|---|---|
   | **Publish to NuGet** | `true` | Must be `true` to trigger the publish job |
   | **Package version override** | `2.1.0` | The exact version string to embed in all packages |
   | **Sign packages** | `true` / `false` | Set `true` only if `SIGNING_CERT_BASE64` + `SIGNING_CERT_PASSWORD` secrets are configured |

6. Click **Run workflow**

The `build` job will run first (full build, test, pack, SBOM). Once it succeeds, the `publish` job will start — but it will **pause at the `nuget-publish` environment gate** waiting for your approval.

7. You will receive a notification (email / GitHub notification). Navigate to the running workflow, find the pending deployment step, and click **Review deployments → Approve and deploy**.

The publish job will then:
- Sign packages (if enabled)
- Attest `.nupkg`, `.snupkg`, and `bom.json` with Sigstore provenance
- Push all packages to NuGet.org

---

## What the Pipeline Does

```
workflow_dispatch
      │
      ▼
┌─────────────────────────────────────────────────────┐
│  build job (windows-latest)                         │
│                                                     │
│  1.  Checkout (full depth for SonarCloud)           │
│  2.  Setup .NET 8, .NET 10, Java 17                 │
│  3.  Restore (NuGetAudit scans all deps for CVEs)   │
│  4.  Build Release                                  │
│  5.  Test net8.0 + code coverage (OpenCover)        │
│  6.  Test net10.0 + code coverage (OpenCover)       │
│  7.  Test net48                                     │
│  8.  SonarCloud analysis (if SONAR_TOKEN present)   │
│  9.  Upload test-results artifact                   │
│  10. Upload coverage-reports artifact               │
│  11. dotnet pack → ./packages/*.nupkg + *.snupkg    │
│  12. CycloneDX SBOM → ./packages/bom.json           │
│  13. Upload nuget-packages artifact                 │
└─────────────────────────────────────────────────────┘
      │
      ▼  (needs: build AND publish == true)
┌─────────────────────────────────────────────────────┐
│  publish job ← PAUSES for nuget-publish approval    │
│                                                     │
│  1.  Download nuget-packages artifact               │
│  2.  (optional) Sign *.nupkg + *.snupkg with PFX    │
│  3.  Attest *.nupkg   → Sigstore provenance         │
│  4.  Attest *.snupkg  → Sigstore provenance         │
│  5.  Attest bom.json  → Sigstore provenance         │
│  6.  Push *.nupkg + *.snupkg to NuGet.org           │
└─────────────────────────────────────────────────────┘
```

---

## Post-Release Verification

### 1. Check NuGet.org

Packages typically appear within 5–15 minutes. Verify all 13 are listed at the correct version:

```
https://www.nuget.org/packages/WorkflowForge/
https://www.nuget.org/packages/WorkflowForge.Extensions.Audit/
... (repeat for all 13)
```

### 2. Verify Sigstore Build Attestation

Each `.nupkg`, `.snupkg`, and `bom.json` has a GitHub-signed Sigstore provenance record. Download the packages from the workflow artifact and verify:

```bash
# Single package
gh attestation verify WorkflowForge.2.1.0.nupkg \
  --repo animatlabs/workflow-forge

# All packages at once (PowerShell)
Get-ChildItem ./packages -Filter *.nupkg | ForEach-Object {
    Write-Host "Verifying $($_.Name)..."
    gh attestation verify $_.FullName --repo animatlabs/workflow-forge
}
```

### 3. Create the GitHub Release

1. Go to **Releases → Draft a new release**
2. Create a new tag: `v2.1.0` pointing to `main`
3. Title: `WorkflowForge v2.1.0`
4. Paste the relevant CHANGELOG.md section as release notes
5. Attach the following files from the `nuget-packages` workflow artifact as release assets:
   - All `*.nupkg` files (13 packages)
   - `bom.json` (CycloneDX SBOM)
6. Click **Publish release**

### 4. Inspect a Package (Optional)

Open any `.nupkg` in [NuGet Package Explorer](https://github.com/NuGetPackageExplorer/NuGetPackageExplorer) and confirm:

- Repository URL: `https://github.com/animatlabs/workflow-forge`
- SourceLink is enabled (embedded PDB, source-mapped to GitHub)
- README and icon are present

---

## Rollback Procedure

NuGet packages **cannot be deleted** once published — they can only be unlisted.

1. **Unlist on NuGet.org**:
   - Log in at nuget.org
   - For each affected package: navigate to that package → **Manage** → unlist the affected version
   - The version will disappear from search but existing `PackageReference` locks still resolve it

2. **Mark the GitHub Release as defective**:
   - Edit the release, prepend `[DEFECTIVE - DO NOT USE]` to the title
   - Add a note explaining the issue and pointing to the fixed version

3. **Fix and re-release**:
   - Bump to the next patch version (e.g., `2.1.0` → `2.1.1`)
   - Follow the full pre-release checklist and release process again

---

## Trust Stack for Consumers

WorkflowForge does not currently have a commercial NuGet code-signing certificate. Instead, every release is hardened with the following verifiable trust signals:

| Signal | Details / How to verify |
|---|---|
| **Sigstore build provenance** | `.nupkg`, `.snupkg`, and SBOM are attested via `actions/attest-build-provenance`. Verify with `gh attestation verify`. |
| **SourceLink** | Full source debugging — step into WorkflowForge code from your IDE, maps to exact commit on GitHub. |
| **Deterministic builds** | `<Deterministic>true</Deterministic>` — same source + same SDK = byte-for-byte identical output. |
| **Strong-name signing** | Assemblies are signed with `WorkflowForge.snk`. Public key is embedded in all `InternalsVisibleTo` attributes. |
| **Embedded PDBs** | `<DebugType>embedded</DebugType>` — full symbols in the `.nupkg`; no separate symbol server needed. |
| **CycloneDX SBOM** | `bom.json` lists the full dependency graph. Attested alongside the packages. |
| **NuGet vulnerability audit** | `<NuGetAudit>all</NuGetAudit>` — any known CVE in direct or transitive deps fails the build. |
| **SonarCloud analysis** | Quality gate checked on every PR and main branch push. |
| **SHA-pinned GitHub Actions** | All CI/CD actions pinned to commit SHAs, not mutable tags. |
| **Environment approval gate** | Human review required before any publish to NuGet.org. |
| **Dependabot** | NuGet + GitHub Actions dependencies auto-updated weekly via PRs. |

---

## Future: NuGet Code Signing

To enable certificate-based NuGet code signing (which adds a padlock icon on nuget.org):

1. **Obtain a certificate** — options:
   - **Azure Trusted Signing** (~$10/month): works directly with `dotnet nuget sign` via Azure identity
   - **SignPath Foundation** (free for qualifying OSS projects): apply at https://about.signpath.io/product/open-source

2. **Export as PFX** and base64-encode it:
   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("signing-cert.pfx")) | Set-Clipboard
   ```

3. **Add to GitHub Secrets**:
   - `SIGNING_CERT_BASE64` — the base64 string
   - `SIGNING_CERT_PASSWORD` — the PFX password

4. When triggering the release, set **Sign packages = true** in the workflow_dispatch inputs.

The pipeline already has the signing step implemented — it will activate automatically once the secrets are present.

---

## Future: EnablePackageValidation

After v2.1.0 ships, add the following to `src/Directory.Build.props` to automatically catch accidental public API breaking changes between releases:

```xml
<EnablePackageValidation>true</EnablePackageValidation>
<PackageValidationBaselineVersion>2.1.0</PackageValidationBaselineVersion>
```

With this enabled, `dotnet pack` will compare the new package's public API surface against the baseline version downloaded from NuGet.org. Any unintentional removals or incompatible changes will fail the build before packages are published.
