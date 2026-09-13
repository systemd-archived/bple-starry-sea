using System;
using UnityEngine;

[Serializable]
public class StarSeaSettings : SettingsBase
{
	private float m_gunProjectileDrag;

	private float m_partDrag;

	private float m_partAngularDrag;

	private bool m_showFPS;

	private int m_targetFrameRate;

	private int m_fixedFrameRate;

	private int m_circuitFrameRate;

	private int m_frameFrameRate;

	private float m_antiTunnelSpeedThreshold;

	private float m_antiTunnelPullBackDistance;

	private bool m_useMultithreadFrameSolver;

	private bool m_enableMultithreadCircuit;

	private int m_physicsSolverIterations;

	private int m_adaptivePhysicsFrame;

	private int m_adaptivePhysicsFrameMin;

	private float m_gateWidthCoefficient = 1f;

	private string m_gateLengthCoefficients = "1,1,1,1,1";

	private float m_gateOpacity = 0.8f;

	private string m_gateDefaultLengths = "1,2,4,8";

	private string m_gateAdjustableRange = "0.1,8,0.1";

	private float m_gateExtensionSpeed = 0.2f;

	private bool m_stableMultiblock = true;

	private bool m_specialPointChargeInvincible = true;

	private float m_specialChargeForce = 70f;

	private float m_specialChargeJointBreakForce = -1f;

	private float m_specialChargeForceRadius = 4f;

	private bool m_chargeJointSnapEnabled = true;

	private float m_chargeJointSize = 1f;

	private float m_specialChargeReinforceRange = 5f;

	private int m_specialChargeReinforceMaxCount = 50;

	private float m_specialChargeReinforceBreakForce = 1000f;

	private bool m_specialChargeCollisionIgnore = true;

	private float m_specialChargeJointDistance = 0.5f;

	private bool m_specialChargeAttractionCollisionIgnore = true;

	private bool m_showJointConnectionCount;

	private bool m_specialChargeDefaultEnabled;

	private bool m_specialChargeForceConnect;

	private bool m_toolBookEnabled = true;

	private float m_toolBookPosX = -60f;

	private float m_toolBookPosY = -60f;

	private float m_toolBookSize = 1f;

	private int m_toolBookPasteOverwriteMode = 1;

	private int m_toolBookClipboardMax = 3;


	private float m_anchorUmbrellaCoefficient = 10f;

	public float GunProjectileDrag
	{
		get
		{
			try
			{
				return INSettings.GetFloat(INFeature.GunProjectileDrag);
			}
			catch
			{
				return m_gunProjectileDrag;
			}
		}
		set
		{
			if (float.IsFinite(value) && value >= 0f)
			{
				m_gunProjectileDrag = value;
				INSettings.SetValue(INFeature.GunProjectileDrag, new Variant<float>(value));
				OnPropertyChanged("GunProjectileDrag");
			}
		}
	}

	public float PartDrag
	{
		get
		{
			return m_partDrag;
		}
		set
		{
			if (m_partDrag != value && float.IsFinite(value) && value >= 0f)
			{
				m_partDrag = value;
				OnPropertyChanged("PartDrag");
			}
		}
	}

	public float PartAngularDrag
	{
		get
		{
			return m_partAngularDrag;
		}
		set
		{
			if (m_partAngularDrag != value && float.IsFinite(value) && value >= 0f)
			{
				m_partAngularDrag = value;
				OnPropertyChanged("PartAngularDrag");
			}
		}
	}

	public bool ShowFPS
	{
		get
		{
			return m_showFPS;
		}
		set
		{
			if (m_showFPS != value)
			{
				m_showFPS = value;
				OnPropertyChanged("ShowFPS");
			}
		}
	}

	public int TargetFrameRate
	{
		get
		{
			return m_targetFrameRate;
		}
		set
		{
			if (m_targetFrameRate != value && value >= 1 && value <= 240)
			{
				m_targetFrameRate = value;
				OnPropertyChanged("TargetFrameRate");
			}
		}
	}

	public int FixedFrameRate
	{
		get
		{
			return m_fixedFrameRate;
		}
		set
		{
			if (m_fixedFrameRate != value && value >= 1)
			{
				m_fixedFrameRate = value;
				OnPropertyChanged("FixedFrameRate");
			}
		}
	}

	public int CircuitFrameRate
	{
		get
		{
			return m_circuitFrameRate;
		}
		set
		{
			if (m_circuitFrameRate != value && value >= 1)
			{
				m_circuitFrameRate = value;
				OnPropertyChanged("CircuitFrameRate");
			}
		}
	}

