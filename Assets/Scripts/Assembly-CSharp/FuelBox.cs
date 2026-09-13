using System;
using System.Collections.Generic;
using Innovation;
using UnityEngine;

public class FuelBox : BasePart, IFuelContainer, IBasePart
{
	public GameObject m_smokeCloud;

	private GameObject m_fuelSprite;

	private float m_fuelAmount;

	private float m_maxFuelAmount;

	private List<(float, float)> m_collisions;

	private List<(float, float)> m_impulses;

	private List<(BasePart, Joint)> m_connectedParts;

	private bool m_triggered;

	private bool m_canExplode;

	private bool m_infiniteFuel;

	private bool m_fuelBasedExplosion;

	public int FuelComponentIndex { get; set; }

	public float FuelAmount => m_fuelAmount;

	public float SupplyFuelAmount => Math.Min(m_fuelAmount, 1f * Time.fixedDeltaTime);

	public float RefuelingAmount => 0.5f * Time.fixedDeltaTime;

	public IEnumerable<BasePart> GetConnectedParts()
	{
		foreach (var connectedPart in m_connectedParts)
		{
			var (basePart, _) = connectedPart;
			if (basePart != null && connectedPart.Item2 != null)
			{
				yield return basePart;
			}
		}
	}

	public void SupplyFuel(float amount)
	{
		SetFuelAmount(m_fuelAmount - amount);
	}

	public void Refuel(float amount)
	{
		SetFuelAmount(m_fuelAmount + amount);
	}

	private void SetFuelAmount(float fuelAmount)
	{
		if (!float.IsNaN(fuelAmount))
		{
			fuelAmount = Math.Clamp(fuelAmount, 0f, m_maxFuelAmount);
			if (m_fuelAmount > 0f)
			{
				fuelAmount = Math.Clamp(fuelAmount, m_fuelAmount - SupplyFuelAmount, m_fuelAmount + RefuelingAmount);
			}
			m_fuelAmount = fuelAmount;
			float num = Math.Min(fuelAmount, 8f);
			base.rigidbody.mass = 2f + Math.Min(num, 4f);
			float num2 = ((m_maxFuelAmount > 100f) ? 1f : (fuelAmount / m_maxFuelAmount));
			Vector3 localPosition = m_fuelSprite.transform.localPosition;
			localPosition.y = -0.26f * (1f - num2);
			m_fuelSprite.transform.localPosition = localPosition;
			m_fuelSprite.transform.localScale = new Vector3(1f, num2, 1f);
		}
	}

	public override void Awake()
	{
		base.Awake();
		m_fuelSprite = base.transform.Find("Fuel").gameObject;
		m_collisions = new List<(float, float)>();
		m_impulses = new List<(float, float)>();
	}

	public override void Initialize()
	{
		base.Initialize();
		m_canExplode = true;
		m_infiniteFuel = false;
		m_fuelBasedExplosion = false;
		m_maxFuelAmount = 4f;
		if (m_gridRotation == BasePart.GridRotation.Deg_0 || m_gridRotation == BasePart.GridRotation.Deg_315)
		{
			m_fuelBasedExplosion = true;
		}
		else if (m_gridRotation == BasePart.GridRotation.Deg_45 || m_gridRotation == BasePart.GridRotation.Deg_90)
		{
			m_canExplode = false;
		}
		else if (m_gridRotation == BasePart.GridRotation.Deg_135 || m_gridRotation == BasePart.GridRotation.Deg_180)
		{
			m_maxFuelAmount = 1E+10f;
			m_infiniteFuel = true;
		}
		else if (m_gridRotation == BasePart.GridRotation.Deg_225 || m_gridRotation == BasePart.GridRotation.Deg_270)
		{
			m_maxFuelAmount = 1E+10f;
			m_infiniteFuel = true;
			m_canExplode = false;
		}
		SetFuelAmount(m_maxFuelAmount);
		FindConnectedParts();
	}

