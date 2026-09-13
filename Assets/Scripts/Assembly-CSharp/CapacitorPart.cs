using System;
using System.Collections.Generic;

public class CapacitorPart : ElectricalPart
{
	private Capacitor m_capacitor;

	private double m_maxU;

	public override IEnumerable<CircuitElement> ElectricalElements => m_capacitor.ToEnumerable();

	public override void CreateElectricalElements()
	{
		double capacitance;
		double resistance;
		double maxU;
		switch (customPartIndex)
		{
		case 15:
			capacitance = 0.1;
			resistance = 1.0;
			maxU = 5.0;
			break;
		case 16:
			capacitance = 1.0;
			resistance = 0.1;
			maxU = 50.0;
			break;
		case 17:
			capacitance = 10.0;
			resistance = 0.01;
			maxU = 500.0;
			break;
		default:
			throw new InvalidOperationException();
		}
		m_maxU = maxU;
		m_capacitor = new Capacitor(capacitance, resistance);
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
			BitDirection.Left => m_capacitor.Anode, 
			BitDirection.Right => m_capacitor.Cathode, 
			_ => null, 
		};
	}

	public override void SetRotation(GridRotation rotation)
	{
		int rotation2 = (int)rotation % 2;
		base.SetRotation((GridRotation)rotation2);
	}

	public override void PostUpdateElements()
	{
		double value = m_capacitor.Charge / m_capacitor.Capacitance;
		if (Math.Abs(value) > m_maxU)
		{
			SetInvalid(invalid: true);
			RemoveAllConnections();
			m_capacitor.Charge = (double)Math.Sign(value) * m_maxU * m_capacitor.Capacitance;
		}
	}
}
