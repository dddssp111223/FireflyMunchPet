$ErrorActionPreference = "Stop"

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$dotnetRoot = Join-Path $projectRoot ".tools\dotnet"
$godotRoot = Join-Path $projectRoot ".tools\godot-dotnet"
$godotDataRoot = Join-Path $projectRoot ".tools\godot-appdata"

$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_CLI_HOME = Join-Path $projectRoot ".tools\dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:NUGET_PACKAGES = Join-Path $projectRoot ".tools\nuget-packages"
$env:APPDATA = Join-Path $godotDataRoot "roaming"
$env:LOCALAPPDATA = Join-Path $godotDataRoot "local"
$env:PATH = "$dotnetRoot;$env:PATH"

$requiredDirectories = @(
    $env:DOTNET_CLI_HOME,
    $env:NUGET_PACKAGES,
    $env:APPDATA,
    $env:LOCALAPPDATA
)

foreach ($directory in $requiredDirectories) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$godot = Get-ChildItem -LiteralPath $godotRoot -Recurse -File |
    Where-Object { $_.Name -like "*console.exe" } |
    Select-Object -First 1

if (-not $godot) {
    throw "Godot .NET console executable was not found under $godotRoot"
}

& $godot.FullName @args
exit $LASTEXITCODE