	private void FixedUpdate()
	{
		if (!base.contraption || !base.contraption.IsRunning)
		{
			return;
		}
		float num = 2f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = num;
		foreach (var (num5, num6) in m_collisions)
		{
			if (float.IsNaN(num6))
			{
				num2 += num5;
				continue;
			}
			num3 += num5 * num6;
			num4 += num6;
		}
		num2 += num * num3 / num4;
		m_collisions.Clear();
		if (num2 > 50f)
		{
			m_impulses.Add((Time.fixedTime, num2));
		}
		float num7 = 0f;
		for (int num8 = m_impulses.Count - 1; num8 >= 0; num8--)
		{
			(float, float) tuple2 = m_impulses[num8];
			if (tuple2.Item1 < Time.time - 1f)
			{
				break;
			}
			num7 += tuple2.Item2;
		}
		if (num7 > 250f)
		{
			Explode();
		}
	}

	public void Explode()
	{
		if (m_triggered)
		{
			return;
		}
		if (!m_canExplode)
		{
			return;
		}
		m_triggered = true;
		float num = (m_fuelBasedExplosion ? Math.Min(m_fuelAmount / 4f, 1f) : 1f);
		Collider[] array = Physics.OverlapSphere(base.transform.position, 8f);
		foreach (Collider obj in array)
		{
			Rigidbody attachedRigidbody = obj.attachedRigidbody;
			if (attachedRigidbody != null)
			{
				float num2 = 1f / (float)attachedRigidbody.GetComponentsInChildren<Collider>().Length;
				AddExplosionForce(forceFactor: num2 * num, target: attachedRigidbody.gameObject);
			}
			BasePart component = obj.GetComponent<BasePart>();
			if (component != null && component != this && !component.HasGeneratorRef && component is TNT tNT)
			{
				tNT.Explode();
			}
		}
		Singleton<AudioManager>.Instance.SpawnOneShotEffect(WPFMonoBehaviour.gameData.commonAudioCollection.tntExplosion, base.transform.position);
		WPFMonoBehaviour.effectManager.CreateParticles(m_smokeCloud, base.transform.position - Vector3.forward * 12f, force: true);
		base.contraption.RemovePart(this);
		List<Joint> list = base.contraption.FindPartJoints(this);
		if (list.Count > 0)
		{
			for (int j = 0; j < list.Count; j++)
			{
				bool flag = list[j].gameObject == this || list[j].connectedBody == this;
				if (!float.IsInfinity(list[j].breakForce) || flag)
				{
					UnityEngine.Object.Destroy(list[j]);
				}
			}
			HandleJointBreak();
		}
		else
		{
			HandleJointBreak(playEffects: false);
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void AddExplosionForce(GameObject target, float forceFactor)
	{
		Vector3 vector = target.transform.position - base.transform.position;
		float f = Math.Max(vector.magnitude, 1f);
		float num = forceFactor * 200f / Mathf.Pow(f, 1.5f);
		Rigidbody component = target.GetComponent<Rigidbody>();
		if (component.mass < 0.1f)
		{
			num *= component.mass;
		}
		else if (component.mass < 0.4f)
		{
			num *= component.mass / 0.4f;
		}
		component.AddForce(num * vector.normalized, ForceMode.Impulse);
	}

	private void FindConnectedParts()
	{
		m_connectedParts = new List<(BasePart, Joint)>();
		List<Joint> list = base.contraption.FindPartJoints(this);
		Rigidbody rigidbody = base.rigidbody;
		foreach (Joint item in list)
		{
			if (!(item == null))
			{
				BasePart component = ((item.connectedBody == rigidbody) ? item.GetComponent<Rigidbody>() : item.connectedBody).GetComponent<BasePart>();
				if (component != null && component.m_enclosedPart != null && component.m_enclosedPart is FuelBox)
				{
					m_connectedParts.Add((component.m_enclosedPart, item));
				}
			}
		}
	}

	public override void OnCollisionEnter(Collision collision)
	{
		base.OnCollisionEnter(collision);
		HandleCollision(collision);
	}

	public override void OnCollisionStay(Collision collision)
	{
		base.OnCollisionStay(collision);
		HandleCollision(collision);
	}

	private void HandleCollision(Collision collision)
	{
		Rigidbody rigidbody = collision.rigidbody;
		if (!(rigidbody == null))
		{
			float item = Vector.Length2(collision.relativeVelocity);
			float mass = rigidbody.mass;
			m_collisions.Add((item, mass));
		}
	}

	public override void OnLightEnter(EntityLightCollision collision)
	{
		float item = Vector.Length2(collision.DeltaVelocity);
		m_collisions.Add((item, float.NaN));
	}
}
