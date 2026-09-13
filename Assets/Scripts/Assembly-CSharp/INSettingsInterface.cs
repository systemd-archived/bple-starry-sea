using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Innovation;
using UnityEngine;
using UnityEngine.UI;

public class INSettingsInterface : MonoBehaviour
{
	private class SettingsGroup
	{
		private SettingsBase m_source;

		private GameObject m_gameObject;

		private Text m_groupName;

		private UITextLocale m_groupNameLocale;

		private List<SettingsItem> m_items;

		public SettingsBase Source => m_source;

		public GameObject GameObject => m_gameObject;

		public Text GroupName => m_groupName;

		public UITextLocale GroupNameLocale => m_groupNameLocale;

		public List<SettingsItem> Items => m_items;

		public SettingsGroup(SettingsBase source, GameObject gameObject)
		{
			m_source = source;
			m_gameObject = gameObject;
			m_groupName = gameObject.transform.Find("GroupName").GetComponent<Text>();
			m_groupNameLocale = GroupName.GetComponent<UITextLocale>();
			m_items = new List<SettingsItem>();
		}

		public void Render()
		{
			foreach (SettingsItem item in m_items)
			{
				item.Render();
			}
		}
	}

	private abstract class SettingsItem
	{
		protected SettingsGroup m_group;

		protected GameObject m_gameObject;

		protected Text m_name;

		protected UITextLocale m_nameLocale;

		public SettingsGroup Group => m_group;

		public GameObject GameObject => m_gameObject;

		public Text Name => m_name;

		public UITextLocale NameLocale => m_nameLocale;

		public SettingsItem(SettingsGroup group, GameObject gameObject)
		{
			m_group = group;
			m_gameObject = gameObject;
			m_name = gameObject.transform.Find("Name").GetComponent<Text>();
			m_nameLocale = m_name.GetComponent<UITextLocale>();
		}

		public abstract object GetBoxedValue();

		public abstract void SetBoxedValue(object value);

		public abstract void Render();

		// 从源重新读入到 UI 代理并回显（供重置后同步）
		public abstract void SyncFromSource();
	}

	private abstract class SettingsItem<T> : SettingsItem
	{
		protected DependencyProperty<T> m_property;

		private Func<T> m_getter;

		public SettingsItem(SettingsGroup group, GameObject gameObject)
			: base(group, gameObject)
		{
			m_property = new DependencyProperty<T>();
		}

		// 从源重新读入到 UI 代理并回显：用于重置后把"源=默认"写回 UI，保证再次编辑能正确生效。
		public override void SyncFromSource()
		{
			if (m_getter != null)
			{
				SetValue(m_getter());
			}
		}

		public void Bind(string propertyName, Func<T> getter, Action<T> setter)
		{
			m_getter = getter;
			Binding.Bind(m_group.Source, propertyName, delegate
			{
				SetValue(getter());
			});
			Binding.Bind(delegate
			{
				setter(GetValue());
				SetValue(getter());
			});
		}

