param (
    [string]$CoreVersion = "2.0.0",
    [string]$TestingVersion = "2.0.0",
    [string]$DependencyInjectionVersion = "2.0.0",
    [string]$SerilogVersion = "2.0.0",
    [string]$PollyVersion = "2.0.0",
    [string]$ResilienceVersion = "2.0.0",
    [string]$PerformanceVersion = "2.0.0",
    [string]$HealthChecksVersion = "2.0.0",
    [string]$OpenTelemetryVersion = "2.0.0",
    [string]$PersistenceVersion = "2.0.0",
    [string]$RecoveryVersion = "2.0.0",
    [string]$ValidationVersion = "2.0.0",
    [string]$AuditVersion = "2.0.0",
    [string]$NuGetApiKey,
    [switch]$Publish
)

if ($Publish -and -not $NuGetApiKey) {
    Write-Error "NuGet API key is required when -Publish is specified. Pass it as the -NuGetApiKey parameter."
    exit 1
}

# Use absolute path for output directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $ScriptDir "nupkgs"

function PackAndPublish {
    param (
        [string]$ProjectPath,
        [string]$PackageName,
        [string]$PackageVersion,
        [string]$OutputDirectory
    )

    Write-Host "Processing $PackageName..." -ForegroundColor Cyan
    
    # Resolve to absolute path
    $AbsProjectPath = Join-Path $ScriptDir $ProjectPath
    if (-not (Test-Path $AbsProjectPath)) {
        Write-Host "ERROR: Project file not found: $AbsProjectPath" -ForegroundColor Red
        return $false
    }

    Write-Host "Building $PackageName..." -ForegroundColor Yellow
    $buildResult = dotnet build $AbsProjectPath --configuration Release --no-restore --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed for $PackageName" -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Red
        return $false
    }
    Write-Host "Build successful" -ForegroundColor Green

    Write-Host "Packing $PackageName version $PackageVersion..." -ForegroundColor Yellow
    
    $packResult = dotnet pack $AbsProjectPath --configuration Release --no-build --output $OutputDirectory /p:Version=$PackageVersion --verbosity normal 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Pack failed for $PackageName" -ForegroundColor Red
        Write-Host $packResult -ForegroundColor Red
        return $false
    }
    
    $PackagePath = Join-Path $OutputDirectory "$PackageName.$PackageVersion.nupkg"
    if (Test-Path $PackagePath) {
        $fileInfo = Get-Item $PackagePath
        Write-Host "Package created: $([math]::Round($fileInfo.Length / 1KB, 1)) KB" -ForegroundColor Green
        
        # Verify package contents
        Write-Host "Verifying package contents..." -ForegroundColor Yellow
        try {
            $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
            New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
            
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            # Use the full resolved path for extraction
            $fullPackagePath = (Resolve-Path $PackagePath).Path
            [System.IO.Compression.ZipFile]::ExtractToDirectory($fullPackagePath, $tempDir)
            
            $hasReadme = Test-Path (Join-Path $tempDir "README.md")
            $hasIcon = Test-Path (Join-Path $tempDir "icon.png")
            
            if ($hasReadme -and $hasIcon) {
                Write-Host "Package includes README.md and icon.png" -ForegroundColor Green
            } else {
                Write-Warning "Package missing assets - README: $hasReadme, Icon: $hasIcon"
            }
            
            Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        catch {
            Write-Warning "Could not verify package contents: $_"
        }
        
        if ($Publish) {
            Write-Host "Publishing $PackageName to NuGet..." -ForegroundColor Yellow
            $pushResult = dotnet nuget push $PackagePath --api-key $NuGetApiKey --source https://api.nuget.org/v3/index.json --timeout 300 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Host "ERROR: Failed to publish $PackageName" -ForegroundColor Red
                Write-Host $pushResult -ForegroundColor Red
                return $false
            }
            Write-Host "$PackageName published successfully!" -ForegroundColor Green
        } else {
            Write-Host "Pack-only mode: Skipping publish for $PackageName" -ForegroundColor Yellow
        }
        return $true
    } else {
        Write-Host "ERROR: Package file not created: $PackagePath" -ForegroundColor Red
        # List what was created in the output directory
        Write-Host "Files in output directory:" -ForegroundColor Yellow
        Get-ChildItem $OutputDirectory -Filter "*.nupkg" | ForEach-Object { Write-Host "  - $($_.Name)" }
        return $false
    }
}

