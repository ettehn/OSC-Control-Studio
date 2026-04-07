param(
    [Parameter(Mandatory = $true)]
    [string] $PackageRoot,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath,

    [string] $AppName,

    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-SafeFileName {
    param([Parameter(Mandatory = $true)][string] $Name)

    $invalid = [System.IO.Path]::GetInvalidFileNameChars()
    $builder = [System.Text.StringBuilder]::new()
    $lastWasUnderscore = $false

    foreach ($character in $Name.ToCharArray()) {
        $safe = if ($invalid -contains $character -or [char]::IsControl($character)) { '_' } else { $character }
        if ($safe -eq '_') {
            if ($lastWasUnderscore) {
                continue
            }

            $lastWasUnderscore = $true
        } else {
            $lastWasUnderscore = $false
        }

        [void] $builder.Append($safe)
    }

    $sanitized = $builder.ToString().Trim().TrimEnd('.', ' ')
    if ([string]::IsNullOrWhiteSpace($sanitized)) {
        return 'OSCControlApp'
    }

    return $sanitized
}

function Get-PackageAppName {
    param([Parameter(Mandatory = $true)][string] $Root)

    $manifestPath = Join-Path $Root 'app\app.manifest.json'
    if (Test-Path -LiteralPath $manifestPath) {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        if ($manifest.name) {
            return [string] $manifest.name
        }
    }

    return Split-Path -Leaf $Root
}

function ConvertTo-Base64LiteralLines {
    param([Parameter(Mandatory = $true)][string] $Value)

    $lines = New-Object System.Collections.Generic.List[string]
    for ($index = 0; $index -lt $Value.Length; $index += 76) {
        $length = [Math]::Min(76, $Value.Length - $index)
        $lines.Add("    '$($Value.Substring($index, $length))'")
    }

    return [string]::Join(",`r`n", $lines)
}

function Escape-SingleQuotedPowerShellString {
    param([Parameter(Mandatory = $true)][string] $Value)
    return $Value.Replace("'", "''")
}

if (-not (Test-Path -LiteralPath $PackageRoot -PathType Container)) {
    throw "Package root not found: $PackageRoot"
}

$resolvedPackageRoot = (Resolve-Path -LiteralPath $PackageRoot).Path
$runCommand = Join-Path $resolvedPackageRoot 'run.cmd'
$appFolder = Join-Path $resolvedPackageRoot 'app'

if (-not (Test-Path -LiteralPath $runCommand -PathType Leaf)) {
    throw "Package root is missing run.cmd: $resolvedPackageRoot"
}

if (-not (Test-Path -LiteralPath $appFolder -PathType Container)) {
    throw "Package root is missing app folder: $resolvedPackageRoot"
}

$templatePath = Join-Path $PSScriptRoot 'OSCControlInstallerTemplate.ps1.in'
if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
    throw "Installer template not found: $templatePath"
}

$resolvedAppName = if ([string]::IsNullOrWhiteSpace($AppName)) { Get-PackageAppName -Root $resolvedPackageRoot } else { $AppName.Trim() }
if ([string]::IsNullOrWhiteSpace($resolvedAppName)) {
    $resolvedAppName = 'OSCControl App'
}

$folderName = Get-SafeFileName -Name $resolvedAppName
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

if ((Test-Path -LiteralPath $resolvedOutputPath) -and -not $Force) {
    throw "Installer already exists: $resolvedOutputPath. Use -Force to overwrite."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("OSCControlInstallerBuild-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null

try {
    $zipPath = Join-Path $tempRoot 'payload.zip'
    Compress-Archive -Path (Join-Path $resolvedPackageRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal
    $payloadLines = ConvertTo-Base64LiteralLines -Value ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($zipPath)))

    $installer = Get-Content -LiteralPath $templatePath -Raw
    $installer = $installer.Replace('APP_NAME_PLACEHOLDER', (Escape-SingleQuotedPowerShellString -Value $resolvedAppName))
    $installer = $installer.Replace('FOLDER_NAME_PLACEHOLDER', (Escape-SingleQuotedPowerShellString -Value $folderName))
    $installer = $installer.Replace('GENERATED_AT_PLACEHOLDER', (Escape-SingleQuotedPowerShellString -Value ([DateTimeOffset]::Now.ToString('O'))))
    $installer = $installer.Replace('PAYLOAD_LINES_PLACEHOLDER', $payloadLines)

    Set-Content -LiteralPath $resolvedOutputPath -Value $installer -Encoding UTF8
    Write-Host "Installer written: $resolvedOutputPath"
    Write-Host "App name: $resolvedAppName"
    Write-Host "Package root: $resolvedPackageRoot"
} finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
