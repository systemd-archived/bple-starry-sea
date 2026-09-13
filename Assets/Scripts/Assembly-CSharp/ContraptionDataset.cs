using System.Collections.Generic;
using System.Xml.Serialization;
using Innovation;

[XmlRoot("ContraptionDataset")]
public class ContraptionDataset : IContraptionData
{
	public class ContraptionDatasetUnit : IContraptionDataUnit
	{
		[XmlAttribute("x")]
		public int x;

		[XmlAttribute("y")]
		public int y;

		[XmlAttribute("partType")]
		public int partType;

		[XmlAttribute("customPartIndex")]
		public int customPartIndex;

		[XmlAttribute("rot")]
		public int rot;

		[XmlAttribute("flipped")]
		public bool flipped;

		int IContraptionDataUnit.Type => (int)((BasePart.PartType)partType).ToSortedPartType();

		int IContraptionDataUnit.Index => customPartIndex;

		int IContraptionDataUnit.X => x;

		int IContraptionDataUnit.Y => y;

		int IContraptionDataUnit.Rotation => rot;

		bool IContraptionDataUnit.Flipped => flipped;

		public ContraptionDatasetUnit()
		{
		}

		public ContraptionDatasetUnit(int x, int y, int partType, int customPartIndex, int rot, bool flipped)
		{
			this.x = x;
			this.y = y;
			this.partType = partType;
			this.customPartIndex = customPartIndex;
			this.rot = rot;
			this.flipped = flipped;
		}
	}

	[XmlArray("ContraptionDatasetList")]
	[XmlArrayItem("ContraptionDatasetUnit")]
	protected List<ContraptionDatasetUnit> m_contraptionDataSet = new List<ContraptionDatasetUnit>();

	public List<ContraptionDatasetUnit> ContraptionDatasetList => m_contraptionDataSet;

	IReadOnlyList<IContraptionDataUnit> IContraptionData.Units => m_contraptionDataSet;

	public void AddPart(int x, int y, int partType, int customPartIndex, BasePart.GridRotation rotation, bool flipped)
	{
		ContraptionDatasetUnit contraptionDatasetUnit = new ContraptionDatasetUnit();
		contraptionDatasetUnit.x = x;
		contraptionDatasetUnit.y = y;
		contraptionDatasetUnit.partType = partType;
		contraptionDatasetUnit.customPartIndex = customPartIndex;
		contraptionDatasetUnit.rot = (int)rotation;
		contraptionDatasetUnit.flipped = flipped;
		m_contraptionDataSet.Add(contraptionDatasetUnit);
	}
}
