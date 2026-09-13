using System.Collections.Generic;
using UnityEngine;

public class Egg : BasePart
{
	private bool m_enabled;

	private bool m_isSpecialEgg;

	private List<Collider> m_colliders;

	private Dictionary<Rigidbody, bool> m_rigidBodyData;

	private int m_safetyPressCount;

	private float m_safetyTimer;

	public override bool CanBeEnclosed()
	{
		return true;
	}

	public override void Awake()
	{
		base.Awake();
		if (customPartIndex == 1)
		{
			m_jointConnectionStrength = JointConnectionStrength.HighlyExtreme;
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		int num = customPartIndex;
		m_isSpecialEgg = num >= 1 && num <= 4;
	}

	protected override void OnTouch()
	{
		if (!INSettings.GetBool(INFeature.SpecialEggs) || !m_isSpecialEgg)
		{
			return;
		}
		if (customPartIndex != 1 || (m_gridRotation != BasePart.GridRotation.Deg_45 && m_gridRotation != BasePart.GridRotation.Deg_135 && m_gridRotation != BasePart.GridRotation.Deg_225 && m_gridRotation != BasePart.GridRotation.Deg_315))
		{
			SetEnabled(!m_enabled);
			return;
		}
		if (!m_enabled)
		{
			if (Time.time - m_safetyTimer > 1f)
			{
				m_safetyPressCount = 0;
			}
			m_safetyTimer = Time.time;
			m_safetyPressCount++;
			Singleton<AudioManager>.Instance.SpawnOneShotEffect(WPFMonoBehaviour.gameData.commonAudioCollection.tntExplosion, base.transform.position);
			WPFMonoBehaviour.effectManager.CreateParticles(WPFMonoBehaviour.gameData.m_dustParticles, base.transform.position - Vector3.forward * 5f, force: true);
			if (m_safetyPressCount >= 3)
			{
				m_safetyPressCount = 0;
				SetEnabled(true);
			}
			return;
		}
		SetEnabled(false);
	}

	public override void SetEnabled(bool enabled)
	{
		if (!INSettings.GetBool(INFeature.SpecialEggs) || !m_isSpecialEgg || m_enabled == enabled)
		{
			return;
		}
		m_enabled = enabled;
		switch (customPartIndex)
		{
		case 2:
			if (m_enabled)
			{
				m_rigidBodyData = new Dictionary<Rigidbody, bool>();
				{
					foreach (BasePart part in base.contraption.Parts)
					{
						if (part.ConnectedComponent != base.ConnectedComponent)
						{
							continue;
						}
						foreach (Rigidbody rigidbody in part.GetRigidbodies())
						{
							m_rigidBodyData.Add(rigidbody, rigidbody.useGravity);
							rigidbody.useGravity = false;
						}
					}
					break;
				}
			}
			foreach (KeyValuePair<Rigidbody, bool> rigidBodyDatum in m_rigidBodyData)
			{
				Rigidbody key = rigidBodyDatum.Key;
				if (key != null)
				{
					key.useGravity = rigidBodyDatum.Value;
				}
			}
			m_rigidBodyData = null;
			break;
		case 3:
		{
			if (!m_enabled)
			{
				foreach (Collider collider2 in m_colliders)
				{
					if (collider2 != null)
					{
						collider2.enabled = true;
					}
				}
				break;
			}
			m_colliders = new List<Collider>();
			Collider[] components = INContraption.Instance.GetComponents<Collider>();
			foreach (Collider collider in components)
			{
				if (collider.attachedRigidbody != null)
				{
					BasePart component = collider.attachedRigidbody.GetComponent<BasePart>();
					if (component != null && component.ConnectedComponent == base.ConnectedComponent)
					{
						collider.enabled = false;
						m_colliders.Add(collider);
					}
				}
			}
			break;
		}
		case 4:
			if (!m_enabled)
			{
				foreach (Collider collider3 in m_colliders)
				{
					if (collider3 != null)
					{
						collider3.enabled = true;
					}
				}
				break;
			}
			m_colliders = new List<Collider>();
			break;
		}
	}

	private void FixedUpdate()
	{
		if (!INSettings.GetBool(INFeature.SpecialEggs) || !m_isSpecialEgg || !base.contraption || !base.contraption.IsRunning || !m_enabled)
		{
			return;
		}
		switch (customPartIndex)
		{
		case 1:
		{
			Rigidbody rigidbody = base.rigidbody;
			float mass = rigidbody.mass;
			Vector3 position4 = rigidbody.position;
			float num5 = INSettings.GetFloat(INFeature.GravityEggForce);
			float num6 = INSettings.GetFloat(INFeature.GravityEggInnerRadius);
			float num7 = INSettings.GetFloat(INFeature.GravityEggOuterRadius);
			Rigidbody[] components2 = INContraption.Instance.Rigidbodies;
			foreach (Rigidbody rigidbody2 in components2)
			{
				Vector3 position5 = rigidbody2.position;
				float num8 = position4.x - position5.x;
				float num9 = position4.y - position5.y;
				float num10 = Mathf.Sqrt(num8 * num8 + num9 * num9);
				if (num10 < num7)
				{
					num10 = ((num10 > num6) ? num10 : num6);
					float num11 = num5 / (num10 * num10 * num10) * mass * rigidbody2.mass;
					rigidbody2.AddForce(new Vector3(num11 * num8, num11 * num9), ForceMode.Force);
					Egg component = rigidbody2.GetComponent<Egg>();
					if (component == null || component.customPartIndex != 1)
					{
						rigidbody.AddForce(new Vector3((0f - num11) * num8, (0f - num11) * num9), ForceMode.Force);
					}
				}
			}
			break;
		}
		case 4:
		{
			Vector3 position = base.transform.position;
			foreach (Collider collider2 in m_colliders)
			{
				if (collider2 != null)
				{
					Vector3 position2 = collider2.transform.position;
					float num = position2.x - position.x;
					float num2 = position2.y - position.y;
					if (num * num + num2 * num2 >= 64f)
					{
						collider2.enabled = true;
					}
				}
			}
			m_colliders.Clear();
			Collider[] components = INContraption.Instance.GetComponents<Collider>();
			foreach (Collider collider in components)
			{
				Vector3 position3 = collider.transform.position;
				float num3 = position3.x - position.x;
				float num4 = position3.y - position.y;
				if (num3 * num3 + num4 * num4 < 64f)
				{
					collider.enabled = false;
					m_colliders.Add(collider);
				}
			}
			break;
		}
		}
	}

	public override void PostInitialize()
	{
		if (INSettings.GetBool(INFeature.SpecialEggs) && m_isSpecialEgg)
		{
			int num = customPartIndex;
			if (num == 1)
			{
				base.contraption.ChangeOneShotPartAmount(m_partType, EffectDirection(), 1);
				base.rigidbody.mass = INSettings.GetFloat(INFeature.GravityEggMass);
			}
			if (num == 2 || num == 3 || num == 4)
			{
				base.contraption.ChangeOneShotPartAmount(m_partType, EffectDirection(), 1);
				OnTouch();
			}
		}
	}

	public override bool IsEnabled()
	{
		if (INSettings.GetBool(INFeature.SpecialEggs))
		{
			return m_enabled;
		}
		return false;
	}
}
