public class MarkerManager : PartManager
{
	private int[] m_indexes;

	public override StatusCode Status => StatusCode.Running;

	public static MarkerManager Instance { get; private set; }

	public static bool IsInstantiated { get; private set; }

	protected override void Initialize()
	{
		base.Initialize();
		Instance = this;
		IsInstantiated = true;
		Contraption.Instance.ConnectedComponentsChanged += OnConnectedComponentsChanged;
	}

	public override void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public int GetTeamIndex(int connectedComponent)
	{
		if (m_indexes == null || connectedComponent < 0 || connectedComponent >= m_indexes.Length)
		{
			return 0;
		}
		return m_indexes[connectedComponent];
	}

	public int GetTeamIndex(BasePart part)
	{
		return GetTeamIndex(part.ConnectedComponent);
	}

	public bool IsInSameTeam(BasePart partA, BasePart partB)
	{
		int connectedComponent = partA.ConnectedComponent;
		int connectedComponent2 = partB.ConnectedComponent;
		if (connectedComponent == connectedComponent2)
		{
			return true;
		}
		int teamIndex = GetTeamIndex(connectedComponent);
		int teamIndex2 = GetTeamIndex(connectedComponent2);
		return (teamIndex & teamIndex2) != 0;
	}

	private void OnConnectedComponentsChanged()
	{
		m_indexes = new int[Contraption.Instance.ConnectedComponentCount];
		foreach (BasePart part in Contraption.Instance.Parts)
		{
			if (part.IsMarker())
			{
				int num = part.ConnectedComponent;
				if (num >= 0 && num < m_indexes.Length)
				{
					m_indexes[num] |= 1 << (int)part.m_gridRotation;
				}
			}
		}
	}

	public static bool IsInSameTeamStatic(BasePart partA, BasePart partB)
	{
		if (IsInstantiated)
		{
			return Instance.IsInSameTeam(partA, partB);
		}
		return partA.ConnectedComponent == partB.ConnectedComponent;
	}
}
