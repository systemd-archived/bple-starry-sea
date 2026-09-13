// Editor construction script: builds CustomBackgroundInterface.prefab programmatically.
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildCustomBackgroundInterface
{
	[MenuItem("Tools/Build CustomBackgroundInterface")]
	public static void Build()
	{
		string assetPath = "Assets/GameObject/CustomBackgroundInterface.prefab";

		Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

		// Temp root in current scene
		GameObject root = new GameObject("CustomBackgroundInterface");
		RectTransform rootRt = root.AddComponent<RectTransform>();
		SetFullRect(rootRt);
		root.AddComponent<CanvasRenderer>();

		// Background mask
		GameObject bg = CreateImage(root.transform, "Background", font);
		bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
		bg.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

		// Title
		GameObject title = CreateText(root.transform, "Title", font, "自定义背景", 36, TextAnchor.MiddleCenter);
		RectTransform trt = title.GetComponent<RectTransform>();
		trt.anchorMin = new Vector2(0f, 1f);
		trt.anchorMax = new Vector2(1f, 1f);
		trt.pivot = new Vector2(0.5f, 1f);
		trt.anchoredPosition = new Vector2(0f, -30f);
		trt.sizeDelta = new Vector2(0f, 60f);

		// Content area (vertical stack container) center
		GameObject content = CreateImageHolder(root.transform, "Content");
		RectTransform crt = content.GetComponent<RectTransform>();
		crt.anchorMin = new Vector2(0.5f, 0.5f);
		crt.anchorMax = new Vector2(0.5f, 0.5f);
		crt.pivot = new Vector2(0.5f, 0.5f);
		crt.localPosition = Vector3.zero;
		crt.sizeDelta = new Vector2(500f, 340f);

		// Upload button
		GameObject uploadBtn = CreateButton(content.transform, "UploadButton", font, "上传图片");
		RectTransform ubRt = uploadBtn.GetComponent<RectTransform>();
		ubRt.anchorMin = new Vector2(0f, 1f);
		ubRt.anchorMax = new Vector2(1f, 1f);
		ubRt.pivot = new Vector2(0.5f, 1f);
		ubRt.anchoredPosition = new Vector2(0f, 0f);
		ubRt.sizeDelta = new Vector2(0f, 80f);

		// Toggle row
		GameObject toggle = CreateToggle(content.transform, "EnableToggle", font, "开启背景");
		RectTransform tgRt = toggle.GetComponent<RectTransform>();
		tgRt.anchorMin = new Vector2(0f, 1f);
		tgRt.anchorMax = new Vector2(1f, 1f);
		tgRt.pivot = new Vector2(0.5f, 1f);
		tgRt.anchoredPosition = new Vector2(0f, -120f);
		tgRt.sizeDelta = new Vector2(0f, 70f);

		// Status text
		GameObject status = CreateText(content.transform, "StatusText", font, "未上传背景", 26, TextAnchor.MiddleCenter);
		RectTransform srt = status.GetComponent<RectTransform>();
		srt.anchorMin = new Vector2(0f, 0f);
		srt.anchorMax = new Vector2(1f, 0f);
		srt.pivot = new Vector2(0.5f, 0f);
		srt.anchoredPosition = new Vector2(0f, 20f);
		srt.sizeDelta = new Vector2(0f, 60f);

		// Attach INCustomBackgroundInterface and wire serialized fields
		var ctrl = root.AddComponent<INCustomBackgroundInterface>();
		var tsw = toggle.transform.Find("ToggleSwitch");
		var upload = uploadBtn.GetComponent<UnityEngine.UI.Button>();
		var togglesw = (tsw != null) ? tsw.GetComponent<ToggleSwitch>() : null;
		var statusTxt = status.GetComponent<UnityEngine.UI.Text>();
		var so = new SerializedObject(ctrl);
		so.FindProperty("m_uploadButton").objectReferenceValue = upload;
		so.FindProperty("m_toggle").objectReferenceValue = togglesw;
		so.FindProperty("m_statusText").objectReferenceValue = statusTxt;
		so.ApplyModifiedProperties();
		if (upload == null) Debug.LogWarning("[BuildCustom] uploadButton not found");
		if (togglesw == null) Debug.LogWarning("[BuildCustom] toggle not found");
		if (statusTxt == null) Debug.LogWarning("[BuildCustom] statusText not found");

		// Save as prefab asset (overwrite the duplicate)
		PrefabUtility.SaveAsPrefabAsset(root, assetPath);

		// Cleanup temp
		Object.DestroyImmediate(root);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("[BuildCustom] saved " + assetPath);
	}

	private static void SetFullRect(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
		rt.pivot = new Vector2(0.5f, 0.5f);
	}

	private static GameObject CreateImage(Transform parent, string name, Font font)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		RectTransform rt = go.AddComponent<RectTransform>();
		SetFullRect(rt);
		go.AddComponent<CanvasRenderer>();
		Image img = go.AddComponent<Image>();
		img.raycastTarget = false;
		return go;
	}

	private static GameObject CreateImageHolder(Transform parent, string name)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		go.AddComponent<RectTransform>();
		go.AddComponent<CanvasRenderer>();
		Image img = go.AddComponent<Image>();
		img.color = new Color(0f, 0f, 0f, 0f);
		img.raycastTarget = false;
		return go;
	}

	private static GameObject CreateText(Transform parent, string name, Font font, string text, int size, TextAnchor align)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		RectTransform rt = go.AddComponent<RectTransform>();
		go.AddComponent<CanvasRenderer>();
		UnityEngine.UI.Text t = go.AddComponent<UnityEngine.UI.Text>();
		t.font = font;
		t.text = text;
		t.fontSize = size;
		t.alignment = align;
		t.color = Color.white;
		t.raycastTarget = false;
		return go;
	}

	private static GameObject CreateButton(Transform parent, string name, Font font, string label)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		RectTransform rt = go.AddComponent<RectTransform>();
		go.AddComponent<CanvasRenderer>();
		UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
		img.color = new Color(0.24f, 0.42f, 0.6f, 1f);
		UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
		btn.targetGraphic = img;
		GameObject labelObj = CreateText(go.transform, "Text", font, label, 30, TextAnchor.MiddleCenter);
		SetFullRect(labelObj.GetComponent<RectTransform>());
		return go;
	}

	private static GameObject CreateToggle(Transform parent, string name, Font font, string label)
	{
		GameObject row = new GameObject(name);
		row.transform.SetParent(parent, false);
		RectTransform rrt = row.AddComponent<RectTransform>();
		row.AddComponent<CanvasRenderer>();

		// label on left
		GameObject lbl = CreateText(row.transform, "Label", font, label, 28, TextAnchor.MiddleLeft);
		RectTransform lrt = lbl.GetComponent<RectTransform>();
		lrt.anchorMin = Vector2.zero;
		lrt.anchorMax = Vector2.zero;
		lrt.pivot = new Vector2(0f, 0.5f);
		lrt.anchoredPosition = new Vector2(10f, 0f);
		lrt.sizeDelta = new Vector2(220f, 40f);

		// toggle switch itself
		GameObject ts = new GameObject("ToggleSwitch");
		ts.transform.SetParent(row.transform, false);
		RectTransform trt = ts.AddComponent<RectTransform>();
		trt.anchorMin = new Vector2(1f, 0.5f);
		trt.anchorMax = new Vector2(1f, 0.5f);
		trt.pivot = new Vector2(1f, 0.5f);
		trt.anchoredPosition = new Vector2(-10f, 0f);
		trt.sizeDelta = new Vector2(100f, 40f);
		ts.AddComponent<CanvasRenderer>();
		Image border = ts.AddComponent<Image>();
		border.color = new Color(1f, 1f, 1f, 0.25f);
		GameObject bg = new GameObject("Background");
		bg.transform.SetParent(ts.transform, false);
		RectTransform brt = bg.AddComponent<RectTransform>();
		SetFullRect(brt);
		bg.AddComponent<CanvasRenderer>();
		Image bgImg = bg.AddComponent<Image>();
		bgImg.color = new Color(0.5f, 0.7f, 0.9f, 0.6f);
		GameObject ellipse = new GameObject("Ellipse");
		ellipse.transform.SetParent(ts.transform, false);
		RectTransform ert = ellipse.AddComponent<RectTransform>();
		ert.sizeDelta = new Vector2(36f, 36f);
		ert.anchoredPosition = new Vector2(-50f, 0f);
		ellipse.AddComponent<CanvasRenderer>();
		Image ellImg = ellipse.AddComponent<Image>();
		ellImg.color = new Color(0.8f, 0.9f, 1f, 1f);

		ToggleSwitch tsw = ts.AddComponent<ToggleSwitch>();
		var so = new SerializedObject(tsw);
		so.FindProperty("m_border").objectReferenceValue = border;
		so.FindProperty("m_background").objectReferenceValue = bgImg;
		so.FindProperty("m_ellipse").objectReferenceValue = ellImg;
		so.FindProperty("m_enabledColor").colorValue = new Color(0.35f, 0.7f, 1f, 1f);
		so.FindProperty("m_disabledColor").colorValue = new Color(0.5f, 0.5f, 0.5f, 1f);
		so.ApplyModifiedProperties();

		return row;
	}
}
