using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class INSelectionManager : MonoBehaviour
{
	public enum State
	{
		Idle,
		Selecting,
		Selected,
		PastePreview,
		ShapeDrawing
	}

	public enum ShapeType
	{
		Square,
		Rectangle,
		Circle,
		Ellipse,
		Line
	}

	public const int OVERWRITE_NONE = 0;
	public const int OVERWRITE_NORMAL = 1;
	public const int OVERWRITE_FULL = 2;

	private State m_state = State.Idle;
	private Vector3 m_selectStart;
	private Vector3 m_selectEnd;
	private int m_minX, m_minY, m_maxX, m_maxY;
	private bool m_isDragging;
	private bool m_ready;
	private int m_waitFrames;

	private List<GameObject> m_selectionPreviews = new List<GameObject>();
	private List<GameObject> m_pastePreviews = new List<GameObject>();
	private List<INContraptionData> m_undoStack = new List<INContraptionData>();

	private INContraptionData m_clipboard;
	private List<INContraptionData> m_clipboardSlots = new List<INContraptionData>();
	private List<int> m_clipboardSlotIds = new List<int>();
	private int m_nextSlotId = 1;

	private Material m_selectionMat;
	private Material m_pasteMat;
	private Material m_pasteBlockedMat;
	private Material m_shapeMat;

	private Camera m_mainCamera;

	// Paste rotation
	private int m_pasteRotation;

	// Shape drawing
	private ShapeType m_currentShape = ShapeType.Rectangle;
	private bool m_shapeSolid = false;
	private Vector3 m_shapeStart;
	private Vector3 m_shapeEnd;
	private bool m_shapeDragging;

	// Material selection
	private bool m_materialOverride = false;
	private int m_materialIndex = 0;

	// Stash
	private INContraptionData m_stash;

	// Shape persistence: confirmed shape layers that stay on screen permanently
	private List<List<GameObject>> m_shapePreviewLayers = new List<List<GameObject>>();
	// Cell sets for each shape layer (for Fill)
	private List<HashSet<Vector2Int>> m_shapeCellSets = new List<HashSet<Vector2Int>>();
	// Origin offsets for each shape layer
	private List<Vector3> m_shapeOrigins = new List<Vector3>();

	private static INSelectionManager s_instance;
	public static INSelectionManager Instance => s_instance;
	public static bool IsInteracting => s_instance != null && s_instance.m_state != State.Idle;

	public State CurrentState => m_state;
	public INContraptionData Clipboard => m_clipboard;
	public List<INContraptionData> ClipboardSlots => m_clipboardSlots;
	public List<int> ClipboardSlotIds => m_clipboardSlotIds;
	public int PasteRotation => m_pasteRotation;
	public ShapeType CurrentShape => m_currentShape;
	public bool ShapeSolid => m_shapeSolid;
	public int PasteOverwriteMode => INUserSettings.Instance?.StarSeaSettings?.ToolBookPasteOverwriteMode ?? 1;
	public bool MaterialOverride => m_materialOverride;
	public int MaterialIndex => m_materialIndex;
	public INContraptionData Stash => m_stash;
	public int ShapePreviewCount => m_shapePreviewLayers.Count;

	private void Awake()
	{
		s_instance = this;
		m_mainCamera = Camera.main;
		CreateMaterials();
		LoadClipboards();
	}

	private void OnDestroy()
	{
		if (s_instance == this) s_instance = null;
		ClearPreviews(m_selectionPreviews);
		ClearPreviews(m_pastePreviews);
		ClearAllShapePreviews();
	}

	private void CreateMaterials()
	{
		Shader shader = Shader.Find("Sprites/Default");
		if (shader == null) shader = Shader.Find("UI/Default");

		m_selectionMat = new Material(shader);
		m_selectionMat.color = new Color(0.3f, 0.5f, 1f, 0.35f);

		m_pasteMat = new Material(shader);
		m_pasteMat.color = new Color(0.3f, 1f, 0.3f, 0.4f);

		m_pasteBlockedMat = new Material(shader);
		m_pasteBlockedMat.color = new Color(1f, 0.2f, 0.2f, 0.4f);

		m_shapeMat = new Material(shader);
		m_shapeMat.color = new Color(1f, 0.8f, 0.2f, 0.4f);
	}

	private void Update()
	{
		switch (m_state)
		{
			case State.Selecting:
				if (!m_ready)
				{
					m_waitFrames++;
					if (m_waitFrames > 3) m_ready = true;
					return;
				}
				UpdateSelecting();
				break;
			case State.PastePreview:
				UpdatePastePreview();
				break;
			case State.ShapeDrawing:
				UpdateShapeDrawing();
				break;
		}
	}

	// --- Frame detection ---

	private static bool IsFrameType(int sortedType)
	{
		return sortedType == 5 || sortedType == 6;
	}

	// --- Serializable wrappers for JSON ---

	[System.Serializable]
	private class SerializableUnit
	{
		public int Type;
		public int Index;
		public int X;
		public int Y;
		public int Rotation;
		public bool Flipped;

		public SerializableUnit() { }
		public SerializableUnit(INContraptionData.Unit u)
		{
			Type = u.Type;
			Index = u.Index;
			X = u.X;
			Y = u.Y;
			Rotation = u.Rotation;
			Flipped = u.Flipped;
		}

		public INContraptionData.Unit ToUnit()
		{
			return new INContraptionData.Unit(Type, Index, X, Y, Rotation, Flipped);
		}
	}

	[System.Serializable]
	private class SerializableContraptionData
	{
		public List<SerializableUnit> Units = new List<SerializableUnit>();

		public SerializableContraptionData() { }
		public SerializableContraptionData(INContraptionData data)
		{
			if (data == null || data.Units == null) return;
			foreach (var u in data.Units)
				Units.Add(new SerializableUnit(u));
		}

		public INContraptionData ToContraptionData()
		{
			var data = new INContraptionData(Units.Count);
			foreach (var su in Units)
				data.Units.Add(su.ToUnit());
			return data;
		}
	}

	[System.Serializable]
	private class ClipboardWrapper
	{
		public List<SerializableContraptionData> Slots = new List<SerializableContraptionData>();
		public List<int> SlotIds = new List<int>();
		public int NextId;
	}

	// --- Clipboard Slots ---

	// SaveToClipboard: directly copies the entire contraption into a new slot
	// No prior CopySelected needed - independent of mouse-held part
	public void SaveToClipboard()
	{
		INContraptionData snapshot = INContraption.CopyContraption();
		if (snapshot == null || snapshot.Units.Count == 0)
		{
			Debug.Log("Nothing to save - contraption is empty");
			return;
		}
		int max = 3;
		if (INUserSettings.Instance?.StarSeaSettings != null)
			max = INUserSettings.Instance.StarSeaSettings.ToolBookClipboardMax;
		if (m_clipboardSlots.Count >= max && m_clipboardSlots.Count > 0)
		{
			m_clipboardSlots.RemoveAt(0);
			if (m_clipboardSlotIds.Count > 0) m_clipboardSlotIds.RemoveAt(0);
			Debug.Log("Clipboard full, evicted oldest slot");
		}
		m_clipboardSlots.Add(snapshot);
		m_clipboardSlotIds.Add(m_nextSlotId++);
		SaveClipboards();
		Debug.Log("Saved to clipboard slot " + m_clipboardSlotIds[m_clipboardSlotIds.Count - 1] + " (" + snapshot.Units.Count + " parts)");
	}

	public void DeleteClipboardSlot(int index)
	{
		if (index < 0 || index >= m_clipboardSlots.Count) return;
		int removedId = m_clipboardSlotIds[index];
		m_clipboardSlots.RemoveAt(index);
		m_clipboardSlotIds.RemoveAt(index);
		SaveClipboards();
		Debug.Log("Deleted clipboard slot " + removedId);
	}

	public void LoadClipboardSlot(int index)
	{
		if (index < 0 || index >= m_clipboardSlots.Count) return;
		m_clipboard = m_clipboardSlots[index];
		m_pasteRotation = 0;
	}

	private void SaveClipboards()
	{
		try
		{
			string path = INUnity.SettingsPath + "/ToolBookClipboard.json";
			var wrapper = new ClipboardWrapper();
			for (int i = 0; i < m_clipboardSlots.Count; i++)
			{
				wrapper.Slots.Add(new SerializableContraptionData(m_clipboardSlots[i]));
			}
			wrapper.SlotIds = new List<int>(m_clipboardSlotIds);
			wrapper.NextId = m_nextSlotId;
			string json = UnityEngine.JsonUtility.ToJson(wrapper, true);
			System.IO.File.WriteAllText(path, json);
			Debug.Log("Clipboards saved to " + path + " (" + wrapper.Slots.Count + " slots)");
		}
		catch (System.Exception e)
		{
			Debug.LogWarning("Failed to save clipboard: " + e.Message);
		}
	}

	private void LoadClipboards()
	{
		try
		{
			string path = INUnity.SettingsPath + "/ToolBookClipboard.json";
			if (!System.IO.File.Exists(path)) return;
			string json = System.IO.File.ReadAllText(path);
			var wrapper = UnityEngine.JsonUtility.FromJson<ClipboardWrapper>(json);
			if (wrapper != null && wrapper.Slots != null && wrapper.Slots.Count > 0)
			{
				m_clipboardSlots.Clear();
				m_clipboardSlotIds.Clear();
				foreach (var s in wrapper.Slots)
					m_clipboardSlots.Add(s.ToContraptionData());

				if (wrapper.SlotIds != null && wrapper.SlotIds.Count == m_clipboardSlots.Count)
					m_clipboardSlotIds = wrapper.SlotIds;
				else
				{
					m_clipboardSlotIds.Clear();
					for (int i = 0; i < m_clipboardSlots.Count; i++)
						m_clipboardSlotIds.Add(i + 1);
				}

				m_nextSlotId = wrapper.NextId > 0 ? wrapper.NextId : m_clipboardSlotIds[m_clipboardSlotIds.Count - 1] + 1;
				Debug.Log("Loaded " + m_clipboardSlots.Count + " clipboard slots from " + path);
			}
		}
		catch (System.Exception e)
		{
			Debug.LogWarning("Failed to load clipboard: " + e.Message);
		}
	}

	// --- Stash ---

	public void StashContraption()
	{
		m_stash = INContraption.CopyContraption();
		Debug.Log("Stashed contraption (" + m_stash.Units.Count + " parts)");
	}

	public void PasteStash()
	{
		if (m_stash == null || m_stash.Units.Count == 0)
		{
			Debug.Log("Nothing in stash");
			return;
		}
		m_clipboard = m_stash;
		m_pasteRotation = 0;
		StartPastePreview();
	}

	public bool HasStash => m_stash != null && m_stash.Units.Count > 0;

	// --- Material Override ---

	public void ToggleMaterialOverride()
	{
		m_materialOverride = !m_materialOverride;
		Debug.Log("Material override: " + (m_materialOverride ? "ON" : "OFF"));
	}

	public void CycleMaterialIndex()
	{
		m_materialIndex = (m_materialIndex + 1) % 4;
		Debug.Log("Material index: " + m_materialIndex);
	}

	// --- Rotation ---

	public void CyclePasteRotation()
	{
		m_pasteRotation = (m_pasteRotation + 1) % 4;
		Debug.Log("Paste rotation: " + (m_pasteRotation * 90) + "\u00b0");
	}

	private INContraptionData GetRotatedClipboard()
	{
		if (m_clipboard == null || m_clipboard.Units.Count == 0) return m_clipboard;
		if (m_pasteRotation == 0) return m_clipboard;

		int cx = 0, cy = 0;
		foreach (var u in m_clipboard.Units) { cx += u.X; cy += u.Y; }
		cx /= m_clipboard.Units.Count;
		cy /= m_clipboard.Units.Count;

		INContraptionData rotated = new INContraptionData();
		foreach (var u in m_clipboard.Units)
		{
			int newX = u.X, newY = u.Y;
			int newRot = (u.Rotation + m_pasteRotation) % 4;
			bool newFlipped = u.Flipped;

			int dx = u.X - cx;
			int dy = u.Y - cy;

			switch (m_pasteRotation)
			{
				case 1: newX = cx + dy; newY = cy - dx; newFlipped = !newFlipped; break;
				case 2: newX = cx - dx; newY = cy - dy; break;
				case 3: newX = cx - dy; newY = cy + dx; newFlipped = !newFlipped; break;
			}

			rotated.Units.Add(new INContraptionData.Unit(u.Type, u.Index, newX, newY, newRot, newFlipped));
		}
		return rotated;
	}

	// --- State Transitions ---

	public void EnterSelectionMode()
	{
		if (m_state == State.PastePreview) CancelPaste();
		if (m_state == State.ShapeDrawing) ExitShapeDrawing();
		ClearPreviews(m_selectionPreviews);
		m_state = State.Selecting;
		m_clipboard = null;
		m_isDragging = false;
		m_ready = false;
		m_waitFrames = 0;
	}

	public void ExitSelectionMode()
	{
		ClearPreviews(m_selectionPreviews);
		m_state = State.Idle;
		m_isDragging = false;
	}

	// --- Shape Drawing ---

	public void SetShapeType(ShapeType type)
	{
		m_currentShape = type;
		Debug.Log("Shape: " + type);
	}

	public void CycleShapeType()
	{
		m_currentShape = (ShapeType)(((int)m_currentShape + 1) % 5);
		Debug.Log("Shape: " + m_currentShape);
	}

	public void ToggleShapeSolid()
	{
		m_shapeSolid = !m_shapeSolid;
		Debug.Log("Shape solid: " + m_shapeSolid);
	}

	public void EnterShapeDrawing()
	{
		if (m_state == State.PastePreview) CancelPaste();
		if (m_state == State.Selecting) ExitSelectionMode();
		m_state = State.ShapeDrawing;
		m_shapeDragging = false;
		m_ready = false;
		m_waitFrames = 0;
	}

	private void ExitShapeDrawing()
	{
		m_shapeDragging = false;
		m_state = State.Idle;
	}

	private void UpdateShapeDrawing()
	{
		if (!m_ready)
		{
			m_waitFrames++;
			if (m_waitFrames > 3) m_ready = true;
			return;
		}

		if (Input.GetMouseButtonDown(0))
		{
			m_shapeStart = GetWorldPos(Input.mousePosition);
			m_shapeDragging = true;
		}

		if (m_shapeDragging && Input.GetMouseButton(0))
		{
			m_shapeEnd = GetWorldPos(Input.mousePosition);
			UpdateShapePreview();
		}

		if (Input.GetMouseButtonUp(0) && m_shapeDragging)
		{
			m_shapeDragging = false;
			m_shapeEnd = GetWorldPos(Input.mousePosition);
			// Auto-confirm shape on mouse release
			ConfirmShape();
		}
	}

	private void UpdateShapePreview()
	{
		ClearPreviews(m_selectionPreviews);

		Vector3 origin = WPFMonoBehaviour.levelManager.StartingPosition;
		int x0 = Mathf.FloorToInt(m_shapeStart.x - origin.x + 0.5f);
		int y0 = Mathf.FloorToInt(m_shapeStart.y - origin.y + 0.5f);
		int x1 = Mathf.FloorToInt(m_shapeEnd.x - origin.x + 0.5f);
		int y1 = Mathf.FloorToInt(m_shapeEnd.y - origin.y + 0.5f);

		HashSet<Vector2Int> cells = GetShapeCells(x0, y0, x1, y1, m_currentShape, m_shapeSolid);

		foreach (var cell in cells)
		{
			CreateQuad(m_selectionPreviews, m_shapeMat, cell.x, cell.y);
		}
	}

	public void ConfirmShape()
	{
		if (m_state != State.ShapeDrawing) return;

		// Move preview quads to a persistent layer
		List<GameObject> layer = new List<GameObject>(m_selectionPreviews);
		m_selectionPreviews.Clear(); // Don't destroy them

		if (layer.Count == 0)
		{
			ExitShapeDrawing();
			return;
		}

		Vector3 origin = WPFMonoBehaviour.levelManager.StartingPosition;
		int x0 = Mathf.FloorToInt(m_shapeStart.x - origin.x + 0.5f);
		int y0 = Mathf.FloorToInt(m_shapeStart.y - origin.y + 0.5f);
		int x1 = Mathf.FloorToInt(m_shapeEnd.x - origin.x + 0.5f);
		int y1 = Mathf.FloorToInt(m_shapeEnd.y - origin.y + 0.5f);

		HashSet<Vector2Int> cells = GetShapeCells(x0, y0, x1, y1, m_currentShape, m_shapeSolid);

		m_shapePreviewLayers.Add(layer);
		m_shapeCellSets.Add(cells);
		m_shapeOrigins.Add(origin);

		Debug.Log("Shape confirmed: " + m_currentShape + (m_shapeSolid ? " solid" : " hollow") + " (" + cells.Count + " cells, " + m_shapePreviewLayers.Count + " layers total)");
		ExitShapeDrawing();
	}

	public void CancelShape()
	{
		ClearPreviews(m_selectionPreviews);
		ExitShapeDrawing();
	}

	// Fill all persistent shapes with parts
	public void FillShapes()
	{
		if (m_shapePreviewLayers.Count == 0)
		{
			Debug.Log("No shapes to fill");
			return;
		}

		SaveUndoState();

		var proto = WPFMonoBehaviour.levelManager.CurrentGameMode?.ContraptionProto;
		int overwriteMode = PasteOverwriteMode;
		int totalPlaced = 0;

		// Use the part type from the currently selected ConstructionUI part (if available)
		// This is independent of what the mouse is "holding" - we just read the part list
		SortedPartType placeType = SortedPartType.WoodenFrame;
		int customPartIndex = 0;
		if (m_materialOverride && m_materialIndex > 0)
		{
			placeType = (SortedPartType)(5 + m_materialIndex - 1);
		}

		foreach (var cells in m_shapeCellSets)
		{
			foreach (var cell in cells)
			{
				if (overwriteMode == OVERWRITE_NONE && proto != null && proto.FindPartAt(cell.x, cell.y) != null)
					continue;

				BasePart existing = proto?.FindPartAt(cell.x, cell.y);
				if (overwriteMode == OVERWRITE_NORMAL && existing != null && IsFrameType((int)existing.Type.ToSortedPartType()))
					continue;

				if (existing != null && (overwriteMode == OVERWRITE_FULL || overwriteMode == OVERWRITE_NORMAL))
				{
					proto.RemovePart(existing);
					Object.Destroy(existing.gameObject);
				}

				ConstructionUI.PartDesc partDesc = WPFMonoBehaviour.levelManager.ConstructionUI.FindPartDesc(placeType.ToPartType());
				if (partDesc != null)
				{
					BasePart customPart = WPFMonoBehaviour.gameData.GetCustomPart(partDesc.part.m_partType, customPartIndex);
					if (customPart != null)
					{
						WPFMonoBehaviour.levelManager.BuildPart(cell.x, cell.y, 0, false, customPart);
						partDesc.useCount++;
						totalPlaced++;
					}
				}
			}
		}

		Debug.Log("Filled " + m_shapePreviewLayers.Count + " shapes with " + totalPlaced + " parts");
	}

	// Remove the last shape layer (undo)
	public void UndoLastShape()
	{
		if (m_shapePreviewLayers.Count == 0)
		{
			Debug.Log("No shape layers to undo");
			return;
		}

		int lastIdx = m_shapePreviewLayers.Count - 1;
		ClearPreviews(m_shapePreviewLayers[lastIdx]);
		m_shapePreviewLayers.RemoveAt(lastIdx);
		m_shapeCellSets.RemoveAt(lastIdx);
		m_shapeOrigins.RemoveAt(lastIdx);

		Debug.Log("Removed last shape layer (" + m_shapePreviewLayers.Count + " remaining)");
	}

	// --- Fill: auto-place the currently selected build part at cursor ---

	public void FillWithSelectedPart()
	{
		ConstructionUI ui = WPFMonoBehaviour.levelManager.ConstructionUI;
		if (ui == null)
		{
			Debug.Log("No ConstructionUI found");
			return;
		}

		var partDescs = ui.PartDescriptors;
		ConstructionUI.PartDesc selectedDesc = null;

		// Find the part at the mouse cursor position to determine what to place
		Vector3 worldPos = GetWorldPos(Input.mousePosition);
		Vector3 origin = WPFMonoBehaviour.levelManager.StartingPosition;
		int targetX = Mathf.FloorToInt(worldPos.x - origin.x + 0.5f);
		int targetY = Mathf.FloorToInt(worldPos.y - origin.y + 0.5f);

		var proto = WPFMonoBehaviour.levelManager.CurrentGameMode?.ContraptionProto;
		BasePart partAtCursor = proto?.FindPartAt(targetX, targetY);

		if (partAtCursor != null)
		{
			// Use the same type as the part under cursor
			SortedPartType cursorType = partAtCursor.Type.ToSortedPartType();
			foreach (var pd in partDescs)
			{
				if (pd != null && pd.part != null &&
					pd.part.Type.ToSortedPartType() == cursorType && pd.CurrentCount > 0)
				{
					selectedDesc = pd;
					break;
				}
			}
		}

		// Fallback: find first available part
		if (selectedDesc == null)
		{
			foreach (var pd in partDescs)
			{
				if (pd != null && pd.part != null && pd.CurrentCount > 0)
				{
					selectedDesc = pd;
					break;
				}
			}
		}

		if (selectedDesc == null || selectedDesc.part == null)
		{
			Debug.Log("No part available for fill");
			return;
		}

		SaveUndoState();

		int overwriteMode = PasteOverwriteMode;
		BasePart existing = proto?.FindPartAt(targetX, targetY);

		if (overwriteMode == OVERWRITE_NONE && existing != null)
		{
			Debug.Log("Fill: cell occupied, skipping (overwrite=none)");
			return;
		}
		if (overwriteMode == OVERWRITE_NORMAL && existing != null && IsFrameType((int)existing.Type.ToSortedPartType()))
		{
			Debug.Log("Fill: cell has frame, skipping (overwrite=normal)");
			return;
		}
		if (existing != null && (overwriteMode == OVERWRITE_FULL || overwriteMode == OVERWRITE_NORMAL))
		{
			proto.RemovePart(existing);
			Object.Destroy(existing.gameObject);
		}

		BasePart customPart = WPFMonoBehaviour.gameData.GetCustomPart(selectedDesc.part.m_partType, selectedDesc.customPartIndex);
		if (customPart != null)
		{
			// Preserve rotation from the part under cursor if it exists
			int rot = partAtCursor != null ? (int)partAtCursor.Rotation : 0;
			bool flipped = partAtCursor != null && partAtCursor.Flipped;
			WPFMonoBehaviour.levelManager.BuildPart(targetX, targetY, rot, flipped, customPart);
			selectedDesc.useCount++;
			Debug.Log("Fill: placed " + selectedDesc.part.m_partType + " at (" + targetX + "," + targetY + ")");
		}
	}

	// --- Delete Selection ---

	public void DeleteSelection()
	{
		if (m_state != State.Selected && m_state != State.Selecting) return;
		var proto = WPFMonoBehaviour.levelManager.CurrentGameMode?.ContraptionProto;
		if (proto == null) return;

		SaveUndoState();

		List<BasePart> toRemove = new List<BasePart>();
		foreach (BasePart part in proto.Parts)
		{
			if (part.CoordX >= m_minX && part.CoordX <= m_maxX &&
				part.CoordY >= m_minY && part.CoordY <= m_maxY)
				toRemove.Add(part);
		}

		int removed = 0;
		foreach (BasePart part in toRemove)
		{
			if (part != null) { proto.RemovePart(part); Object.Destroy(part.gameObject); removed++; }
		}

		Debug.Log("Deleted " + removed + " parts from selection");
		ClearPreviews(m_selectionPreviews);
		m_state = State.Idle;
		m_isDragging = false;
	}

	// --- Selection Input ---

	private void UpdateSelecting()
	{
		if (Input.GetMouseButtonDown(0))
		{
			m_selectStart = GetWorldPos(Input.mousePosition);
			m_isDragging = true;
		}

		if (m_isDragging && Input.GetMouseButton(0))
		{
			m_selectEnd = GetWorldPos(Input.mousePosition);
			UpdateSelectionRect();
		}

		if (Input.GetMouseButtonUp(0) && m_isDragging)
		{
			m_isDragging = false;
			m_selectEnd = GetWorldPos(Input.mousePosition);
			UpdateSelectionRect();
			if (m_minX <= m_maxX && m_minY <= m_maxY)
			{
				m_state = State.Selected;
				CopySelected();
			}
		}
	}

	private void UpdateSelectionRect()
	{
		Vector3 origin = WPFMonoBehaviour.levelManager.StartingPosition;
		int sx = Mathf.FloorToInt(m_selectStart.x - origin.x + 0.5f);
		int sy = Mathf.FloorToInt(m_selectStart.y - origin.y + 0.5f);
		int ex = Mathf.FloorToInt(m_selectEnd.x - origin.x + 0.5f);
		int ey = Mathf.FloorToInt(m_selectEnd.y - origin.y + 0.5f);

		m_minX = Mathf.Min(sx, ex);
		m_maxX = Mathf.Max(sx, ex);
		m_minY = Mathf.Min(sy, ey);
		m_maxY = Mathf.Max(sy, ey);

		ClearPreviews(m_selectionPreviews);

		for (int x = m_minX; x <= m_maxX; x++)
			for (int y = m_minY; y <= m_maxY; y++)
				CreateQuad(m_selectionPreviews, m_selectionMat, x, y);
	}

	// --- Copy ---

	private void CopySelected()
	{
		var proto = WPFMonoBehaviour.levelManager.CurrentGameMode?.ContraptionProto;
		if (proto == null || proto.Parts.Count == 0) return;

		INContraptionData data = new INContraptionData();
		foreach (BasePart part in proto.Parts)
		{
			if (part.CoordX >= m_minX && part.CoordX <= m_maxX &&
				part.CoordY >= m_minY && part.CoordY <= m_maxY)
			{
				data.Units.Add(new INContraptionData.Unit(
					(int)part.Type.ToSortedPartType(), part.Index,
					part.CoordX, part.CoordY, (int)part.Rotation, part.Flipped));
			}
		}

		if (data.Units.Count > 0)
		{
			m_clipboard = data;
			m_pasteRotation = 0;
			Debug.Log("Copied " + data.Units.Count + " parts");
		}
	}

	public void CopyFullContraption()
	{
		m_clipboard = INContraption.CopyContraption();
		m_pasteRotation = 0;
	}

	// --- Paste Preview ---

	public void StartPastePreview()
	{
		if (m_clipboard == null || m_clipboard.Units.Count == 0) return;
		m_pasteRotation = 0;
		m_state = State.PastePreview;
		ClearPreviews(m_pastePreviews);
	}

	private void UpdatePastePreview()
	{
		INContraptionData clip = GetRotatedClipboard();
		if (clip == null) return;

		Vector3 origin = WPFMonoBehaviour.levelManager.StartingPosition;
		Vector3 worldPos = GetWorldPos(Input.mousePosition);
		int targetX = Mathf.FloorToInt(worldPos.x - origin.x + 0.5f);
		int targetY = Mathf.FloorToInt(worldPos.y - origin.y + 0.5f);

		int cx = 0, cy = 0;
		foreach (var u in clip.Units) { cx += u.X; cy += u.Y; }
		cx /= clip.Units.Count;
		cy /= clip.Units.Count;

		int ox = targetX - cx;
		int oy = targetY - cy;

		ClearPreviews(m_pastePreviews);

		var proto = WPFMonoBehaviour.levelManager.CurrentGameMode?.ContraptionProto;
		int overwriteMode = PasteOverwriteMode;

		foreach (var u in clip.Units)
		{
			int px = u.X + ox;
			int py = u.Y + oy;
			BasePart existing = proto != null ? proto.FindPartAt(px, py) : null;
			bool occupied = existing != null;

			if (!occupied)
				CreateQuad(m_pastePreviews, m_pasteMat, px, py);
			else if (overwriteMode == OVERWRITE_NONE)
				CreateQuad(m_pastePreviews, m_pasteBlockedMat, px, py);
			else if (overwriteMode == OVERWRITE_NORMAL && IsFrameType((int)existing.Type.ToSortedPartType()))
				CreateQuad(m_pastePreviews, m_pasteBlockedMat, px, py);
			else
				CreateQuad(m_pastePreviews, m_pasteMat, px, py);
		}
	}

	// --- Confirm / Cancel ---

	public void ConfirmPaste()
	{
		INContraptionData clip = GetRotatedClipboard();
		if (clip == null || clip.Units.Count == 0) return;

		Vector3 origin = WPFMonoBehaviour.levelManager.StartingPosition;
		Vector3 worldPos = GetWorldPos(Input.mousePosition);
		int targetX = Mathf.FloorToInt(worldPos.x - origin.x + 0.5f);
		int targetY = Mathf.FloorToInt(worldPos.y - origin.y + 0.5f);

		int cx = 0, cy = 0;
		foreach (var u in clip.Units) { cx += u.X; cy += u.Y; }
		cx /= clip.Units.Count;
		cy /= clip.Units.Count;

		int ox = targetX - cx;
		int oy = targetY - cy;

		SaveUndoState();

		var proto = WPFMonoBehaviour.levelManager.CurrentGameMode?.ContraptionProto;
		int overwriteMode = PasteOverwriteMode;
		int placed = 0;

		foreach (var u in clip.Units)
		{
			int px = u.X + ox;
			int py = u.Y + oy;

			BasePart existing = proto?.FindPartAt(px, py);
			bool occupied = existing != null;

			if (overwriteMode == OVERWRITE_NONE && occupied) continue;
			if (overwriteMode == OVERWRITE_NORMAL && occupied && IsFrameType((int)existing.Type.ToSortedPartType())) continue;

			if (occupied && (overwriteMode == OVERWRITE_FULL || overwriteMode == OVERWRITE_NORMAL))
			{
				proto.RemovePart(existing);
				Object.Destroy(existing.gameObject);
			}

			SortedPartType type = (SortedPartType)u.Type;

			if (m_materialOverride && (type == SortedPartType.WoodenFrame || type == SortedPartType.MetalFrame))
			{
				type = m_materialIndex > 0 ? SortedPartType.MetalFrame : SortedPartType.WoodenFrame;
			}

			ConstructionUI.PartDesc partDesc = WPFMonoBehaviour.levelManager.ConstructionUI.FindPartDesc(type.ToPartType());
			if (partDesc != null)
			{
				BasePart customPart = WPFMonoBehaviour.gameData.GetCustomPart(partDesc.part.m_partType, u.Index);
				if (customPart != null)
				{
					WPFMonoBehaviour.levelManager.BuildPart(px, py, u.Rotation, u.Flipped, customPart);
					partDesc.useCount++;
					placed++;
				}
			}
		}

		Debug.Log("Pasted " + placed + " parts (rot " + (m_pasteRotation * 90) + "\u00b0)");
		ClearPreviews(m_pastePreviews);
		m_pasteRotation = 0;
		m_state = State.Idle;
	}

	public void CancelPaste()
	{
		ClearPreviews(m_pastePreviews);
		m_pasteRotation = 0;
		m_state = State.Idle;
	}

	// --- Undo ---

	private void SaveUndoState()
	{
		var proto = WPFMonoBehaviour.levelManager.CurrentGameMode?.ContraptionProto;
		if (proto == null) return;

		INContraptionData snap = new INContraptionData(proto.Parts.Count);
		foreach (BasePart p in proto.Parts)
		{
			snap.Units.Add(new INContraptionData.Unit(
				(int)p.Type.ToSortedPartType(), p.Index,
				p.CoordX, p.CoordY, (int)p.Rotation, p.Flipped));
		}
		m_undoStack.Add(snap);
		if (m_undoStack.Count > 20) m_undoStack.RemoveAt(0);
	}

	public void Undo()
	{
		// First try to undo last shape layer
		if (m_shapePreviewLayers.Count > 0)
		{
			UndoLastShape();
			return;
		}

		// Then try contraption undo
		if (m_undoStack.Count == 0) return;
		if (m_state == State.PastePreview) CancelPaste();

		INContraptionData snap = m_undoStack[m_undoStack.Count - 1];
		m_undoStack.RemoveAt(m_undoStack.Count - 1);

		var proto = WPFMonoBehaviour.levelManager.CurrentGameMode?.ContraptionProto;
		if (proto == null) return;

		List<BasePart> toRemove = new List<BasePart>(proto.Parts);
		foreach (BasePart p in toRemove)
		{
			if (p != null) Object.Destroy(p.gameObject);
		}
		proto.Parts.Clear();

		foreach (var u in snap.Units)
		{
			SortedPartType type = (SortedPartType)u.Type;
			ConstructionUI.PartDesc pd = WPFMonoBehaviour.levelManager.ConstructionUI.FindPartDesc(type.ToPartType());
			if (pd != null)
			{
				BasePart cp = WPFMonoBehaviour.gameData.GetCustomPart(pd.part.m_partType, u.Index);
				if (cp != null) WPFMonoBehaviour.levelManager.BuildPart(u.X, u.Y, u.Rotation, u.Flipped, cp);
			}
		}

		Debug.Log("Undo: restored " + snap.Units.Count + " parts");
	}

	private void ClearAllShapePreviews()
	{
		foreach (var layer in m_shapePreviewLayers)
			ClearPreviews(layer);
		m_shapePreviewLayers.Clear();
		m_shapeCellSets.Clear();
		m_shapeOrigins.Clear();
	}

	// --- Helpers ---

	private void CreateQuad(List<GameObject> list, Material mat, int x, int y)
	{
		Vector3 origin = WPFMonoBehaviour.levelManager.StartingPosition;
		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
		go.name = "Preview";
		go.transform.position = new Vector3(origin.x + x, origin.y + y, -2f);
		go.transform.localScale = Vector3.one * 0.95f;
		var r = go.GetComponent<Renderer>();
		r.material = mat;
		var c = go.GetComponent<Collider>();
		if (c != null) Destroy(c);
		list.Add(go);
	}

	private void ClearPreviews(List<GameObject> list)
	{
		foreach (var go in list) { if (go != null) Destroy(go); }
		list.Clear();
	}

	private Vector3 GetWorldPos(Vector3 screenPos)
	{
		if (m_mainCamera == null) m_mainCamera = Camera.main;
		if (m_mainCamera == null) return Vector3.zero;
		Vector3 wp = m_mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
		wp.z = 0f;
		return wp;
	}

	private HashSet<Vector2Int> GetShapeCells(int x0, int y0, int x1, int y1, ShapeType shape, bool solid)
	{
		HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

		int minX = Mathf.Min(x0, x1);
		int maxX = Mathf.Max(x0, x1);
		int minY = Mathf.Min(y0, y1);
		int maxY = Mathf.Max(y0, y1);

		switch (shape)
		{
			case ShapeType.Rectangle:
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
						if (solid || x == minX || x == maxX || y == minY || y == maxY)
							cells.Add(new Vector2Int(x, y));
				break;

			case ShapeType.Square:
				int side = Mathf.Max(maxX - minX, maxY - minY);
				maxX = minX + side;
				maxY = minY + side;
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
						if (solid || x == minX || x == maxX || y == minY || y == maxY)
							cells.Add(new Vector2Int(x, y));
				break;

			case ShapeType.Circle:
				int radius = Mathf.Max(maxX - minX, maxY - minY) / 2;
				int cx = (minX + maxX) / 2;
				int cy = (minY + maxY) / 2;
				for (int x = cx - radius; x <= cx + radius; x++)
					for (int y = cy - radius; y <= cy + radius; y++)
					{
						float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
						if (solid)
						{ if (dist <= radius + 0.5f) cells.Add(new Vector2Int(x, y)); }
						else
						{ if (dist >= radius - 0.5f && dist <= radius + 0.5f) cells.Add(new Vector2Int(x, y)); }
					}
				break;

			case ShapeType.Ellipse:
				float a = (maxX - minX) / 2f;
				float b = (maxY - minY) / 2f;
				float ecx = (minX + maxX) / 2f;
				float ecy = (minY + maxY) / 2f;
				if (a < 0.5f) a = 0.5f;
				if (b < 0.5f) b = 0.5f;
				for (int x = minX; x <= maxX; x++)
					for (int y = minY; y <= maxY; y++)
					{
						float norm = ((x - ecx) * (x - ecx)) / (a * a) + ((y - ecy) * (y - ecy)) / (b * b);
						if (solid)
						{ if (norm <= 1.0f) cells.Add(new Vector2Int(x, y)); }
						else
						{ if (norm >= 0.6f && norm <= 1.4f) cells.Add(new Vector2Int(x, y)); }
					}
				break;

			case ShapeType.Line:
				int lx0 = x0, ly0 = y0, lx1 = x1, ly1 = y1;
				int dx = Mathf.Abs(lx1 - lx0), dy = Mathf.Abs(ly1 - ly0);
				int sx = lx0 < lx1 ? 1 : -1, sy = ly0 < ly1 ? 1 : -1;
				int err = dx - dy;
				while (true)
				{
					cells.Add(new Vector2Int(lx0, ly0));
					if (lx0 == lx1 && ly0 == ly1) break;
					int e2 = 2 * err;
					if (e2 > -dy) { err -= dy; lx0 += sx; }
					if (e2 < dx) { err += dx; ly0 += sy; }
				}
				break;
		}

		return cells;
	}
}
