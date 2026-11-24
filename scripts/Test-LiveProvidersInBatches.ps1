<#
.SYNOPSIS
Runs real upstream provider checks in sequential batches and stops on the first failed batch.
#>
[CmdletBinding()]
param(
    [ValidateRange(1, 20)]
    [int]$BatchSize = 20,

    [ValidateRange(1, 200)]
    [int]$MaximumProviderCount = 67,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$solutionPath = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..\DotNetWebSearchApi.sln"))

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution file was not found: $solutionPath"
}

$environmentNames = @(
    "DOTNETWEBSEARCHAPI_LIVE_TESTS",
    "DOTNETWEBSEARCHAPI_LIVE_PROVIDER_OFFSET",
    "DOTNETWEBSEARCHAPI_LIVE_PROVIDER_COUNT"
)
$previousValues = @{}
foreach ($name in $environmentNames) {
    $previousValues[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

try {
    $env:DOTNETWEBSEARCHAPI_LIVE_TESTS = "1"
    $env:DOTNETWEBSEARCHAPI_LIVE_PROVIDER_COUNT = [string]$BatchSize

    Write-Host "Building the solution ($Configuration)..."
    & dotnet build $solutionPath -c $Configuration --no-incremental
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed with exit code $LASTEXITCODE. No live provider batches were started."
    }

    for ($offset = 0; $offset -lt $MaximumProviderCount; $offset += $BatchSize) {
        $batchNumber = [int][Math]::Floor($offset / $BatchSize) + 1
        $firstProvider = $offset + 1
        $lastProvider = [Math]::Min($offset + $BatchSize, $MaximumProviderCount)
        $env:DOTNETWEBSEARCHAPI_LIVE_PROVIDER_OFFSET = [string]$offset

        Write-Host ""
        Write-Host "Live provider batch $batchNumber (catalogue positions $firstProvider-$lastProvider)..."

        & dotnet test $solutionPath -c $Configuration --no-build --no-restore --filter "FullyQualifiedName~AllRegisteredProviders_RespondToLiveRequests" --logger "console;verbosity=normal"

        if ($LASTEXITCODE -ne 0) {
            throw "Live provider batch $batchNumber failed. Stopping; later batches were not run."
        }
    }

    Write-Host ""
    Write-Host "All configured-provider batches completed successfully."
    Write-Host "Providers requiring API keys are tested only when their keys are configured locally."
}
finally {
    foreach ($name in $environmentNames) {
        $previousValue = $previousValues[$name]
        if ($null -eq $previousValue) {
            Remove-Item -Path "Env:$name" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -Path "Env:$name" -Value $previousValue
        }
    }
}
