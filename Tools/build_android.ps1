<#
.SYNOPSIS
    One command: builds the test APK (development build, IL2CPP, ARM64, portrait) with Unity in batch mode, and
    says where the file is. Optionally installs it on a connected phone or the emulator and starts it.

.DESCRIPTION
    Finds the Unity version the project is pinned to (ProjectSettings\ProjectVersion.txt) in the usual Unity Hub
    folders - and in the Hub's own "install location" setting - and checks that it has Android Build Support
    before starting the (long) build. The APK lands in <project>\Builds\Android\Zmywak-dev.apk and the Unity
    log in <project>\Logs\build_android.log.

    Needs: Unity of the pinned version with the Android Build Support module (Unity Hub > Installs > the editor >
    Add modules: Android Build Support, with OpenJDK and Android SDK & NDK Tools); the project NOT open in the
    Unity Editor at the same time.

.PARAMETER Project
    The Unity project folder. Default: the folder above Tools. Where the project lives under a path with
    non-ASCII characters (a user name like "Pawel" with a Polish letter), or in OneDrive with cloud-only
    files, build a local copy instead: -Project E:\Dev\Zmywak.

.PARAMETER Install
    After a successful build: adb install -r, then starts the game. Needs one phone with USB debugging allowed,
    or a running emulator.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\build_android.ps1
    powershell -ExecutionPolicy Bypass -File Tools\build_android.ps1 -Install
    powershell -ExecutionPolicy Bypass -File Tools\build_android.ps1 -Project E:\Dev\Zmywak
#>
param(
    [string]$Project = "",
    [string]$Unity = "",
    [switch]$Install,
    [switch]$Emulator,
    [string]$Log = ""
)

$ErrorActionPreference = "Stop"
$package = "com.zielonawyspagames.zmywak"
if ($Project -eq "") { $Project = Split-Path $PSScriptRoot -Parent }
$Project = (Resolve-Path $Project).Path
if (-not (Test-Path (Join-Path $Project "ProjectSettings\ProjectVersion.txt"))) {
    Write-Host "$Project is not a Unity project (no ProjectSettings\ProjectVersion.txt)."
    exit 2
}

# --- which Unity -----------------------------------------------------------------------------------------------
$version = ((Get-Content (Join-Path $Project "ProjectSettings\ProjectVersion.txt") | Select-Object -First 1) -replace "m_EditorVersion:\s*", "").Trim()
if ($Unity -eq "") {
    $roots = @()
    if ($env:UNITY_EXE) { $roots += $env:UNITY_EXE }
    $secondary = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path $secondary) {
        $hubFolder = (Get-Content $secondary -Raw | ConvertFrom-Json)
        if ($hubFolder) { $roots += (Join-Path $hubFolder "$version\Editor\Unity.exe") }
    }
    $roots += "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
    $Unity = $roots | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
}
if (-not $Unity -or -not (Test-Path $Unity)) {
    Write-Host "Unity $version was not found. Install it with Unity Hub (the project is pinned to it), or pass -Unity <path to Unity.exe>."
    exit 2
}

$androidPlayer = Join-Path (Split-Path $Unity -Parent) "Data\PlaybackEngines\AndroidPlayer"
if (-not (Test-Path $androidPlayer)) {
    Write-Host "Unity $version at $Unity has no Android Build Support."
    Write-Host "Unity Hub > Installs > $version > (gear) > Add modules > Android Build Support, ticking OpenJDK and"
    Write-Host "Android SDK & NDK Tools as well (about 2.4 GB to download; Windows asks to elevate once - answer Yes)."
    exit 2
}

if ($Project -match "[^\x00-\x7F]") {
    Write-Host "Note: $Project has non-ASCII characters. If Unity exits at once (exit code 127, no log), build a local copy: -Project E:\Dev\Zmywak"
}

# Java 17 (Gradle) on Windows makes its internal loopback "pipe" as a Unix-domain socket in the temp folder, and fails
# with "Unable to establish loopback connection" when that path has non-ASCII letters - a user folder like the one with
# a Polish letter. Such a TEMP is swapped, for this build only, for an ASCII folder in the project's Library. The user
# folder is checked too: TEMP is often its 8.3 short form (PAWE~1), which looks ASCII but Java expands to the real path.
$javaOptions = @()
if ("$env:TEMP$env:TMP$env:USERPROFILE" -match "[^\x00-\x7F]") {
    $buildTemp = Join-Path $Project "Library\BuildTemp"
    New-Item -ItemType Directory -Force $buildTemp | Out-Null
    $env:TEMP = $buildTemp
    $env:TMP = $buildTemp
    $javaOptions += "-Djava.io.tmpdir=$buildTemp", "-Djdk.net.unixdomain.tmpdir=$buildTemp"
    Write-Host "Temp folder   : $buildTemp (the user's TEMP has non-ASCII characters, which Gradle cannot use)"
    # Gradle's own cache lives in the user folder too, and the NDK's prefab step runs Java from a .bat file whose
    # class path to that cache comes out garbled (the Polish letter in mojibake) - ClassNotFoundException. An ASCII
    # Gradle home instead.
    if (-not $env:GRADLE_USER_HOME -or $env:GRADLE_USER_HOME -match "[^\x00-\x7F]") {
        $env:GRADLE_USER_HOME = Join-Path $Project "Library\GradleHome"
        Write-Host "Gradle home   : $env:GRADLE_USER_HOME"
    }
}

