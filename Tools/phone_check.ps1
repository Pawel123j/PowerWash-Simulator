<#
.SYNOPSIS
    First look at the game on a connected Android phone: installs the development APK, starts it, takes
    screenshots from the phone and collects the Unity log - errors and exceptions pulled out on top.

.DESCRIPTION
    Needs: USB debugging on, one phone connected, a Development APK (local build or the "Zmywak-dev-apk"
    artifact from GitHub Actions, unzipped). Everything lands in Logs\phone_<date>_<time>\:
    screen_*.png (straight from the phone), logcat.txt, errors.txt.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\phone_check.ps1
    powershell -ExecutionPolicy Bypass -File Tools\phone_check.ps1 -Apk "$HOME\Downloads\Zmywak-dev.apk"
#>
param(
    [string]$Apk = "",
    [string]$Package = "com.zielonawyspagames.zmywak",
    [int]$Seconds = 40
)

$ErrorActionPreference = "Continue" # adb reports progress on stderr; failures are checked by hand
# adb: the Android SDK from the environment or Android Studio's usual place, else the one bundled with Unity, else PATH.
$adb = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, (Join-Path $env:LOCALAPPDATA "Android\Sdk")) |
    Where-Object { $_ } | ForEach-Object { Join-Path $_ "platform-tools\adb.exe" } | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $adb) { $adb = "C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" }
if (-not (Test-Path $adb)) { $adb = "adb" }
$proj = Split-Path $PSScriptRoot -Parent
if ($Apk -eq "") { $Apk = Join-Path $proj "Builds\Android\Zmywak-dev.apk" }
$out = Join-Path $proj ("Logs\phone_" + (Get-Date -Format "yyyyMMdd_HHmmss"))

$phones = @(& $adb devices | Select-String "`tdevice$")
if ($phones.Count -ne 1) {
    Write-Host "Expected exactly one phone with USB debugging allowed; adb sees $($phones.Count):"
    & $adb devices
    exit 1
}

if (-not (Test-Path $Apk)) {
    Write-Error "No APK at $Apk (build it - README section 9 step 5 - or download the Zmywak-dev-apk artifact)."
    exit 1
}

New-Item -ItemType Directory -Force $out | Out-Null
$model = (& $adb shell getprop ro.product.model).Trim()
$android = (& $adb shell getprop ro.build.version.release).Trim()
$size = (& $adb shell wm size) -join " "
$density = (& $adb shell wm density) -join " "
Write-Host "Phone: $model, Android $android, $size, $density"
"Phone: $model, Android $android, $size, $density" | Set-Content (Join-Path $out "phone.txt") -Encoding utf8

Write-Host "Installing $Apk ..."
& $adb install -r $Apk
if ($LASTEXITCODE -ne 0) {
    Write-Error "adb install failed (an older build signed with another key? uninstall it first: adb uninstall $Package)"
    exit 1
}

& $adb logcat -c
& $adb shell am force-stop $Package
& $adb shell monkey -p $Package -c android.intent.category.LAUNCHER 1 | Out-Null
$logcat = Start-Process -FilePath $adb -ArgumentList @("logcat", "-v", "time", "-s", "Unity", "AndroidRuntime", "CRASH", "DEBUG") `
    -RedirectStandardOutput (Join-Path $out "logcat.txt") -NoNewWindow -PassThru

# Screenshots on the phone, then pulled: piping `adb exec-out screencap -p` through Windows PowerShell 5.1
# re-encodes the bytes and breaks the PNG.
$shots = @(10, [math]::Max(20, [int]($Seconds / 2)), $Seconds)
$elapsed = 0
foreach ($at in $shots) {
    Start-Sleep -Seconds ($at - $elapsed)
    $elapsed = $at
    $name = "screen_{0:D2}s.png" -f $at
    & $adb shell screencap -p "/sdcard/$name"
    & $adb pull "/sdcard/$name" (Join-Path $out $name) 2>$null | Out-Null
    & $adb shell rm "/sdcard/$name"
    Write-Host "  $name"
}

Stop-Process -Id $logcat.Id -ErrorAction SilentlyContinue
# Error and fatal log levels (" E/Unity", " F/DEBUG" in -v time format) and anything that names an exception.
$errors = Select-String -Path (Join-Path $out "logcat.txt") -Pattern " [EF]/|Exception" -CaseSensitive |
    ForEach-Object { $_.Line }
$errors | Set-Content (Join-Path $out "errors.txt") -Encoding utf8

$running = (& $adb shell pidof $Package)
Write-Host ""
Write-Host ("Game process: " + $(if ($running) { "running (pid $running)" } else { "NOT RUNNING - see logcat.txt" }))
Write-Host "Errors/exceptions in the Unity log: $(@($errors).Count)"
@($errors) | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
Write-Host "Everything: $out"
