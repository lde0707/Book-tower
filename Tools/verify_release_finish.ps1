param([string[]]$Methods=@('ShadowDuetVerifier.Run','ReadingRoomVerifier.RunBatch','ReadingEntranceVerifier.Run','F0PlayVerifier.Run','FoyerTransitionVerifier.Run','OpeningStoryVerifier.RunBatch','RunSettingsVerifier.Run','ReadingMusicVerifier.Run','ReleaseReadingStructureCheck.Run','ReleaseStructureTraversal.Run','ReleaseArtBuild.Run'),[string]$ResultName='release-finish-regressions')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskClone=Join-Path $taskRoot 'Logs/F0ArtValidation'
$taskUnity='C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe'
$taskMethods=$Methods
$taskResults=@()
foreach($taskMethod in $taskMethods){
 $taskLog=Join-Path $taskRoot ('Logs/final-'+$taskMethod+'.log')
 $taskArgs=@('-batchmode','-projectPath',('"'+$taskClone+'"'),'-executeMethod',('BookTower.EditorTools.'+$taskMethod),'-logFile',('"'+$taskLog+'"'))
 $taskStart=Get-Date
 $taskProcess=Start-Process -FilePath $taskUnity -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
 if(-not $taskProcess.WaitForExit(900000)){$taskProcess.Kill();$taskExit=124}else{$taskExit=$taskProcess.ExitCode}
 $taskResults+=[pscustomobject]@{method=$taskMethod;exit=$taskExit;seconds=[math]::Round(((Get-Date)-$taskStart).TotalSeconds,1);log=$taskLog}
 $taskResults | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot ('Logs/'+$ResultName+'.json')) -Encoding utf8
 Write-Output ($taskMethod+' exit='+$taskExit)
}
