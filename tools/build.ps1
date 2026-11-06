<#
.SYNOPSIS
    Builds the standalone Windows player to Build/Overworld/Overworld.exe (Unity batch mode; the Editor must be closed).
.DESCRIPTION
    Runs Unity 6000.4.3f1 headless with OverworldBuild.BuildPlayer (the same build play.ps1 -Build runs). Exits
    non-zero when Unity fails or the exe is not produced. The Unity log goes to Logs/build-player.log. Set
    UNITY_EDITOR to use a different Unity.exe.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'player-args.ps1')
$exe = Join-Path $root 'Build/Overworld/Overworld.exe'
$buildLog = Join-Path $root 'Logs/build-player.log'
New-Item -ItemType Directory -Force (Split-Path $buildLog) | Out-Null
$unity = if ($env:UNITY_EDITOR) { $env:UNITY_EDITOR } else { 'C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe' }

Write-Host '  Building Windows player...' -ForegroundColor Cyan
$p = Start-Process -FilePath $unity -Wait -PassThru -ArgumentList (Join-PlayerArguments @(
    '-batchmode', '-projectPath', $root,
    '-executeMethod', 'MindAttic.Overworld.Editor.OverworldBuild.BuildPlayer', '-logFile', $buildLog))
if ($p.ExitCode -ne 0 -or -not (Test-Path $exe)) {
    Write-Host "Player build failed; see $buildLog" -ForegroundColor Red
    if ($p.ExitCode -ne 0) { exit $p.ExitCode }
    exit 1
}
Write-Host "Built $exe"
exit 0
