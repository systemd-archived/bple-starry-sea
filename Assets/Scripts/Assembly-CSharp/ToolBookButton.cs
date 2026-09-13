using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UButton = UnityEngine.UI.Button;
using UImage = UnityEngine.UI.Image;
using UText = UnityEngine.UI.Text;

public class ToolBookButton : MonoBehaviour
{
	private RectTransform m_mainRect;
	private List<RectTransform> m_childRects = new List<RectTransform>();
	private List<UImage> m_childBgs = new List<UImage>();
	private List<UText> m_childTexts = new List<UText>();
	private bool m_isOpen;
	private bool m_coroutineRunning;
	private float m_slideSpeed = 800f;
	private float m_size = 1f;
	private bool m_isDragging;
	private Canvas m_canvas;
	private bool m_hiddenBySetting;
	private int m_activeChildIndex = -1;

	// Column 2 buttons
	private GameObject m_deleteBtnObj;
	private RectTransform m_deleteRect;
	private UImage m_deleteBg;
	private UText m_deleteText;
	private GameObject m_overwriteObj;
	private UImage m_overwriteBg;
	private UText m_overwriteText;
	private GameObject m_rotateObj;
	private UText m_rotateText;
	private GameObject m_materialObj;
	private UImage m_materialBg;
	private UText m_materialText;
	private GameObject m_shapeTypeObj;
	private UImage m_shapeTypeBg;
	private UText m_shapeTypeText;
	private GameObject m_shapeSolidObj;
	private UImage m_shapeSolidBg;
	private UText m_shapeSolidText;

	// Column 3: clipboard slots
	private List<GameObject> m_clipSlotObjs = new List<GameObject>();
	private List<RectTransform> m_clipSlotRects = new List<RectTransform>();
	private List<UImage> m_clipSlotBgs = new List<UImage>();
	private List<UText> m_clipSlotTexts = new List<UText>();

	// Confirm/Cancel for shape drawing
	private GameObject m_confirmObj;
	private GameObject m_cancelObj;

	// Clip drag state
	private bool m_clipDragging;
	private Vector2 m_clipDragStartScreenPos;
	private int m_clipDragIndex = -1;

	// Paste drag state
	private bool m_pasteDragging;
	private Vector2 m_pasteDragStartScreenPos;

	// Undo button reference
	private RectTransform m_undoRect;
	private UImage m_undoBg;

	// Child button labels: 复制/粘贴/选区/撤销/暂存/存剪/形状/填充
	private static readonly string[] s_toolLabels = { "复制", "粘贴", "选区", "撤销", "暂存", "存剪", "形状", "填充" };

	// Shape type names
	private static readonly string[] s_shapeNames = { "方形", "矩形", "圆形", "椭圆", "直线" };

	private static readonly Color s_normalBg = new Color(0.2f, 0.2f, 0.2f, 0.9f);
	private static readonly Color s_normalText = Color.white;
	private static readonly Color s_activeBg = new Color(0.9f, 0.9f, 0.9f, 0.95f);
	private static readonly Color s_activeText = new Color(0.1f, 0.1f, 0.1f, 1f);
	private static readonly Color s_redBg = new Color(0.9f, 0.15f, 0.15f, 0.95f);
	private static readonly Color s_clipFilledBg = new Color(0.15f, 0.45f, 0.75f, 0.9f);
	private static readonly Color s_clipEmptyBg = new Color(0.25f, 0.25f, 0.25f, 0.7f);

	private void Awake()
	{
		m_canvas = gameObject.AddComponent<Canvas>();
		m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		m_canvas.sortingOrder = 100;
		var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
		scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 0.5f;
		gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

		ApplySettings();
		CreateMainButton();
		CreateChildButtons();
		CreateColumn2Buttons();
		CreateClipboardSlots();
		ApplyPosition();

		gameObject.AddComponent<INSelectionManager>();
	}

