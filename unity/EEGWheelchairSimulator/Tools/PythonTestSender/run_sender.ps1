# Temporary test tools v0.1/v0.2. Does not install Python or change system settings.
param(
    [int]$Port = 5055,
    [switch]$Stream,
    [double]$Interval = 1.0,
    [int]$Count = 0
)
$ErrorActionPreference = 'Stop'
$candidates = @()
if (Get-Command py -ErrorAction SilentlyContinue) {
    $candidates += @{ Exe = (Get-Command py).Source; Prefix = @('-3'); Label = 'py -3' }
}
if (Get-Command python -ErrorAction SilentlyContinue) {
    $candidates += @{ Exe = (Get-Command python).Source; Prefix = @(); Label = 'python' }
}
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
foreach ($venv in @((Join-Path $project '.venv\Scripts\python.exe'), (Join-Path $project 'venv\Scripts\python.exe'), (Join-Path $PSScriptRoot '.venv\Scripts\python.exe'))) {
    if (Test-Path -LiteralPath $venv) { $candidates += @{ Exe = $venv; Prefix = @(); Label = 'project venv' } }
}
$bundled = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
if (Test-Path -LiteralPath $bundled) { $candidates += @{ Exe = $bundled; Prefix = @(); Label = 'Codex internal Python (temporary fallback, not the final team environment)' } }
foreach ($candidate in $candidates) {
    $prefix = @($candidate.Prefix)
    $available = $false
    try { $null = & $candidate.Exe @prefix -c 'import sys; sys.exit(0 if sys.version_info >= (3, 6) else 1)' 2>$null; $available = ($LASTEXITCODE -eq 0) } catch { }
    if (-not $available) { continue }
    Write-Host ('Python environment: ' + $candidate.Label)
    Write-Host ('Executable: ' + $candidate.Exe)
    $scriptName = if ($Stream) { 'stream_sender.py' } else { 'send_commands.py' }
    $senderArgs = @((Join-Path $PSScriptRoot $scriptName), '--port', $Port.ToString())
    if ($Stream) { $senderArgs += @('--interval', $Interval.ToString([Globalization.CultureInfo]::InvariantCulture), '--count', $Count.ToString()) }
    & $candidate.Exe @prefix @senderArgs
    exit $LASTEXITCODE
}
Write-Host 'Python 3 was not found. Use your team Python environment to run send_commands.py or stream_sender.py. Nothing was installed.'
exit 1
