using System;
using System.Collections.Generic;

public class JointManager : PartManager
{
	private List<JointBase> m_joints;

	private Graph<JointBase> m_jointGraph;

	public List<JointBase> Joint => m_joints;

	public override StatusCode Status => StatusCode.Running;

	public static JointManager Instance { get; private set; }

	protected override void Initialize()
	{
		base.Initialize();
		m_joints = new List<JointBase>();
		Instance = this;
		INPhysicsManager.Instance.BeforeSimulation += BeforeSimulation;
		INPhysicsManager.Instance.AfterSimulation += AfterSimulation;
	}

	public override void OnDestroy()
	{
		INPhysicsManager.Instance.BeforeSimulation -= BeforeSimulation;
		INPhysicsManager.Instance.AfterSimulation -= AfterSimulation;
	}

	public void Register(JointBase joint)
	{
		if (joint == null)
		{
			throw new ArgumentNullException("joint");
		}
		m_joints.Add(joint);
	}

	public override void FixedUpdate()
	{
		foreach (JointBase joint in m_joints)
		{
			joint.Prepare();
		}
	}

	private void BeforeSimulation()
	{
		for (int i = 0; i < 8; i++)
		{
			foreach (JointBase joint in m_joints)
			{
				joint.PreSolve();
			}
		}
		for (int j = 0; j < 8; j++)
		{
			foreach (JointBase joint2 in m_joints)
			{
				joint2.PostSolve();
			}
		}
	}

	private void AfterSimulation()
	{
	}
}
