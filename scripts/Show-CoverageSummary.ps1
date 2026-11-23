param(
    [string]$ResultsPath = (Join-Path $PSScriptRoot "..\tests\DotNetWebSearchApi.Tests\TestResults")
)

$reports = @(Get-ChildItem -Path $ResultsPath -Filter "coverage.cobertura.xml" -File -Recurse -ErrorAction SilentlyContinue)
if ($reports.Count -eq 0) {
    throw "No Cobertura report found under '$ResultsPath'. Run dotnet test -c Release with the XPlat Code Coverage collector first."
}

$latest = $reports | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
[xml]$coverage = Get-Content -LiteralPath $latest.FullName -Raw
$root = $coverage.coverage

function Convert-RateToPercent([string]$rate) {
    if ([string]::IsNullOrWhiteSpace($rate)) { return 0.0 }
    return [Math]::Round(([double]::Parse($rate, [Globalization.CultureInfo]::InvariantCulture) * 100), 2)
}

$linePercent = Convert-RateToPercent $root.'line-rate'
$branchPercent = Convert-RateToPercent $root.'branch-rate'

Write-Host "Coverage report: $($latest.FullName)"
Write-Host ("Lines:    {0}% ({1}/{2})" -f $linePercent, $root.'lines-covered', $root.'lines-valid')
Write-Host ("Branches: {0}% ({1}/{2})" -f $branchPercent, $root.'branches-covered', $root.'branches-valid')
Write-Host ""
Write-Host "Lowest line-coverage classes (minimum 10 executable lines):"

$classes = foreach ($class in $root.packages.package.classes.class) {
    $valid = [int]$class.'lines-valid'
    if ($valid -lt 10) { continue }
    [pscustomobject]@{
        Class = [string]$class.name
        LineCoverage = Convert-RateToPercent ([string]$class.'line-rate')
        LinesCovered = [int]$class.'lines-covered'
        LinesValid = $valid
        BranchCoverage = Convert-RateToPercent ([string]$class.'branch-rate')
    }
}

$classes |
    Sort-Object LineCoverage, Class |
    Select-Object -First 25 |
    Format-Table -AutoSize
