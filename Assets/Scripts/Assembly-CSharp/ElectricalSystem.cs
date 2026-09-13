using System;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalSystem : PartManager
{
	private List<ElectricalPart> m_electricalParts;

	private List<CircuitElement> m_electricalElements;

	private List<InterfacePart> m_interfaces;

	private CircuitSimulator m_circuitSimulator;

	private float m_accumulator;

	private static int s_circuitFrameRate = 50;

	private static bool s_enableMultithreadCircuit;

	private static HashSet<long> s_ignoredColliderPairs = new HashSet<long>();

	public static bool EnableMultithreadCircuit
	{
		get
		{
			return s_enableMultithreadCircuit;
		}
		set
		{
			s_enableMultithreadCircuit = value;
		}
	}

	public static int CircuitFrameRate
	{
		get
		{
			return s_circuitFrameRate;
		}
		set
		{
			if (value >= 1)
			{
				s_circuitFrameRate = value;
			}
		}
	}

	public override StatusCode Status => StatusCode.Running;

	public static ElectricalSystem Instance { get; private set; }

	protected override void Initialize()
	{
		base.Initialize();
		Instance = this;
	}

	public override void Start()
	{
		m_electricalParts = new List<ElectricalPart>();
		m_electricalElements = new List<CircuitElement>();
		m_interfaces = new List<InterfacePart>();
		m_circuitSimulator = new CircuitSimulator(Time.fixedDeltaTime);
		InitializeElectricalParts(Contraption.Instance.Parts);
	}

	public void InitializeElectricalParts(List<BasePart> parts)
	{
		GetElectricalParts(parts, m_electricalParts);
		foreach (ElectricalPart electricalPart in m_electricalParts)
		{
			electricalPart.CreateElectricalElements();
		}
		GetElectricalElements(m_electricalParts, m_electricalElements);
		foreach (ElectricalPart electricalPart2 in m_electricalParts)
		{
			electricalPart2.InitializeConnections();
		}
		foreach (ElectricalPart electricalPart3 in m_electricalParts)
		{
			electricalPart3.ConnectElectricalElements();
		}
		foreach (ElectricalPart electricalPart4 in m_electricalParts)
		{
			electricalPart4.InitializeElectricalElements();
		}
		m_electricalParts.Clear();
		m_electricalElements.Clear();
	}

	private void GetElectricalParts(List<BasePart> parts, List<ElectricalPart> electricalParts)
	{
		CheckList(electricalParts, "electricalParts");
		foreach (BasePart part in parts)
		{
			if (part is ElectricalPart item)
			{
				electricalParts.Add(item);
			}
		}
	}

	private void GetElectricalElements(List<ElectricalPart> electricalParts, List<CircuitElement> elements)
	{
		CheckList(elements, "elements");
		foreach (ElectricalPart electricalPart in electricalParts)
		{
			if (electricalPart.ElectricalElements == null)
			{
				continue;
			}
			foreach (CircuitElement electricalElement in electricalPart.ElectricalElements)
			{
				if (electricalElement != null)
				{
					elements.Add(electricalElement);
				}
			}
		}
	}

	private List<T> SelectElectricalParts<T>() where T : ElectricalPart
	{
		List<T> list = new List<T>();
		SelectElectricalParts(list);
		return list;
	}

	private void SelectElectricalParts<T>(List<T> list) where T : ElectricalPart
	{
		foreach (ElectricalPart electricalPart in m_electricalParts)
		{
			if (electricalPart is T item)
			{
				list.Add(item);
			}
		}
	}

	public override void FixedUpdate()
	{
		float step = 1f / CircuitFrameRate;
		m_accumulator += Time.fixedDeltaTime;
		if (m_accumulator < step)
		{
			return;
		}
		do
		{
			m_accumulator -= step;
			SimulateOnce(step);
		}
		while (m_accumulator >= step);
	}

	private void SimulateOnce(float step)
	{
		List<BasePart> parts = Contraption.Instance.Parts;
		GetElectricalParts(parts, m_electricalParts);
		GetElectricalElements(m_electricalParts, m_electricalElements);
		if (m_electricalParts.Count == 0)
		{
			return;
		}
		m_circuitSimulator.SetDeltaTime(step);
		foreach (ElectricalPart electricalPart in m_electricalParts)
		{
			electricalPart.RemoveInvalidConnections();
		}
		UpdateInterfaces();
		UpdatePowerTransmitters();
		foreach (ElectricalPart electricalPart2 in m_electricalParts)
		{
			electricalPart2.PreUpdateElements();
		}
		m_circuitSimulator.Simulate(m_electricalElements);
		foreach (ElectricalPart electricalPart3 in m_electricalParts)
		{
			electricalPart3.PostUpdateElements();
		}
		UpdatePointCharges();
		UpdateMagneticFieldGenerators();
		m_electricalParts.Clear();
		m_electricalElements.Clear();
	}

	private void UpdateInterfaces()
	{
		foreach (ElectricalPart electricalPart in m_electricalParts)
		{
			if (electricalPart is InterfacePart item)
			{
				m_interfaces.Add(item);
			}
		}
		foreach (InterfacePart @interface in m_interfaces)
		{
			@interface.UpdateConnections();
		}
		m_interfaces.Clear();
	}

	private void UpdatePowerTransmitters()
	{
		Dictionary<int, List<PowerTransmitterPart>> dictionary = new Dictionary<int, List<PowerTransmitterPart>>();
		List<PowerTransmitterPart> list = new List<PowerTransmitterPart>();
		foreach (ElectricalPart electricalPart in m_electricalParts)
		{
			PowerTransmitterPart powerTransmitterPart = electricalPart as PowerTransmitterPart;
			if (powerTransmitterPart == null)
			{
				continue;
			}
			if (powerTransmitterPart.IsSender)
			{
				int channel = powerTransmitterPart.GetChannel();
				if (!dictionary.ContainsKey(channel))
				{
					dictionary[channel] = new List<PowerTransmitterPart>();
				}
				dictionary[channel].Add(powerTransmitterPart);
			}
			else
			{
				list.Add(powerTransmitterPart);
			}
		}
		foreach (PowerTransmitterPart powerTransmitterPart2 in list)
		{
			int channel2 = powerTransmitterPart2.GetChannel();
			Vector3 position = powerTransmitterPart2.transform.position;
			float num = float.MaxValue;
			PowerTransmitterPart powerTransmitterPart3 = null;
			List<PowerTransmitterPart> list2;
			if (dictionary.TryGetValue(channel2, out list2))
			{
				foreach (PowerTransmitterPart powerTransmitterPart4 in list2)
				{
					float num2 = Vector.DistanceSquared2(position, powerTransmitterPart4.transform.position);
					if (num2 < num)
					{
						num = num2;
						powerTransmitterPart3 = powerTransmitterPart4;
					}
				}
			}
			if (powerTransmitterPart3 != null)
			{
				powerTransmitterPart2.Connect(powerTransmitterPart3, Mathf.Sqrt(num));
			}
			else
			{
				powerTransmitterPart2.Disconnect();
			}
		}
	}

	private void UpdatePointCharges()
	{
		List<PointChargePart> list = SelectElectricalParts<PointChargePart>();
		List<ElectricFieldGenerator> list2 = SelectElectricalParts<ElectricFieldGenerator>();
		List<MagneticFieldGenerator> list3 = SelectElectricalParts<MagneticFieldGenerator>();
		for (int i = 0; i < list.Count; i++)
		{
			PointChargePart pointChargePart = list[i];
			Vector3 position = pointChargePart.transform.position;
			for (int j = i + 1; j < list.Count; j++)
			{
				PointChargePart pointChargePart2 = list[j];
				if (pointChargePart.IsSpecialCharge || pointChargePart2.IsSpecialCharge)
				{
					continue;
				}
				Vector3 position2 = pointChargePart2.transform.position;
				float num = position2.x - position.x;
				float num2 = position2.y - position.y;
				float num3 = num * num + num2 * num2;
				if (num3 < 4096f)
				{
					float num4 = Mathf.Sqrt(num3);
					num4 = ((num4 > 1f) ? num4 : 1f);
					float num5 = 1.5f * pointChargePart.Charge * pointChargePart2.Charge / (num4 * num4 * num4);
					pointChargePart.rigidbody.AddForce(new Vector3((0f - num5) * num, (0f - num5) * num2));
					pointChargePart2.rigidbody.AddForce(new Vector3(num5 * num, num5 * num2));
				}
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			PointChargePart charge1 = list[k];
			if (!charge1.IsSpecialCharge || charge1.rigidbody == null) continue;
			for (int l = k + 1; l < list.Count; l++)
			{
				PointChargePart charge2 = list[l];
				if (!charge2.IsSpecialCharge || charge2.rigidbody == null) continue;
				if (charge1.IsDeg90 == charge2.IsDeg90) continue;
				if (charge1.HasAutoJoint || charge2.HasAutoJoint) continue;
				if (charge1.ConnectedComponent == charge2.ConnectedComponent) continue;
				if ((charge1.IsDeg270 && !charge1.IsEnabled()) || (charge2.IsDeg270 && !charge2.IsEnabled())) continue;
				int dx = charge2.m_coordX - charge1.m_coordX;
				int dy = charge2.m_coordY - charge1.m_coordY;
				float gridDist = Mathf.Sqrt(dx * dx + dy * dy);
				float forceRadius = INUserSettings.Instance?.StarSeaSettings?.SpecialChargeForceRadius ?? 4f;
				if (gridDist > forceRadius) continue;
				Vector3 dir = charge1.transform.position - charge2.transform.position;
				float worldDist = dir.magnitude;
				worldDist = ((worldDist > 0.1f) ? worldDist : 0.1f);
				float baseForce = INUserSettings.Instance?.StarSeaSettings?.SpecialChargeForce ?? 70f;
				float forceMagnitude = baseForce;
				float fx = forceMagnitude * dir.x / worldDist;
				float fy = forceMagnitude * dir.y / worldDist;
				charge1.rigidbody.AddForce(new Vector3(-fx, -fy));
				charge2.rigidbody.AddForce(new Vector3(fx, fy));
				// Remove vehicle collision on attraction (once per charge pair)
				if (INUserSettings.Instance?.StarSeaSettings?.SpecialChargeAttractionCollisionIgnore ?? true)
				{
					int id1 = charge1.GetInstanceID();
					int id2 = charge2.GetInstanceID();
					long pairKey = id1 < id2 ? ((long)id1 << 32 | (uint)id2) : ((long)id2 << 32 | (uint)id1);
					if (!s_ignoredColliderPairs.Contains(pairKey))
					{
						s_ignoredColliderPairs.Add(pairKey);
						int cc1 = charge1.ConnectedComponent;
						int cc2 = charge2.ConnectedComponent;
						var allParts = Contraption.Instance?.Parts;
						if (allParts != null)
						{
							var bufa = new List<Collider>();
							var bufb = new List<Collider>();
							for (int pi = 0; pi < allParts.Count; pi++)
							{
								if (allParts[pi].ConnectedComponent != cc1) continue;
								bufa.Clear();
								allParts[pi].GetComponentsInChildren<Collider>(bufa);
								for (int ci = 0; ci < bufa.Count; ci++)
								{
									if (bufa[ci] == null) continue;
									for (int pj = 0; pj < allParts.Count; pj++)
									{
										if (allParts[pj].ConnectedComponent != cc2) continue;
										bufb.Clear();
										allParts[pj].GetComponentsInChildren<Collider>(bufb);
										for (int cj = 0; cj < bufb.Count; cj++)
										{
											if (bufb[cj] != null) Physics.IgnoreCollision(bufa[ci], bufb[cj], true);
										}
									}
								}
							}
						}
					}
				}
				float jointDist = INUserSettings.Instance?.StarSeaSettings?.SpecialChargeJointDistance ?? 0.5f;
				if (worldDist < jointDist && charge1.IsEnabled() && charge2.IsEnabled())
				{
					charge1.TryCreateRotatableJoint(charge2);
				}
			}
		}
		// ForceConnect: connect ANY two different-type special charges immediately
		// skips forceRadius check AND attraction force — only checks jointDist
		if (INUserSettings.Instance?.StarSeaSettings?.SpecialChargeForceConnect ?? false)
		{
			float fcJointDist = INUserSettings.Instance?.StarSeaSettings?.SpecialChargeJointDistance ?? 0.5f;
			for (int k = 0; k < list.Count; k++)
			{
				PointChargePart fc1 = list[k];
				if (!fc1.IsSpecialCharge || fc1.rigidbody == null) continue;
				if (!fc1.IsEnabled()) continue;
				for (int l = k + 1; l < list.Count; l++)
				{
					PointChargePart fc2 = list[l];
					if (!fc2.IsSpecialCharge || fc2.rigidbody == null) continue;
					if (!fc2.IsEnabled()) continue;
					if (fc1.IsDeg90 == fc2.IsDeg90) continue;
					if (fc1.ConnectedComponent == fc2.ConnectedComponent) continue;
					if (fc1.HasAutoJoint || fc2.HasAutoJoint) continue;
					float fWorldDist = Vector3.Distance(fc1.transform.position, fc2.transform.position);
					if (fWorldDist < fcJointDist)
					{
						fc1.TryCreateRotatableJoint(fc2);
					}
				}
			}
		}
		foreach (ElectricFieldGenerator item in list2)
		{
			if (!item.IsEnabled())
			{
				continue;
			}
			Vector3 up = item.transform.up;
			Vector3 position3 = item.transform.position;
			foreach (PointChargePart item2 in list)
			{
				Vector3 position4 = item2.transform.position;
				float num6 = Vector.DistanceSquared2(position3, position4);
				if (!(num6 > 64f))
				{
					float num7 = item.GetElectricFieldIntensityByDistance(Mathf.Sqrt(num6)) * item2.Charge;
					item2.rigidbody.AddForce(new Vector3(num7 * up.x, num7 * up.y));
					item.rigidbody.AddForce(new Vector3((0f - num7) * up.x, (0f - num7) * up.y));
				}
			}
		}
		foreach (MagneticFieldGenerator item3 in list3)
		{
			if (!item3.IsEnabled())
			{
				continue;
			}
			Vector3 position5 = item3.transform.position;
			Vector3 velocity = item3.rigidbody.velocity;
			_ = item3.transform.right;
			foreach (PointChargePart item4 in list)
			{
				Vector3 velocity2 = item4.rigidbody.velocity;
				float num8 = velocity2.x - velocity.x;
				float num9 = velocity2.y - velocity.y;
				float distance = Vector.Distance2(position5, item4.transform.position);
				float num10 = item3.GetMagneticFluxDensityByDistance(distance) * item4.Charge;
				item4.rigidbody.AddForce(new Vector3((0f - num10) * num9, num10 * num8));
				item3.rigidbody.AddForce(new Vector3(num10 * num9, (0f - num10) * num8));
			}
		}
	}

	private void UpdateMagneticFieldGenerators()
	{
		List<WirePartBase> list = new List<WirePartBase>();
		foreach (ElectricalPart electricalPart in m_electricalParts)
		{
			if (electricalPart is WirePartBase wirePartBase && wirePartBase.IsValid() && wirePartBase.IsElectromagnetic())
			{
				bool flag = false;
				bool flag2 = true;
				double[] currents = wirePartBase.Currents;
				foreach (double num in currents)
				{
					flag = flag || num != 0.0;
					flag2 &= !double.IsNaN(num);
				}
				if (flag && flag2)
				{
					list.Add(wirePartBase);
				}
			}
		}
		foreach (ElectricalPart electricalPart2 in m_electricalParts)
		{
			if (!(electricalPart2 is MagneticFieldGenerator magneticFieldGenerator) || !magneticFieldGenerator.IsEnabled())
			{
				continue;
			}
			Vector3 position = magneticFieldGenerator.transform.position;
			foreach (WirePartBase item in list)
			{
				Vector3 position2 = item.transform.position;
				float num2 = Vector.DistanceSquared2(position, position2);
				if (!(num2 > 64f))
				{
					Vector3 right = item.transform.right;
					Vector3 vector = new Vector2(0f - right.y, right.x);
					double[] currents2 = item.Currents;
					Vector3 vector2 = 0.5f * (float)(currents2[0] - currents2[2]) * vector + 0.5f * (float)(currents2[1] - currents2[3]) * right;
					vector2 *= magneticFieldGenerator.GetMagneticFluxDensityByDistance(Mathf.Sqrt(num2));
					item.rigidbody.AddForce(vector2);
					magneticFieldGenerator.rigidbody.AddForce(-vector2);
				}
			}
		}
	}

	public override void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void CheckList<T>(List<T> list, string name)
	{
		if (list == null)
		{
			throw new ArgumentNullException(name);
		}
		if (list.Count > 0)
		{
			list.Clear();
		}
	}
}
