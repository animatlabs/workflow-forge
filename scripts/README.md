# WorkflowForge Scripts

## Package Publishing

### Cross-Platform (Recommended): `publish-packages.py`

Python 3.6+ script for building, packing, signing, and publishing NuGet packages.

**Usage:**

```bash
# Dry run (build + pack only)
python scripts/publish-packages.py --version 2.1.0

# Sign packages
python scripts/publish-packages.py --version 2.1.0 --sign --cert-path path/to/cert.pfx --cert-password PASSWORD

# Publish to NuGet
python scripts/publish-packages.py --version 2.1.0 --publish --api-key YOUR_NUGET_API_KEY

# Full pipeline: sign + publish
python scripts/publish-packages.py --version 2.1.0 --sign --cert-path cert.pfx --cert-password PWD --publish --api-key KEY
```

### Legacy PowerShell: `publish-packages.legacy.ps1`

Original PowerShell script (Windows only). Kept for reference. Use the Python script instead.

---

## Strong Name Key (SNK) Setup

### Step 1: Generate the SNK File

```bash
# Generate a new strong name key pair
sn -k WorkflowForge.snk

# Extract the public key
sn -p WorkflowForge.snk WorkflowForge.pub

# Display the public key token (needed for InternalsVisibleTo)
sn -tp WorkflowForge.pub
```

### Step 2: Configure Assembly Signing

Edit `src/Directory.Build.props` and uncomment the signing section:

```xml
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)..\WorkflowForge.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>
```

### Step 3: Update InternalsVisibleTo

In `src/core/WorkflowForge/WorkflowForge.csproj`, update the `InternalsVisibleTo` entries with the public key:

```xml
<InternalsVisibleTo Include="WorkflowForge.Tests, PublicKey=YOUR_PUBLIC_KEY_HERE" />
```

### Step 4: NuGet Package Signing

```bash
# Sign all packages (both .nupkg and .snupkg)
dotnet nuget sign ./packages/*.nupkg --certificate-path cert.pfx --timestamper http://timestamp.digicert.com
dotnet nuget sign ./packages/*.snupkg --certificate-path cert.pfx --timestamper http://timestamp.digicert.com
```

---

## GitHub Actions Secrets

To publish packages from GitHub Actions, configure these repository secrets:

| Secret Name | Description | How to Get |
|---|---|---|
| `NUGET_API_KEY` | NuGet.org API key | NuGet.org > Account > API Keys > Create |
| `SIGNING_CERT_BASE64` | Base64-encoded .pfx certificate | `base64 -i cert.pfx` (macOS/Linux) or `[Convert]::ToBase64String([IO.File]::ReadAllBytes("cert.pfx"))` (PowerShell) |
| `SIGNING_CERT_PASSWORD` | Certificate password | Your certificate password |

**To add secrets:**
1. Navigate to your GitHub repository
2. Go to **Settings** > **Secrets and variables** > **Actions**
3. Click **New repository secret**
4. Add each secret listed above

---

## Microsoft.SourceLink.GitHub

SourceLink is configured in `src/Directory.Build.props` and enables source-level debugging for NuGet consumers. When someone steps into WorkflowForge code in the debugger, Visual Studio automatically fetches the correct source from GitHub. No additional setup is needed.
