# WorkflowForge Release Checklist

This document outlines the steps required to sign, package, and publish WorkflowForge to NuGet.org.

## Pre-Release Status

| Item | Status |
|------|--------|
| Code changes merged | Done |
| All tests passing (net48/net8.0/net10.0) | Done |
| CHANGELOG.md updated | Done |
| Documentation updated | Done |
| Benchmarks captured and documented | Done |
| Version bumped in all csproj files | Done |
| Zero-warning build (net48/net8.0/net10.0) | Done |

## Pending: Steps on Your End

### 1. Strong-Name Signing (SNK) — Optional but Recommended

Strong-name signing allows consumers to reference WorkflowForge from strong-named assemblies.

```bash
# Run from repository root

# Generate the key pair
sn -k WorkflowForge.snk

# Extract and display the public key (needed for InternalsVisibleTo)
sn -p WorkflowForge.snk WorkflowForge.pub
sn -tp WorkflowForge.pub
```

Strong-name signing is already configured:

- `src/Directory.Build.props` and `tests/Directory.Build.props` set `SignAssembly=true` and reference the SNK file
- All `InternalsVisibleTo` entries in `src/core/WorkflowForge/WorkflowForge.csproj` include the `PublicKey=` suffix
- The `WorkflowForge.Extensions.Resilience.Polly` ILRepack targets re-sign the merged assembly via `KeyFile`
- Public key token: `50cc659aecc59b65`

The `WorkflowForge.snk` file is committed to the repository (standard for OSS projects).
Strong-name signing provides assembly identity only, not security. NuGet code signing (PFX
via SignPath) provides the tamper-proof security guarantee.

If you ever need to regenerate the SNK (this will change assembly identity):

```bash
sn -k WorkflowForge.snk
sn -p WorkflowForge.snk WorkflowForge.pub
sn -tp WorkflowForge.pub
```

Then update the `PublicKey=` values in all `InternalsVisibleTo` entries and the `DynamicProxyGenAssembly2`
entry (use the Moq well-known public key for that one).

### 2. NuGet Package Signing (PFX) — Optional but Recommended

NuGet package signing provides tamper-evident publishing and eliminates install warnings.

**Option A: SignPath Foundation (Free for Open Source) — Recommended**

[SignPath Foundation](https://signpath.org) provides **free code signing certificates** for qualifying open source projects:

- No cost for OSS projects with an OSI-approved license
- No personal identification required -- signs against your repository
- Private keys stored on Hardware Security Modules (HSM)
- Supports NuGet `.nupkg` and `.snupkg` signing
- Integrates with CI/CD pipelines (GitHub Actions, Azure DevOps)

To apply:
1. Visit [signpath.org](https://signpath.org) and submit your project
2. Your project must be actively maintained, released, and use an OSI-approved license
3. Once approved, configure the signing pipeline per their [documentation](https://docs.signpath.io)

**Option B: Create a self-signed certificate (for testing/development only)**

Self-signed certificates are not trusted by package managers and will show warnings. Use only for local testing.

```powershell
$cert = New-SelfSignedCertificate -Subject "CN=AnimatLabs" -Type CodeSigningCert -CertStoreLocation "Cert:\CurrentUser\My"
Export-PfxCertificate -Cert $cert -FilePath "AnimatLabs.pfx" -Password (ConvertTo-SecureString -String "YOUR_PASSWORD" -Force -AsPlainText)
```

**Option C: Purchase a code-signing certificate**
- Obtain from a CA (DigiCert, Sectigo, etc.)
- Export as `.pfx` with a password
- Most expensive option but provides broadest trust

### 3. Configure GitHub Actions Secrets

Navigate to **GitHub Repository > Settings > Secrets and variables > Actions** and add:

| Secret | Value | Required For |
|--------|-------|--------------|
| `SONAR_TOKEN` | SonarCloud project token | Code quality analysis |
| `NUGET_API_KEY` | NuGet.org API key | Publishing packages |
| `SIGNING_CERT_BASE64` | Base64-encoded `.pfx` file | Package signing |
| `SIGNING_CERT_PASSWORD` | Certificate password | Package signing |

To generate the Base64 value:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("path\to\cert.pfx"))
```

### 4. Build, Pack, and Publish via GitHub Actions

1. Push the `release/2.x` branch to GitHub
2. Navigate to **Actions > Build and Test** workflow
3. Trigger the workflow manually (`workflow_dispatch`) with:
   - `publish: true` to publish to NuGet
   - `sign: true` to sign packages (requires signing secrets)
   - `version: 2.1.0` to set the package version
4. The CI pipeline will build, test, pack, sign (if secrets configured), and publish

The SonarCloud quality gate runs as part of the build but is **non-blocking** -- check the [SonarCloud dashboard](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge) before publishing.

### 5. Create GitHub Issues (Optional but Recommended)

A PowerShell script is provided to create all 60 work items as GitHub Issues:

```powershell
# Requires GitHub CLI (gh) installed and authenticated
.\create-github-issues.ps1
```

This creates labels, issues (WF-001 through WF-060), and closes them as completed. The script outputs a `wf-to-github-issues.json` mapping file.

### 6. Create GitHub Release

1. Create a Git tag: `git tag v2.1.0 && git push origin v2.1.0`
2. Go to **GitHub > Releases > Draft a new release**
3. Select tag `v2.1.0`
4. Title: `WorkflowForge 2.1.0`
5. Body: Copy the `[2.1.0]` section from `CHANGELOG.md`
6. Attach the `.nupkg` files if publishing manually

## Packages to Publish (13 total)

| Package | Description |
|---------|-------------|
| WorkflowForge | Core library (zero dependencies) |
| WorkflowForge.Testing | Test utilities and helpers |
| WorkflowForge.Extensions.DependencyInjection | Microsoft.Extensions.DI integration |
| WorkflowForge.Extensions.Logging.Serilog | Serilog structured logging |
| WorkflowForge.Extensions.Resilience | Core retry/resilience abstractions |
| WorkflowForge.Extensions.Resilience.Polly | Polly-based circuit breakers and retries |
| WorkflowForge.Extensions.Observability.Performance | Performance metrics and profiling |
| WorkflowForge.Extensions.Observability.HealthChecks | Health check monitoring |
| WorkflowForge.Extensions.Observability.OpenTelemetry | Distributed tracing |
| WorkflowForge.Extensions.Persistence | Workflow state storage |
| WorkflowForge.Extensions.Persistence.Recovery | Resume interrupted workflows |
| WorkflowForge.Extensions.Validation | DataAnnotations validation |
| WorkflowForge.Extensions.Audit | Audit logging |

## Known .NET Framework 4.8 Limitations

The following extensions have reduced functionality on .NET Framework 4.8 (documented in `ISSUES.md`):

- **Validation Extension**: DataAnnotations behavior differences
- **HealthChecks Extension**: Limited `IHealthCheck` support

These are documented limitations and do not block release.

## Post-Release Verification

After publishing, verify:

### NuGet.org
1. Check all 13 packages appear at `https://www.nuget.org/profiles/AnimatLabs`
2. Verify package versions show `2.1.0`
3. Confirm `.snupkg` symbol packages are linked
4. Test installation: `dotnet add package WorkflowForge --version 2.1.0`
5. Verify SourceLink by stepping into WorkflowForge code in a debugger
6. Verify README renders correctly on each package page (logo, badges, content)

### Quality and CI
7. Verify [SonarCloud dashboard](https://sonarcloud.io/summary/new_code?id=animatlabs_workflow-forge) shows quality gate results
8. Verify SonarCloud badges render correctly on GitHub README
9. Verify GitHub Actions build badge shows passing status
