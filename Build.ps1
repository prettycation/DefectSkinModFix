param(
    [string]$GameDir = $env:STS2_DIR
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($GameDir)) {
    throw 'Game directory not set. Pass -GameDir, set STS2_DIR, or configure GAME_DIR in local.mk.'
}
$dataDir = Join-Path $GameDir 'data_sts2_windows_x86_64'
foreach ($required in @('sts2.dll', '0Harmony.dll', 'GodotSharp.dll')) {
    if (!(Test-Path (Join-Path $dataDir $required))) {
        throw "Game assembly missing: $(Join-Path $dataDir $required)"
    }
}
$dotnetExe = $null
if (Get-Command scoop -ErrorAction SilentlyContinue) {
    $originalErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $sdkDir = [string](& scoop prefix dotnet9-sdk 2>$null | Select-Object -Last 1)
        if ($LASTEXITCODE -eq 0 -and ![string]::IsNullOrWhiteSpace($sdkDir)) {
            $scoopDotnet = Join-Path $sdkDir.Trim() 'dotnet.exe'
            if (Test-Path -LiteralPath $scoopDotnet -PathType Leaf) {
                $dotnetExe = $scoopDotnet
            }
        }
    } catch {
        # Use dotnet from PATH if Scoop cannot resolve the SDK.
    } finally {
        $ErrorActionPreference = $originalErrorActionPreference
    }
}
if (!$dotnetExe) {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if (!$dotnetCommand) {
        throw '.NET 9 SDK not found. Install it and make dotnet available in PATH.'
    }
    $dotnetExe = $dotnetCommand.Source
}
$buildOutput = Join-Path $root 'BuildOutput'
$project = Join-Path $root 'Source\DefectSkinModFix.csproj'
& $dotnetExe build $project -c Release -p:Sts2Dir="$GameDir" -o $buildOutput
if ($LASTEXITCODE -ne 0) { throw "Build failed ($LASTEXITCODE)." }
$dll = Join-Path $buildOutput 'DefectSkinModFix.dll'
if (!(Test-Path $dll)) { throw "DLL not produced: $dll" }
$dist = Join-Path $root 'Dist\DefectSkinModFix'
$workshop = Join-Path $root 'WorkshopWorkspace\content'
New-Item -ItemType Directory -Force $dist, $workshop | Out-Null
foreach ($dest in @($dist, $workshop)) {
    Copy-Item $dll (Join-Path $dest 'DefectSkinModFix.dll') -Force
    Copy-Item (Join-Path $root 'Source\DefectSkinModFix.json') (Join-Path $dest 'DefectSkinModFix.json') -Force
}
Write-Host "Build staged: $dist"
Write-Host "Workshop workspace content staged: $workshop"
