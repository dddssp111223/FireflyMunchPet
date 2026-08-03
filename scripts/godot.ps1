$ErrorActionPreference = "Stop"

$realProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $hashBytes = $sha256.ComputeHash(
        [System.Text.Encoding]::UTF8.GetBytes($realProjectRoot))
}
finally {
    $sha256.Dispose()
}
$hash = ([System.BitConverter]::ToString($hashBytes) -replace "-", "").Substring(0, 12)
$projectRoot = Join-Path ([System.IO.Path]::GetTempPath()) "MunchPetGodot-$hash"

if (Test-Path -LiteralPath $projectRoot) {
    $junction = Get-Item -LiteralPath $projectRoot -Force
    if ($junction.LinkType -ne "Junction" -or $junction.Target -notcontains $realProjectRoot) {
        throw "The ASCII Godot runtime path already exists with an unexpected target: $projectRoot"
    }
}
else {
    New-Item -ItemType Junction -Path $projectRoot -Target $realProjectRoot | Out-Null
}
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

$godotArguments = @($args)
for ($index = 0; $index -lt $godotArguments.Count - 1; $index++) {
    if ($godotArguments[$index] -eq "--path") {
        $godotArguments[$index + 1] = $projectRoot
    }
}

& $godot.FullName @godotArguments
exit $LASTEXITCODE