	private void Update()
	{
		if (m_childBgs.Count == 0) return;
		bool shouldBeOn = INUserSettings.Instance?.StarSeaSettings?.ToolBookEnabled ?? false;
		if (!shouldBeOn)
		{
			if (!m_hiddenBySetting) { m_mainRect.gameObject.SetActive(false); m_hiddenBySetting = true; }
			return;
		}
		if (m_hiddenBySetting) { m_mainRect.gameObject.SetActive(true); m_hiddenBySetting = false; }

		// Active button highlight (选区)
		bool selecting = INSelectionManager.Instance != null &&
			INSelectionManager.Instance.CurrentState != INSelectionManager.State.Idle;
		int selectIdx = -1;
		for (int i = 0; i < s_toolLabels.Length; i++)
			if (s_toolLabels[i] == "选区") { selectIdx = i; break; }
		int newActive = selecting ? selectIdx : -1;
		if (newActive != m_activeChildIndex)
		{
			if (m_activeChildIndex >= 0 && m_activeChildIndex < m_childBgs.Count && m_childBgs[m_activeChildIndex] != null)
			{
				m_childBgs[m_activeChildIndex].color = s_normalBg;
				if (m_childTexts[m_activeChildIndex] != null) m_childTexts[m_activeChildIndex].color = s_normalText;
			}
			if (newActive >= 0 && newActive < m_childBgs.Count && m_childBgs[newActive] != null)
			{
				m_childBgs[newActive].color = s_activeBg;
				if (m_childTexts[newActive] != null) m_childTexts[newActive].color = s_activeText;
			}
			m_activeChildIndex = newActive;
		}

		// Undo red highlight during drag
		if (m_pasteDragging || m_clipDragging)
		{
			if (m_undoBg != null && IsOverUndoButton())
				m_undoBg.color = s_redBg;
			else
				ResetUndoColor();
		}
		else
		{
			ResetUndoColor();
		}

		// Delete red during clip drag
		if (m_clipDragging && m_deleteBg != null)
			m_deleteBg.color = IsOverDeleteButton() ? s_redBg : new Color(0.8f, 0.1f, 0.1f, 0.9f);

		UpdateDeleteButtonLabel();
		RefreshClipboardSlots();
		UpdateOverwriteToggle();
		UpdateRotateButton();
		UpdateMaterialButton();
		UpdateShapeTypeButton();
		UpdateShapeSolidButton();
		UpdateConfirmCancelVisibility();
	}

	private void ResetUndoColor()
	{
		if (m_undoBg == null) return;
		int undoIdx = -1;
		for (int i = 0; i < s_toolLabels.Length; i++)
			if (s_toolLabels[i] == "撤销") { undoIdx = i; break; }
		if (undoIdx >= 0 && undoIdx < m_childBgs.Count)
		{
			Color expected = (undoIdx == m_activeChildIndex) ? s_activeBg : s_normalBg;
			if (m_undoBg.color != expected) m_undoBg.color = expected;
		}
	}

	private bool IsOverUndoButton()
	{
		if (m_undoRect == null) return false;
		return IsOverRect(m_undoRect);
	}

	private bool IsOverDeleteButton()
	{
		if (m_deleteRect == null || m_deleteBtnObj == null || !m_deleteBtnObj.activeSelf) return false;
		return IsOverRect(m_deleteRect);
	}

