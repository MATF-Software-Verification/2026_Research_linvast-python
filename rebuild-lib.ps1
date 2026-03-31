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

function Restore-Environment()
{
    Write-Host "Restoring environment"
    if (Test-Path ./Nuget.config) 
    {
    	Remove-Item ./NuGet.config
    }
}

function Prepare-Environment([string] $target_dir_path)
{
    if (Test-Path ./.nuget/Nuget.config) 
    {
        Copy-Item ./.nuget/NuGet.config ./
    }
    
    if (Test-Path "$target_dir_path")
    {
        Write-Host "Target directory exists, deleting"
        Remove-Item -recurse -force "$target_dir_path"
    }
    
    $dir = New-Item -type directory "$target_dir_path"
}

function Build-All([string] $target_dir_path, [string] $version_suffix, [string] $build_number, [string] $bcfg)
{
    $dir = Get-Item "$target_dir_path"
    $target_dir = $dir.FullName
    Write-Host "Will place packages in $target_dir"
    
    Write-Host "Cleaning previous build"
    & dotnet clean -v minimal -c "$bcfg" | Out-Host
    if ($LastExitCode -ne 0)
    {
        Write-Host "Cleanup failed"
        Return $LastExitCode
    }
    
    Write-Host "Restoring NuGet packages"
    & dotnet restore -v minimal | Out-Host
    if ($LastExitCode -ne 0)
    {
        Write-Host "Restoring packages failed"
        Return $LastExitCode
    }

    if (-not $build_number)
    {
        $build_number_string = ""
    }
    else
    {
        $build_number_string = [int]::Parse($build_number).ToString("00000")
    }
    
    Write-Host "Building everything"
    if (-not $version_suffix)
    {
        & dotnet build -v minimal -c "$bcfg" | Out-Host
    }
    elseif (-not $build_number_string)
    {
      & dotnet build -v minimal -c "$bcfg" --version-suffix "$version_suffix" | Out-Host
    }
    else
    {
        & dotnet build -v minimal -c "$bcfg" --version-suffix "$version_suffix" -p:BuildNumber="$build_number_string" | Out-Host
    }
    if ($LastExitCode -ne 0)
    {
        Write-Host "Build failed"
        Return $LastExitCode
    }
        
    Write-Host "Creating NuGet packages"
    if (-not $version_suffix)
    {
        & dotnet pack -v minimal -c "$bcfg" --no-build -o "$target_dir" --include-symbols | Out-Host
    }
    elseif (-not $build_number_string)
    {
        & dotnet pack -v minimal -c "$bcfg" --version-suffix "$version_suffix" --no-build -o "$target_dir" | Out-Host
    }
    else
    {
        & dotnet pack -v minimal -c "$bcfg" --version-suffix "$version_suffix" -p:BuildNumber="$build_number_string" --no-build -o "$target_dir" | Out-Host
    }
    if ($LastExitCode -ne 0)
    {
        Write-Host "Packaging failed"
        Return $LastExitCode
    }
    
    Return 0
}

if ($VersionSuffix -and $BuildNumber -ge 0)
{
    Write-Host "Building pre-production package with version suffix of `"$VersionSuffix-$($BuildNumber.ToString("00000"))`""
}
elseif ($VersionSuffix -and (-not $BuildNumber -or $BuildNumber -lt 0))
{
	Write-Host "Building pre-production package with version suffix of `"$VersionSuffix`""
	Remove-Variable BuildNumber
	$BuildNumber = $null
}

Prepare-Environment "$ArtifactLocation"
$BuildResult = Build-All "$ArtifactLocation" "$VersionSuffix" "$BuildNumber" "$Configuration"
Restore-Environment
if ($BuildResult -ne 0)
{
    Write-Host "Build failed with code $BuildResult"
    $host.SetShouldExit($BuildResult)
    Exit $BuildResult
}
else
{
    Exit 0
}