# Gradle fetches the Android build tools over HTTPS and checks the certificates against Java's own list. Antivirus HTTPS
# scanning (Norton Web/Mail Shield on the desktop PC) re-signs those connections with a root only Windows knows, and Java
# refuses them ("PKIX path building failed"). Java is told to trust what Windows trusts; certificates are still checked.
# Unity strips JAVA_TOOL_OPTIONS from Gradle's environment, so the build hook GradleWindowsTrustStore (AndroidBuildScript.cs)
# writes it into the generated gradle.properties when this flag is set.
$javaOptions += "-Djavax.net.ssl.trustStoreType=Windows-ROOT"
$env:JAVA_TOOL_OPTIONS = ($javaOptions -join " ")
$env:ZMYWAK_GRADLE_WINDOWS_TRUST = "1"

# --- the build -------------------------------------------------------------------------------------------------
$method = "BuildAndroid"
$apkName = "Zmywak-dev.apk"
if ($Emulator) {
    # 32-bit ARM for an x86 emulator (its ARM translation cannot run Unity 6.6's 64-bit build); phones get the ARM64 one.
    $method = "BuildAndroidEmulator"
    $apkName = "Zmywak-dev-emulator.apk"
}
if ($Log -eq "") { $Log = Join-Path $Project "Logs\build_android.log" }
New-Item -ItemType Directory -Force (Split-Path $Log -Parent) | Out-Null
Remove-Item $Log -ErrorAction SilentlyContinue # a finished build's log must not read as this one being done

Write-Host "Unity $version : $Unity"
Write-Host "Project       : $Project"
Write-Host "Building      : $apkName (the first IL2CPP build takes 10-40 minutes, later ones are incremental)"
Write-Host "Log           : $Log"

$unityArgs = @("-batchmode", "-nographics", "-projectPath", "`"$Project`"", "-buildTarget", "Android",
    "-executeMethod", "Zmywak.EditorTools.AndroidBuildScript.$method", "-logFile", "`"$Log`"")
$watch = [Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath $Unity -ArgumentList $unityArgs -PassThru

# Unity sometimes finishes and then hangs on the way out: once the log says it is done, give it a minute, then stop it.
$doneSince = $null
while (-not $process.HasExited) {
    Start-Sleep -Seconds 3
    if (-not $doneSince -and (Test-Path $Log) -and (Select-String -Path $Log -Pattern "Zmywak: Android build (succeeded|failed)|Exiting batchmode" -Quiet)) {
        $doneSince = Get-Date
    }
    if ($doneSince -and ((Get-Date) - $doneSince).TotalSeconds -gt 60) {
        Write-Host "Unity finished but did not exit within 60 s - stopping it."
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        break
    }
}
Write-Host ("Unity exit code {0} after {1:N0} s" -f $process.ExitCode, $watch.Elapsed.TotalSeconds)

$success = if (Test-Path $Log) {
    Select-String -Path $Log -Pattern "Zmywak: Android build succeeded - (.+) \((\d+) MB APK" | Select-Object -Last 1
}
if (-not $success) {
    Write-Host ""
    Write-Host "BUILD FAILED. The first problems in the log:"
    if (Test-Path $Log) {
        Select-String -Path $Log -Pattern "error CS|Zmywak: .*(failed|not installed|no scenes|Boot|Active Input)|Error building Player|BuildFailedException|Exception:" |
            Select-Object -First 15 | ForEach-Object { Write-Host "  $($_.Line.Trim())" }
    } else {
        Write-Host "  (no log was written - see the note about non-ASCII paths above)"
    }
    exit 1
}

$apk = $success.Matches[0].Groups[1].Value
Write-Host ""
Write-Host "BUILD OK: $apk ($($success.Matches[0].Groups[2].Value) MB)"

# --- install and start -----------------------------------------------------------------------------------------
if ($Install) {
    $adbCandidates = @()
    foreach ($sdk in @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, (Join-Path $env:LOCALAPPDATA "Android\Sdk"))) {
        if ($sdk) { $adbCandidates += (Join-Path $sdk "platform-tools\adb.exe") }
    }
    $adbCandidates += (Join-Path $androidPlayer "SDK\platform-tools\adb.exe")
    $adb = $adbCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $adb) { $adb = "adb" }

    $devices = @(& $adb devices | Select-String "`tdevice$")
    if ($devices.Count -ne 1) {
        Write-Host "Not installed: adb needs exactly one phone (USB debugging allowed) or emulator; it sees $($devices.Count)."
        & $adb devices
        exit 1
    }

    & $adb install -r $apk
    if ($LASTEXITCODE -ne 0) {
        Write-Host "adb install failed (an older build signed with another key? adb uninstall $package, then again)."
        exit 1
    }

    & $adb logcat -c
    & $adb shell monkey -p $package -c android.intent.category.LAUNCHER 1 | Out-Null
    Write-Host "Installed and started. The log: adb logcat -s Unity   (or Tools\phone_check.ps1 for screenshots and errors)"
}
exit 0
