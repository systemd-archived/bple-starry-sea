using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class INDataPanel : MonoBehaviour
{
	private class DataGroup
	{
		public GameObject GameObject { get; private set; }

		public Text GroupName { get; private set; }

		public UITextLocale GroupNameLocale { get; private set; }

		public List<DataItem> Items { get; private set; }

		public DataGroup(GameObject gameObject)
		{
			GameObject = gameObject;
			GroupName = gameObject.transform.Find("GroupName").GetComponent<Text>();
			GroupNameLocale = GroupName.GetComponent<UITextLocale>();
			Items = new List<DataItem>();
		}
	}

	private class DataItem
	{
		public GameObject GameObject { get; private set; }

		public Text Name { get; private set; }

		public UITextLocale NameLocale { get; private set; }

		public Text Value { get; private set; }

		public Func<string> Getter { get; private set; }

		public DataItem(GameObject gameObject, Func<string> getter)
		{
			GameObject = gameObject;
			Name = gameObject.transform.Find("Name").GetComponent<Text>();
			NameLocale = Name.GetComponent<UITextLocale>();
			Value = gameObject.transform.Find("Value").GetComponent<Text>();
			Getter = getter;
		}

		public void UpdateValue()
		{
			if (Getter != null)
			{
				Value.text = Getter();
			}
		}
	}

	[SerializeField]
	private GameObject m_content;

	[SerializeField]
	private GameObject m_dataItemTemplate;

	[SerializeField]
	private GameObject m_dataGroupTemplate;

	private List<DataGroup> m_dataGroups;

	private INDataDetector m_detector;

	public static INDataPanel Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		m_dataGroups = new List<DataGroup>();
		m_detector = UnityEngine.Object.Instantiate(INUnity.LoadGameObject("INDataDetector")).GetComponent<INDataDetector>();
	}

	private void Start()
	{
		DataGroup dataGroup = GenerateDataGroup(0, "DataPanel_TimeData");
		GenerateDataItem(dataGroup, 0, "DataPanel_Time", () => DateTime.Now.ToString("s"));
		GenerateDataItem(dataGroup, 1, "DataPanel_RunningTime", () => Time.realtimeSinceStartup.ToString("F2"));
		DataGroup dataGroup2 = GenerateDataGroup(1, "DataPanel_PerformanceData");
		GenerateDataItem(dataGroup2, 0, "DataPanel_FPS", () => m_detector.FPS.ToString("F2"));
		GenerateDataItem(dataGroup2, 1, "DataPanel_FixedFPS", () => m_detector.FixedFPS.ToString("F2"));
		GenerateDataItem(dataGroup2, 2, "DataPanel_AllocatedManagedHeapSize", () => m_detector.AllocatedManagedHeapSize.ToString("F2") + " MB");
		GenerateDataItem(dataGroup2, 3, "DataPanel_ReservedManagedHeapSize", () => m_detector.ReservedManagedHeapSize.ToString("F2") + " MB");
		GenerateDataItem(dataGroup2, 4, "DataPanel_TotalAllocatedMemorySize", () => m_detector.TotalAllocatedMemorySize.ToString("F2") + " MB");
		GenerateDataItem(dataGroup2, 5, "DataPanel_TotalReservedMemorySize", () => m_detector.TotalReservedMemorySize.ToString("F2") + " MB");
		DataGroup dataGroup3 = GenerateDataGroup(2, "DataPanel_DeviceData");
		GenerateDataItem(dataGroup3, 0, "DataPanel_DeviceModel", () => SystemInfo.deviceModel);
		GenerateDataItem(dataGroup3, 1, "DataPanel_DeviceName", () => SystemInfo.deviceName);
		GenerateDataItem(dataGroup3, 2, "DataPanel_DeviceType", () => SystemInfo.deviceType.ToString());
		GenerateDataItem(dataGroup3, 3, "DataPanel_OperatingSystem", () => SystemInfo.operatingSystem);
		GenerateDataItem(dataGroup3, 4, "DataPanel_ProcessorType", () => SystemInfo.processorType);
		GenerateDataItem(dataGroup3, 5, "DataPanel_ProcessorFrequency", () => SystemInfo.processorFrequency + " MHz");
		GenerateDataItem(dataGroup3, 6, "DataPanel_ProcessorCount", () => SystemInfo.processorCount.ToString());
		GenerateDataItem(dataGroup3, 7, "DataPanel_GraphicsDeviceName", () => SystemInfo.graphicsDeviceName);
		GenerateDataItem(dataGroup3, 8, "DataPanel_GraphicsDeviceType", () => SystemInfo.graphicsDeviceType.ToString());
		GenerateDataItem(dataGroup3, 9, "DataPanel_GraphicsMemorySize", () => SystemInfo.graphicsMemorySize + " MB");
		GenerateDataItem(dataGroup3, 10, "DataPanel_SystemMemorySize", () => SystemInfo.systemMemorySize + " MB");
		GenerateDataItem(dataGroup3, 11, "DataPanel_BatteryLevel", () => SystemInfo.batteryLevel.ToString("P"));
		GenerateDataItem(dataGroup3, 12, "DataPanel_BatteryStatus", () => SystemInfo.batteryStatus.ToString());
	}

	private DataGroup GenerateDataGroup(int index, string groupName)
	{
		GameObject obj = UnityEngine.Object.Instantiate(m_dataGroupTemplate);
		obj.SetActive(value: true);
		obj.name = "DataGroup_" + index;
		obj.transform.SetParent(m_content.transform, worldPositionStays: false);
		DataGroup dataGroup = new DataGroup(obj);
		dataGroup.GroupNameLocale.ID = groupName;
		dataGroup.GroupNameLocale.UpdateText();
		m_dataGroups.Add(dataGroup);
		return dataGroup;
	}

	private DataItem GenerateDataItem(DataGroup dataGroup, int index, string name, Func<string> getter)
	{
		GameObject obj = UnityEngine.Object.Instantiate(m_dataItemTemplate);
		obj.SetActive(value: true);
		obj.name = "DataItem_" + index;
		obj.transform.SetParent(dataGroup.GameObject.transform, worldPositionStays: false);
		DataItem dataItem = new DataItem(obj, getter);
		dataItem.NameLocale.ID = name;
		dataItem.NameLocale.UpdateText();
		dataItem.Value.text = getter();
		dataGroup.Items.Add(dataItem);
		return dataItem;
	}

	private void Update()
	{
		foreach (DataGroup dataGroup in m_dataGroups)
		{
			foreach (DataItem item in dataGroup.Items)
			{
				item.UpdateValue();
			}
		}
	}
}