		public void Bind(PropertyInfo property)
		{
			Func<T> getter = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), m_group.Source, property.GetGetMethod());
			Action<T> setter = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), m_group.Source, property.GetSetMethod());
			Bind(property.Name, getter, setter);
		}

		public override object GetBoxedValue()
		{
			return GetValue();
		}

		public override void SetBoxedValue(object value)
		{
			SetValue((T)value);
		}

		public virtual T GetValue()
		{
			return m_property.Value;
		}

		public virtual void SetValue(T value)
		{
			m_property.Value = value;
		}
	}

	private class SettingsItemWithToggle : SettingsItem<bool>
	{
		private ToggleSwitch m_toggle;

		public ToggleSwitch Toggle => m_toggle;

		public SettingsItemWithToggle(SettingsGroup group, GameObject gameObject)
			: base(group, gameObject)
		{
			m_toggle = gameObject.transform.Find("ToggleSwitch").GetComponent<ToggleSwitch>();
			m_toggle.gameObject.SetActive(value: true);
			m_toggle.OnValueChanged.AddListener(OnValueChanged);
		}

		public override void SetBoxedValue(object value)
		{
			if (value is bool value2)
			{
				SetValue(value2);
				return;
			}
			if (value is string value3 && bool.TryParse(value3, out var result))
			{
				SetValue(result);
				return;
			}
			throw new InvalidCastException();
		}

		public override void SetValue(bool value)
		{
			if (m_property.RawValue != value)
			{
				base.SetValue(value);
				m_group.Source.Apply();
				Render();
			}
		}

		private void OnValueChanged(bool value)
		{
			SetValue(value);
			Render();
		}

		public override void Render()
		{
			m_toggle.IsOn = m_property.RawValue;
		}
	}

	private class SettingsItemWithInputField<T> : SettingsItem<T>
	{
		private InputField m_inputField;

		private Parsable.TryParser<T> m_parser;

		public InputField InputField => m_inputField;

		public SettingsItemWithInputField(SettingsGroup group, GameObject gameObject, Parsable.TryParser<T> parser)
			: base(group, gameObject)
		{
			m_inputField = gameObject.transform.Find("InputField").GetComponent<InputField>();
			m_inputField.gameObject.SetActive(value: true);
			m_inputField.onEndEdit.AddListener(OnEndEdit);
			m_parser = parser;
			Render();
		}

		public override void SetBoxedValue(object value)
		{
			if (value is T value2)
			{
				SetValue(value2);
				return;
			}
			if (value is string s && m_parser(s, CultureInfo.CurrentCulture, out var result))
			{
				SetValue(result);
				return;
			}
			throw new InvalidCastException();
		}

		public override void SetValue(T value)
		{
			if (!EqualityComparer<T>.Default.Equals(m_property.RawValue, value))
			{
				base.SetValue(value);
				m_group.Source.Apply();
				Render();
			}
		}

		private void OnEndEdit(string text)
		{
			if (m_parser(text, CultureInfo.CurrentCulture, out var result))
			{
				SetValue(result);
			}
			Render();
		}

		public override void Render()
		{
			InputField inputField = m_inputField;
			T rawValue = m_property.RawValue;
			inputField.text = ((rawValue != null) ? rawValue.ToString() : null);
		}
	}

	[SerializeField]
	private GameObject m_content;

	[SerializeField]
	private GameObject m_itemTemplate;

	[SerializeField]
	private GameObject m_groupTemplate;

	[SerializeField]
	private UnityEngine.UI.Button m_saveButton;

	[SerializeField]
	private UnityEngine.UI.Button m_resetButton;

	[SerializeField]
	private string m_onlyGroupName;

	private bool m_dirty;

	private List<SettingsGroup> m_settingsGroups;

	private Dictionary<string, SettingsItem> m_settingsMap;

	public static INSettingsInterface Instance { get; private set; }

	public object GetValue(string name)
	{
		return m_settingsMap[name].GetBoxedValue();
	}

	public void SetValue(string name, object value)
	{
		m_settingsMap[name].SetBoxedValue(value);
	}

	public void SetDirty()
	{
		m_dirty = true;
	}

	public void Save()
	{
		INUserSettings.Save();
		m_dirty = false;
	}

	public void Reset()
	{
		// 先强制把与部件调整界面共享的 INFeature 项重置为版本默认，
		// 避免双向代理属性(get 实时读 INFeature)导致 Update(Default) 拿到"当前值"而重置失效。
		INSettings.ResetUiSharedDefaults();
		if (string.IsNullOrEmpty(m_onlyGroupName))
		{
			INUserSettings.Reset();
		}
		else
		{
			SettingsBase source = GetGroupSource(INUserSettings.Instance, m_onlyGroupName);
			SettingsBase defaultValue = GetGroupSource(INUserSettings.Default, m_onlyGroupName);
			if (source != null && defaultValue != null)
			{
				MethodInfo updateMethod = source.GetType().GetMethod("Update", new[] { source.GetType() });
				if (updateMethod != null)
				{
					updateMethod.Invoke(source, new object[] { defaultValue });
				}
				source.Apply();
			}
		}
		m_dirty = true;
		// 重置后把每个设置项从源重新读入 UI 代理并回显（原版写回通道），
		// 保证重置后再次编辑能正确比较、写入源并生效（否则需退出重进设置界面才行）。
		foreach (SettingsGroup settingsGroup in m_settingsGroups)
		{
			foreach (SettingsItem item in settingsGroup.Items)
			{
				item.SyncFromSource();
			}
			settingsGroup.Render();
		}
	}

	private void Awake()
	{
		Instance = this;
		m_saveButton.onClick.AddListener(Save);
		m_resetButton.onClick.AddListener(Reset);
	}

	private void Start()
	{
		m_settingsGroups = GenerateGroupList(INUserSettings.Instance);
		m_settingsMap = new Dictionary<string, SettingsItem>(StringComparer.OrdinalIgnoreCase);
		foreach (SettingsGroup settingsGroup in m_settingsGroups)
		{
			foreach (SettingsItem item in settingsGroup.Items)
			{
				m_settingsMap.Add(item.NameLocale.ID, item);
			}
		}
	}

	private List<SettingsGroup> GenerateGroupList(INUserSettings userSettings)
	{
		List<SettingsGroup> list = new List<SettingsGroup>();
		if (string.IsNullOrEmpty(m_onlyGroupName))
		{
			foreach (SettingsBase settings in userSettings.SettingsList)
			{
				SettingsGroup item = GenerateGroup(settings.GetType().Name, settings.GetType().Name + "_Name", settings);
				list.Add(item);
			}
			return list;
		}
		if (m_onlyGroupName == "StarSeaSettings")
		{
			SettingsBase starSea = GetGroupSource(userSettings, "StarSeaSettings");
			list.Add(GenerateStarSeaGroup(starSea, "StarSeaSettings_Behavior", "Behavior", "FixedFrameRate", "CircuitFrameRate", "AntiTunnelSpeedThreshold", "AntiTunnelPullBackDistance", "PartDrag", "PartAngularDrag", "GunProjectileDrag", "StableMultiblock", "AnchorUmbrellaCoefficient"));
			list.Add(GenerateStarSeaGroup(starSea, "StarSeaSettings_ToolBook", "辅助菜单", "ToolBookEnabled", "ToolBookPosX", "ToolBookPosY", "ToolBookSize", "ToolBookPasteOverwriteMode", "ToolBookClipboardMax"));
			list.Add(GenerateStarSeaGroup(starSea, "StarSeaSettings_Performance", "Performance", "ShowFPS", "TargetFrameRate", "PhysicsSolverIterations", "AdaptivePhysicsFrame", "AdaptivePhysicsFrameMin", "FrameFrameRate", "UseMultithreadFrameSolver", "EnableMultithreadCircuit"));
			list.Add(GenerateStarSeaGroup(starSea, "StarSeaSettings_MechanicalGate", "MechanicalGate", "GateWidthCoefficient", "GateLengthCoefficients", "GateOpacity", "GateDefaultLengths", "GateAdjustableRange", "GateExtensionSpeed"));
			list.Add(GenerateStarSeaGroup(starSea, "StarSeaSettings_PointCharge", "PointCharge", "SpecialPointChargeInvincible", "SpecialChargeDefaultEnabled", "SpecialChargeForce", "SpecialChargeForceRadius", "SpecialChargeJointBreakForce", "SpecialChargeJointDistance", "ChargeJointSnapEnabled", "ChargeJointSize", "SpecialChargeReinforceRange", "SpecialChargeReinforceMaxCount", "SpecialChargeReinforceBreakForce", "SpecialChargeCollisionIgnore", "SpecialChargeAttractionCollisionIgnore", "SpecialChargeForceConnect", "ShowJointConnectionCount"));
			return list;
		}
		SettingsGroup settingsGroup2 = GenerateGroup(m_onlyGroupName, m_onlyGroupName + "_Name", GetGroupSource(userSettings, m_onlyGroupName));
		list.Add(settingsGroup2);
		return list;
	}

	private SettingsGroup GenerateStarSeaGroup(SettingsBase source, string groupLocaleName, string groupName, params string[] propertyNames)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(m_groupTemplate);
		gameObject.SetActive(value: true);
		gameObject.name = "SettingsGroup_" + groupName;
		gameObject.transform.SetParent(m_content.transform, worldPositionStays: false);
		SettingsGroup settingsGroup = new SettingsGroup(source, gameObject);
		settingsGroup.GroupNameLocale.ID = groupLocaleName;
		settingsGroup.GroupNameLocale.UpdateText();
		Type type = source.GetType();
		foreach (string propName in propertyNames)
		{
			PropertyInfo propertyInfo = type.GetProperty(propName);
			if (propertyInfo == null)
			{
				continue;
			}
			Type propertyType = propertyInfo.PropertyType;
			string text = propName;
			string text2 = type.Name + "_" + text;
			if (propertyType == typeof(bool))
			{
				GenerateItemWithToggle(settingsGroup, text, text2, propertyInfo);
				continue;
			}
			if (propertyType == typeof(HexColor))
			{
				GenerateItemWithInputField(settingsGroup, text, text2, propertyInfo, delegate(string s, IFormatProvider provider, out HexColor result)
				{
					return HexColor.TryParse(s, out result);
				});
				continue;
			}
			GetType().GetMethod("GenerateItemWithInputField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).MakeGenericMethod(propertyType).Invoke(this, new object[5] { settingsGroup, text, text2, propertyInfo, null });
		}
		return settingsGroup;
	}

	private static SettingsBase GetGroupSource(INUserSettings userSettings, string groupName)
	{
		System.Reflection.PropertyInfo property = userSettings.GetType().GetProperty(groupName);
		if (property != null && property.GetValue(userSettings) is SettingsBase settingsBase)
		{
			return settingsBase;
		}
		return null;
	}

	private SettingsGroup GenerateGroup(string name, string groupLocaleName, SettingsBase source)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(m_groupTemplate);
		gameObject.SetActive(value: true);
		gameObject.name = "SettingsGroup_" + name;
		gameObject.transform.SetParent(m_content.transform, worldPositionStays: false);
		SettingsGroup settingsGroup = new SettingsGroup(source, gameObject);
		settingsGroup.GroupNameLocale.ID = groupLocaleName;
		settingsGroup.GroupNameLocale.UpdateText();
		Type type = source.GetType();
		PropertyInfo[] properties = type.GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			Type propertyType = propertyInfo.PropertyType;
			string text = propertyInfo.Name;
			string text2 = type.Name + "_" + text;
			if (propertyType == typeof(bool))
			{
				GenerateItemWithToggle(settingsGroup, text, text2, propertyInfo);
				continue;
			}
			if (propertyType == typeof(HexColor))
			{
				GenerateItemWithInputField(settingsGroup, text, text2, propertyInfo, delegate(string s, IFormatProvider provider, out HexColor result)
				{
					return HexColor.TryParse(s, out result);
				});
				continue;
			}
			GetType().GetMethod("GenerateItemWithInputField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).MakeGenericMethod(propertyType).Invoke(this, new object[5] { settingsGroup, text, text2, propertyInfo, null });
		}
		return settingsGroup;
	}

	private GameObject GenerateItem(SettingsGroup group, string name)
	{
		GameObject obj = UnityEngine.Object.Instantiate(m_itemTemplate);
		obj.SetActive(value: true);
		obj.name = "SettingsItem_" + name;
		obj.transform.SetParent(group.GameObject.transform, worldPositionStays: false);
		return obj;
	}

	private SettingsItemWithToggle GenerateItemWithToggle(SettingsGroup group, string name, string localeName, PropertyInfo property)
	{
		GameObject gameObject = GenerateItem(group, name);
		SettingsItemWithToggle settingsItemWithToggle = new SettingsItemWithToggle(group, gameObject);
		settingsItemWithToggle.NameLocale.ID = localeName;
		settingsItemWithToggle.NameLocale.UpdateText();
		settingsItemWithToggle.Bind(property);
		group.Items.Add(settingsItemWithToggle);
		return settingsItemWithToggle;
	}

	private SettingsItemWithInputField<T> GenerateItemWithInputField<T>(SettingsGroup group, string name, string localeName, PropertyInfo property, Parsable.TryParser<T> converter = null)
	{
		if (converter == null)
		{
			converter = Parsable.GetTryParser<T>();
		}
		if (converter == null)
		{
			return null;
		}
		GameObject gameObject = GenerateItem(group, name);
		SettingsItemWithInputField<T> settingsItemWithInputField = new SettingsItemWithInputField<T>(group, gameObject, converter);
		settingsItemWithInputField.NameLocale.ID = localeName;
		settingsItemWithInputField.NameLocale.UpdateText();
		settingsItemWithInputField.Bind(property);
		group.Items.Add(settingsItemWithInputField);
		return settingsItemWithInputField;
	}

	private void Update()
	{
		Text component = m_saveButton.transform.Find("Text").GetComponent<Text>();
		string text = component.GetComponent<UITextLocale>().Text;
		component.text = (m_dirty ? (text + "*") : text);
	}
}
