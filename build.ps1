#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

$CI = $env:CI -eq "true"

$DOTNET_IMAGE = "mcr.microsoft.com/dotnet/sdk:10.0"

function Get-TermWidth {
    try {
        if ($Host.UI.RawUI.WindowSize.Width) {
            return $Host.UI.RawUI.WindowSize.Width
        }
    } catch {}

    return 80
}

$TermWidth = Get-TermWidth

function Full-Line {
    param([string]$Char = "=")

    $line = $Char * $TermWidth
    Write-Output $line
}

function Center-Text {
    param([string]$Text)

    if ($CI) {
        Write-Output $Text
        return
    }

    $padding = [math]::Max(
        0,
        [math]::Floor(($TermWidth - $Text.Length) / 2)
    )

    $pad = " " * $padding

    Write-Output "$pad$Text"
}

function Build-Failed {
    Write-Host ""
    Write-Host (Full-Line "=") -ForegroundColor Red
    Write-Host (Center-Text "BUILD FAILED") -ForegroundColor Red
    Write-Host (Full-Line "=") -ForegroundColor Red
    Write-Host ""

    exit 1
}

function Build-Succeeded {
    Write-Host ""
    Write-Host (Full-Line "=") -ForegroundColor Green
    Write-Host (Center-Text "BUILD SUCCEEDED") -ForegroundColor Green
    Write-Host (Full-Line "=") -ForegroundColor Green
    Write-Host ""

    exit 0
}

$Project = "src/ShibbyPgcr/ShibbyPgcr.csproj"
$ProjectName = "ShibbyPgcr"
$OutputDirectory = "/workspace/build/$ProjectName"

Remove-Item -Recurse -Force build -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path build | Out-Null

Write-Host ""
Write-Host (Full-Line "-")
Write-Host (Center-Text "BUILDING $ProjectName") -ForegroundColor Cyan
Write-Host (Full-Line "-")
Write-Host ""

Write-Host "Project: $Project"
Write-Host "Target:  win-x64"
Write-Host "Output:  build/$ProjectName"
Write-Host ""

podman run --rm `
    -v "${PWD}:/workspace:Z" `
    -w /workspace `
    $DOTNET_IMAGE `
    dotnet publish $Project `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --property:PublishSingleFile=true `
        --output $OutputDirectory

if ($LASTEXITCODE -ne 0) {
    Build-Failed
}

Get-ChildItem -Path src -Directory -Recurse |
    Where-Object {
        $_.Name -eq "bin" -or $_.Name -eq "obj"
    } |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinu

Write-Host ""
Write-Host "Build artifacts:" -ForegroundColor Cyan

Get-ChildItem -Path build -Recurse -File |
    ForEach-Object {
        $RelativePath = $_.FullName.Substring(
            (Get-Location).Path.Length + 1
        )

        Write-Host "  $RelativePath"
    }

Build-Succeeded
