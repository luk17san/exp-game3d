param([ValidateSet('Setup','Verify','Build')][string]$Action='Verify')
$ErrorActionPreference='Stop'
$combatRoot=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$combatUnity='C:\Program Files\Unity\Hub\Editor\2022.3.6f1\Editor\Unity.exe'
$combatMethod=switch($Action){ 'Setup' {'CombatV2Setup.Apply'} 'Verify' {'CombatV2Verification.PrepareAndRun'} 'Build' {'CombatV2Verification.Build'} }
$combatLog=Join-Path $PSScriptRoot "Results/$($Action.ToLowerInvariant()).log"
$combatArguments="-batchmode -nographics -projectPath `"$combatRoot`" -executeMethod $combatMethod -logFile `"$combatLog`""
if($Action -ne 'Verify') { $combatArguments+=' -quit' }
$combatProcess=Start-Process -FilePath $combatUnity -ArgumentList $combatArguments -WindowStyle Hidden -PassThru
if(-not $combatProcess.WaitForExit(600000)) { throw "Unity timed out (PID $($combatProcess.Id)). Inspect $combatLog before stopping the process." }
Write-Output "Unity exit: $($combatProcess.ExitCode); log: $combatLog"
if($combatProcess.ExitCode -ne 0) { exit $combatProcess.ExitCode }
exit 0
