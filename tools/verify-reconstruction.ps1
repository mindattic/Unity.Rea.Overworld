<#
.SYNOPSIS
    U.OVW-T37: plays every mechanic in RECONSTRUCTION.md's build order (docs/RECONSTRUCTION.md section 20) against the
    real Windows player and captures a screenshot of each, so the repo's proof-of-concept claim is backed by evidence
    instead of asserted. Builds the player once, then launches it once per scenario (play.ps1 -Capture, each scripted
    argument documented on OverworldBootstrap's own class summary), saving Logs/verify/<name>.png and
    Logs/verify/<name>.log.
.DESCRIPTION
    Run from the repo root: .\tools\verify-reconstruction.ps1
    play.ps1's default capture ("after a few frames") races every coroutine-driven scripted argument (ApplyScripted*
    almost always defers through StartCoroutine, even at "0 seconds"), so every scenario here that is not a plain,
    static capture gives its own -overworld-capture-after, long enough for its own waits (the ring's own default
    settle, typed console text, the death fade's 255 ticks, ...) to finish first. A scenario also names a regular
    expression its own log must match (the proof the mechanic actually ran, not only that a PNG appeared) in "Expect".
    A launch fails the run if it exits non-zero, no screenshot appears, its log holds a scripted argument's own
    refusal, or (when given) its "Expect" pattern is not found.
    Exits 0 when every scenario passes, 1 otherwise (the failing scenarios are named on stderr).
.PARAMETER From
    1-based index of the first scenario to run (default 1). Lets a long run be split into smaller batches to keep
    memory bounded, each launching and quitting the player once per scenario rather than holding many in flight.
.PARAMETER To
    1-based index of the last scenario to run (default: the last one). -Build is passed only when this batch's first
    scenario is scenario 1; otherwise the player built by an earlier batch is reused.
#>
param(
    [int]$From = 1,
    [int]$To = 0
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot | Split-Path -Parent
Set-Location $root
$verifyDir = Join-Path $root 'Logs/verify'
New-Item -ItemType Directory -Force $verifyDir | Out-Null

# Name, RECONSTRUCTION section(s), the player's own arguments, how long (seconds) to let them finish before the
# capture, and a regex the log must match as evidence the mechanic actually ran.
$scenarios = @(
    @{ Name = '01-world-camera';          Section = '3, 4.1';           Args = @();                                                                            After = 0;   Expect = $null }
    @{ Name = '02-movement-locomotion';   Section = '4.3, 4.4, 5.1';    Args = @('-overworld-hold', 'right');                                                  After = 1;   Expect = 'Walking Right' }
    @{ Name = '03-camera-free';           Section = '3.2, 3.7, 5.1';    Args = @('-overworld-free-camera');                                                    After = 0.5; Expect = $null }
    @{ Name = '04-party-follow-recall';   Section = '4.5';              Args = @('-overworld-recall', '1');                                                    After = 1;   Expect = 'scripted recall of Belmont' }
    @{ Name = '05-hero-select';           Section = '4.7';              Args = @('-overworld-select', 'next');                                                 After = 0.5; Expect = 'scripted leader Belmont' }
    @{ Name = '06-clock-night';           Section = '6.1, 6.2, 6.3';    Args = @('-overworld-clock', '22:00,0');                                                After = 1.5; Expect = 'Night' }
    @{ Name = '07-vitals-status';         Section = '7';                Args = @('-overworld-vitals', '0:poisoned;1:hp=1');                                    After = 0.5; Expect = 'Poisoned' }
    @{ Name = '08-rest-sleep';            Section = '6.4';              Args = @('-overworld-rest', 'lay');                                                    After = 1.5; Expect = $null }
    @{ Name = '09-pickup-reach';          Section = '8.1, 8.2';         Args = @('-overworld-reach', '0');                                                     After = 0.5; Expect = 'selected (?!none)' }
    @{ Name = '10-pickup-take';           Section = '8.2';              Args = @('-overworld-pickup', '0');                                                    After = 1;   Expect = 'pack \[(?!\])' }
    @{ Name = '11-inventory';             Section = '8.3';              Args = @('-overworld-give', '0*3,105', '-overworld-inventory', '0');                   After = 1.5; Expect = 'inventory panel open' }
    @{ Name = '12-examine';               Section = '8.4';              Args = @('-overworld-examine-pickup', '0');                                            After = 1;   Expect = $null }
    @{ Name = '13-quickbar';              Section = '8.5';              Args = @('-overworld-give', '0*3', '-overworld-quickbar');                             After = 1;   Expect = $null }
    @{ Name = '14-console-script';        Section = '9.1, 9.2, 9.3';    Args = @('-overworld-script', 'CHANGE HERO HP -5');                                    After = 3;   Expect = 'HP (?!31)[0-9]+/50' }
    @{ Name = '15-area-event-poison';     Section = '9.4, 16.2';        Args = @('-overworld-place', '3,-0.25');                                               After = 1;   Expect = 'HP (?!31)[0-9]+/50' }
    @{ Name = '16-quest-screen';          Section = '10';               Args = @('-overworld-quest-screen', 'main');                                           After = 0.5; Expect = 'scripted quest screen' }
    @{ Name = '17-book';                  Section = '11';               Args = @('-overworld-book', '1,1');                                                    After = 1.5; Expect = $null }
    @{ Name = '18-weather';               Section = '14.1';             Args = @('-overworld-weather', 'Raining');                                             After = 1.5; Expect = 'weather Raining' }
    @{ Name = '19-battle-stance';         Section = '12.1, 12.4';       Args = @('-overworld-battle', '0');                                                    After = 1;   Expect = 'scripted End' }
    @{ Name = '20-ring-attack';           Section = '12.2, 12.3';       Args = @('-overworld-battle', '0', '-overworld-ring', '1,choose,0.5');                 After = 2.5; Expect = 'scripted ring' }
    @{ Name = '21-ring-skill-flame';      Section = '12.2, 18 (T36)';   Args = @('-overworld-battle', '0', '-overworld-ring', '-2,choose,0.5');                After = 3.5; Expect = 'MP 7/20' }
    @{ Name = '22-hud-options';           Section = '13';               Args = @('-overworld-options', 'main');                                                After = 2;   Expect = $null }
    @{ Name = '23-hero-screen-equipment'; Section = '13, 16.3 (T34)';   Args = @('-overworld-hero-screen');                                                    After = 1;   Expect = $null }
    @{ Name = '24-npcs-met';              Section = '16.2 (T31)';       Args = @('-overworld-place', '1,-0.2');                                                After = 1.5; Expect = $null }
    @{ Name = '25-enemies-met';           Section = '16.2 (T32)';       Args = @('-overworld-place', '0,-0.2');                                                After = 1.5; Expect = $null }
    @{ Name = '26-landscape-props';       Section = '3.4, 16.2 (T29)';  Args = @('-overworld-place', '7,-0.3');                                                After = 1.5; Expect = $null }
    @{ Name = '27-gamepad';               Section = '5.5';              Args = @('-overworld-gamepad', '0.5,0,buttonSouth');                                   After = 2;   Expect = $null }
    @{ Name = '28-audio-regen';           Section = '15 (T26)';         Args = @('-overworld-regen-test');                                                     After = 0.5; Expect = $null }
    @{ Name = '29-fullscreen';            Section = '2.2 (T27)';        Args = @('-overworld-full-screen-test');                                               After = 0.5; Expect = 'full screen test' }
    @{ Name = '30-sprite-pack-belmont';   Section = '4.1, 17 (T35)';    Args = @('-overworld-sprite-pack', 'Belmont');                                         After = 0;   Expect = 'sprite pack Belmont' }
    @{ Name = '31-game-over-reset';       Section = '6.5 (T28)';        Args = @('-overworld-vitals', '0:hp=0;1:hp=0;2:hp=0');                                 After = 5;   Expect = $null }
)

if ($To -le 0) { $To = $scenarios.Count }

$failures = @()
for ($i = $From - 1; $i -le $To - 1; $i++) {
    $s = $scenarios[$i]
    $png = "Logs/verify/$($s.Name).png"
    Write-Host "[$($i + 1)/$($scenarios.Count)] $($s.Name) (RECONSTRUCTION $($s.Section))"
    $extra = @($s.Args)
    if ($s.After -gt 0) { $extra += @('-overworld-capture-after', "$($s.After)") }
    # -Build and -Capture are play.ps1's own named parameters: they must be typed literally, not splatted from an array
    # together with the player's own remaining arguments, or PowerShell's binder mis-threads which value goes where.
    if ($i -eq 0) {
        & "$root\play.ps1" -Build -Capture $png @extra
    } else {
        & "$root\play.ps1" -Capture $png @extra
    }
    $exitCode = $LASTEXITCODE
    $logCopy = "Logs/verify/$($s.Name).log"
    Copy-Item 'Logs/play.log' $logCopy -Force -ErrorAction SilentlyContinue
    $log = Get-Content 'Logs/play.log' -Raw -ErrorAction SilentlyContinue
    $refused = $log -and ([regex]::Matches($log, 'was refused:|wants .*, not ').Count -gt 0)
    $captured = Test-Path $png
    $expected = $true
    if ($s.Expect) { $expected = $log -and ($log -match $s.Expect) }
    if ($exitCode -ne 0 -or -not $captured -or $refused -or -not $expected) {
        $failures += $s.Name
        Write-Host "  FAILED (exit $exitCode, captured $captured, refused $refused, expected-match $expected)" -ForegroundColor Red
    } else {
        Write-Host "  ok -> $png" -ForegroundColor Green
    }
}

Write-Host ''
if ($failures.Count -eq 0) {
    Write-Host "Scenarios $From-$To of $($scenarios.Count) captured and verified cleanly." -ForegroundColor Green
    exit 0
} else {
    Write-Host "$($failures.Count) scenario(s) failed: $($failures -join ', ')" -ForegroundColor Red
    exit 1
}
