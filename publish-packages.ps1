param (
    [string]$CoreVersion = "1.0.0",
    [string]$SerilogVersion = "1.0.0",
    [string]$PollyVersion = "1.0.0",
    [string]$ResilienceVersion = "1.0.0",
    [string]$PerformanceVersion = "1.0.0",
    [string]$HealthChecksVersion = "1.0.0",
    [string]$OpenTelemetryVersion = "1.0.0",
    [string]$NuGetApiKey
)

# Ensure NuGet API key is provided
if (-not $NuGetApiKey) {
    Write-Error "NuGet API key is required. Pass it as the -NuGetApiKey parameter."
    exit 1
}

# Function to pack and publish a package
function PackAndPublish {
    param (
        [string]$ProjectPath,
        [string]$PackageName,
        [string]$PackageVersion
    )

    Write-Host "Processing $PackageName..." -ForegroundColor Cyan
    
    # Check if project file exists
    if (-not (Test-Path $ProjectPath)) {
        Write-Error "Project file not found: $ProjectPath"
        return $false
    }

    # Build the specific project first
    Write-Host "Building $PackageName..." -ForegroundColor Yellow
    try {
        dotnet build $ProjectPath --configuration Release --no-restore --verbosity quiet
        Write-Host "Build successful" -ForegroundColor Green
    } catch {
        Write-Error "Build failed for $PackageName"
        return $false
    }

    $OutputDir = "./nupkgs"
    Write-Host "Packing $PackageName version $PackageVersion..." -ForegroundColor Yellow
    
    try {
        # Pack without symbol packages to avoid .pdb issues
        dotnet pack $ProjectPath --configuration Release --no-build --output $OutputDir /p:Version=$PackageVersion /p:IncludeSymbols=false --verbosity quiet
        
        $PackagePath = Join-Path $OutputDir "$PackageName.$PackageVersion.nupkg"
        if (Test-Path $PackagePath) {
            $fileInfo = Get-Item $PackagePath
            Write-Host "Package created: $([math]::Round($fileInfo.Length / 1KB, 1)) KB" -ForegroundColor Green
            
            Write-Host "Publishing $PackageName to NuGet..." -ForegroundColor Yellow
            dotnet nuget push $PackagePath --api-key $NuGetApiKey --source https://api.nuget.org/v3/index.json --timeout 300
            Write-Host "$PackageName published successfully!" -ForegroundColor Green
            return $true
        } else {
            Write-Error "Package file not created: $PackagePath"
            return $false
        }
    } catch {
        Write-Error "Failed to pack/publish $PackageName : $_"
        return $false
    }
}

# Create output directory
$OutputDir = "./nupkgs"
if (-not (Test-Path $OutputDir)) {
    Write-Host "Creating output directory: $OutputDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

# Clean up old packages
Write-Host "Cleaning old packages..." -ForegroundColor Yellow
Get-ChildItem $OutputDir -Filter "*.nupkg" | Remove-Item -Force
Get-ChildItem $OutputDir -Filter "*.snupkg" | Remove-Item -Force

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
try {
    dotnet restore --verbosity quiet
    Write-Host "Dependencies restored" -ForegroundColor Green
} catch {
    Write-Error "Failed to restore dependencies"
    exit 1
}

# Build the entire solution first
Write-Host "Building solution..." -ForegroundColor Yellow
try {
    dotnet build --configuration Release --no-restore --verbosity quiet
    Write-Host "Solution built successfully" -ForegroundColor Green
} catch {
    Write-Warning "Solution build had issues, will try individual project builds"
}

Write-Host ""
Write-Host "Starting package publishing..." -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor DarkGray

# Track results
$results = @()

# Define packages with their details
$packages = @(
    @{ Path = "./src/core/WorkflowForge/WorkflowForge.csproj"; Name = "WorkflowForge"; Version = $CoreVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Logging.Serilog/WorkflowForge.Extensions.Logging.Serilog.csproj"; Name = "WorkflowForge.Extensions.Logging.Serilog"; Version = $SerilogVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Resilience.Polly/WorkflowForge.Extensions.Resilience.Polly.csproj"; Name = "WorkflowForge.Extensions.Resilience.Polly"; Version = $PollyVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Resilience/WorkflowForge.Extensions.Resilience.csproj"; Name = "WorkflowForge.Extensions.Resilience"; Version = $ResilienceVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Observability.Performance/WorkflowForge.Extensions.Observability.Performance.csproj"; Name = "WorkflowForge.Extensions.Observability.Performance"; Version = $PerformanceVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Observability.HealthChecks/WorkflowForge.Extensions.Observability.HealthChecks.csproj"; Name = "WorkflowForge.Extensions.Observability.HealthChecks"; Version = $HealthChecksVersion },
    @{ Path = "./src/extensions/WorkflowForge.Extensions.Observability.OpenTelemetry/WorkflowForge.Extensions.Observability.OpenTelemetry.csproj"; Name = "WorkflowForge.Extensions.Observability.OpenTelemetry"; Version = $OpenTelemetryVersion }
)

# Process each package
foreach ($package in $packages) {
    Write-Host ""
    $success = PackAndPublish -ProjectPath $package.Path -PackageName $package.Name -PackageVersion $package.Version
    $results += @{ Name = $package.Name; Version = $package.Version; Success = $success }
    Write-Host "-" * 60 -ForegroundColor DarkGray
}

# Final summary
Write-Host ""
Write-Host "PUBLISHING SUMMARY" -ForegroundColor Yellow
Write-Host "=" * 60 -ForegroundColor DarkGray

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