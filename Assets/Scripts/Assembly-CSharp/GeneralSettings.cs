using System;
using UnityEngine;

[Serializable]
public class GeneralSettings : SettingsBase
{
	private float m_gravityX;

	private float m_gravityY;

	private float m_timeScale;

	private bool m_fullScreen;

	public float GravityX
	{
		get
		{
			return m_gravityX;
		}
		set
		{
			if (m_gravityX != value && float.IsFinite(value))
			{
				m_gravityX = value;
				OnPropertyChanged("GravityX");
			}
		}
	}

	public float GravityY
	{
		get
		{
			return m_gravityY;
		}
		set
		{
			if (m_gravityY != value && float.IsFinite(value))
			{
				m_gravityY = value;
				OnPropertyChanged("GravityY");
			}
		}
	}

	public float TimeScale
	{
		get
		{
			return m_timeScale;
		}
		set
		{
			if (m_timeScale != value && float.IsFinite(value) && value >= 0f && value <= 10f)
			{
				m_timeScale = value;
				OnPropertyChanged("TimeScale");
			}
		}
	}

	public bool FullScreen
	{
		get
		{
			return m_fullScreen;
		}
		set
		{
			if (m_fullScreen != value)
			{
				m_fullScreen = value;
				OnPropertyChanged("FullScreen");
			}
		}
	}

	public GeneralSettings()
	{
		GravityX = 0f;
		GravityY = -9.81f;
		TimeScale = 1f;
		FullScreen = Screen.fullScreen;
	}

	public GeneralSettings(GeneralSettings settings)
		: this()
	{
		Update(settings);
	}

	public override void Apply()
	{
		Physics.gravity = new Vector2(GravityX, GravityY);
		if (!GameTime.IsPaused())
		{
			Time.timeScale = TimeScale;
		}
		INGameManager.Instance?.SetFullScreen(FullScreen);
	}

	public void Update(GeneralSettings settings)
	{
		if (settings != null)
		{
			GravityX = settings.GravityX;
			GravityY = settings.GravityY;
			TimeScale = settings.TimeScale;
			FullScreen = settings.FullScreen;
		}
	}
}
