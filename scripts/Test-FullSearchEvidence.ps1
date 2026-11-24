<#
.SYNOPSIS
Builds, regression-tests and live-tests the complete search service, preserving machine-readable evidence.
.DESCRIPTION
Runs the offline suite, every locally registered live provider in batches of up to 20 (continuing
after failed batches), and one end-to-end aggregate API search. Writes JSON evidence and a full log.
API keys are read only from the caller's existing environment; no secrets are written to evidence.
#>
[CmdletBinding()]
param(
    [ValidateRange(1, 20)]
    [int]$BatchSize = 20,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$EvidenceDirectory = (Join-Path $PSScriptRoot "..\evidence"),

    [switch]$PublishEvidence
)

$ErrorActionPreference = "Stop"
$solutionPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\DotNetWebSearchApi.sln"))
if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution file was not found: $solutionPath"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$evidencePath = [System.IO.Path]::GetFullPath($EvidenceDirectory)
[System.IO.Directory]::CreateDirectory($evidencePath) | Out-Null
$coverageResultsDirectory = Join-Path $evidencePath "coverage"
[System.IO.Directory]::CreateDirectory($coverageResultsDirectory) | Out-Null
$jsonPath = Join-Path $evidencePath "search-assessment-$timestamp.json"
$logPath = Join-Path $evidencePath "search-assessment-$timestamp.log"
$providerRuns = [System.Collections.Generic.List[object]]::new()
$providerOutcomeKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$stages = [System.Collections.Generic.List[object]]::new()
$logLines = [System.Collections.Generic.List[string]]::new()
$environmentNames = @(
    "DOTNETWEBSEARCHAPI_LIVE_TESTS",
    "DOTNETWEBSEARCHAPI_LIVE_PROVIDER_OFFSET",
    "DOTNETWEBSEARCHAPI_LIVE_PROVIDER_COUNT"
)
$previousValues = @{}
foreach ($name in $environmentNames) {
    $previousValues[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

function Invoke-DotNetStage {
    param(
        [string]$Name,
        [string[]]$Arguments
    )

    Write-Host ""
    Write-Host "=== $Name ==="
    $logLines.Add("=== $Name ===")
    $output = @()
    $exitCode = 1
    $previousPreference = $ErrorActionPreference
    try {
        # A failing test process is evidence, not a PowerShell control-flow failure.
        # Keep stderr captured so the batch output and xUnit diagnostics are still parsed.
        $ErrorActionPreference = "Continue"
        $output = @(& dotnet @Arguments 2>&1)
        if ($null -ne $LASTEXITCODE) {
            $exitCode = $LASTEXITCODE
        }
    }
    catch {
        $output += $_.ToString()
        $exitCode = 1
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    foreach ($line in $output) {
        $textLine = [string]$line
        Write-Host $textLine
        $logLines.Add($textLine)
    }
    $stage = [pscustomobject]@{
        name = $Name
        exitCode = $exitCode
        passed = ($exitCode -eq 0)
    }
    $stages.Add($stage)
    return $stage
}

$buildPassed = $false
$regressionPassed = $false
$coverageSummary = $null
$apiPassed = $false
$registeredProviderCount = $null
$expectedProviderCount = $null
$batchOffset = 0
$batchNumber = 0
$overallError = $null
$publishError = $null
$apiEvidenceLines = @()

try {
    $build = Invoke-DotNetStage "Release build" @("build", $solutionPath, "-c", $Configuration, "--no-incremental")
    $buildPassed = $build.passed
    if (-not $buildPassed) {
        throw "Build failed; tests cannot provide reliable evidence."
    }

    $env:DOTNETWEBSEARCHAPI_LIVE_TESTS = "0"
    $regression = Invoke-DotNetStage "Offline regression suite" @(
        "test", $solutionPath, "-c", $Configuration, "--no-build", "--no-restore",
        "--logger", "console;verbosity=minimal", "--collect", "XPlat Code Coverage", "--results-directory", $coverageResultsDirectory
    )
    $regressionPassed = $regression.passed

    $coverageReports = @(Get-ChildItem -Path $coverageResultsDirectory -Filter "coverage.cobertura.xml" -File -Recurse -ErrorAction SilentlyContinue)
    if ($coverageReports.Count -eq 0) {
        throw "The offline test run did not produce a Cobertura coverage report."
    }
    $latestCoverage = $coverageReports | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    [xml]$coverageDocument = Get-Content -LiteralPath $latestCoverage.FullName -Raw
    $coverageRoot = $coverageDocument.coverage
    $coverageSummary = [ordered]@{
        reportFile = $latestCoverage.FullName.Substring($evidencePath.Length).TrimStart([char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)).Replace([System.IO.Path]::DirectorySeparatorChar, '/')
        lineCoveragePercent = [Math]::Round(([double]::Parse([string]$coverageRoot.'line-rate', [System.Globalization.CultureInfo]::InvariantCulture) * 100), 2)
        branchCoveragePercent = [Math]::Round(([double]::Parse([string]$coverageRoot.'branch-rate', [System.Globalization.CultureInfo]::InvariantCulture) * 100), 2)
        linesCovered = [int]$coverageRoot.'lines-covered'
        linesValid = [int]$coverageRoot.'lines-valid'
        branchesCovered = [int]$coverageRoot.'branches-covered'
        branchesValid = [int]$coverageRoot.'branches-valid'
    }
    Write-Host ("Coverage: lines {0}% ({1}/{2}); branches {3}% ({4}/{5})." -f $coverageSummary.lineCoveragePercent, $coverageSummary.linesCovered, $coverageSummary.linesValid, $coverageSummary.branchCoveragePercent, $coverageSummary.branchesCovered, $coverageSummary.branchesValid)
    $logLines.Add(("Coverage: lines {0}%; branches {1}%." -f $coverageSummary.lineCoveragePercent, $coverageSummary.branchCoveragePercent))

    $env:DOTNETWEBSEARCHAPI_LIVE_TESTS = "1"
    $env:DOTNETWEBSEARCHAPI_LIVE_PROVIDER_COUNT = [string]$BatchSize

    while ($true) {
        $batchNumber++
        $env:DOTNETWEBSEARCHAPI_LIVE_PROVIDER_OFFSET = [string]$batchOffset
        $lineStart = $logLines.Count
        $batch = Invoke-DotNetStage "Live provider batch $batchNumber (offset $batchOffset)" @(
            "test", $solutionPath, "-c", $Configuration, "--no-build", "--no-restore",
            "--filter", "FullyQualifiedName~AllRegisteredProviders_RespondToLiveRequests",
            "--logger", "console;verbosity=detailed"
        )

        $batchText = ($logLines | Select-Object -Skip $lineStart) -join "`n"
        $inventoryMatch = [regex]::Match($batchText, "Provider inventory: (\d+) registered; batch offset (\d+); selected (\d+)\.")
        if (-not $inventoryMatch.Success) {
            throw "Could not read provider inventory from batch $batchNumber output. See the log."
        }

        $registeredProviderCount = [int]$inventoryMatch.Groups[1].Value
        $selectedCount = [int]$inventoryMatch.Groups[3].Value
        if ($registeredProviderCount -lt 1) {
            throw "The application registered no live search providers."
        }
        if ($null -eq $expectedProviderCount) {
            $expectedProviderCount = $registeredProviderCount
        }
        elseif ($registeredProviderCount -ne $expectedProviderCount) {
            throw "Provider inventory changed during the live assessment."
        }
        $expectedSelectedCount = [Math]::Min($BatchSize, [Math]::Max(0, $expectedProviderCount - $batchOffset))
        if ($selectedCount -ne $expectedSelectedCount) {
            throw "Batch $batchNumber selected $selectedCount providers; expected $expectedSelectedCount."
        }
        $providerLines = $batchText -split "`r?`n"
        $batchOutcomeCount = 0
        foreach ($providerLine in $providerLines) {
            $passMatch = [regex]::Match($providerLine, "PASS ([^:]+): (\d+) results\.")
            if ($passMatch.Success) {
                $providerName = $passMatch.Groups[1].Value.Trim()
                $outcomeKey = "$batchNumber|$providerName"
                if ($providerOutcomeKeys.Add($outcomeKey)) {
                    $providerRuns.Add([pscustomobject]@{
                        batch = $batchNumber
                        offset = $batchOffset
                        provider = $providerName
                        status = "passed"
                        resultCount = [int]$passMatch.Groups[2].Value
                        detail = $providerLine.Trim()
                    })
                    $batchOutcomeCount++
                }
                continue
            }

            $emptyMatch = [regex]::Match($providerLine, "EMPTY ([^:]+): (.*)$")
            if ($emptyMatch.Success) {
                $providerName = $emptyMatch.Groups[1].Value.Trim()
                $outcomeKey = "$batchNumber|$providerName"
                if ($providerOutcomeKeys.Add($outcomeKey)) {
                    $providerRuns.Add([pscustomobject]@{
                        batch = $batchNumber
                        offset = $batchOffset
                        provider = $providerName
                        status = "empty"
                        resultCount = 0
                        detail = $emptyMatch.Groups[2].Value.Trim()
                    })
                    $batchOutcomeCount++
                }
                continue
            }

            $failMatch = [regex]::Match($providerLine, "FAIL ([^:]+): (.*)$")
            if ($failMatch.Success) {
                $providerName = $failMatch.Groups[1].Value.Trim()
                $outcomeKey = "$batchNumber|$providerName"
                if ($providerOutcomeKeys.Add($outcomeKey)) {
                    $providerRuns.Add([pscustomobject]@{
                        batch = $batchNumber
                        offset = $batchOffset
                        provider = $providerName
                        status = "failed"
                        resultCount = 0
                        detail = $failMatch.Groups[2].Value.Trim()
                    })
                    $batchOutcomeCount++
                }
            }
        }

        if ($batchOutcomeCount -ne $selectedCount) {
            throw "Batch $batchNumber captured $batchOutcomeCount distinct provider outcomes; expected $selectedCount."
        }

        Write-Host "Batch $batchNumber exit code: $($batch.exitCode); providers selected: $selectedCount."
        if (($batchOffset + $selectedCount) -ge $registeredProviderCount) {
            break
        }

        $batchOffset += $BatchSize
        Start-Sleep -Seconds 3
    }

    $apiLineStart = $logLines.Count
    $apiTest = Invoke-DotNetStage "End-to-end aggregate API and compatibility search" @(
        "test", $solutionPath, "-c", $Configuration, "--no-build", "--no-restore",
        "--filter", "FullyQualifiedName~FullSearchApi_ReturnsAggregatedLiveResultsAndCompatibilityJson",
        "--logger", "console;verbosity=detailed"
    )
    $apiPassed = $apiTest.passed
    $apiEvidenceLines = @($logLines | Select-Object -Skip $apiLineStart)
}
catch {
    $overallError = $_.Exception.Message
    Write-Host "ERROR: $overallError"
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

    $passedProviders = @($providerRuns | Where-Object status -eq "passed").Count
    $emptyProviders = @($providerRuns | Where-Object status -eq "empty").Count
    $failedProviders = @($providerRuns | Where-Object status -eq "failed").Count
    $providerChecksResponded = $passedProviders + $emptyProviders
    $providerBatchStagesPassed = @($stages | Where-Object { $_.name -like "Live provider batch *" -and -not $_.passed }).Count -eq 0
    $evidence = [ordered]@{
        generatedAtUtc = [DateTime]::UtcNow.ToString("o")
        machine = [Environment]::MachineName
        operatingSystem = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        dotnetSdk = (& dotnet --version 2>$null | Select-Object -First 1)
        configuration = $Configuration
        query = "dotnet"
        batchSize = $BatchSize
        registeredProviderCount = $registeredProviderCount
        summary = [ordered]@{
            buildPassed = $buildPassed
            regressionSuitePassed = $regressionPassed
            providerChecksPassed = $passedProviders
            providerChecksEmpty = $emptyProviders
            providerChecksResponded = $providerChecksResponded
            providerChecksFailed = $failedProviders
            providerBatchStagesPassed = $providerBatchStagesPassed
            aggregateApiPassed = $apiPassed
            coverageReportGenerated = ($null -ne $coverageSummary)
            overallPassed = ($buildPassed -and $regressionPassed -and $null -ne $coverageSummary -and $providerBatchStagesPassed -and $failedProviders -eq 0 -and $apiPassed -and $null -eq $overallError)
        }
        stages = @($stages)
        providers = @($providerRuns)
        coverage = $coverageSummary
        aggregateApiEvidence = @($apiEvidenceLines)
        error = $overallError
        logFile = [System.IO.Path]::GetFileName($logPath)
    }
    $evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
    $logLines | Set-Content -LiteralPath $logPath -Encoding utf8
    Write-Host ""
    Write-Host "Evidence JSON: $jsonPath"
    Write-Host "Full execution log: $logPath"
    Write-Host "Provider checks: $passedProviders returned results; $emptyProviders responded empty; $failedProviders failed."

    if ($PublishEvidence) {
        try {
            $repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
            $publisherPath = Join-Path $PSScriptRoot "Publish-SearchAssessmentEvidence.ps1"
            $publishedCommit = & $publisherPath -RepositoryRoot $repositoryRoot -JsonPath $jsonPath -LogPath $logPath -RunTimestamp $timestamp
            Write-Host "Evidence published to origin/main in commit $publishedCommit."
        }
        catch {
            $publishError = $_.Exception.Message
            Write-Host "ERROR publishing evidence: $publishError"
        }
    }
}

if (-not $buildPassed -or -not $regressionPassed -or $null -eq $coverageSummary -or -not $providerBatchStagesPassed -or $failedProviders -gt 0 -or -not $apiPassed -or $null -ne $overallError -or $null -ne $publishError) {
    exit 1
}
