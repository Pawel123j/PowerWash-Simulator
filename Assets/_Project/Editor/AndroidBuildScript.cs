using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Zmywak.EditorTools
{
    /// <summary>Development APK build for on-device testing. Run headlessly with -buildTarget Android
    /// -executeMethod Zmywak.EditorTools.AndroidBuildScript.BuildAndroid.</summary>
    public static class AndroidBuildScript
    {
        private const string OutputDir = "Builds/Android";
        private const string ApkFileName = "Zmywak-dev.apk";
        private const string EmulatorApkFileName = "Zmywak-dev-emulator.apk";

        [MenuItem("Zmywak/Build/Build Android (Development APK)")]
        public static void BuildAndroid()
        {
            Build(AndroidArchitecture.ARM64, ApkFileName);
        }

        /// <summary>
        /// The same development build in 32-bit ARM, for an x86 emulator only: its ARM translation (ndk_translation) does not
        /// know the 64-bit SEVL instruction Unity 6.6 spins its thread locks on, and the ARM64 build dies with SIGILL there.
        /// 32-bit ARM has no such instruction. Needs an emulator image that translates 32-bit ARM (Android 11, API 30, x86_64
        /// Google APIs). Phones and testers get the ARM64 build.
        /// </summary>
        [MenuItem("Zmywak/Build/Build Android (Emulator APK, ARMv7)")]
        public static void BuildAndroidEmulator()
        {
            Build(AndroidArchitecture.ARMv7, EmulatorApkFileName);
        }

        private static void Build(AndroidArchitecture architecture, string apkFileName)
        {
            // Without the module Unity only says the target is "unsupported", after a long import: say what to install.
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Zmywak: Android Build Support is not installed for this Unity. Unity Hub > Installs > " +
                               $"{Application.unityVersion} > Add modules > Android Build Support (with OpenJDK and Android SDK & NDK Tools).");
                BuildPreflight.ExitIfHeadless(1);
                return;
            }

            string[] scenes = BuildPreflight.GetScenesOrLogError();
            if (scenes == null)
            {
                BuildPreflight.ExitIfHeadless(1);
                return;
            }

            ClearReadOnlyFlagsInGradleProject();
            DeleteStaleGradleApks();
            Directory.CreateDirectory(OutputDir);
            string apkPath = Path.GetFullPath(Path.Combine(OutputDir, apkFileName));

            // Whatever the project was last left at, the phone gets a game that stays upright. ARM64 only: phones are
            // ARM64, and this Unity no longer builds x86_64 for Android (an emulator runs the ARM64 APK through
            // Android's own translation).
            VerticalSliceSetup.EnsurePortraitOnly();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = architecture;

            // The editor's own build setting: a test APK must not leave an APK where the release steps want an AAB.
            bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;

            BuildSummary summary;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false; // APK, not AAB - quicker for local device testing

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.Development
                };

                summary = BuildPipeline.BuildPlayer(options).summary;
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousAppBundle;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; // the project stays ARM64 for phones
            }

            if (summary.result == BuildResult.Succeeded && File.Exists(apkPath))
            {
                // The report's totalSize counts uncompressed data and debug symbols; the file is what gets installed.
                long apkBytes = new FileInfo(apkPath).Length;
                Debug.Log($"Zmywak: Android build succeeded - {apkPath} ({apkBytes / 1024 / 1024} MB APK, {summary.totalTime}).");
                BuildPreflight.ExitIfHeadless(0);
            }
            else
            {
                Debug.LogError($"Zmywak: Android build failed - result={summary.result}, errors={summary.totalErrors}, " +
                               $"warnings={summary.totalWarnings}, APK {(File.Exists(apkPath) ? "present" : "missing")} at {apkPath}.");
                BuildPreflight.ExitIfHeadless(1);
            }
        }

        /// <summary>
        /// Gradle packages incrementally into the previous APK: changed entries are appended and the old ones
        /// left behind as dead space, so the file grew by 15-20 MB every build (98 MB for 44 MB of content).
        /// Removing the old package makes it write a fresh one; compilation stays incremental.
        /// </summary>
        private static void DeleteStaleGradleApks()
        {
            const string gradleProject = "Library/Bee/Android/Prj";
            if (!Directory.Exists(gradleProject))
            {
                return;
            }

            foreach (string apk in Directory.EnumerateFiles(gradleProject, "*.apk", SearchOption.AllDirectories))
            {
                if (apk.Replace('\\', '/').Contains("/build/outputs/apk/"))
                {
                    File.Delete(apk);
                }
            }
        }

        /// <summary>
        /// Workaround for the project living inside a OneDrive folder: OneDrive turns synced folders into
        /// read-only cloud placeholders, and on Windows Gradle (Java) cannot delete a read-only directory,
        /// so every incremental build after the first failed in :unityLibrary:mergeDebugNativeLibs with
        /// AccessDeniedException. Only touches Unity's own regenerable Gradle project under Library/.
        /// The real fix is keeping the project (or at least Library/) out of OneDrive - see README.
        /// </summary>
        private static void ClearReadOnlyFlagsInGradleProject()
        {
            const string gradleProject = "Library/Bee/Android/Prj";
            if (!Directory.Exists(gradleProject))
            {
                return;
            }

            int cleared = 0;
            foreach (string dir in Directory.EnumerateDirectories(gradleProject, "*", SearchOption.AllDirectories))
            {
                var info = new DirectoryInfo(dir);
                if ((info.Attributes & FileAttributes.ReadOnly) != 0)
                {
                    info.Attributes &= ~FileAttributes.ReadOnly;
                    cleared++;
                }
            }

            if (cleared > 0)
            {
                Debug.Log($"Zmywak: cleared the read-only flag (set by OneDrive sync) on {cleared} folders of the Gradle project.");
            }
        }
    }

#if UNITY_ANDROID
    /// <summary>
    /// After Unity writes the Gradle project of a build started by Tools\build_android.ps1 on Windows (it sets
    /// ZMYWAK_GRADLE_WINDOWS_TRUST=1): Gradle checks HTTPS certificates against the Windows certificate store instead of
    /// Java's own list. Antivirus HTTPS scanning (Norton Web/Mail Shield on the desktop PC) re-signs Gradle's downloads
    /// with a root only Windows trusts, and Java refused them ("PKIX path building failed"). Unity strips JAVA_TOOL_OPTIONS
    /// from Gradle's environment, so the setting goes into the generated gradle.properties. Certificates are still
    /// checked; other machines and the cloud build (no flag) are untouched.
    /// </summary>
    public class GradleWindowsTrustStore : UnityEditor.Android.IPostGenerateGradleAndroidProject
    {
        private const string Line = "systemProp.javax.net.ssl.trustStoreType=Windows-ROOT";

        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor ||
                System.Environment.GetEnvironmentVariable("ZMYWAK_GRADLE_WINDOWS_TRUST") != "1")
            {
                return;
            }

            string properties = Path.GetFullPath(Path.Combine(path, "..", "gradle.properties"));
            if (!File.Exists(properties) || File.ReadAllText(properties).Contains(Line))
            {
                return;
            }

            File.AppendAllText(properties, "\n" + Line + "\n");
            Debug.Log($"Zmywak: Gradle checks certificates against the Windows store ({properties}).");
        }
    }
#endif
}
