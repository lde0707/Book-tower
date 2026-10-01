param([string]$ExpectedSceneHash='AC325294E53EBAE2C67B7F1B3AF28E6E448AAB74C959C947C995E0AF33BB9C7F')
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$taskClone=Join-Path $taskRoot 'Logs/F0ArtValidation'
$taskScene='Assets/Scenes/InGame.unity'
if((Get-FileHash -LiteralPath (Join-Path $taskRoot $taskScene)).Hash -ne $ExpectedSceneHash){throw 'Main scene changed since the verified starting snapshot. Preserve it before promotion.'}
$taskFiles=[Collections.Generic.List[string]]::new()
$taskFiles.Add('Assets/_BookTower/Generated/ReleaseFinish.meta')
Get-ChildItem -LiteralPath (Join-Path $taskClone 'Assets/_BookTower/Generated/ReleaseFinish') -Recurse -File | ForEach-Object {$taskFiles.Add([IO.Path]::GetRelativePath($taskClone,$_.FullName).Replace('\','/'))}
$taskSources=@(
 'Editor/ReleaseArtBuild.cs','Editor/ReleaseArtFinishBuilder.cs','Editor/ReleaseFinishAudit.cs','Editor/ReleaseFinishReview.cs',
 'Editor/OpeningStoryVerifier.cs','Editor/ReleaseLightingReview.cs',
 'Scripts/Camera/PaperCamera.cs','Scripts/Presentation/FloatingPage.cs','Scripts/Presentation/PaperRoomMotion.cs','Scripts/Presentation/ReleaseArtBuildProbe.cs',
 'Scripts/Puzzle/ShadowDuet.cs','Scripts/Puzzle/StorybookLightPuzzle.cs','Scripts/Rooms/StorybookPaperMotion.cs','Scripts/UI/PaperSettingsRuntime.cs',
 'Resources/ReleaseProjectionCookie.asset'
)
foreach($taskFile in $taskSources){$taskFiles.Add('Assets/_BookTower/'+$taskFile);$taskFiles.Add('Assets/_BookTower/'+$taskFile+'.meta')}
# Dependencies first, scene last. Do not copy clone-only PlayerSettings or Unity's build-prefilter caches.
$taskFiles.Add($taskScene)
$taskFiles=@($taskFiles | Select-Object -Unique)
foreach($taskRelative in $taskFiles){
 $taskSource=[IO.Path]::GetFullPath((Join-Path $taskClone $taskRelative))
 $taskTarget=[IO.Path]::GetFullPath((Join-Path $taskRoot $taskRelative))
 if(-not $taskSource.StartsWith($taskClone+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or -not $taskTarget.StartsWith($taskRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Promotion path escaped its workspace'}
 if(-not (Test-Path -LiteralPath $taskSource -PathType Leaf)){throw ('Missing verified dependency: '+$taskRelative)}
 if($taskRelative.EndsWith('.cs') -and (Test-Path -LiteralPath $taskTarget) -and ((Get-FileHash -LiteralPath $taskTarget).Hash -ne (Get-FileHash -LiteralPath $taskSource).Hash)){throw ('Main and validated source differ: '+$taskRelative)}
}
$taskBackup=Join-Path $taskRoot ('Logs/ReleaseFinishMainBackup-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $taskBackup -Force | Out-Null
$taskEntries=@()
foreach($taskRelative in $taskFiles){
 $taskSource=Join-Path $taskClone $taskRelative
 $taskTarget=Join-Path $taskRoot $taskRelative
 $taskOldHash=$null
 if(Test-Path -LiteralPath $taskTarget){
  $taskOldHash=(Get-FileHash -LiteralPath $taskTarget).Hash
  $taskOldCopy=Join-Path $taskBackup $taskRelative
  New-Item -ItemType Directory -Path (Split-Path $taskOldCopy -Parent) -Force | Out-Null
  Copy-Item -LiteralPath $taskTarget -Destination $taskOldCopy
 }
 New-Item -ItemType Directory -Path (Split-Path $taskTarget -Parent) -Force | Out-Null
 $taskVerifiedHash=(Get-FileHash -LiteralPath $taskSource).Hash
 if($taskOldHash -ne $taskVerifiedHash){Copy-Item -LiteralPath $taskSource -Destination $taskTarget -Force}
 $taskAppliedHash=(Get-FileHash -LiteralPath $taskTarget).Hash
 if($taskAppliedHash -ne $taskVerifiedHash){throw ('Applied bytes differ: '+$taskRelative)}
 $taskEntries+=[pscustomobject]@{path=$taskRelative;beforeSha256=$taskOldHash;verifiedSha256=$taskVerifiedHash;appliedSha256=$taskAppliedHash;bytes=(Get-Item -LiteralPath $taskTarget).Length}
}
$taskOut=Join-Path $taskRoot 'Docs/ArtReview/ReleaseArt/08-Final'
New-Item -ItemType Directory -Path $taskOut -Force | Out-Null
$taskManifest=[pscustomobject]@{appliedAt=(Get-Date -Format o);source=$taskClone;backup=$taskBackup;sceneBeforeSha256=$ExpectedSceneHash;files=$taskEntries;fileCount=$taskEntries.Count;verified=$true}
$taskManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskOut 'asset-manifest.json') -Encoding utf8
$taskManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskBackup 'promotion-manifest.json') -Encoding utf8
Write-Output ('Promoted and SHA-256 verified '+$taskEntries.Count+' files; backup '+$taskBackup)
