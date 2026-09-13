using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AppInterfaceSidebar : MonoBehaviour
{
	[Serializable]
	public class SidebarElementData
	{
		public string Name;

		public GameObject RelatedObject;
	}

	[SerializeField]
	private List<SidebarElementData> m_sidebarElements;

	[SerializeField]
	private GameObject m_sidebarElementTemplate;

	private bool m_enabled;

	public bool Enabled => m_enabled;

	private void Awake()
	{
		m_enabled = true;
		GenerateSidebarElements();
	}

	private void GenerateSidebarElements()
	{
		GameObject sidebarElementTemplate = m_sidebarElementTemplate;
		int num = 0;
		foreach (SidebarElementData sidebarElement in m_sidebarElements)
		{
			GameObject obj = UnityEngine.Object.Instantiate(sidebarElementTemplate);
			obj.SetActive(value: true);
			obj.name = "SidebarElement_" + (num + 1);
			obj.transform.SetParent(base.transform, worldPositionStays: false);
			obj.GetComponent<AppInterfaceSidebarItem>().Initialize(sidebarElement.Name, sidebarElement.RelatedObject);
			num++;
		}
	}

	public void SetEnabled(bool enabled)
	{
		if (m_enabled ^ enabled)
		{
			m_enabled = enabled;
			CanvasGroup component = GetComponent<CanvasGroup>();
			component.alpha = (enabled ? 1f : 0f);
			component.blocksRaycasts = enabled;
		}
	}

	public void SetElementEnabled(AppInterfaceSidebarItem element, bool enabled)
	{
		if (!GetComponent<ToggleGroup>().AnyTogglesOn())
		{
			string text = INLocalization.Instance.GetText("AppInterface_AppName");
			string text2 = INLocalization.Instance.GetText("AppInterface_Name");
			INAppInterface.Instance.SetTitle(text + " " + text2);
		}
		element.SetEnabled(enabled);
	}
}
