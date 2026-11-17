<#
.SYNOPSIS
    Soak test on a connected Android phone: starts the development build with autoplay, then every
    -IntervalSeconds reads the phone's battery temperature and thermal status over adb, keeps the game's
    "[Soak]" log lines (FPS, memory, textures) and finally pulls the local gameplay log.

.DESCRIPTION
    Needs: a Development APK installed (README, section 9, steps 5-6), USB debugging on, one phone connected.
    Leave the phone alone during the run - a touch would join in with the autoplay. Everything lands in
    Logs\soak_<date>_<time>\: thermal.csv, logcat.txt, soak_lines.txt, gameplay_log.jsonl.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\phone_soak.ps1 -Minutes 15
#>
param(
    [int]$Minutes = 15,
    [string]$Package = "com.zielonawyspagames.zmywak",
    [int]$IntervalSeconds = 30
)

$ErrorActionPreference = "Continue" # adb reports progress on stderr; failures are checked by hand
# adb: the Android SDK from the environment or Android Studio's usual place, else the one bundled with Unity, else PATH.
$adb = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, (Join-Path $env:LOCALAPPDATA "Android\Sdk")) |
    Where-Object { $_ } | ForEach-Object { Join-Path $_ "platform-tools\adb.exe" } | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $adb) { $adb = "C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" }
if (-not (Test-Path $adb)) { $adb = "adb" }
$proj = Split-Path $PSScriptRoot -Parent
$out = Join-Path $proj ("Logs\soak_" + (Get-Date -Format "yyyyMMdd_HHmmss"))

if (-not (Test-Path $adb)) {
    Write-Error "adb not found at $adb (it comes with Unity's Android Build Support module)."
    exit 1
}

$phones = @(& $adb devices | Select-String "`tdevice$")
if ($phones.Count -ne 1) {
    Write-Host "Expected exactly one phone with USB debugging allowed; adb sees $($phones.Count):"
    & $adb devices
    exit 1
}

$installed = & $adb shell pm list packages $Package
if (-not ($installed -match "package:$([regex]::Escape($Package))\s*$")) {
    Write-Error "$Package is not installed on the phone (README, section 9, steps 5-6)."
    exit 1
}

New-Item -ItemType Directory -Force $out | Out-Null
$model = (& $adb shell getprop ro.product.model).Trim()
$android = (& $adb shell getprop ro.build.version.release).Trim()
Write-Host "Soak on $model (Android $android) for $Minutes min -> $out"

& $adb logcat -c
& $adb shell am force-stop $Package
& $adb shell am start -n "$Package/com.unity3d.player.UnityPlayerGameActivity" --ez zmywak_autoplay true --ei zmywak_soak_minutes $Minutes | Out-Null
$logcat = Start-Process -FilePath $adb -ArgumentList @("logcat", "-s", "Unity") -RedirectStandardOutput (Join-Path $out "logcat.txt") -NoNewWindow -PassThru

$csv = Join-Path $out "thermal.csv"
"time,minute,battery_temp_c,battery_level,thermal_status" | Set-Content $csv -Encoding utf8
$start = Get-Date
$end = $start.AddMinutes($Minutes).AddSeconds(30)
$nextShot = 1 # a screenshot from the phone after the first minute, then every 5
while ((Get-Date) -lt $end) {
    $battery = & $adb shell dumpsys battery
    $tempMatch = $battery | Select-String "temperature:\s*(\d+)" | Select-Object -First 1
    $levelMatch = $battery | Select-String "level:\s*(\d+)" | Select-Object -First 1
    $thermalMatch = & $adb shell dumpsys thermalservice | Select-String "Thermal Status:\s*(\d+)" | Select-Object -First 1
    $temp = if ($tempMatch) { [double]$tempMatch.Matches[0].Groups[1].Value / 10 } else { "" }
    $level = if ($levelMatch) { $levelMatch.Matches[0].Groups[1].Value } else { "" }
    $thermal = if ($thermalMatch) { $thermalMatch.Matches[0].Groups[1].Value } else { "" }
    $minute = [math]::Round(((Get-Date) - $start).TotalMinutes, 1)
    "$(Get-Date -Format HH:mm:ss),$minute,$temp,$level,$thermal" | Add-Content $csv -Encoding utf8
    Write-Host ("{0,5} min  battery {1} C  {2}%  thermal status {3}" -f $minute, $temp, $level, $thermal)
    if ($minute -ge $nextShot) {
        # Taken on the phone and pulled: piping screencap through Windows PowerShell 5.1 breaks the PNG.
        $name = "screen_{0:D2}min.png" -f [int]$nextShot
        & $adb shell screencap -p "/sdcard/$name"
        & $adb pull "/sdcard/$name" (Join-Path $out $name) 2>$null | Out-Null
        & $adb shell rm "/sdcard/$name"
        $nextShot += 5
    }

    Start-Sleep -Seconds $IntervalSeconds
}

Stop-Process -Id $logcat.Id -ErrorAction SilentlyContinue
Select-String -Path (Join-Path $out "logcat.txt") -Pattern "\[Soak\]" | ForEach-Object { $_.Line } | Set-Content (Join-Path $out "soak_lines.txt") -Encoding utf8

$remoteLog = "/sdcard/Android/data/$Package/files/gameplay_log.jsonl"
& $adb pull $remoteLog (Join-Path $out "gameplay_log.jsonl") 2>$null
if ($LASTEXITCODE -ne 0) {
    & $adb exec-out run-as $Package cat $remoteLog | Set-Content (Join-Path $out "gameplay_log.jsonl") -Encoding utf8
}

Write-Host ""
Write-Host "Thermal status: 0 none, 1 light, 2 moderate, 3 severe (throttling), 4 critical, 5 emergency, 6 shutdown."
Import-Csv $csv | Format-Table -AutoSize
Write-Host "The game's own samples (FPS, memory):"
Get-Content (Join-Path $out "soak_lines.txt") | Select-Object -Last 6
Write-Host "Everything: $out"
