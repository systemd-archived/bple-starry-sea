using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildAndroidApk
{
	private const string OutputApk = "Builds/新创unity星海901-7.apk";
	private const string AppPackage = "com.star.tech.build";
	private const string AppName = "新创unity星海";

	private const string AndroidPlayerHome =
		@"C:\Program Files\Unity\Hub\Editor\2021.3.45f2c1\Editor\Data\PlaybackEngines\AndroidPlayer";

	private static void EnsureAndroidToolsConfigured()
	{
		string sdk = Path.Combine(AndroidPlayerHome, "SDK");
		string ndk = Path.Combine(AndroidPlayerHome, "NDK");
		string jdk = Path.Combine(AndroidPlayerHome, "OpenJDK");
		EditorPrefs.SetString("AndroidSdkRoot", sdk);
		EditorPrefs.SetString("AndroidNdkRoot", ndk);
		EditorPrefs.SetString("AndroidJdkRoot", jdk);
	}

	[MenuItem("Tools/Build/Build Android APK")]
	public static void Build()
	{
		EnsureAndroidToolsConfigured();

		PlayerSettings.productName = AppName;
		PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AppPackage);
		PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
		PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
		// Mono backend (no code stripping) — settings UI reflection works, build is fast & stable
		PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
		PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Disabled);
		PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
		PlayerSettings.Android.bundleVersionCode = 2022901;
		PlayerSettings.bundleVersion = "2022.1.901";

		EditorUserBuildSettings.buildAppBundle = false;
		EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;

		Directory.CreateDirectory("Builds");
		BuildPlayerOptions options = new BuildPlayerOptions
		{
			scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
			locationPathName = Path.GetFullPath(OutputApk),
			target = BuildTarget.Android,
			options = BuildOptions.None
		};

		EditorUtility.DisplayProgressBar("Build", "Building APK (Mono)...", 0.3f);
		BuildReport report = BuildPipeline.BuildPlayer(options);
		EditorUtility.ClearProgressBar();

		if (report.summary.result == BuildResult.Succeeded)
		{
			Debug.Log($"Build SUCCEEDED, size={report.summary.totalSize / (1024 * 1024)}MB, output={OutputApk}");
		}
		else
		{
			Debug.LogError($"Build FAILED: {report.summary.result}, errors={report.summary.totalErrors}");
		}
	}
}
