using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class INUnity
{
	public enum VersionType
	{
		Original = 0,
		ModeO = 1,
		ModeA = 2,
		ModeB = 3
	}

	private static Dictionary<(string, string), UnityEngine.Object> s_resources;

	public static bool Enabled => true;

	public static Version Version { get; private set; }

	public static string VersionText { get; private set; }

	public static int BuildVersion { get; private set; }

	public static string DataPath { get; private set; }

	public static string SettingsPath { get; private set; }

	public static SystemLanguage Language { get; private set; }

	public static Font ArialFont { get; private set; }

	public static Mesh QuadMesh { get; private set; }

	public static Shader ColorShader { get; private set; }

	public static Shader ColorTransparentShader { get; private set; }

	public static Shader CustomTransparentShader { get; private set; }

	static INUnity()
	{
		Version = Version.Parse(Application.version);
		VersionText = Application.version;
		string[] array = VersionText.Split('-', 2);
		Version = Version.Parse(array[0]);
		BuildVersion = -1;
		if (array.Length > 1)
		{
			BuildVersion = int.Parse(array[1]);
		}
		SystemLanguage systemLanguage = Application.systemLanguage;
		if (systemLanguage == SystemLanguage.Chinese || systemLanguage == SystemLanguage.ChineseSimplified || systemLanguage == SystemLanguage.ChineseTraditional)
		{
			Language = SystemLanguage.Chinese;
		}
		else
		{
			Language = SystemLanguage.English;
		}
		ArialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
		QuadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
		s_resources = new Dictionary<(string, string), UnityEngine.Object>();
	}

	public static void InitializeRoot()
	{
		DataPath = INFileSystem.Root;
		SettingsPath = Path.Combine(DataPath, "Settings");
	}

	public static void Initialize(ResourceData data)
	{
		InitializeResources("GameObject", data.Prefabs);
		InitializeResources("Font", data.Fonts);
		InitializeResources("Texture", data.Textures);
		InitializeResources("Shader", data.Shaders);
		InitializeResources("Material", data.Materials);
		InitializeResources("TextAsset", data.TextAssets);
		InitializeResources("ScriptableObject", data.ScriptableObjects);
		s_resources.Add(("Font", "DefaultFont-Regular"), LoadFont("FrutigerNeueLTW1G-Regular"));
		ColorShader = LoadShader("Unlit_Color");
		ColorTransparentShader = LoadShader("Unlit_ColorTransparent");
		CustomTransparentShader = LoadShader("PreAlpha_Unlit_ColorTransparent_Geometry");
	}

	private static void InitializeResources<T>(string typeName, List<T> data) where T : UnityEngine.Object
	{
		foreach (T datum in data)
		{
			s_resources.Add((typeName, GetName(datum)), datum);
		}
	}

	private static string GetName(UnityEngine.Object obj)
	{
		string name = obj.name;
		if (obj is Shader)
		{
			int num = name.LastIndexOf('/');
			if (num != -1)
			{
				return name.Substring(num + 1);
			}
			return name;
		}
		return name;
	}

	public static Font LoadFont(string name)
	{
		return (Font)LoadObject("Font", name);
	}

	public static GameObject LoadGameObject(string name)
	{
		return (GameObject)LoadObject("GameObject", name);
	}

	public static Texture LoadTexture(string name)
	{
		return (Texture)LoadObject("Texture", name);
	}

	public static Material LoadMaterial(string name)
	{
		return (Material)LoadObject("Material", name);
	}

	public static Shader LoadShader(string name)
	{
		return (Shader)LoadObject("Shader", name);
	}

	public static TextAsset LoadTextAsset(string name)
	{
		return (TextAsset)LoadObject("TextAsset", name);
	}

	public static T LoadScriptableObject<T>(string name) where T : ScriptableObject
	{
		return (T)LoadObject("ScriptableObject", name);
	}

	public static UnityEngine.Object LoadObject(string typeName, string name)
	{
		return s_resources[(typeName, name)];
	}
}
