$ErrorActionPreference = "Stop"

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
Push-Location $projectRoot
try {
    & (Join-Path $PSScriptRoot "dotnet.ps1") run --project tests\DesktopPet.Tests
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & (Join-Path $PSScriptRoot "dotnet.ps1") build DesktopPet.sln --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & (Join-Path $PSScriptRoot "godot.ps1") --headless --path . --editor --quit
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    & (Join-Path $PSScriptRoot "godot.ps1") --headless --path . --quit-after 3
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    git diff --check
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
