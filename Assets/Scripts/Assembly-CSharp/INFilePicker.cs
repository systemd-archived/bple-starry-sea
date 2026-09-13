using System;
using System.Collections;
using UnityEngine;

// 跨平台文件选择：Windows/Linux 用 ShellFileDialogs；Android 用系统文件选择器(ACTION_OPEN_DOCUMENT)，
// 由 Android 侧 FileDialogBridge 复制选中文件到缓存目录，Unity 协程轮询结果后回调。
public static class INFilePicker
{
	public static void PickFile(string filterDesc, string filterExt, Action<string> onPicked)
	{
		if (onPicked == null)
			return;
#if UNITY_ANDROID && !UNITY_EDITOR
		PickAndroid(filterDesc, filterExt, onPicked);
#else
		PickDesktop(filterDesc, filterExt, onPicked);
#endif
	}

#if UNITY_ANDROID && !UNITY_EDITOR
	private static void PickAndroid(string filterDesc, string filterExt, Action<string> onPicked)
	{
		try
		{
			string mime = "*/*";
			if (!string.IsNullOrEmpty(filterExt))
			{
				mime = filterExt.ToLowerInvariant() switch
				{
					"*.png" => "image/png",
					"*.jpg" or "*.jpeg" => "image/jpeg",
					"*.bmp" => "image/bmp",
					"*.tga" => "image/x-tga",
					"*.gif" => "image/gif",
					"*.mp4" => "video/mp4",
					"*.mov" => "video/quicktime",
					"*.webm" => "video/webm",
					"*.m4v" => "video/x-m4v",
					"*.avi" => "video/x-msvideo",
					"*" or "*.*" => "*/*",
					_ => "*/*",
				};
			}
			using (AndroidJavaClass bridge = new AndroidJavaClass("com.innovation.filedialog.FileDialogBridge"))
			{
				bridge.CallStatic("SetMimeType", mime);
				bridge.CallStatic("OpenFile");
			}
			CoroutineRunner.Instance.StartCoroutine(PollAndroidResult(onPicked));
		}
		catch (Exception ex)
		{
			Debug.LogError("INFilePicker(PickFile) android: " + ex.Message);
			onPicked(null);
		}
	}

	private static IEnumerator PollAndroidResult(Action<string> onPicked)
	{
		float time = 0f;
		const float timeout = 120f;
		bool done = false;
		string result = null;
		AndroidJavaClass bridge = null;
		try
		{
			bridge = new AndroidJavaClass("com.innovation.filedialog.FileDialogBridge");
			while (!done && time < timeout)
			{
				yield return null;
				time += Time.unscaledDeltaTime;
				try
				{
					if (bridge.CallStatic<bool>("IsReady"))
					{
						result = bridge.CallStatic<string>("GetResult");
						done = true;
					}
				}
				catch
				{
					done = true;
				}
			}
		}
		finally
		{
			if (bridge != null)
				bridge.Dispose();
		}
		if (onPicked != null)
			onPicked(string.IsNullOrEmpty(result) ? null : result);
	}
#else
	private static void PickDesktop(string filterDesc, string filterExt, Action<string> onPicked)
	{
		try
		{
			ShellFileDialogs.Filter[] filters = new ShellFileDialogs.Filter[1]
			{
				new ShellFileDialogs.Filter(filterDesc, filterExt)
			};
			string path = ShellFileDialogs.FileOpenDialog.ShowSingleSelectDialog(IntPtr.Zero, string.Empty, UnityEngine.Application.dataPath, string.Empty, filters, 0);
			onPicked(string.IsNullOrEmpty(path) ? null : path);
		}
		catch (Exception ex)
		{
			Debug.LogError("INFilePicker(PickFile) desktop: " + ex.Message);
			onPicked(null);
		}
	}
#endif
}
