param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$appData = Join-Path $repoRoot '.appdata'
$nugetPackages = Join-Path $repoRoot '.nuget\packages'

New-Item -ItemType Directory -Force -Path (Join-Path $appData 'NuGet') | Out-Null
New-Item -ItemType Directory -Force -Path $nugetPackages | Out-Null

$env:APPDATA = $appData
$env:NUGET_PACKAGES = $nugetPackages

$buildArgs = @('-m:1', '-nr:false', '-v:minimal')
$projects = @(
    'src\OSCControl.Compiler\OSCControl.Compiler.csproj',
    'src\OSCControl.Packaging\OSCControl.Packaging.csproj',
    'src\OSCControl.AppHost\OSCControl.AppHost.csproj',
    'src\OSCControl.Packager\OSCControl.Packager.csproj',
    'src\OSCControl.DesktopHost\OSCControl.DesktopHost.csproj',
    'tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj'
)

foreach ($project in $projects) {
    $projectPath = Join-Path $repoRoot $project
    & dotnet build $projectPath @buildArgs
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if (-not $SkipTests) {
    $testProject = Join-Path $repoRoot 'tests\OSCControl.Compiler.Tests\OSCControl.Compiler.Tests.csproj'
    & dotnet run --project $testProject --no-restore --no-build
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