	public int FrameFrameRate
	{
		get
		{
			return m_frameFrameRate;
		}
		set
		{
			if (m_frameFrameRate != value && value >= 1)
			{
				m_frameFrameRate = value;
				OnPropertyChanged("FrameFrameRate");
			}
		}
	}

	public float AntiTunnelSpeedThreshold
	{
		get
		{
			return m_antiTunnelSpeedThreshold;
		}
		set
		{
			if (m_antiTunnelSpeedThreshold != value && float.IsFinite(value) && value >= 0f)
			{
				m_antiTunnelSpeedThreshold = value;
				OnPropertyChanged("AntiTunnelSpeedThreshold");
			}
		}
	}

	public float AntiTunnelPullBackDistance
	{
		get
		{
			return m_antiTunnelPullBackDistance;
		}
		set
		{
			if (m_antiTunnelPullBackDistance != value && float.IsFinite(value) && value >= 0f)
			{
				m_antiTunnelPullBackDistance = value;
				OnPropertyChanged("AntiTunnelPullBackDistance");
			}
		}
	}

	public bool UseMultithreadFrameSolver
	{
		get
		{
			return m_useMultithreadFrameSolver;
		}
		set
		{
			if (m_useMultithreadFrameSolver != value)
			{
				m_useMultithreadFrameSolver = value;
				OnPropertyChanged("UseMultithreadFrameSolver");
			}
		}
	}

	public bool EnableMultithreadCircuit
	{
		get
		{
			return m_enableMultithreadCircuit;
		}
		set
		{
			if (m_enableMultithreadCircuit != value)
			{
				m_enableMultithreadCircuit = value;
				OnPropertyChanged("EnableMultithreadCircuit");
			}
		}
	}

	public int PhysicsSolverIterations
	{
		get
		{
			return m_physicsSolverIterations;
		}
		set
		{
			if (m_physicsSolverIterations != value && value >= 1 && value <= 10)
			{
				m_physicsSolverIterations = value;
				OnPropertyChanged("PhysicsSolverIterations");
			}
		}
	}

	public int AdaptivePhysicsFrame
	{
		get
		{
			return m_adaptivePhysicsFrame;
		}
		set
		{
			if (m_adaptivePhysicsFrame != value && value >= 0)
			{
				m_adaptivePhysicsFrame = value;
				OnPropertyChanged("AdaptivePhysicsFrame");
			}
		}
	}

	public int AdaptivePhysicsFrameMin
	{
		get
		{
			return m_adaptivePhysicsFrameMin;
		}
		set
		{
			if (m_adaptivePhysicsFrameMin != value && value >= 0)
			{
				m_adaptivePhysicsFrameMin = value;
				OnPropertyChanged("AdaptivePhysicsFrameMin");
			}
		}
	}

	public float GateWidthCoefficient
	{
		get
		{
			return m_gateWidthCoefficient;
		}
		set
		{
			if (m_gateWidthCoefficient != value && float.IsFinite(value) && value > 0f)
			{
				m_gateWidthCoefficient = value;
				OnPropertyChanged("GateWidthCoefficient");
			}
		}
	}

	public string GateLengthCoefficients
	{
		get
		{
			return m_gateLengthCoefficients;
		}
		set
		{
			if (m_gateLengthCoefficients != value && !string.IsNullOrEmpty(value))
			{
				m_gateLengthCoefficients = value;
				OnPropertyChanged("GateLengthCoefficients");
			}
		}
	}

	public float GateOpacity
	{
		get
		{
			return m_gateOpacity;
		}
		set
		{
			if (m_gateOpacity != value && float.IsFinite(value) && value >= 0f && value <= 1f)
			{
				m_gateOpacity = value;
				OnPropertyChanged("GateOpacity");
			}
		}
	}

	public string GateDefaultLengths
	{
		get
		{
			return m_gateDefaultLengths;
		}
		set
		{
			if (m_gateDefaultLengths != value && !string.IsNullOrEmpty(value))
			{
				m_gateDefaultLengths = value;
				OnPropertyChanged("GateDefaultLengths");
			}
		}
	}

	public string GateAdjustableRange
	{
		get
		{
			return m_gateAdjustableRange;
		}
		set
		{
			if (m_gateAdjustableRange != value && !string.IsNullOrEmpty(value))
			{
				m_gateAdjustableRange = value;
				OnPropertyChanged("GateAdjustableRange");
			}
		}
	}

