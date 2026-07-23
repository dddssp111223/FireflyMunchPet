$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$env:APPDATA = Join-Path $repoRoot '.tools\appdata'
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.tools\dotnet-cli-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:NUGET_PACKAGES = Join-Path $repoRoot '.tools\nuget-packages'

$dotnetExe = Join-Path $repoRoot '.tools\dotnet\dotnet.exe'
& $dotnetExe @args
exit $LASTEXITCODE
