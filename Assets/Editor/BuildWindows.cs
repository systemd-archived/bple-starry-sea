using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWindows
{
	private const string OutputDir = "Builds/Windows";
	private const string AppName = "新创unity星海";

	[MenuItem("Tools/Build/Build Windows")]
	public static void Build()
	{
		PlayerSettings.productName = AppName;
		PlayerSettings.bundleVersion = "2022.1.901";

		Directory.CreateDirectory(OutputDir);
		BuildPlayerOptions options = new BuildPlayerOptions
		{
			scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
			locationPathName = Path.Combine(Path.GetFullPath(OutputDir), "新创unity星海901-7.exe"),
			target = BuildTarget.StandaloneWindows64,
			options = BuildOptions.None
		};

		EditorUtility.DisplayProgressBar("Build", "Building Windows player...", 0.3f);
		BuildReport report = BuildPipeline.BuildPlayer(options);
		EditorUtility.ClearProgressBar();

		if (report.summary.result == BuildResult.Succeeded)
		{
			Debug.Log($"Windows Build SUCCEEDED, size={report.summary.totalSize / (1024 * 1024)}MB, output={OutputDir}");
		}
		else
		{
			Debug.LogError($"Windows Build FAILED: {report.summary.result}, errors={report.summary.totalErrors}");
		}
	}
}