	public float GateExtensionSpeed
	{
		get
		{
			return m_gateExtensionSpeed;
		}
		set
		{
			if (m_gateExtensionSpeed != value && float.IsFinite(value) && value > 0f)
			{
				m_gateExtensionSpeed = value;
				OnPropertyChanged("GateExtensionSpeed");
			}
		}
	}

	public bool StableMultiblock
	{
		get
		{
			return m_stableMultiblock;
		}
		set
		{
			if (m_stableMultiblock != value)
			{
				m_stableMultiblock = value;
				OnPropertyChanged("StableMultiblock");
			}
		}
	}

	public bool SpecialPointChargeInvincible
	{
		get
		{
			return m_specialPointChargeInvincible;
		}
		set
		{
			if (m_specialPointChargeInvincible != value)
			{
				m_specialPointChargeInvincible = value;
				OnPropertyChanged("SpecialPointChargeInvincible");
			}
		}
	}

	public float SpecialChargeForce
	{
		get
		{
			return m_specialChargeForce;
		}
		set
		{
			if (m_specialChargeForce != value)
			{
				m_specialChargeForce = value;
				OnPropertyChanged("SpecialChargeForce");
			}
		}
	}

	public float SpecialChargeJointBreakForce
	{
		get
		{
			return m_specialChargeJointBreakForce;
		}
		set
		{
			if (m_specialChargeJointBreakForce != value)
			{
				m_specialChargeJointBreakForce = value;
				OnPropertyChanged("SpecialChargeJointBreakForce");
			}
		}
	}

	public float SpecialChargeForceRadius
	{
		get
		{
			return m_specialChargeForceRadius;
		}
		set
		{
			if (m_specialChargeForceRadius != value)
			{
				m_specialChargeForceRadius = value;
				OnPropertyChanged("SpecialChargeForceRadius");
			}
		}
	}

	public bool ChargeJointSnapEnabled
	{
		get
		{
			return m_chargeJointSnapEnabled;
		}
		set
		{
			if (m_chargeJointSnapEnabled != value)
			{
				m_chargeJointSnapEnabled = value;
				OnPropertyChanged("ChargeJointSnapEnabled");
			}
		}
	}

	public float ChargeJointSize
	{
		get
		{
			return m_chargeJointSize;
		}
		set
		{
			if (m_chargeJointSize != value)
			{
				m_chargeJointSize = value;
				OnPropertyChanged("ChargeJointSize");
			}
		}
	}

	public float SpecialChargeReinforceRange
	{
		get
		{
			return m_specialChargeReinforceRange;
		}
		set
		{
			if (m_specialChargeReinforceRange != value)
			{
				m_specialChargeReinforceRange = value;
				OnPropertyChanged("SpecialChargeReinforceRange");
			}
		}
	}

	public int SpecialChargeReinforceMaxCount
	{
		get
		{
			return m_specialChargeReinforceMaxCount;
		}
		set
		{
			if (m_specialChargeReinforceMaxCount != value)
			{
				m_specialChargeReinforceMaxCount = value;
				OnPropertyChanged("SpecialChargeReinforceMaxCount");
			}
		}
	}

	public float SpecialChargeReinforceBreakForce
	{
		get
		{
			return m_specialChargeReinforceBreakForce;
		}
		set
		{
			if (m_specialChargeReinforceBreakForce != value)
			{
				m_specialChargeReinforceBreakForce = value;
				OnPropertyChanged("SpecialChargeReinforceBreakForce");
			}
		}
	}

	public bool SpecialChargeCollisionIgnore
	{
		get
		{
			return m_specialChargeCollisionIgnore;
		}
		set
		{
			if (m_specialChargeCollisionIgnore != value)
			{
				m_specialChargeCollisionIgnore = value;
				OnPropertyChanged("SpecialChargeCollisionIgnore");
			}
		}
	}

	public float SpecialChargeJointDistance
	{
		get
		{
			return m_specialChargeJointDistance;
		}
		set
		{
			if (m_specialChargeJointDistance != value && float.IsFinite(value) && value >= 0f)
			{
				m_specialChargeJointDistance = value;
				OnPropertyChanged("SpecialChargeJointDistance");
			}
		}
	}

	public bool SpecialChargeAttractionCollisionIgnore
	{
		get
		{
			return m_specialChargeAttractionCollisionIgnore;
		}
		set
		{
			if (m_specialChargeAttractionCollisionIgnore != value)
			{
				m_specialChargeAttractionCollisionIgnore = value;
				OnPropertyChanged("SpecialChargeAttractionCollisionIgnore");
			}
		}
	}

	public bool ShowJointConnectionCount
	{
		get
		{
			return m_showJointConnectionCount;
		}
		set
		{
			if (m_showJointConnectionCount != value)
			{
				m_showJointConnectionCount = value;
				OnPropertyChanged("ShowJointConnectionCount");
			}
		}
	}

