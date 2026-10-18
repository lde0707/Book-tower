param([switch]$Apply)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$logsRoot = [IO.Path]::GetFullPath((Join-Path $workspace 'Logs'))
if ((Get-Item -LiteralPath $logsRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Logs must not be a linked directory' }
# These are historical recovery snapshots, never current editor logs or source assets.
$folders = @('AtmosphereBackups', 'MergeRecovery', 'CollisionFit', 'GravityMigration')
foreach ($name in $folders) {
    $source = [IO.Path]::GetFullPath((Join-Path $logsRoot $name))
    if (-not $source.StartsWith($logsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Backup path escaped Logs' }
    if (-not (Test-Path -LiteralPath $source -PathType Container)) { continue }
    if ((Get-Item -LiteralPath $source).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Linked backup directory: $source" }
    $files = @(Get-ChildItem -LiteralPath $source -Recurse -File -Force)
    if (@(Get-ChildItem -LiteralPath $source -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw "Reparse point in $source" }
    $bytes = ($files | Measure-Object Length -Sum).Sum
    if (-not $Apply) { Write-Output "$name : $bytes bytes; rerun with -Apply to archive and verify"; continue }
    $destination = Join-Path $logsRoot ($name + '.verified.zip')
    if (Test-Path -LiteralPath $destination) { throw "Archive already exists: $destination. No source was removed." }
    [IO.Compression.ZipFile]::CreateFromDirectory($source, $destination, [IO.Compression.CompressionLevel]::Optimal, $false)
    $archive = [IO.Compression.ZipFile]::OpenRead($destination)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $entries = @($archive.Entries | Where-Object { $_.Name -ne '' })
        if ($entries.Count -ne $files.Count) { throw "Archive entry count mismatch: $name" }
        foreach ($file in $files) {
            $relative = $file.FullName.Substring($source.Length + 1).Replace('\', '/')
            $entry = $archive.GetEntry($relative)
            if (-not $entry -or $entry.Length -ne $file.Length) { throw "Archive size mismatch: $relative" }
            $original = [IO.File]::OpenRead($file.FullName)
            $compressed = $entry.Open()
            try {
                $a = [Convert]::ToBase64String($sha.ComputeHash($original))
                $b = [Convert]::ToBase64String($sha.ComputeHash($compressed))
                if ($a -ne $b) { throw "Archive hash mismatch: $relative" }
            } finally { $original.Dispose(); $compressed.Dispose() }
        }
    } finally { $sha.Dispose(); $archive.Dispose() }
    # Delete only the verified source within the fixed workspace Logs allowlist.
    Remove-Item -LiteralPath $source -Recurse -Force
    $packed = (Get-Item -LiteralPath $destination).Length
    Write-Output "VERIFIED $name : $($files.Count) files, $bytes -> $packed bytes, saved $($bytes-$packed) bytes"
}