	private bool IsOverRect(RectTransform rt)
	{
		Vector2 mousePos = Input.mousePosition;
		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);
		return mousePos.x >= corners[0].x && mousePos.x <= corners[2].x &&
			   mousePos.y >= corners[0].y && mousePos.y <= corners[2].y;
	}

	private void ApplySettings()
	{
		var settings = INUserSettings.Instance?.StarSeaSettings;
		if (settings == null) return;
		m_size = settings.ToolBookSize;
		if (m_size < 0.1f) m_size = 1f;
	}

	// --- Create UI ---

	private void CreateMainButton()
	{
		GameObject mainBtn = new GameObject("MainToggle");
		mainBtn.transform.SetParent(transform, false);

		float sz = 48f * m_size;
		m_mainRect = mainBtn.AddComponent<RectTransform>();
		m_mainRect.anchorMin = new Vector2(1f, 1f);
		m_mainRect.anchorMax = new Vector2(1f, 1f);
		m_mainRect.pivot = new Vector2(1f, 1f);
		m_mainRect.sizeDelta = new Vector2(sz, sz);

		UImage bg = mainBtn.AddComponent<UImage>();
		bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

		UButton btn = mainBtn.AddComponent<UButton>();
		btn.targetGraphic = bg;
		btn.onClick.AddListener(OnToggleClicked);

		EventTrigger trigger = mainBtn.AddComponent<EventTrigger>();
		AddEventTrigger(trigger, EventTriggerType.BeginDrag, OnBeginDrag);
		AddEventTrigger(trigger, EventTriggerType.Drag, OnDrag);
		AddEventTrigger(trigger, EventTriggerType.EndDrag, OnEndDrag);

		CreateLabel(mainBtn, "\u2261", 32, Color.white);
	}

	private void CreateChildButtons()
	{
		m_childRects.Clear();
		m_childBgs.Clear();
		m_childTexts.Clear();

		for (int i = 0; i < s_toolLabels.Length; i++)
		{
			string label = s_toolLabels[i];
			GameObject btn = CreateButtonObj(label, m_mainRect.transform);
			RectTransform rect = btn.GetComponent<RectTransform>();
			UImage bg = btn.GetComponent<UImage>();

			m_childBgs.Add(bg);

			UButton b = btn.GetComponent<UButton>();
			int idx = i;

			if (label == "粘贴")
			{
				EventTrigger trigger = btn.AddComponent<EventTrigger>();
				AddEventTrigger(trigger, EventTriggerType.BeginDrag, OnPasteBeginDrag);
				AddEventTrigger(trigger, EventTriggerType.Drag, OnPasteDrag);
				AddEventTrigger(trigger, EventTriggerType.EndDrag, OnPasteEndDrag);
			}
			else
			{
				b.onClick.AddListener(() => OnChildClicked(idx));
			}

			UText t = CreateLabel(btn, label, 16, Color.white);
			m_childTexts.Add(t);

			btn.SetActive(false);
			m_childRects.Add(rect);

			if (label == "撤销")
			{
				m_undoRect = rect;
				m_undoBg = bg;
			}
		}
	}

	private void CreateColumn2Buttons()
	{
		// Delete
		m_deleteBtnObj = CreateButtonObj("删除", m_mainRect.transform);
		m_deleteRect = m_deleteBtnObj.GetComponent<RectTransform>();
		m_deleteBg = m_deleteBtnObj.GetComponent<UImage>();
		m_deleteBg.color = new Color(0.8f, 0.1f, 0.1f, 0.9f);
		m_deleteBtnObj.GetComponent<UButton>().onClick.AddListener(OnDeleteClicked);
		m_deleteText = CreateLabel(m_deleteBtnObj, "删除", 16, Color.white);
		m_deleteBtnObj.SetActive(false);

		// Overwrite
		m_overwriteObj = CreateButtonObj("Overwrite", m_mainRect.transform);
		m_overwriteBg = m_overwriteObj.GetComponent<UImage>();
		m_overwriteBg.color = s_normalBg;
		m_overwriteObj.GetComponent<UButton>().onClick.AddListener(OnOverwriteToggle);
		m_overwriteText = CreateLabel(m_overwriteObj, "覆盖", 12, Color.white);
		m_overwriteObj.SetActive(false);

		// Rotate
		m_rotateObj = CreateButtonObj("Rotate", m_mainRect.transform);
		m_rotateObj.GetComponent<UImage>().color = s_normalBg;
		m_rotateObj.GetComponent<UButton>().onClick.AddListener(OnRotateClicked);
		m_rotateText = CreateLabel(m_rotateObj, "转0\u00b0", 12, Color.white);
		m_rotateObj.SetActive(false);

		// Material
		m_materialObj = CreateButtonObj("Material", m_mainRect.transform);
		m_materialBg = m_materialObj.GetComponent<UImage>();
		m_materialBg.color = s_normalBg;
		m_materialObj.GetComponent<UButton>().onClick.AddListener(OnMaterialToggle);
		m_materialText = CreateLabel(m_materialObj, "木框", 12, Color.white);
		m_materialObj.SetActive(false);

		// Shape type selector
		m_shapeTypeObj = CreateButtonObj("ShapeType", m_mainRect.transform);
		m_shapeTypeBg = m_shapeTypeObj.GetComponent<UImage>();
		m_shapeTypeBg.color = s_normalBg;
		m_shapeTypeObj.GetComponent<UButton>().onClick.AddListener(OnShapeTypeCycle);
		m_shapeTypeText = CreateLabel(m_shapeTypeObj, "方形", 12, Color.white);
		m_shapeTypeObj.SetActive(false);

		// Shape solid/hollow toggle
		m_shapeSolidObj = CreateButtonObj("ShapeSolid", m_mainRect.transform);
		m_shapeSolidBg = m_shapeSolidObj.GetComponent<UImage>();
		m_shapeSolidBg.color = s_normalBg;
		m_shapeSolidObj.GetComponent<UButton>().onClick.AddListener(OnShapeSolidToggle);
		m_shapeSolidText = CreateLabel(m_shapeSolidObj, "空心", 12, Color.white);
		m_shapeSolidObj.SetActive(false);
	}

	private void CreateClipboardSlots()
	{
		int max = 3;
		if (INUserSettings.Instance?.StarSeaSettings != null)
			max = INUserSettings.Instance.StarSeaSettings.ToolBookClipboardMax;
		if (max < 1) max = 3;
		for (int i = 0; i < max; i++)
			CreateClipSlotObj(i);
	}

	private void CreateClipSlotObj(int idx)
	{
		float w = 48f * m_size;
		float h = 36f * m_size;

		GameObject slotBtn = new GameObject("Clip" + (idx + 1));
		slotBtn.transform.SetParent(m_mainRect.transform, false);
		RectTransform rect = slotBtn.AddComponent<RectTransform>();
		rect.anchorMin = new Vector2(1f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(1f, 1f);
		rect.sizeDelta = new Vector2(w, h);

		UImage bg = slotBtn.AddComponent<UImage>();
		bg.color = s_clipEmptyBg;
		m_clipSlotBgs.Add(bg);

		// No UButton - use EventTrigger only to avoid click/drag conflict
		EventTrigger trigger = slotBtn.AddComponent<EventTrigger>();
		int slotIdx = idx;
		AddEventTrigger(trigger, EventTriggerType.BeginDrag, (data) => OnClipSlotBeginDrag(data, slotIdx));
		AddEventTrigger(trigger, EventTriggerType.Drag, OnClipSlotDrag);
		AddEventTrigger(trigger, EventTriggerType.EndDrag, OnClipSlotEndDrag);

		UText t = CreateLabel(slotBtn, "-", 16, Color.white);
		m_clipSlotTexts.Add(t);

		slotBtn.SetActive(false);
		m_clipSlotObjs.Add(slotBtn);
		m_clipSlotRects.Add(rect);
	}

	// --- Helpers ---

	private GameObject CreateButtonObj(string name, Transform parent)
	{
		float w = 48f * m_size;
		float h = 36f * m_size;
		GameObject btn = new GameObject(name);
		btn.transform.SetParent(parent, false);
		RectTransform rect = btn.AddComponent<RectTransform>();
		rect.anchorMin = new Vector2(1f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(1f, 1f);
		rect.sizeDelta = new Vector2(w, h);
		rect.anchoredPosition = Vector2.zero;
		UImage bg = btn.AddComponent<UImage>();
		bg.color = s_normalBg;
		UButton b = btn.AddComponent<UButton>();
		b.targetGraphic = bg;
		return btn;
	}

	private UText CreateLabel(GameObject parent, string text, int fontSize, Color color)
	{
		GameObject txt = new GameObject("Text");
		txt.transform.SetParent(parent.transform, false);
		RectTransform txtRect = txt.AddComponent<RectTransform>();
		txtRect.anchorMin = Vector2.zero;
		txtRect.anchorMax = Vector2.one;
		txtRect.sizeDelta = Vector2.zero;
		UText t = txt.AddComponent<UText>();
		t.text = text;
		t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		t.fontSize = Mathf.RoundToInt(fontSize * m_size);
		t.alignment = TextAnchor.MiddleCenter;
		t.color = color;
		return t;
	}

	private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
	{
		var entry = new EventTrigger.Entry { eventID = type };
		entry.callback.AddListener(callback);
		trigger.triggers.Add(entry);
	}

	private void RefreshClipboardSlots()
	{
		if (!m_isOpen) return;
		var sm = INSelectionManager.Instance;
		if (sm == null) return;

		var slots = sm.ClipboardSlots;
		var slotIds = sm.ClipboardSlotIds;

		for (int i = 0; i < m_clipSlotObjs.Count; i++)
		{
			bool hasData = i < slots.Count;
			m_clipSlotBgs[i].color = hasData ? s_clipFilledBg : s_clipEmptyBg;
			m_clipSlotTexts[i].text = hasData && i < slotIds.Count ? slotIds[i].ToString() : "-";
		}

		// Reposition column 3
		float spacing = 6f * m_size;
		float btnH = 36f * m_size;
		float mainW = m_mainRect.sizeDelta.x;
		float btnW = 48f * m_size;
		float col3X = -(mainW + spacing + btnW + spacing + btnW + spacing);

		for (int i = 0; i < m_clipSlotObjs.Count; i++)
		{
			if (m_clipSlotObjs[i].activeSelf)
				m_clipSlotRects[i].anchoredPosition = new Vector2(col3X, -(i * (btnH + spacing)));
		}
	}

	private void ApplyPosition()
	{
		var settings = INUserSettings.Instance?.StarSeaSettings;
		if (settings == null) return;
		m_mainRect.anchoredPosition = new Vector2(settings.ToolBookPosX, settings.ToolBookPosY);
	}

	private void SavePosition()
	{
		var settings = INUserSettings.Instance?.StarSeaSettings;
		if (settings == null) return;
		settings.ToolBookPosX = m_mainRect.anchoredPosition.x;
		settings.ToolBookPosY = m_mainRect.anchoredPosition.y;
		INUserSettings.Save();
	}

	// --- Button handlers ---

	private void OnToggleClicked()
	{
		if (m_isDragging || m_coroutineRunning) return;
		if (!m_isOpen) ForceCloseToolbox();
		Toggle();
	}

	private void ForceCloseToolbox()
	{
		var toolboxBtn = FindSceneObjectOfType<ToolboxButton>();
		if (toolboxBtn != null) toolboxBtn.gameObject.SetActive(false);
		var tutorialBtn = GameObject.Find("TutorialButton");
		if (tutorialBtn != null) tutorialBtn.SetActive(false);
	}

	private void OnChildClicked(int index)
	{
		string label = s_toolLabels[index];
		var sm = INSelectionManager.Instance;
		if (sm == null) return;

		switch (label)
		{
			case "复制": EventManager.Send(new UIEvent(UIEvent.Type.ToolBookCopy)); break;
			case "选区":
				if (sm.CurrentState == INSelectionManager.State.Idle) sm.EnterSelectionMode();
				else sm.ExitSelectionMode();
				break;
			case "撤销": sm.Undo(); break;
			case "暂存": sm.StashContraption(); break;
			case "存剪": EventManager.Send(new UIEvent(UIEvent.Type.ToolBookSaveClipboard)); break;
			case "形状":
				if (sm.CurrentState == INSelectionManager.State.ShapeDrawing)
					sm.CancelShape();
				else
					sm.EnterShapeDrawing();
				break;
			case "填充":
				if (sm.ShapePreviewCount > 0)
					sm.FillShapes();
				else
					sm.FillWithSelectedPart();
				break;
		}
	}

	private void OnOverwriteToggle()
	{
		var settings = INUserSettings.Instance?.StarSeaSettings;
		if (settings == null) return;
		settings.ToolBookPasteOverwriteMode = (settings.ToolBookPasteOverwriteMode + 1) % 3;
		INUserSettings.Save();
	}

	private void OnRotateClicked()
	{
		if (INSelectionManager.Instance != null)
			INSelectionManager.Instance.CyclePasteRotation();
	}

	private void OnMaterialToggle()
	{
		var sm = INSelectionManager.Instance;
		if (sm == null) return;
		sm.CycleMaterialIndex();
	}

	private void OnShapeTypeCycle()
	{
		var sm = INSelectionManager.Instance;
		if (sm == null) return;
		sm.CycleShapeType();
	}

	private void OnShapeSolidToggle()
	{
		var sm = INSelectionManager.Instance;
		if (sm == null) return;
		sm.ToggleShapeSolid();
	}

	private void OnDeleteClicked()
	{
		var sm = INSelectionManager.Instance;
		if (sm == null) return;
		if (sm.CurrentState == INSelectionManager.State.Selected)
			sm.DeleteSelection();
	}

	private void OnConfirmShape() { INSelectionManager.Instance?.ConfirmShape(); }
	private void OnCancelShape() { INSelectionManager.Instance?.CancelShape(); }

	private void UpdateDeleteButtonLabel()
	{
		if (m_deleteText == null) return;
		var sm = INSelectionManager.Instance;
		if (sm == null) return;
		if (sm.CurrentState == INSelectionManager.State.Selected)
			m_deleteText.text = "删选区";
		else
			m_deleteText.text = "删除";
	}

	private void UpdateOverwriteToggle()
	{
		if (m_overwriteText == null) return;
		int mode = INUserSettings.Instance?.StarSeaSettings?.ToolBookPasteOverwriteMode ?? 1;
		switch (mode)
		{
			case 0: m_overwriteText.text = "不覆盖"; m_overwriteBg.color = s_normalBg; break;
			case 1: m_overwriteText.text = "覆盖"; m_overwriteBg.color = new Color(0.15f, 0.5f, 0.15f, 0.9f); break;
			case 2: m_overwriteText.text = "全覆盖"; m_overwriteBg.color = new Color(0.7f, 0.4f, 0.1f, 0.9f); break;
		}
	}

	private void UpdateRotateButton()
	{
		if (m_rotateText == null) return;
		int rot = INSelectionManager.Instance != null ? INSelectionManager.Instance.PasteRotation : 0;
		m_rotateText.text = "转" + (rot * 90) + "\u00b0";
	}

	private void UpdateMaterialButton()
	{
		if (m_materialText == null || m_materialBg == null) return;
		var sm = INSelectionManager.Instance;
		if (sm == null) return;
		bool on = sm.MaterialOverride;
		int idx = sm.MaterialIndex;
		string[] names = { "木框", "金属", "自定2", "自定3" };
		m_materialText.text = on ? names[idx] : "材质";
		m_materialBg.color = on ? new Color(0.5f, 0.3f, 0.1f, 0.9f) : s_normalBg;
	}

	private void UpdateShapeTypeButton()
	{
		if (m_shapeTypeText == null) return;
		var sm = INSelectionManager.Instance;
		if (sm == null) return;
		int shapeIdx = (int)sm.CurrentShape;
		if (shapeIdx >= 0 && shapeIdx < s_shapeNames.Length)
			m_shapeTypeText.text = s_shapeNames[shapeIdx];
	}

	private void UpdateShapeSolidButton()
	{
		if (m_shapeSolidText == null) return;
		var sm = INSelectionManager.Instance;
		if (sm == null) return;
		bool solid = sm.ShapeSolid;
		m_shapeSolidText.text = solid ? "实心" : "空心";
		m_shapeSolidBg.color = solid ? new Color(0.5f, 0.3f, 0.1f, 0.9f) : s_normalBg;
	}

	private void UpdateConfirmCancelVisibility()
	{
		bool show = INSelectionManager.Instance != null &&
			INSelectionManager.Instance.CurrentState == INSelectionManager.State.ShapeDrawing;
		if (m_confirmObj != null) m_confirmObj.SetActive(m_isOpen && show);
		if (m_cancelObj != null) m_cancelObj.SetActive(m_isOpen && show);
	}

	// --- Clipboard Slot Drag (handles both click and drag) ---

	private void OnClipSlotBeginDrag(BaseEventData data, int index)
	{
		var sm = INSelectionManager.Instance;
		if (sm == null || index >= sm.ClipboardSlots.Count) return;
		PointerEventData ped = (PointerEventData)data;
		m_clipDragStartScreenPos = ped.position;
		m_clipDragging = true;
		m_clipDragIndex = index;
		sm.LoadClipboardSlot(index);
		sm.StartPastePreview();
	}

	private void OnClipSlotDrag(BaseEventData data) { }

	private void OnClipSlotEndDrag(BaseEventData data)
	{
		if (!m_clipDragging) return;
		m_clipDragging = false;
		PointerEventData ped = (PointerEventData)data;

		if (IsOverDeleteButton())
		{
			// Drag to delete = remove the clipboard slot
			INSelectionManager.Instance?.DeleteClipboardSlot(m_clipDragIndex);
			INSelectionManager.Instance?.CancelPaste();
		}
		else if (IsOverUndoButton())
		{
			// Drag to undo = cancel the paste
			INSelectionManager.Instance?.CancelPaste();
		}
		else if (Vector2.Distance(ped.position, m_clipDragStartScreenPos) > 20f)
		{
			// Drag far enough = confirm paste
			INSelectionManager.Instance?.ConfirmPaste();
		}
		// else: click (distance < 20) = keep paste preview active (BeginDrag already started it)
		m_clipDragIndex = -1;
	}

	// --- Paste Drag ---

	private void OnPasteBeginDrag(BaseEventData data)
	{
		PointerEventData ped = (PointerEventData)data;
		m_pasteDragStartScreenPos = ped.position;
		m_pasteDragging = true;
		EventManager.Send(new UIEvent(UIEvent.Type.ToolBookPaste));
	}

	private void OnPasteDrag(BaseEventData data) { }

	private void OnPasteEndDrag(BaseEventData data)
	{
		if (!m_pasteDragging) return;
		m_pasteDragging = false;
		PointerEventData ped = (PointerEventData)data;

		if (IsOverUndoButton())
			EventManager.Send(new UIEvent(UIEvent.Type.ToolBookPasteCancel));
		else if (Vector2.Distance(ped.position, m_pasteDragStartScreenPos) > 30f)
			EventManager.Send(new UIEvent(UIEvent.Type.ToolBookPasteConfirm));
		else
			EventManager.Send(new UIEvent(UIEvent.Type.ToolBookPasteCancel));
	}

	// --- Main Button Drag ---

	private void OnBeginDrag(BaseEventData data) { m_isDragging = true; }

	private void OnDrag(BaseEventData data)
	{
		PointerEventData ped = (PointerEventData)data;
		m_mainRect.anchoredPosition += ped.delta / m_canvas.scaleFactor;
	}

	private void OnEndDrag(BaseEventData data)
	{
		SavePosition();
		StartCoroutine(ResetDragging());
	}

	private IEnumerator ResetDragging()
	{
		yield return null;
		m_isDragging = false;
	}

	// --- Open/Close ---

	public void Toggle()
	{
		if (m_coroutineRunning) return;
		ApplySettings();
		float sz = 48f * m_size;
		m_mainRect.sizeDelta = new Vector2(sz, sz);

		if (m_isOpen) { m_isOpen = false; StartCoroutine(SlideClose()); }
		else { m_isOpen = true; StartCoroutine(SlideOpen()); }
	}

	// Layout: Col1=child buttons | Col2=delete/overwrite/rotate/material/shapeType/shapeSolid | Col3=clipboard
	private IEnumerator SlideOpen()
	{
		m_coroutineRunning = true;
		float mainW = m_mainRect.sizeDelta.x;
		float spacing = 6f * m_size;
		float btnW = 48f * m_size;
		float btnH = 36f * m_size;

		float col1X = -(mainW + spacing);
		float col2X = -(mainW + spacing + btnW + spacing);
		float col3X = -(mainW + spacing + btnW + spacing + btnW + spacing);

		// Column 1: child buttons
		for (int i = 0; i < m_childRects.Count; i++)
		{
			float targetY = -(i * (btnH + spacing));
			m_childRects[i].anchoredPosition = new Vector2(0f, targetY);
			m_childRects[i].gameObject.SetActive(true);
			StartCoroutine(SlideTo(m_childRects[i], new Vector2(col1X, targetY)));
		}

		// Column 2: delete(0) → overwrite(1) → rotate(2) → material(3) → shapeType(4) → shapeSolid(5)
		float col2Y = 0f;
		System.Action<GameObject, float> slideCol2 = (obj, y) =>
		{
			if (obj == null) return;
			RectTransform rt = obj.GetComponent<RectTransform>();
			rt.anchoredPosition = new Vector2(0f, 0f);
			obj.SetActive(true);
			StartCoroutine(SlideTo(rt, new Vector2(col2X, y)));
		};
		slideCol2(m_deleteBtnObj, col2Y);
		col2Y -= (btnH + spacing);
		slideCol2(m_overwriteObj, col2Y);
		col2Y -= (btnH + spacing);
		slideCol2(m_rotateObj, col2Y);
		col2Y -= (btnH + spacing);
		slideCol2(m_materialObj, col2Y);
		col2Y -= (btnH + spacing);
		slideCol2(m_shapeTypeObj, col2Y);
		col2Y -= (btnH + spacing);
		slideCol2(m_shapeSolidObj, col2Y);

		// Column 3: clipboard slots
		for (int i = 0; i < m_clipSlotObjs.Count; i++)
		{
			float slotTargetY = -(i * (btnH + spacing));
			m_clipSlotObjs[i].SetActive(true);
			m_clipSlotRects[i].anchoredPosition = new Vector2(0f, 0f);
			StartCoroutine(SlideTo(m_clipSlotRects[i], new Vector2(col3X, slotTargetY)));
		}

		yield return new WaitForSeconds(0.3f);
		m_coroutineRunning = false;
	}

	private IEnumerator SlideClose()
	{
		m_coroutineRunning = true;
		for (int i = 0; i < m_childRects.Count; i++)
			StartCoroutine(SlideTo(m_childRects[i], new Vector2(0f, m_childRects[i].anchoredPosition.y), deactivate: true));
		for (int i = 0; i < m_clipSlotObjs.Count; i++)
			StartCoroutine(SlideTo(m_clipSlotRects[i], new Vector2(0f, m_clipSlotRects[i].anchoredPosition.y), deactivate: true));

		GameObject[] col2Objs = { m_deleteBtnObj, m_overwriteObj, m_rotateObj, m_materialObj, m_shapeTypeObj, m_shapeSolidObj };
		foreach (var obj in col2Objs)
		{
			if (obj != null)
			{
				RectTransform rt = obj.GetComponent<RectTransform>();
				StartCoroutine(SlideTo(rt, new Vector2(0f, rt.anchoredPosition.y), deactivate: true));
			}
		}

		if (m_confirmObj != null) m_confirmObj.SetActive(false);
		if (m_cancelObj != null) m_cancelObj.SetActive(false);

		yield return new WaitForSeconds(0.3f);
		m_coroutineRunning = false;
	}

	private IEnumerator SlideTo(RectTransform rt, Vector2 target, bool deactivate = false)
	{
		Vector2 start = rt.anchoredPosition;
		float step = 0f;
		float dist = Vector2.Distance(start, target);
		if (dist < 1f) dist = 1f;
		float duration = dist / m_slideSpeed;

		while (step < duration)
		{
			step += Time.unscaledDeltaTime;
			rt.anchoredPosition = Vector2.Lerp(start, target, step / duration);
			yield return null;
		}
		rt.anchoredPosition = target;

		if (deactivate) rt.gameObject.SetActive(false);
	}

	private static T FindSceneObjectOfType<T>() where T : Object
	{
		return FindObjectOfType<T>();
	}
}