	public bool SpecialChargeDefaultEnabled
	{
		get
		{
			return m_specialChargeDefaultEnabled;
		}
		set
		{
			if (m_specialChargeDefaultEnabled != value)
			{
				m_specialChargeDefaultEnabled = value;
				OnPropertyChanged("SpecialChargeDefaultEnabled");
			}
		}
	}

	public bool SpecialChargeForceConnect
	{
		get
		{
			return m_specialChargeForceConnect;
		}
		set
		{
			if (m_specialChargeForceConnect != value)
			{
				m_specialChargeForceConnect = value;
				OnPropertyChanged("SpecialChargeForceConnect");
			}
		}
	}

	public bool ToolBookEnabled
	{
		get
		{
			return m_toolBookEnabled;
		}
		set
		{
			if (m_toolBookEnabled != value)
			{
				m_toolBookEnabled = value;
				OnPropertyChanged("ToolBookEnabled");
			}
		}
	}

	public float ToolBookPosX
	{
		get { return m_toolBookPosX; }
		set
		{
			if (m_toolBookPosX != value)
			{
				m_toolBookPosX = value;
				OnPropertyChanged("ToolBookPosX");
			}
		}
	}

	public float ToolBookPosY
	{
		get { return m_toolBookPosY; }
		set
		{
			if (m_toolBookPosY != value)
			{
				m_toolBookPosY = value;
				OnPropertyChanged("ToolBookPosY");
			}
		}
	}

	public float ToolBookSize
	{
		get { return m_toolBookSize; }
		set
		{
			if (m_toolBookSize != value)
			{
				m_toolBookSize = value;
				OnPropertyChanged("ToolBookSize");
			}
		}
	}

	public int ToolBookPasteOverwriteMode
	{
		get { return m_toolBookPasteOverwriteMode; }
		set
		{
			if (m_toolBookPasteOverwriteMode != value)
			{
				m_toolBookPasteOverwriteMode = value;
				OnPropertyChanged("ToolBookPasteOverwriteMode");
			}
		}
	}

	public int ToolBookClipboardMax
	{
		get { return m_toolBookClipboardMax; }
		set
		{
			if (m_toolBookClipboardMax != value)
			{
				m_toolBookClipboardMax = value;
				OnPropertyChanged("ToolBookClipboardMax");
			}
		}
	}

	public float AnchorUmbrellaCoefficient
	{
		get
		{
			return m_anchorUmbrellaCoefficient;
		}
		set
		{
			if (m_anchorUmbrellaCoefficient != value && float.IsFinite(value) && value >= 0f)
			{
				m_anchorUmbrellaCoefficient = value;
				OnPropertyChanged("AnchorUmbrellaCoefficient");
			}
		}
	}

	public StarSeaSettings()
	{
		GunProjectileDrag = INSettings.GetFloat(INFeature.GunProjectileDrag);
		PartDrag = 0.2f;
		PartAngularDrag = 0.05f;
		ShowFPS = true;
		TargetFrameRate = 60;
		FixedFrameRate = 50;
		CircuitFrameRate = 50;
		FrameFrameRate = 50;
		AntiTunnelSpeedThreshold = 50f;
		AntiTunnelPullBackDistance = 0f;
		UseMultithreadFrameSolver = false;
		EnableMultithreadCircuit = false;
		PhysicsSolverIterations = 6;
		AdaptivePhysicsFrame = 0;
		AdaptivePhysicsFrameMin = 20;
		GateWidthCoefficient = 1f;
		GateLengthCoefficients = "1,1,1,1,1";
		GateOpacity = 0.8f;
		GateDefaultLengths = "1,2,4,8";
		GateAdjustableRange = "0.1,8,0.1";
		GateExtensionSpeed = 0.2f;
		StableMultiblock = true;
		SpecialPointChargeInvincible = true;
		SpecialChargeForce = 70f;
		SpecialChargeJointBreakForce = -1f;
		SpecialChargeForceRadius = 4f;
		ChargeJointSnapEnabled = true;
		ChargeJointSize = 1f;
		SpecialChargeReinforceRange = 5f;
		SpecialChargeReinforceMaxCount = 50;
		SpecialChargeReinforceBreakForce = 1000f;
		SpecialChargeCollisionIgnore = true;
		SpecialChargeJointDistance = 0.5f;
		SpecialChargeAttractionCollisionIgnore = true;
		ShowJointConnectionCount = false;
		SpecialChargeDefaultEnabled = false;
		SpecialChargeForceConnect = false;
		AnchorUmbrellaCoefficient = 10f;
		ToolBookEnabled = true;
		ToolBookPosX = -60f;
		ToolBookPosY = -60f;
		ToolBookSize = 1f;
		ToolBookPasteOverwriteMode = 1;
		ToolBookClipboardMax = 3;
	}

