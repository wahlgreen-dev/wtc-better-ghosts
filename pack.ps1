#Requires -Version 5.1
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $OutDir,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not $OutDir) {
    $OutDir = Join-Path $PSScriptRoot 'dist'
}

$MelonLoaderVersion = '0.7.3'
$MelonLoaderUrl = "https://github.com/LavaGang/MelonLoader/releases/download/v$MelonLoaderVersion/MelonLoader.x64.zip"
$MelonLoaderSha256 = '5B2B2F3D1CD42B59EC886C5BDC2663EDAE87A0097A4F4A8F58C0965A99DDA416'

$RuntimeVersion = '6.0.36'
$RuntimeUrl = "https://builds.dotnet.microsoft.com/dotnet/Runtime/$RuntimeVersion/dotnet-runtime-$RuntimeVersion-win-x64.zip"
$RuntimeSha256 = 'C077CCC342C5571CEB7A3C06D1DFBC99B989ED5A6E0F3A0B0B450531C1259615'

$CacheDir = Join-Path $PSScriptRoot '.cache'
$StageDir = Join-Path $PSScriptRoot 'obj\pack-stage'
$ModDll = Join-Path $PSScriptRoot "bin\$Configuration\net6.0\BetterGhosts.dll"

$RequiredInPayload = @(
    'version.dll'
    'MelonLoader\net6\MelonLoader.dll'
    'MelonLoader\Documentation\LICENSE.md'
    'MelonLoader\Dependencies\dotnet\host\fxr'
    'Mods\BetterGhosts.dll'
)

function Get-ModVersion {
    $modSource = Join-Path $PSScriptRoot 'BetterGhostsMod.cs'
    $pattern = 'MelonInfo\s*\(\s*typeof\([^)]*\)\s*,\s*"[^"]*"\s*,\s*"([^"]+)"'
    $found = [regex]::Match((Get-Content $modSource -Raw), $pattern)

    if (-not $found.Success) {
        throw "No MelonInfo version in $modSource"
    }

    return $found.Groups[1].Value
}

function Invoke-ModBuild {
    Write-Host "Building ($Configuration)..."
    & dotnet build (Join-Path $PSScriptRoot 'BetterGhosts.csproj') -c $Configuration -v:m

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
}

function Get-PinnedArchive {
    param(
        [string] $Name,
        [string] $Version,
        [string] $Url,
        [string] $Sha256
    )

    if (-not (Test-Path $CacheDir)) {
        New-Item -ItemType Directory -Path $CacheDir | Out-Null
    }

    $archive = Join-Path $CacheDir "$Name-$Version.zip"

    if (-not (Test-Path $archive)) {
        Write-Host "  downloading $Name $Version..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $Url -OutFile $archive -UseBasicParsing
    }

    $actual = (Get-FileHash -Path $archive -Algorithm SHA256).Hash

    if ($actual -ne $Sha256) {
        Remove-Item $archive -Force
        throw "$Name $Version hash mismatch, expected $Sha256 got $actual"
    }

    Write-Host "  $Name $Version ok"
    return $archive
}

function New-StagedPayload {
    param(
        [string] $MelonLoaderZip,
        [string] $RuntimeZip
    )

    if (Test-Path $StageDir) {
        Remove-Item $StageDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $StageDir | Out-Null

    [IO.Compression.ZipFile]::ExtractToDirectory($MelonLoaderZip, $StageDir)

    $portableDotnet = Join-Path $StageDir 'MelonLoader\Dependencies\dotnet'
    New-Item -ItemType Directory -Path $portableDotnet -Force | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($RuntimeZip, $portableDotnet)

    $mods = Join-Path $StageDir 'Mods'
    New-Item -ItemType Directory -Path $mods | Out-Null
    Copy-Item $ModDll -Destination $mods
}

function Confirm-StagedPayload {
    foreach ($required in $RequiredInPayload) {
        if (-not (Test-Path (Join-Path $StageDir $required))) {
            throw "Staged payload is missing '$required'."
        }
    }

    if (Test-Path (Join-Path $StageDir 'MelonLoader\Il2CppAssemblies')) {
        throw "Staged payload contains Il2CppAssemblies"
    }
}

function New-ZipFromDirectory {
    param(
        [string] $SourceDir,
        [string] $DestinationPath
    )

    $zip = [IO.Compression.ZipFile]::Open($DestinationPath, [IO.Compression.ZipArchiveMode]::Create)

    try {
        $root = (Resolve-Path $SourceDir).Path.TrimEnd('\') + '\'

        foreach ($file in Get-ChildItem -Path $SourceDir -Recurse -File) {
            $entry = $file.FullName.Substring($root.Length).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip, $file.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }
}

$modVersion = Get-ModVersion
Write-Host "Packing Better Ghosts $modVersion" -ForegroundColor Cyan

if (-not $SkipBuild) {
    Invoke-ModBuild
}

if (-not (Test-Path $ModDll)) {
    throw "No built mod at $ModDll"
}

Write-Host "Resolving pinned payload..."

$melonLoaderZip = Get-PinnedArchive -Name 'MelonLoader' -Version $MelonLoaderVersion -Url $MelonLoaderUrl -Sha256 $MelonLoaderSha256
$runtimeZip = Get-PinnedArchive -Name 'dotnet-runtime' -Version $RuntimeVersion -Url $RuntimeUrl -Sha256 $RuntimeSha256

Write-Host "Assembling..."

New-StagedPayload -MelonLoaderZip $melonLoaderZip -RuntimeZip $runtimeZip
Confirm-StagedPayload

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir | Out-Null
}

$zipPath = Join-Path $OutDir "Better-Ghosts-v$modVersion.zip"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

New-ZipFromDirectory -SourceDir $StageDir -DestinationPath $zipPath
Remove-Item $StageDir -Recurse -Force

$dllPath = Join-Path $OutDir 'BetterGhosts.dll'
Copy-Item $ModDll -Destination $dllPath -Force

$builtZip = Get-Item $zipPath
$builtDll = Get-Item $dllPath
$zipSizeMb = [math]::Round($builtZip.Length / 1MB, 1)
$dllSizeKb = [math]::Round($builtDll.Length / 1KB)

Write-Host ""
Write-Host "Built $($builtZip.Name) ($zipSizeMb MB)" -ForegroundColor Green
Write-Host "Built $($builtDll.Name) ($dllSizeKb KB)" -ForegroundColor Green
Write-Host ""
Write-Host "For the release notes:"
Write-Host "$($builtZip.Name) SHA-256: $((Get-FileHash -Path $zipPath -Algorithm SHA256).Hash)"
Write-Host "$($builtDll.Name) SHA-256: $((Get-FileHash -Path $dllPath -Algorithm SHA256).Hash)"
Write-Host "Bundles stock MelonLoader $MelonLoaderVersion ($MelonLoaderSha256)"
Write-Host "Bundles stock .NET runtime $RuntimeVersion ($RuntimeSha256)"
