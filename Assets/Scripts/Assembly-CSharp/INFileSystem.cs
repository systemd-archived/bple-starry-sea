using System;
using System.IO;
using UnityEngine;

public static class INFileSystem
{
	private static string m_root;

	// Ensure MANAGE_EXTERNAL_STORAGE (All Files Access) is granted so writes to the
	// public Documents folder succeed on Android 11+ (Scoped Storage). Called from Root.
	public static void EnsureAndroidStoragePermission()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		try
		{
			using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
			using (AndroidJavaClass settings = new AndroidJavaClass("android.provider.Settings"))
			using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				bool hasAllFilesAccess = environment.CallStatic<bool>("isExternalStorageManager");
				if (hasAllFilesAccess)
				{
					return;
				}

				// Intent(Settings.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION, Uri.parse("package:"+pkg))
				using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", settings.GetStatic<string>("ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION")))
				using (AndroidJavaClass uri = new AndroidJavaClass("android.net.Uri"))
				{
					string packageName = activity.Call<string>("getPackageName");
					using (AndroidJavaObject uriObj = uri.CallStatic<AndroidJavaObject>("parse", "package:" + packageName))
					{
						intent.Call<AndroidJavaObject>("setData", uriObj);
						activity.Call("startActivity", intent);
					}
					Debug.Log("Requested MANAGE_EXTERNAL_STORAGE (All Files Access) permission.");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Could not ensure MANAGE_EXTERNAL_STORAGE: " + ex.Message);
		}
#else
#endif
	}

	public static string Root
	{
		get
		{
			m_root = GetDefaultRoot();
			if (!string.IsNullOrEmpty(m_root) && !Directory.Exists(m_root))
			{
				Directory.CreateDirectory(m_root);
			}
			return m_root;
		}
	}

	private static string GetDefaultRoot()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("android.os.Environment"))
		{
			AndroidJavaObject androidJavaObject = androidJavaClass.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", androidJavaClass.GetStatic<string>("DIRECTORY_DOCUMENTS"));
			if (androidJavaObject == null)
			{
				return string.Empty;
			}
			string text = androidJavaObject.Call<string>("getAbsolutePath");
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			return Path.Combine(text, Application.productName);
		}
#else
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.productName);
#endif
	}
}
