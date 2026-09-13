using System;
using System.Collections.Generic;
using UnityEngine;

public class PowerTransmitterPart : ElectricalPart
{
	public enum PowerTransmitterType
	{
		Sender = 0,
		Receiver = 1
	}

	private CircuitElement m_element;

	private PowerTransmitterPart m_connectedPart;

	private Electrode m_electrode;

	private Transform m_visual;

	private bool m_visualSeparated;

	internal int no90 => (m_gridRotation >= GridRotation.Deg_45) ? 1 : 0;

	public bool IsSender => TransmitterType == PowerTransmitterType.Sender;

	public bool IsReceiver => TransmitterType == PowerTransmitterType.Receiver;

	public PowerTransmitterType TransmitterType => (PowerTransmitterType)(customPartIndex - 40);

	public override IEnumerable<CircuitElement> ElectricalElements => m_element.ToEnumerable();

	public override void Awake()
	{
		base.Awake();
	}

	public override void SetRotation(GridRotation rotation)
	{
		base.SetRotation(rotation);
		EnsureVisualSeparated();
		ApplyOrientation();
	}

	private void EnsureVisualSeparated()
	{
		if (m_visualSeparated)
		{
			return;
		}
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
		INSerializedSprite sprite = GetComponent<INSerializedSprite>();
		if (meshFilter == null || meshRenderer == null)
		{
			return;
		}
		// 确保 mesh 已生成再分离，避免空 mesh 导致建造界面隐身
		if (meshFilter.sharedMesh == null)
		{
			return;
		}
		m_visualSeparated = true;
		GameObject visualObject = new GameObject("Visual");
		visualObject.transform.SetParent(base.transform, worldPositionStays: false);
		MeshFilter visualMeshFilter = visualObject.AddComponent<MeshFilter>();
		visualMeshFilter.sharedMesh = meshFilter.sharedMesh;
		MeshRenderer visualMeshRenderer = visualObject.AddComponent<MeshRenderer>();
		visualMeshRenderer.sharedMaterials = meshRenderer.sharedMaterials;
		if (sprite != null)
		{
			INSerializedSprite visualSprite = visualObject.AddComponent<INSerializedSprite>();
			visualSprite.SpriteName = sprite.SpriteName;
			try
			{
				visualSprite.UpdateMesh();
			}
			catch
			{
			}
		}
		DestroyComponent(meshFilter);
		DestroyComponent(meshRenderer);
		if (sprite != null)
		{
			DestroyComponent(sprite);
		}
		m_visual = visualObject.transform;
	}

	private void DestroyComponent(Component component)
	{
		if (Application.isPlaying)
		{
			Destroy(component);
		}
		else
		{
			DestroyImmediate(component);
		}
	}

	private void ApplyOrientation()
	{
		if (no90 == 1)
		{
			GridRotation ortho = (GridRotation)(((int)m_gridRotation - (int)GridRotation.Deg_45) % 4);
			base.transform.localRotation = Quaternion.AngleAxis(GetRotationAngle(ortho), Vector3.forward);
			if ((bool)m_visual)
			{
				m_visual.localRotation = Quaternion.AngleAxis(45f, Vector3.forward);
			}
		}
		else
		{
			base.transform.localRotation = Quaternion.AngleAxis(GetRotationAngle(m_gridRotation), Vector3.forward);
			if ((bool)m_visual)
			{
				m_visual.localRotation = Quaternion.identity;
			}
		}
	}

	public int GetChannel()
	{
		if (m_enclosedInto != null && m_enclosedInto.IsColoredrame())
		{
			return m_enclosedInto.Index;
		}
		return -1;
	}

	public override void CreateElectricalElements()
	{
		switch (TransmitterType)
		{
		case PowerTransmitterType.Sender:
			m_element = new Wire(1);
			break;
		case PowerTransmitterType.Receiver:
			m_element = new Resistor(0.0);
			break;
		}
	}

	protected override BitDirection GetConnectionDirection()
	{
		return TransmitterType switch
		{
			PowerTransmitterType.Sender => BitDirection.Down.Rotate((int)m_gridRotation), 
			PowerTransmitterType.Receiver => BitDirection.Up.Rotate((int)m_gridRotation), 
			_ => BitDirection.None, 
		};
	}

	protected override Electrode FindElectrode(BitDirection direction)
	{
		direction = direction.Rotate(0 - m_gridRotation);
		if (direction == BitDirection.Down && TransmitterType == PowerTransmitterType.Sender)
		{
			return m_element.Electrodes[0];
		}
		if (direction == BitDirection.Up && TransmitterType == PowerTransmitterType.Receiver)
		{
			return m_element.Electrodes[0];
		}
		return null;
	}

	public void Connect(PowerTransmitterPart other, float distance)
	{
		if (m_connectedPart != other)
		{
			Disconnect();
			Electrode electrode = new Electrode(m_element, null);
			Electrode electrode2 = new Electrode(other.m_element, null);
			CircuitFactory.Connect(electrode, electrode2);
			electrode.Element.Electrodes.Add(electrode);
			electrode2.Element.Electrodes.Add(electrode2);
			m_electrode = electrode;
			m_connectedPart = other;
		}
		if (no90 == 1)
		{
			((Resistor)m_element).Resistance = 0.0;
		}
		if (no90 == 0)
		{
			Resistor resistor = (Resistor)m_element;
			distance = Math.Max(distance - 1f, 0f);
			if (distance <= 32f)
			{
				resistor.Resistance = 0.1f * distance * distance + 0.01f;
			}
			else
			{
				Disconnect();
			}
		}
	}

	public void Disconnect()
	{
		if (m_connectedPart != null)
		{
			Electrode electrode = m_electrode;
			Electrode connectedElectrode = m_electrode.ConnectedElectrode;
			CircuitFactory.Disconnect(electrode, connectedElectrode);
			electrode.Element.Electrodes.Remove(electrode);
			connectedElectrode.Element.Electrodes.Remove(connectedElectrode);
			m_electrode = null;
			m_connectedPart = null;
		}
		((Resistor)m_element).Resistance = 0.0;
	}
}
