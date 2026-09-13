using System;
using System.Collections.Generic;
using UnityEngine;

public class ResistorPart : ElectricalPart
{
	private bool m_variable;

	private Resistor m_resistor;

	public int CurrentResistorType => customPartIndex - 6;

	public override IEnumerable<CircuitElement> ElectricalElements => m_resistor.ToEnumerable();

	public override bool IsTriggerable()
	{
		if (!base.HasGeneratorRef)
		{
			return m_variable;
		}
		return false;
	}

	public override IEnumerable<UIPartTriggerButtonInfo> GetTriggerButtonInfo()
	{
		yield break;
	}

	public override IEnumerable<UIPartSliderButtonInfo> GetSliderButtonInfo()
	{
		if (m_variable)
		{
			yield return new UIPartSliderButtonInfo(UIPartButtonType.Slider, 0, base.Type, 2, base.ConnectedComponent, new UIPartSliderButton.Range((m_resistor == null) ? 1f : ((float)m_resistor.Resistance), 1f, 0.1f, 10f, 0.1f, 0.01f));
		}
	}

	public override void OnSliderButtonTriggered(UIPartSliderButton button)
	{
		if (m_variable)
		{
			m_resistor.Resistance = button.Value;
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		m_variable = CurrentResistorType == 5;
	}

	public override void CreateElectricalElements()
	{
		double num = CurrentResistorType switch
		{
			0 => 0.01, 
			1 => 0.1, 
			2 => 1.0, 
			3 => 10.0, 
			4 => 100.0, 
			5 => 1.0, 
			_ => throw new InvalidOperationException(), 
		};
		double num2 = 1.0;
		if (m_enclosedInto is ColoredFrame)
		{
			ColoredFrame coloredFrame = (ColoredFrame)m_enclosedInto;
			if (coloredFrame.m_partTier == PartTier.Legendary)
			{
				int num3 = coloredFrame.customPartIndex - 10;
				if (num3 < 0)
				{
					num3 = 0;
				}
				num2 = num3 % 10 + 1;
				num *= num2;
			}
			else if (coloredFrame.m_partTier == PartTier.Common)
			{
				int num4 = coloredFrame.customPartIndex - 12;
				if (num4 < 0)
				{
					num4 = 0;
				}
				if (num4 > 35)
				{
					num4 = 35;
				}
				if (num4 <= 8)
				{
					num2 = (num4 + 1) * 1000.0;
				}
				else if (num4 <= 18)
				{
					num2 = (num4 - 8) * 10000.0;
				}
				else if (num4 <= 26)
				{
					num2 = (num4 - 17) * 100000.0;
				}
				else
				{
					num2 = (num4 - 26) * 0.001;
				}
				num *= num2;
			}
			else if (coloredFrame.m_partTier == PartTier.Rare)
			{
				double[] array = new double[]
				{
					1.0, 1.2, 1.5, 1.8, 2.2, 2.7, 3.3, 3.9, 4.7, 5.6, 
					6.8, 8.2
				};
				int num5 = coloredFrame.customPartIndex - 48;
				if (num5 < 0)
				{
					num5 = 0;
				}
				if (num5 > 35)
				{
					num5 = 35;
				}
				if (num5 <= 11)
				{
					num2 = array[num5];
				}
				else if (num5 <= 23)
				{
					num2 = array[num5 - 12] * 10000.0;
				}
				else
				{
					num2 = array[num5 - 24] * 0.001;
				}
				num *= num2;
			}
		}
		m_resistor = new Resistor(num);
		Transform transform = base.transform.Find("MultiplierText");
		TextMesh textMesh;
		if (transform == null)
		{
			textMesh = new GameObject("MultiplierText")
			{
				transform =
				{
					parent = base.transform,
					localPosition = new Vector3(0f, 0f, -0.5f),
					localRotation = Quaternion.identity
				}
			}.AddComponent<TextMesh>();
			textMesh.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			textMesh.fontSize = 30;
			textMesh.characterSize = 0.08f;
			textMesh.anchor = TextAnchor.MiddleCenter;
			textMesh.alignment = TextAlignment.Center;
			textMesh.color = new Color32(byte.MaxValue, 160, 70, byte.MaxValue);
		}
		else
		{
			textMesh = transform.GetComponent<TextMesh>();
		}
		if (num2 > 1.0 || (num2 > 0.0 && num2 < 1.0))
		{
			if (num2 >= 1000.0)
			{
				textMesh.text = "x" + (num2 / 1000.0).ToString("0.###") + "k";
			}
			else if (num2 < 1.0)
			{
				textMesh.text = "x" + (num2 * 1000.0).ToString("0.###") + "‰";
			}
			else
			{
				textMesh.text = "x" + num2.ToString("0.###");
			}
			textMesh.gameObject.SetActive(true);
			return;
		}
		textMesh.gameObject.SetActive(false);
	}

	protected override BitDirection GetConnectionDirection()
	{
		return BitDirection.LeftAndRight.Rotate((int)m_gridRotation);
	}

	protected override Electrode FindElectrode(BitDirection direction)
	{
		direction = direction.Rotate(0 - m_gridRotation);
		return direction switch
		{
			BitDirection.Left => m_resistor.Electrode1, 
			BitDirection.Right => m_resistor.Electrode2, 
			_ => null, 
		};
	}

	public override void SetRotation(GridRotation rotation)
	{
		int rotation2 = (int)rotation % 2;
		base.SetRotation((GridRotation)rotation2);
	}
}
