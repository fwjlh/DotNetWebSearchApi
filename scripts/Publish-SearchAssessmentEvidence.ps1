[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot,

    [Parameter(Mandatory = $true)]
    [string]$JsonPath,

    [Parameter(Mandatory = $true)]
    [string]$LogPath,

    [Parameter(Mandatory = $true)]
    [string]$RunTimestamp
)

$ErrorActionPreference = "Stop"

function Invoke-GitCommand {
    param(
        [string]$RepositoryRoot,
        [string[]]$Arguments
    )

    $output = @(& git -C $RepositoryRoot @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw ("Git command failed ({0}): git {1}{2}{3}" -f $exitCode, ($Arguments -join ' '), [Environment]::NewLine, ($output -join [Environment]::NewLine))
    }

    return ($output -join [Environment]::NewLine).Trim()
}

function Publish-Evidence {
    param(
        [string]$RepositoryRoot,
        [string]$JsonPath,
        [string]$LogPath,
        [string]$RunTimestamp
    )

    $actualRoot = Invoke-GitCommand -RepositoryRoot $RepositoryRoot -Arguments @("rev-parse", "--show-toplevel")
    $expectedRoot = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar))
    $actualRoot = [System.IO.Path]::GetFullPath($actualRoot).TrimEnd([char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar))
    if (-not $actualRoot.Equals($expectedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Git repository root does not match the assessment script directory."
    }

    $branch = Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("branch", "--show-current")
    if ($branch -ne "main") {
        throw "Evidence publishing requires the current branch to be main; found '$branch'."
    }

    $origin = Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("remote", "get-url", "origin")
    $originIsExpected = $origin.IndexOf("github.com", [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and (
        $origin.EndsWith("fwjlh/DotNetWebSearchApi.git", [System.StringComparison]::OrdinalIgnoreCase) -or
        $origin.EndsWith("fwjlh/DotNetWebSearchApi", [System.StringComparison]::OrdinalIgnoreCase)
    )
    if (-not $originIsExpected) {
        throw "The origin remote is not fwjlh/DotNetWebSearchApi; refusing to publish evidence."
    }

    Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("fetch", "origin", "main") | Out-Null
    $localHead = Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("rev-parse", "HEAD")
    $remoteHead = Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("rev-parse", "origin/main")
    if ($localHead -ne $remoteHead) {
        throw "Local main is not synchronized with origin/main. Pull or reconcile changes before publishing evidence."
    }

    $existingStagedPaths = Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("diff", "--cached", "--name-only")
    if (-not [string]::IsNullOrWhiteSpace($existingStagedPaths)) {
        throw "There are already staged changes. Commit or unstage them before publishing assessment evidence."
    }

    foreach ($path in @($JsonPath, $LogPath)) {
        $fullPath = [System.IO.Path]::GetFullPath($path)
        if (-not $fullPath.StartsWith($actualRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Evidence file is outside the repository: $fullPath"
        }
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            throw "Evidence file does not exist: $fullPath"
        }
    }

    $jsonRelativePath = $JsonPath.Substring($actualRoot.Length).TrimStart([char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)).Replace("\", "/")
    $logRelativePath = $LogPath.Substring($actualRoot.Length).TrimStart([char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)).Replace("\", "/")
    Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("add", "-f", "--", $jsonRelativePath, $logRelativePath) | Out-Null

    $stagedOutput = Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("diff", "--cached", "--name-only")
    $stagedPaths = @([regex]::Split($stagedOutput, "\r?\n") | Where-Object { $_ })
    $expectedPaths = @($jsonRelativePath, $logRelativePath)
    if ($stagedPaths.Count -ne 2 -or @($stagedPaths | Where-Object { $expectedPaths -notcontains $_ }).Count -ne 0) {
        throw "Git staging contains unexpected files; refusing to commit."
    }

    Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("commit", "-m", "Add search assessment evidence $RunTimestamp") | Out-Null
    $commit = Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("rev-parse", "--short", "HEAD")
    Invoke-GitCommand -RepositoryRoot $actualRoot -Arguments @("push", "origin", "HEAD:main") | Out-Null
    return $commit
}

$commit = Publish-Evidence -RepositoryRoot $RepositoryRoot -JsonPath $JsonPath -LogPath $LogPath -RunTimestamp $RunTimestamp
Write-Output $commit
