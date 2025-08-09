param (
    [string]$CoreVersion = "1.1.0",
    [string]$SerilogVersion = "1.1.0",
    [string]$PollyVersion = "1.1.0",
    [string]$ResilienceVersion = "1.1.0",
    [string]$PerformanceVersion = "1.1.0",
    [string]$HealthChecksVersion = "1.1.0",
    [string]$OpenTelemetryVersion = "1.1.0",
    [string]$PersistenceVersion = "1.0.0",
    [string]$RecoveryVersion = "1.0.0",
    [string]$NuGetApiKey,
    [switch]$Publish
)

if ($Publish -and -not $NuGetApiKey) {
    Write-Error "NuGet API key is required when -Publish is specified. Pass it as the -NuGetApiKey parameter."
    exit 1
}

function PackAndPublish {
    param (
        [string]$ProjectPath,
        [string]$PackageName,
        [string]$PackageVersion
    )

    Write-Host "Processing $PackageName..." -ForegroundColor Cyan
    
    if (-not (Test-Path $ProjectPath)) {
        Write-Error "Project file not found: $ProjectPath"
        return $false
    }

    Write-Host "Building $PackageName..." -ForegroundColor Yellow
    try {
        dotnet build $ProjectPath --configuration Release --no-restore --verbosity quiet
        Write-Host "Build successful" -ForegroundColor Green
    }
    catch {
        Write-Error "Build failed for $PackageName"
        return $false
    }

    $OutputDir = "./nupkgs"
    Write-Host "Packing $PackageName version $PackageVersion..." -ForegroundColor Yellow
    
    try {
        dotnet pack $ProjectPath --configuration Release --no-build --output $OutputDir /p:Version=$PackageVersion --verbosity normal
        
        $PackagePath = Join-Path $OutputDir "$PackageName.$PackageVersion.nupkg"
        if (Test-Path $PackagePath) {
            $fileInfo = Get-Item $PackagePath
            Write-Host "Package created: $([math]::Round($fileInfo.Length / 1KB, 1)) KB" -ForegroundColor Green
            
            Write-Host "Verifying package contents..." -ForegroundColor Yellow
            try {
                $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
                New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
                
                Add-Type -AssemblyName System.IO.Compression.FileSystem
                [System.IO.Compression.ZipFile]::ExtractToDirectory($PackagePath, $tempDir)
                
                $hasReadme = Test-Path (Join-Path $tempDir "README.md")
                $hasIcon = Test-Path (Join-Path $tempDir "icon.png")
                
                if ($hasReadme -and $hasIcon) {
                    Write-Host "✓ Package includes README.md and icon.png" -ForegroundColor Green
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
                dotnet nuget push $PackagePath --api-key $NuGetApiKey --source https://api.nuget.org/v3/index.json --timeout 300
                Write-Host "$PackageName published successfully!" -ForegroundColor Green
            } else {
                Write-Host "Pack-only mode: Skipping publish for $PackageName" -ForegroundColor Yellow
            }
            return $true
        } else {
            Write-Error "Package file not created: $PackagePath"
            return $false
        }
    }
    catch {
        Write-Error "Failed to pack/publish $PackageName : $_"
        return $false
    }
}

$OutputDir = "./nupkgs"
if (-not (Test-Path $OutputDir)) {
    Write-Host "Creating output directory: $OutputDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

Write-Host "Cleaning old packages..." -ForegroundColor Yellow
Get-ChildItem $OutputDir -Filter "*.nupkg" | Remove-Item -Force
Get-ChildItem $OutputDir -Filter "*.snupkg" | Remove-Item -Force

Write-Host "Restoring dependencies..." -ForegroundColor Yellow
try {
    dotnet restore --verbosity quiet
    Write-Host "Dependencies restored" -ForegroundColor Green
}
catch {
    Write-Error "Failed to restore dependencies"
    exit 1
}

Write-Host "Building solution..." -ForegroundColor Yellow
try {
    dotnet build --configuration Release --no-restore --verbosity quiet
    Write-Host "Solution built successfully" -ForegroundColor Green
}
catch {
    Write-Warning "Solution build had issues, will try individual project builds"
}

Write-Host ""
Write-Host ("Starting package {0}..." -f ($Publish ? "publishing" : "packing")) -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor DarkGray

$results = @()

$packages = @(
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj"; Name = "WorkflowForge.Extensions.Logging.Serilog"; Version = $SerilogVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Resilience/WorkflowForge.Extensions.Resilience.csproj"; Name = "WorkflowForge.Extensions.Resilience"; Version = $ResilienceVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Resilience.Polly/WorkflowForge.Extensions.Resilience.Polly.csproj"; Name = "WorkflowForge.Extensions.Resilience.Polly"; Version = $PollyVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Observability.Performance/WorkflowForge.Extensions.Observability.Performance.csproj"; Name = "WorkflowForge.Extensions.Observability.Performance"; Version = $PerformanceVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/WorkflowForge.Extensions.Observability.HealthChecks.csproj"; Name = "WorkflowForge.Extensions.Observability.HealthChecks"; Version = $HealthChecksVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/WorkflowForge.Extensions.Observability.OpenTelemetry.csproj"; Name = "WorkflowForge.Extensions.Observability.OpenTelemetry"; Version = $OpenTelemetryVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Persistence/WorkflowForge.Extensions.Persistence.csproj"; Name = "WorkflowForge.Extensions.Persistence"; Version = $PersistenceVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Persistence.Recovery/WorkflowForge.Extensions.Persistence.Recovery.csproj"; Name = "WorkflowForge.Extensions.Persistence.Recovery"; Version = $RecoveryVersion }
)

foreach ($package in $packages) {
    Write-Host ""
    $success = PackAndPublish -ProjectPath $package.Path -PackageName $package.Name -PackageVersion $package.Version
    $results += @{ Name = $package.Name; Version = $package.Version; Success = $success }
    Write-Host "------------------------------------------------------------" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "Processing WorkflowForge Core (dependency package)..." -ForegroundColor Cyan
$corePackagePath = Join-Path $OutputDir "WorkflowForge.$CoreVersion.nupkg"
if (Test-Path $corePackagePath) {
    Write-Host "Core package already created as dependency" -ForegroundColor Green
    
    Write-Host "Verifying core package contents..." -ForegroundColor Yellow
    try {
        $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory($corePackagePath, $tempDir)
        
        $hasReadme = Test-Path (Join-Path $tempDir "README.md")
        $hasIcon = Test-Path (Join-Path $tempDir "icon.png")
        
        if ($hasReadme -and $hasIcon) {
            Write-Host "✓ Core package includes README.md and icon.png" -ForegroundColor Green
        } else {
            Write-Warning "Core package missing assets - README: $hasReadme, Icon: $hasIcon"
        }
        
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    catch {
        Write-Warning "Could not verify core package contents: $_"
    }
    
    if ($Publish) {
        Write-Host "Publishing WorkflowForge Core to NuGet..." -ForegroundColor Yellow
        try {
            dotnet nuget push $corePackagePath --api-key $NuGetApiKey --source https://api.nuget.org/v3/index.json --timeout 300
            Write-Host "WorkflowForge Core published successfully!" -ForegroundColor Green
            $results += @{ Name = "WorkflowForge"; Version = $CoreVersion; Success = $true }
        }
        catch {
            Write-Error "Failed to publish WorkflowForge Core: $_"
            $results += @{ Name = "WorkflowForge"; Version = $CoreVersion; Success = $false }
        }
    } else {
        Write-Host "Pack-only mode: Skipping publish for WorkflowForge Core" -ForegroundColor Yellow
        $results += @{ Name = "WorkflowForge"; Version = $CoreVersion; Success = $true }
    }
} else {
    Write-Warning "Core package not found, attempting to build it directly..."
    $success = PackAndPublish -ProjectPath "./src/core/WorkflowForge/WorkflowForge.csproj" -PackageName "WorkflowForge" -PackageVersion $CoreVersion
    $results += @{ Name = "WorkflowForge"; Version = $CoreVersion; Success = $success }
}
Write-Host "------------------------------------------------------------" -ForegroundColor DarkGray

Write-Host ""
Write-Host (("{0} SUMMARY" -f ($Publish ? "PUBLISHING" : "PACKING"))) -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor DarkGray

$successful = ($results | Where-Object { $_.Success }).Count
$total = $results.Count

foreach ($result in $results) {
    $status = if ($result.Success) { "[SUCCESS]" } else { "[FAILED]" }
    $color = if ($result.Success) { "Green" } else { "Red" }
    Write-Host "$status $($result.Name) v$($result.Version)" -ForegroundColor $color
}

Write-Host ""
if ($successful -eq $total) {
    Write-Host "All $total packages published successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Published packages:" -ForegroundColor Yellow
    foreach ($result in $results | Where-Object { $_.Success }) {
        Write-Host "  - $($result.Name) v$($result.Version)" -ForegroundColor White
    }
} else {
    Write-Host "$successful of $total packages published successfully" -ForegroundColor Yellow
    if ($successful -lt $total) {
        Write-Host "$($total - $successful) packages failed to publish" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "View your packages at: https://www.nuget.org/profiles/AnimatLabs" -ForegroundColor Cyan
