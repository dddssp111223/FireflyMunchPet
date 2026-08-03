[CmdletBinding()]
param(
    [switch]$ForcePortable
)

$ErrorActionPreference = "Stop"

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$exportRoot = Join-Path $projectRoot "exports\MunchPet"
$artifactRoot = Join-Path $projectRoot "artifacts"
$stagingRoot = Join-Path $artifactRoot "MunchPet-win-x64"
$archive = Join-Path $artifactRoot "MunchPet-win-x64.zip"

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
if (Test-Path -LiteralPath $stagingRoot) {
    $resolvedStaging = (Resolve-Path -LiteralPath $stagingRoot).Path
    $resolvedArtifacts = (Resolve-Path -LiteralPath $artifactRoot).Path
    if (-not $resolvedStaging.StartsWith($resolvedArtifacts + [System.IO.Path]::DirectorySeparatorChar)) {
        throw "Refusing to replace staging outside artifacts: $resolvedStaging"
    }
    Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null

$standardExport = Join-Path $exportRoot "MunchPet.exe"
$standardData = Join-Path $exportRoot "data_DesktopPet_windows_x86_64"
if (-not $ForcePortable -and
    (Test-Path -LiteralPath $standardExport) -and
    (Test-Path -LiteralPath $standardData)) {
    Copy-Item -LiteralPath $standardExport -Destination $stagingRoot
    Copy-Item -LiteralPath $standardData -Destination $stagingRoot -Recurse
    $packageKind = "Godot self-contained release export"
}
else {
    $appRoot = Join-Path $stagingRoot "app"
    $runtimeRoot = Join-Path $stagingRoot "runtime"
    New-Item -ItemType Directory -Force -Path $appRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $runtimeRoot | Out-Null

    $godotRoot = Get-ChildItem -LiteralPath (Join-Path $projectRoot ".tools\godot-dotnet") -Directory |
        Select-Object -First 1
    if (-not $godotRoot) { throw "Workspace-local Godot .NET runtime not found." }
    $godotExe = Get-ChildItem -LiteralPath $godotRoot.FullName -File |
        Where-Object { $_.Name -like "*.exe" -and $_.Name -notlike "*console*" } |
        Select-Object -First 1
    if (-not $godotExe) { throw "Godot .NET GUI executable not found." }

    Copy-Item -LiteralPath $godotExe.FullName -Destination (Join-Path $appRoot "MunchPet.Runtime.exe")
    Copy-Item -LiteralPath (Join-Path $godotRoot.FullName "GodotSharp") -Destination $appRoot -Recurse

    foreach ($item in @("project.godot", "DesktopPet.csproj")) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $item) -Destination $appRoot
    }
    foreach ($directory in @("assets", "scenes", "shaders", "src")) {
        Copy-Item -LiteralPath (Join-Path $projectRoot $directory) -Destination $appRoot -Recurse
    }

    $godotCache = Join-Path $appRoot ".godot"
    New-Item -ItemType Directory -Force -Path $godotCache | Out-Null
    Copy-Item -LiteralPath (Join-Path $projectRoot ".godot\imported") -Destination $godotCache -Recurse
    foreach ($cacheFile in @("uid_cache.bin", "global_script_class_cache.cfg")) {
        $source = Join-Path $projectRoot ".godot\$cacheFile"
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination $godotCache
        }
    }
    $monoTarget = Join-Path $godotCache "mono\temp\bin"
    New-Item -ItemType Directory -Force -Path $monoTarget | Out-Null
    Copy-Item -LiteralPath (Join-Path $projectRoot ".godot\mono\temp\bin\Debug") -Destination $monoTarget -Recurse

    $dotnetRoot = Join-Path $projectRoot ".tools\dotnet"
    Copy-Item -LiteralPath (Join-Path $dotnetRoot "dotnet.exe") -Destination $runtimeRoot
    Copy-Item -LiteralPath (Join-Path $dotnetRoot "host") -Destination $runtimeRoot -Recurse
    New-Item -ItemType Directory -Force -Path (Join-Path $runtimeRoot "shared") | Out-Null
    Copy-Item -LiteralPath (Join-Path $dotnetRoot "shared\Microsoft.NETCore.App") `
        -Destination (Join-Path $runtimeRoot "shared") -Recurse

    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (-not (Test-Path -LiteralPath $csc)) { throw "Windows C# launcher compiler not found." }
    & $csc /nologo /target:winexe /optimize+ `
        /win32icon:"$(Join-Path $projectRoot 'assets\icons\app.ico')" `
        /out:"$(Join-Path $stagingRoot 'MunchPet.exe')" `
        "$(Join-Path $PSScriptRoot 'PortableLauncher.cs')"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $packageKind = "portable Godot .NET runtime bundle"
}

$readme = @(
    "MunchPet desktop pet (Windows 10/11 x64)",
    "",
    "Double-click MunchPet.exe. No installation is required. Keep the app and runtime folders beside it.",
    "",
    "- Drop files, folders, or multiple selected items on the pet to move them to Windows Recycle Bin.",
    "- Successful deletion is silent. Windows shows its normal warning if an item cannot be recycled.",
    "- Click the pet for a squash bounce and sound.",
    "- Drag the upper hair area to move the window.",
    "- Use the notification-area item 和谐版 to switch the dry platform-safe visuals on or off.",
    "- Available scales: 30%, 50%, 75%, 100%, 125%, and 150%.",
    "- Use 开启备忘录提醒 and 编辑任务列表… to manage up to 5 local reminders (200 characters each).",
    "- Reminder data is stored in %APPDATA%\Godot\app_userdata\MunchPet\reminders.json; missed reminders are not backfilled while the app or reminder switch is off.",
    "- The notification-area icon also controls topmost, mute, reset position, and exit.",
    "",
    "For the first test, use a newly created disposable file. It can be restored from Recycle Bin."
) -join [System.Environment]::NewLine
Set-Content -LiteralPath (Join-Path $stagingRoot "README.txt") -Value $readme -Encoding utf8

if (Test-Path -LiteralPath $archive) {
    Remove-Item -LiteralPath $archive -Force
}
Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $archive -CompressionLevel Optimal

$item = Get-Item -LiteralPath $archive
$hash = Get-FileHash -LiteralPath $archive -Algorithm SHA256
Remove-Item -LiteralPath $stagingRoot -Recurse -Force
Write-Output "Package kind: $packageKind"
Write-Output "Archive: $($item.FullName)"
Write-Output "Bytes: $($item.Length)"
Write-Output "SHA256: $($hash.Hash)"
