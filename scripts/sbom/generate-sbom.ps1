#requires -Version 7.0

<#
.SYNOPSIS
    Linux Edge Inspection Platform のSBOMを生成します。

.DESCRIPTION
    Runtime と CaptureRequestListener を Release publish し、
    Microsoft sbom-tool を使用して、それぞれのSBOMを生成します。

.REQUIREMENTS
    - .NET 10 SDK
    - Microsoft sbom-tool
      dotnet tool install --global Microsoft.Sbom.DotNetTool
#>

$ErrorActionPreference = "Stop"

# ------------------------------------------------------------
# Repository root
# ------------------------------------------------------------
# このスクリプトは、
#
#   scripts/sbom/generate-sbom.ps1
#
# に配置する前提です。
#
# $PSScriptRoot から2階層上をRepositoryルートとして取得します。
# ------------------------------------------------------------
$repoRoot = Resolve-Path (
    Join-Path $PSScriptRoot "..\.."
)

# ------------------------------------------------------------
# Package information
# ------------------------------------------------------------
$packageSupplier = "mono-tec"

$packageVersion = Read-Host `
    "Enter version (e.g. 0.1.0)"

if ([string]::IsNullOrWhiteSpace($packageVersion)) {
    throw "Package version is required."
}

# ------------------------------------------------------------
# Output directories
# ------------------------------------------------------------
$artifactsRoot = Join-Path `
    $repoRoot `
    "artifacts"

$publishRoot = Join-Path `
    $artifactsRoot `
    "publish"

$sbomRoot = Join-Path `
    $artifactsRoot `
    "sbom"

# ------------------------------------------------------------
# Runtime
# ------------------------------------------------------------
$runtimeProject = Join-Path `
    $repoRoot `
    "src\LinuxEdgeInspection.Runtime\LinuxEdgeInspection.Runtime.csproj"

$runtimePublish = Join-Path `
    $publishRoot `
    "runtime"

$runtimeSbom = Join-Path `
    $sbomRoot `
    "runtime"

Write-Host ""
Write-Host "========================================"
Write-Host "Publishing Runtime"
Write-Host "========================================"

dotnet publish `
    $runtimeProject `
    -c Release `
    -o $runtimePublish

if ($LASTEXITCODE -ne 0) {
    throw "Runtime publish failed."
}

# ------------------------------------------------------------
# Remove development-only files
# ------------------------------------------------------------
$runtimeDevelopmentSettings = Join-Path `
    $runtimePublish `
    "appsettings.Development.json"

if (Test-Path $runtimeDevelopmentSettings) {
    Remove-Item `
        $runtimeDevelopmentSettings `
        -Force
}

Get-ChildItem `
    $runtimePublish `
    -Filter "*.pdb" `
    -ErrorAction SilentlyContinue |
    Remove-Item -Force

# ------------------------------------------------------------
# Generate Runtime SBOM
# ------------------------------------------------------------
Write-Host ""
Write-Host "========================================"
Write-Host "Generating Runtime SBOM"
Write-Host "========================================"

sbom-tool generate `
    -b $runtimePublish `
    -m $runtimeSbom `
    -pn "LinuxEdgeInspection.Runtime" `
    -pv $packageVersion `
    -ps $packageSupplier

if ($LASTEXITCODE -ne 0) {
    throw "Runtime SBOM generation failed."
}

# ------------------------------------------------------------
# CaptureRequestListener
# ------------------------------------------------------------
$listenerProject = Join-Path `
    $repoRoot `
    "src\LinuxEdgeInspection.CaptureRequestListener\LinuxEdgeInspection.CaptureRequestListener.csproj"

$listenerPublish = Join-Path `
    $publishRoot `
    "capture-request-listener"

$listenerSbom = Join-Path `
    $sbomRoot `
    "capture-request-listener"

Write-Host ""
Write-Host "========================================"
Write-Host "Publishing CaptureRequestListener"
Write-Host "========================================"

dotnet publish `
    $listenerProject `
    -c Release `
    -o $listenerPublish

if ($LASTEXITCODE -ne 0) {
    throw "CaptureRequestListener publish failed."
}

# ------------------------------------------------------------
# Remove development-only files
# ------------------------------------------------------------
$listenerDevelopmentSettings = Join-Path `
    $listenerPublish `
    "appsettings.Development.json"

if (Test-Path $listenerDevelopmentSettings) {
    Remove-Item `
        $listenerDevelopmentSettings `
        -Force
}

Get-ChildItem `
    $listenerPublish `
    -Filter "*.pdb" `
    -ErrorAction SilentlyContinue |
    Remove-Item -Force

# ------------------------------------------------------------
# Generate CaptureRequestListener SBOM
# ------------------------------------------------------------
Write-Host ""
Write-Host "========================================"
Write-Host "Generating CaptureRequestListener SBOM"
Write-Host "========================================"

sbom-tool generate `
    -b $listenerPublish `
    -m $listenerSbom `
    -pn "LinuxEdgeInspection.CaptureRequestListener" `
    -pv $packageVersion `
    -ps $packageSupplier

if ($LASTEXITCODE -ne 0) {
    throw "CaptureRequestListener SBOM generation failed."
}

# ------------------------------------------------------------
# Completed
# ------------------------------------------------------------
Write-Host ""
Write-Host "========================================"
Write-Host "SBOM generation completed."
Write-Host "========================================"
Write-Host ""
Write-Host "Runtime:"
Write-Host "  $runtimeSbom"
Write-Host ""
Write-Host "CaptureRequestListener:"
Write-Host "  $listenerSbom"
Write-Host ""