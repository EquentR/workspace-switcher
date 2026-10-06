<#
.SYNOPSIS
    Publishes the Workspace Switcher WPF application and compiles the Windows installer.

.DESCRIPTION
    Two stages:
      1. dotnet publish of src/WorkspaceSwitcher.UI as a self-contained win-x64
         build (no .NET runtime required on the target machine).
      2. Inno Setup (ISCC) compilation of installer/WorkspaceSwitcher.iss,
         producing <OutputDir>/WorkspaceSwitcher-<Version>-Setup.exe.

    Requires Inno Setup 6.5 or newer. Pass -IsccPath if ISCC.exe is not in PATH
    nor in one of the default installation folders
    (winget install JRSoftware.InnoSetup / choco install innosetup).

.EXAMPLE
    pwsh -File installer/build-installer.ps1 -Version 1.2.3

.EXAMPLE
    pwsh -File installer/build-installer.ps1 -SkipPublish
#>
[CmdletBinding()]
param(
    [string] $Version,
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [string] $RuntimeIdentifier = 'win-x64',
    [string] $PublishDir,
    [string] $OutputDir,
    [string] $IsccPath,
    [switch] $SkipPublish
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'src/WorkspaceSwitcher.UI/WorkspaceSwitcher.UI.csproj'
$scriptPath = Join-Path $PSScriptRoot 'WorkspaceSwitcher.iss'

if (-not $PublishDir) { $PublishDir = Join-Path $repoRoot 'publish/gui' }
if (-not $OutputDir) { $OutputDir = Join-Path $repoRoot 'dist' }

function Resolve-Version {
    param([string] $Requested)

    if ($Requested) {
        if ($Requested -notmatch '^\d+(\.\d+){1,3}$') {
            throw "Version '$Requested' is not a numeric version such as 1.2.3."
        }
        return $Requested
    }

    Push-Location $repoRoot
    try {
        $tag = $null
        try { $tag = git describe --tags --abbrev=0 --match 'v[0-9]*' 2>$null } catch { }
    }
    finally {
        Pop-Location
    }

    if ($tag -and $tag.Trim() -match '^v(?<version>\d+(\.\d+){1,3})$') {
        return $Matches['version']
    }

    Write-Warning 'No version supplied and no v* git tag found - using 0.0.0.'
    return '0.0.0'
}

function Resolve-Iscc {
    param([string] $Requested)

    $candidates = @($Requested)
    foreach ($base in @($env:ProgramFiles, ${env:ProgramFiles(x86)}, (Join-Path $env:LOCALAPPDATA 'Programs'))) {
        if ($base) { $candidates += (Join-Path $base 'Inno Setup 6/ISCC.exe') }
    }
    $candidates += 'C:/ProgramData/chocolatey/bin/ISCC.exe'
    $candidates = $candidates | Where-Object { $_ }

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return (Resolve-Path $candidate).Path }
    }

    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    throw @'
ISCC.exe (Inno Setup 6 command-line compiler) was not found.
Install it first, for example:
  winget install JRSoftware.InnoSetup
  choco install innosetup
'@
}

$version = Resolve-Version -Requested $Version
Write-Host "Building Workspace Switcher $version ($Configuration, $RuntimeIdentifier)"

if (-not $SkipPublish) {
    # A previous publish with different flags could leave stale files behind
    # that would then end up inside the installer payload.
    if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

    dotnet publish $solution `
        --configuration $Configuration `
        --runtime $RuntimeIdentifier `
        --self-contained true `
        --output $PublishDir `
        -p:Version=$version `
        -p:DebugType=none `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
}
elseif (-not (Test-Path $PublishDir)) {
    throw "Publish directory '$PublishDir' does not exist; run without -SkipPublish first."
}

$iscc = Resolve-Iscc -Requested $IsccPath
Write-Host "Compiling installer with $iscc"

& $iscc "/DAppVersion=$version" "/DPublishDir=$PublishDir" "/DOutputDir=$OutputDir" $scriptPath
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE." }

$setup = Join-Path $OutputDir "WorkspaceSwitcher-$version-Setup.exe"
if (-not (Test-Path $setup)) { throw "Expected installer '$setup' was not created." }

$size = [Math]::Round((Get-Item $setup).Length / 1MB, 1)
Write-Host "Installer ready: $setup ($size MB)"