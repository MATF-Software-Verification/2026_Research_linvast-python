#!/usr/bin/env pwsh

param
(
    [parameter(Mandatory = $true)]
    [string] $ArtifactLocation,

    [parameter(Mandatory = $true)]
    [string] $Configuration,

    [parameter(Mandatory = $false)]
    [string] $VersionSuffix,

    [parameter(Mandatory = $false)]
    [int] $BuildNumber = -1
)

if ($Configuration -ne "Debug" -and $Configuration -ne "Release")
{
    Write-Host "Invalid configuration specified. Must be Release or Debug."
    Exit 1
}

if (-not $VersionSuffix)
{
    Write-Host "Building production packages"
    & .\rebuild-lib.ps1 -ArtifactLocation "$ArtifactLocation" -Configuration "$Configuration" | Out-Host
}
else
{
    Write-Host "Building pre-production packages"
    if (-not $BuildNumber -or $BuildNumber -lt 0)
    {
        $BuildNumber = -1
    }

    & .\rebuild-lib.ps1 -ArtifactLocation "$ArtifactLocation" -Configuration "$Configuration" -VersionSuffix "$VersionSuffix" -BuildNumber $BuildNumber | Out-Host
}

if ($LastExitCode -ne 0)
{
    Write-Host "Build failed with code $LastExitCode"
    $host.SetShouldExit($LastExitCode)
    Exit $LastExitCode
}

Exit 0
