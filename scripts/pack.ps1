param(
    [string]$GameDir
)

$ErrorActionPreference = "Stop"

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "src\PitOfGoblinCheats\PitOfGoblinCheats.csproj"

$buildArgs = @("build", $project, "-c", "Release")
if ($GameDir) { $buildArgs += "-p:GameDir=$GameDir" }

dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Select-Object -First 1
$dist = Join-Path $root "dist"
$stage = Join-Path $dist "stage"
$zip = Join-Path $dist "PitOfGoblinCheats-v$version.zip"

Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force (Join-Path $stage "BepInEx\plugins") | Out-Null

Copy-Item (Join-Path $root "src\PitOfGoblinCheats\bin\Release\PitOfGoblinCheats.dll") (Join-Path $stage "BepInEx\plugins")
Copy-Item (Join-Path $root "LICENSE") $stage

Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
Remove-Item $zip -ErrorAction SilentlyContinue

$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem $stage -Recurse -File | ForEach-Object {
        $name = $_.FullName.Substring($stage.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $name) | Out-Null
    }
}
finally {
    $archive.Dispose()
}

Remove-Item $stage -Recurse -Force

Write-Host "Done: $zip"