# Create output directory if needed
if (-not (Test-Path $OutputDir)) {
    Write-Host "Creating output directory: $OutputDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

Write-Host "Cleaning old packages..." -ForegroundColor Yellow
Get-ChildItem $OutputDir -Filter "*.nupkg" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem $OutputDir -Filter "*.snupkg" -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "Restoring dependencies..." -ForegroundColor Yellow
Push-Location $ScriptDir
try {
    dotnet restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to restore dependencies" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host "Dependencies restored" -ForegroundColor Green
}
catch {
    Write-Host "ERROR: Failed to restore dependencies: $_" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "Solution built successfully" -ForegroundColor Green
} else {
    Write-Warning "Solution build had issues, will try individual project builds"
}
Pop-Location

Write-Host ""
$mode = if ($Publish) { "publishing" } else { "packing" }
Write-Host ("Starting package {0}..." -f $mode) -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor DarkGray

$results = @()

$packages = @(
    # Core packages (must be built first as dependencies)
    @{ Path = "src/core/WorkflowForge/WorkflowForge.csproj"; Name = "WorkflowForge"; Version = $CoreVersion },
    @{ Path = "src/core/WorkflowForge.Testing/WorkflowForge.Testing.csproj"; Name = "WorkflowForge.Testing"; Version = $TestingVersion },
    
    # Extension packages
    @{ Path = "src/extensions/WorkflowForge.Extensions.DependencyInjection/WorkflowForge.Extensions.DependencyInjection.csproj"; Name = "WorkflowForge.Extensions.DependencyInjection"; Version = $DependencyInjectionVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Validation/WorkflowForge.Extensions.Validation.csproj"; Name = "WorkflowForge.Extensions.Validation"; Version = $ValidationVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Audit/WorkflowForge.Extensions.Audit.csproj"; Name = "WorkflowForge.Extensions.Audit"; Version = $AuditVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj"; Name = "WorkflowForge.Extensions.Logging.Serilog"; Version = $SerilogVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Resilience/WorkflowForge.Extensions.Resilience.csproj"; Name = "WorkflowForge.Extensions.Resilience"; Version = $ResilienceVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Resilience.Polly/WorkflowForge.Extensions.Resilience.Polly.csproj"; Name = "WorkflowForge.Extensions.Resilience.Polly"; Version = $PollyVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Observability.Performance/WorkflowForge.Extensions.Observability.Performance.csproj"; Name = "WorkflowForge.Extensions.Observability.Performance"; Version = $PerformanceVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/WorkflowForge.Extensions.Observability.HealthChecks.csproj"; Name = "WorkflowForge.Extensions.Observability.HealthChecks"; Version = $HealthChecksVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/WorkflowForge.Extensions.Observability.OpenTelemetry.csproj"; Name = "WorkflowForge.Extensions.Observability.OpenTelemetry"; Version = $OpenTelemetryVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Persistence/WorkflowForge.Extensions.Persistence.csproj"; Name = "WorkflowForge.Extensions.Persistence"; Version = $PersistenceVersion },
    @{ Path = "src/extensions/WorkflowForge.Extensions.Persistence.Recovery/WorkflowForge.Extensions.Persistence.Recovery.csproj"; Name = "WorkflowForge.Extensions.Persistence.Recovery"; Version = $RecoveryVersion }
)

foreach ($package in $packages) {
    Write-Host ""
    $success = PackAndPublish -ProjectPath $package.Path -PackageName $package.Name -PackageVersion $package.Version -OutputDirectory $OutputDir
    $results += @{ Name = $package.Name; Version = $package.Version; Success = $success }
    Write-Host "------------------------------------------------------------" -ForegroundColor DarkGray
}

Write-Host ""
$summary = if ($Publish) { "PUBLISHING" } else { "PACKING" }
Write-Host ("{0} SUMMARY" -f $summary) -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor DarkGray

$successful = ($results | Where-Object { $_.Success -eq $true }).Count
$total = $results.Count

foreach ($result in $results) {
    $status = if ($result.Success -eq $true) { "[SUCCESS]" } else { "[FAILED]" }
    $color = if ($result.Success -eq $true) { "Green" } else { "Red" }
    Write-Host "$status $($result.Name) v$($result.Version)" -ForegroundColor $color
}

Write-Host ""
if ($successful -eq $total) {
    Write-Host "All $total packages completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Packages:" -ForegroundColor Yellow
    foreach ($result in $results | Where-Object { $_.Success -eq $true }) {
        Write-Host "  - $($result.Name) v$($result.Version)" -ForegroundColor White
    }
} else {
    Write-Host "$successful of $total packages completed successfully" -ForegroundColor Yellow
    if ($successful -lt $total) {
        Write-Host "$($total - $successful) packages failed" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Output directory: $OutputDir" -ForegroundColor Cyan
Write-Host "View your packages at: https://www.nuget.org/profiles/AnimatLabs" -ForegroundColor Cyan