	public StarSeaSettings(StarSeaSettings settings)
		: this()
	{
		Update(settings);
	}

	public override void Apply()
	{
		INSettings.SetValue(INFeature.GunProjectileDrag, new Variant<float>(GunProjectileDrag));
		Application.targetFrameRate = TargetFrameRate;
		Time.fixedDeltaTime = 1f / FixedFrameRate;
		ElectricalSystem.CircuitFrameRate = CircuitFrameRate;
		ElectricalSystem.EnableMultithreadCircuit = EnableMultithreadCircuit;
		FrameJointManager.FrameFrameRate = FrameFrameRate;
		INContraption.AntiTunnelSpeedThreshold = AntiTunnelSpeedThreshold;
		INContraption.AntiTunnelPullBackDistance = AntiTunnelPullBackDistance;
		Physics.defaultSolverIterations = PhysicsSolverIterations;
	}

	public void Update(StarSeaSettings settings)
	{
		if (settings != null)
		{
			GunProjectileDrag = settings.GunProjectileDrag;
			PartDrag = settings.PartDrag;
			PartAngularDrag = settings.PartAngularDrag;
			ShowFPS = settings.ShowFPS;
			TargetFrameRate = settings.TargetFrameRate;
			FixedFrameRate = settings.FixedFrameRate;
			CircuitFrameRate = settings.CircuitFrameRate;
			FrameFrameRate = settings.FrameFrameRate;
			AntiTunnelSpeedThreshold = settings.AntiTunnelSpeedThreshold;
			AntiTunnelPullBackDistance = settings.AntiTunnelPullBackDistance;
			UseMultithreadFrameSolver = settings.UseMultithreadFrameSolver;
			EnableMultithreadCircuit = settings.EnableMultithreadCircuit;
			PhysicsSolverIterations = settings.PhysicsSolverIterations;
			AdaptivePhysicsFrame = settings.AdaptivePhysicsFrame;
			AdaptivePhysicsFrameMin = settings.AdaptivePhysicsFrameMin;
			GateWidthCoefficient = settings.GateWidthCoefficient;
			GateLengthCoefficients = settings.GateLengthCoefficients;
			GateOpacity = settings.GateOpacity;
			GateDefaultLengths = settings.GateDefaultLengths;
			GateAdjustableRange = settings.GateAdjustableRange;
			GateExtensionSpeed = settings.GateExtensionSpeed;
			StableMultiblock = settings.StableMultiblock;
			SpecialPointChargeInvincible = settings.SpecialPointChargeInvincible;
			SpecialChargeForce = settings.SpecialChargeForce;
			SpecialChargeJointBreakForce = settings.SpecialChargeJointBreakForce;
			SpecialChargeForceRadius = settings.SpecialChargeForceRadius;
			ChargeJointSnapEnabled = settings.ChargeJointSnapEnabled;
			ChargeJointSize = settings.ChargeJointSize;
			SpecialChargeReinforceRange = settings.SpecialChargeReinforceRange;
			SpecialChargeReinforceMaxCount = settings.SpecialChargeReinforceMaxCount;
			SpecialChargeReinforceBreakForce = settings.SpecialChargeReinforceBreakForce;
			SpecialChargeCollisionIgnore = settings.SpecialChargeCollisionIgnore;
			SpecialChargeJointDistance = settings.SpecialChargeJointDistance;
			SpecialChargeAttractionCollisionIgnore = settings.SpecialChargeAttractionCollisionIgnore;
			ShowJointConnectionCount = settings.ShowJointConnectionCount;
			SpecialChargeDefaultEnabled = settings.SpecialChargeDefaultEnabled;
			SpecialChargeForceConnect = settings.SpecialChargeForceConnect;
			AnchorUmbrellaCoefficient = settings.AnchorUmbrellaCoefficient;
			ToolBookEnabled = settings.ToolBookEnabled;
			ToolBookPosX = settings.ToolBookPosX;
			ToolBookPosY = settings.ToolBookPosY;
			ToolBookSize = settings.ToolBookSize;
			ToolBookPasteOverwriteMode = settings.ToolBookPasteOverwriteMode;
			ToolBookClipboardMax = settings.ToolBookClipboardMax;
		}
	}
}
