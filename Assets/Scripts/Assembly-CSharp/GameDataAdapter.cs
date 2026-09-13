using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GameDataAdapter : GameData, IPartFactory
{
	private IPartFactory m_partFactory;

	public IPartFactory PartFactory
	{
		get
		{
			return m_partFactory;
		}
		set
		{
			m_partFactory = value;
		}
	}

	public IEnumerable<IPartFactoryUnit> Units => m_partFactory.Units;

	public override IEnumerable<GameObject> Parts => m_partFactory.Units.Select((IPartFactoryUnit unit) => unit.RegularPart?.gameObject);

	public override IEnumerable<IReadOnlyList<BasePart>> CustomParts => m_partFactory.Units.Select((IPartFactoryUnit unit) => unit.CustomParts);

	public static GameDataAdapter Create(GameData source)
	{
		return Create(source, null);
	}

	public static GameDataAdapter Create(GameData source, IPartFactory factory)
	{
		GameDataAdapter gameDataAdapter = ScriptableObject.CreateInstance<GameDataAdapter>();
		FieldInfo[] fields = source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			fieldInfo.SetValue(gameDataAdapter, fieldInfo.GetValue(source));
		}
		gameDataAdapter.m_partFactory = factory;
		return gameDataAdapter;
	}

	public IPartFactoryUnit FindUnit(BasePart.PartType partType)
	{
		return m_partFactory.FindUnit(partType);
	}

	public BasePart FindPart(BasePart.PartType partType)
	{
		return m_partFactory.FindPart(partType);
	}

	public BasePart FindCustomPart(BasePart.PartType partType, int partIndex)
	{
		return m_partFactory.FindCustomPart(partType, partIndex);
	}

	public override GameObject GetPart(BasePart.PartType type)
	{
		return m_partFactory.FindPart(type)?.gameObject;
	}

	public override CustomPartInfo GetCustomPart(BasePart.PartType type)
	{
		IPartFactoryUnit partFactoryUnit = m_partFactory.FindUnit(type);
		if (partFactoryUnit == null)
		{
			return null;
		}
		return new CustomPartInfo(partFactoryUnit.RegularPart.Type, (List<BasePart>)partFactoryUnit.CustomParts);
	}

	public override BasePart GetCustomPart(BasePart.PartType type, int customIndex)
	{
		return m_partFactory.FindCustomPart(type, customIndex);
	}

	public override int GetCustomPartIndex(BasePart.PartType type, string partName)
	{
		IPartFactoryUnit partFactoryUnit = m_partFactory.FindUnit(type);
		if (partFactoryUnit.RegularPart.name == partName)
		{
			return 0;
		}
		foreach (BasePart customPart in partFactoryUnit.CustomParts)
		{
			if (customPart.name == partName)
			{
				return customPart.Index;
			}
		}
		return -1;
	}
}
