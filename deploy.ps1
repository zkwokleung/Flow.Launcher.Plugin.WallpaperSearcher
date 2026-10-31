$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
dotnet build "$projectRoot\Flow.Launcher.Plugin.WallpaperSearcher.csproj" -c Release

$pluginsDir = Join-Path $env:APPDATA "FlowLauncher\Plugins"
$targetDir = Get-ChildItem $pluginsDir -Directory -Filter "Wallpaper Searcher*" | Select-Object -First 1

if (-not $targetDir) {
    $targetPath = Join-Path $pluginsDir "Wallpaper Searcher-1.0.2"
    New-Item -ItemType Directory -Force -Path $targetPath | Out-Null
} else {
    $targetPath = $targetDir.FullName
}

$flowProc = Get-Process -Name "Flow.Launcher" -ErrorAction SilentlyContinue | Select-Object -First 1
$flowExe = if ($flowProc) { $flowProc.Path } else { $null }
if (-not $flowExe) {
    $candidate = Get-ChildItem (Join-Path $env:LOCALAPPDATA "FlowLauncher") -Filter "Flow.Launcher.exe" -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($candidate) { $flowExe = $candidate.FullName }
}

if ($flowProc) {
    Stop-Process -Name "Flow.Launcher" -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

$outDir = Join-Path $projectRoot "bin\Release"
Copy-Item "$outDir\Flow.Launcher.Plugin.WallpaperSearcher.dll" $targetPath -Force
Copy-Item "$outDir\Flow.Launcher.Plugin.WallpaperSearcher.deps.json" $targetPath -Force
Copy-Item "$outDir\Flow.Launcher.Plugin.WallpaperSearcher.xml" $targetPath -Force
Copy-Item "$projectRoot\Languages\*.xaml" (Join-Path $targetPath "Languages") -Force

if ($flowExe -and (Test-Path $flowExe)) {
    Start-Process $flowExe
}

Write-Host "Deployed successfully to $targetPath"
