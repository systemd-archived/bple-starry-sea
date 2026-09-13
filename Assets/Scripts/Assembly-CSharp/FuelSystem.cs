using System;
using System.Collections.Generic;
using UnityEngine;

public class FuelSystem : PartManager
{
	private struct FuelPartData
	{
		public BasePart Part;

		public int FuelComponentIndex;

		public FuelPartData(BasePart part, int fuelComponentIndex)
		{
			Part = part;
			FuelComponentIndex = fuelComponentIndex;
		}
	}

	public struct FuelComponentData
	{
		public int FuelConsumerCount;

		public int FuelContainerCount;

		public float RequiredFuelAmount;

		public float ExpectedSupplyFuelAmount;

		public float RealSupplyFuelAmount;
	}

	private bool m_needsUpdate;

	private int m_fuelComponentCount;

	private List<FuelPartData> m_fuelParts;

	private FuelComponentData[] m_fuelComponents;

	public bool NeedsUpdate
	{
		get
		{
			return m_needsUpdate;
		}
		set
		{
			m_needsUpdate = value;
		}
	}

	public override StatusCode Status => StatusCode.Running;

	public static FuelSystem Instance { get; private set; }

	protected override void Initialize()
	{
		base.Initialize();
		Instance = this;
		Contraption.Instance.ConnectedComponentsChanged += OnConnectedComponentsChanged;
	}

	public FuelComponentData GetFuelComponent(int index)
	{
		return m_fuelComponents[index];
	}

	private void OnConnectedComponentsChanged()
	{
		UpdateFuelParts();
		m_needsUpdate = false;
	}

	public void UpdateFuelParts()
	{
		List<BasePart> list = new List<BasePart>();
		foreach (BasePart part in Contraption.Instance.Parts)
		{
			if (part is IFuelConsumer || part is IFuelContainer || part is FuelTube)
			{
				list.Add(part);
			}
		}
		int count = list.Count;
		Dictionary<BasePart, int> dictionary = new Dictionary<BasePart, int>();
		DisjointSet disjointSet = new DisjointSet(count);
		for (int i = 0; i < count; i++)
		{
			dictionary[list[i]] = i;
		}
		for (int j = 0; j < count; j++)
		{
			BasePart basePart = list[j];
			IEnumerable<BasePart> connectedParts;
			if (basePart is FuelTube fuelTube)
			{
				connectedParts = fuelTube.GetConnectedParts();
			}
			else
			{
				if (!(basePart is IFuelContainer fuelContainer))
				{
					continue;
				}
				connectedParts = fuelContainer.GetConnectedParts();
			}
			foreach (BasePart item in connectedParts)
			{
				if (dictionary.TryGetValue(item, out var value))
				{
					disjointSet.Union(j, value);
				}
			}
		}
		int componentCount;
		int[] componentIndexes = disjointSet.GetComponentIndexes(out componentCount);
		m_fuelComponentCount = componentCount;
		m_fuelParts = new List<FuelPartData>(count);
		m_fuelComponents = new FuelComponentData[componentCount];
		for (int k = 0; k < count; k++)
		{
			int num = componentIndexes[k];
			if (list[k] is IFuelConsumer fuelConsumer)
			{
				fuelConsumer.FuelComponentIndex = num;
				m_fuelComponents[num].FuelConsumerCount++;
			}
			else if (list[k] is IFuelContainer fuelContainer2)
			{
				fuelContainer2.FuelComponentIndex = componentCount;
				m_fuelComponents[num].FuelContainerCount++;
			}
			m_fuelParts.Add(new FuelPartData(list[k], num));
		}
		if (INSettings.GetBool(INFeature.UIPartButtonSystem) && UIPartButtonList.Enabled)
		{
			UIPartButtonList.Instance.NeedsUpdate = true;
		}
	}

	public override void FixedUpdate()
	{
		if (m_needsUpdate)
		{
			UpdateFuelParts();
			m_needsUpdate = false;
		}
		if (m_fuelComponentCount == 0)
		{
			return;
		}
		Array.Clear(m_fuelComponents, 0, m_fuelComponents.Length);
		foreach (FuelPartData fuelPart in m_fuelParts)
		{
			BasePart part = fuelPart.Part;
			ref FuelComponentData reference = ref m_fuelComponents[fuelPart.FuelComponentIndex];
			if (part is IFuelConsumer fuelConsumer)
			{
				reference.FuelConsumerCount++;
				reference.RequiredFuelAmount += fuelConsumer.RequiredFuelAmount;
			}
			else if (part is IFuelContainer fuelContainer)
			{
				reference.FuelContainerCount++;
				reference.ExpectedSupplyFuelAmount += fuelContainer.SupplyFuelAmount;
			}
		}
		foreach (FuelPartData fuelPart2 in m_fuelParts)
		{
			BasePart part2 = fuelPart2.Part;
			ref FuelComponentData reference2 = ref m_fuelComponents[fuelPart2.FuelComponentIndex];
			if (part2 is IFuelConsumer fuelConsumer2 && part2.IsEnabled())
			{
				float num = Math.Min(reference2.ExpectedSupplyFuelAmount / reference2.RequiredFuelAmount, 1f) * fuelConsumer2.RequiredFuelAmount;
				fuelConsumer2.ConsumeFuel(num);
				reference2.RealSupplyFuelAmount += num;
			}
		}
		foreach (FuelPartData fuelPart3 in m_fuelParts)
		{
			BasePart part3 = fuelPart3.Part;
			FuelComponentData fuelComponentData = m_fuelComponents[fuelPart3.FuelComponentIndex];
			if (part3 is IFuelContainer fuelContainer2)
			{
				fuelContainer2.SupplyFuel(fuelComponentData.RealSupplyFuelAmount * fuelContainer2.SupplyFuelAmount / fuelComponentData.ExpectedSupplyFuelAmount);
			}
		}
		int connectedComponentCount = Contraption.Instance.ConnectedComponentCount;
		int[] array = new int[connectedComponentCount];
		float[] array2 = new float[connectedComponentCount];
		foreach (BasePart part5 in Contraption.Instance.Parts)
		{
			if (part5 is Engine)
			{
				array2[part5.ConnectedComponent] += 0.2f * Time.fixedDeltaTime;
			}
			else if (part5 is IFuelContainer)
			{
				array[part5.ConnectedComponent]++;
			}
		}
		foreach (FuelPartData fuelPart4 in m_fuelParts)
		{
			BasePart part4 = fuelPart4.Part;
			if (part4 is IFuelContainer fuelContainer3)
			{
				int connectedComponent = part4.ConnectedComponent;
				fuelContainer3.Refuel(array2[connectedComponent] / (float)array[connectedComponent]);
			}
		}
	}
}
