using System.Collections.Generic;
using Innovation;

public class INContraptionData : IContraptionData
{
	public class Unit : IContraptionDataUnit
	{
		public int Type { get; set; }

		public int Index { get; set; }

		public int X { get; set; }

		public int Y { get; set; }

		public int Rotation { get; set; }

		public bool Flipped { get; set; }

		public Unit()
		{
		}

		public Unit(int type, int index, int x, int y, int rotation, bool flipped)
		{
			Type = type;
			Index = index;
			X = x;
			Y = y;
			Rotation = rotation;
			Flipped = flipped;
		}
	}

	public List<Unit> Units { get; set; }

	IReadOnlyList<IContraptionDataUnit> IContraptionData.Units => Units;

	public INContraptionData()
		: this(0)
	{
	}

	public INContraptionData(int count)
	{
		Units = new List<Unit>(count);
	}

	public static INContraptionData Create(ContraptionDataset contraptionDataset)
	{
		List<ContraptionDataset.ContraptionDatasetUnit> contraptionDatasetList = contraptionDataset.ContraptionDatasetList;
		int count = contraptionDatasetList.Count;
		INContraptionData iNContraptionData = new INContraptionData(count);
		for (int i = 0; i < count; i++)
		{
			ContraptionDataset.ContraptionDatasetUnit contraptionDatasetUnit = contraptionDatasetList[i];
			iNContraptionData.Units.Add(new Unit((int)((BasePart.PartType)contraptionDatasetUnit.partType).ToSortedPartType(), contraptionDatasetUnit.customPartIndex, contraptionDatasetUnit.x, contraptionDatasetUnit.y, contraptionDatasetUnit.rot, contraptionDatasetUnit.flipped));
		}
		return iNContraptionData;
	}

	public ContraptionDataset ConvertTo()
	{
		ContraptionDataset contraptionDataset = new ContraptionDataset();
		foreach (Unit unit in Units)
		{
			contraptionDataset.AddPart(unit.X, unit.Y, (int)((SortedPartType)unit.Type).ToPartType(), unit.Index, (BasePart.GridRotation)unit.Rotation, unit.Flipped);
		}
		return contraptionDataset;
	}
}
