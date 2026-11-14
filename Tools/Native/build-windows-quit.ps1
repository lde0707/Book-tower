$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskVswhere = 'C:/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe'
$taskCompiler = & $taskVswhere -latest -products '*' -find 'VC/Tools/MSVC/**/bin/Hostx64/x64/cl.exe' | Select-Object -First 1
if (-not $taskCompiler) { throw 'MSVC x64 compiler not installed' }
$taskVc = (Resolve-Path (Join-Path (Split-Path $taskCompiler -Parent) '../../..')).Path
$taskSdk = 'C:/Program Files (x86)/Windows Kits/10'
$taskVersion = (Get-ChildItem (Join-Path $taskSdk 'Include') -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1).Name
$taskOutput = Join-Path $taskRoot 'Assets/_BookTower/Plugins/x86_64'
$taskLogs = Join-Path $taskRoot 'Logs/TitleScreenV2'
New-Item -ItemType Directory -Path $taskOutput, $taskLogs -Force | Out-Null
$taskCommon = @('/nologo', '/O2', '/W4', '/WX', '/MT',
    ('/I' + (Join-Path $taskVc 'include')),
    ('/I' + (Join-Path $taskSdk "Include/$taskVersion/um")),
    ('/I' + (Join-Path $taskSdk "Include/$taskVersion/shared")),
    ('/I' + (Join-Path $taskSdk "Include/$taskVersion/ucrt")))
$taskLibraries = @('/link', '/NOLOGO', '/INCREMENTAL:NO',
    ('/LIBPATH:' + (Join-Path $taskVc 'lib/x64')),
    ('/LIBPATH:' + (Join-Path $taskSdk "Lib/$taskVersion/um/x64")),
    ('/LIBPATH:' + (Join-Path $taskSdk "Lib/$taskVersion/ucrt/x64")))
$taskDll = Join-Path $taskOutput 'BookTowerWindowsQuit.dll'
& $taskCompiler @taskCommon /LD ('/Fo:' + (Join-Path $taskLogs 'WindowsQuit.obj')) (Join-Path $PSScriptRoot 'WindowsQuit.cpp') ('/Fe:' + $taskDll) @taskLibraries ('/IMPLIB:' + (Join-Path $taskLogs 'WindowsQuit.lib')) User32.lib Comctl32.lib
if ($LASTEXITCODE -ne 0) { throw 'Shutdown guard compilation failed' }
$taskTest = Join-Path $taskLogs 'WindowsQuitTest.exe'
& $taskCompiler @taskCommon ('/Fo:' + (Join-Path $taskLogs 'WindowsQuitTest.obj')) (Join-Path $PSScriptRoot 'WindowsQuitTest.cpp') ('/Fe:' + $taskTest) @taskLibraries User32.lib
if ($LASTEXITCODE -ne 0) { throw 'Shutdown guard test compilation failed' }
& $taskTest $taskDll | Tee-Object -FilePath (Join-Path $taskLogs 'native-checks.txt')
if ($LASTEXITCODE -ne 0) { throw 'Shutdown guard native regression failed' }
