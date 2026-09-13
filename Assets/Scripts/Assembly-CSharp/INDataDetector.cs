using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

public class INDataDetector : MonoBehaviour
{
	private static int s_currentFrameRate = -1;

	private float m_smoothFps;

	private float m_frameRateCooldown;

	private int m_lowFpsStreak;

	private int m_recoverStreak;

	private class FPSCounter
	{
		private float m_updateInterval;

		private float m_frameCount;

		private Stopwatch m_stopwatch;

		private float m_result;

		public bool IsRunning => m_stopwatch.IsRunning;

		public float FPS => m_result;

		public FPSCounter(float updateInterval)
		{
			m_updateInterval = updateInterval;
			m_frameCount = 0f;
			m_stopwatch = new Stopwatch();
		}

		public void Tick()
		{
			if (m_stopwatch.IsRunning)
			{
				m_frameCount += 1f;
				float num = (float)m_stopwatch.ElapsedMilliseconds / 1000f;
				if (num >= m_updateInterval)
				{
					m_result = m_frameCount / num;
					m_frameCount = 0f;
					m_stopwatch.Restart();
				}
			}
		}

		public void Start()
		{
			m_stopwatch.Start();
		}

		public void Stop()
		{
			m_stopwatch.Stop();
		}

		public void Reset()
		{
			m_frameCount = 0f;
			m_result = 0f;
			m_stopwatch.Reset();
		}
	}

	private FPSCounter m_counter;

	private FPSCounter m_fixedCounter;

	public float FPS => m_counter.FPS;

	public float FixedFPS => m_fixedCounter.FPS;

	public static int CurrentFrameRate => s_currentFrameRate;

	public float AllocatedManagedHeapSize { get; private set; }

	public float ReservedManagedHeapSize { get; private set; }

	public float TotalAllocatedMemorySize { get; private set; }

	public float TotalReservedMemorySize { get; private set; }

	public static INDataDetector Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		Object.DontDestroyOnLoad(this);
		m_counter = new FPSCounter(0.5f);
		m_counter.Start();
		m_fixedCounter = new FPSCounter(0.5f);
		m_fixedCounter.Start();
		INAppInterface.Instance.AppInterfaceEnabled += OnAppInterfaceEnabled;
		INAppInterface.Instance.AppInterfaceDisabled += OnAppInterfaceDisabled;
	}

	private void OnAppInterfaceEnabled()
	{
		m_counter.Stop();
		m_fixedCounter.Stop();
	}

	private void OnAppInterfaceDisabled()
	{
		m_counter.Start();
		m_fixedCounter.Start();
	}

	private void FixedUpdate()
	{
		m_fixedCounter.Tick();
		AllocatedManagedHeapSize = (float)Profiler.GetMonoUsedSizeLong() / 1048576f;
		ReservedManagedHeapSize = (float)Profiler.GetMonoHeapSizeLong() / 1048576f;
		TotalAllocatedMemorySize = (float)Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
		TotalReservedMemorySize = (float)Profiler.GetTotalReservedMemoryLong() / 1048576f;
	}

	private void Update()
	{
		m_counter.Tick();
		ApplyAdaptivePhysicsFrame();
	}

	private void ApplyAdaptivePhysicsFrame()
	{
		StarSeaSettings settings = INUserSettings.Instance?.StarSeaSettings;
		if (settings == null || settings.AdaptivePhysicsFrame <= 0)
		{
			s_currentFrameRate = -1;
			return;
		}
		int targetFrameRate = settings.FixedFrameRate;
		if (s_currentFrameRate < 0)
		{
			s_currentFrameRate = targetFrameRate;
		}
		float fps = Instance != null ? Instance.FPS : 0f;
		if (fps > 0f)
		{
			m_smoothFps = ((m_smoothFps <= 0f) ? fps : (m_smoothFps * 0.85f + fps * 0.15f));
		}
		if (m_frameRateCooldown > 0f)
		{
			m_frameRateCooldown -= Time.unscaledDeltaTime;
			return;
		}
		int threshold = settings.AdaptivePhysicsFrame;
		int hysteresis = Mathf.Max(2, Mathf.RoundToInt(threshold * 0.1f));
		int lowerThreshold = threshold;
		int upperThreshold = threshold + hysteresis;
		int minFrameRate = settings.AdaptivePhysicsFrameMin;
		int lowerBound = (minFrameRate > 0) ? minFrameRate : 1;
		if (m_smoothFps < lowerThreshold)
		{
			m_lowFpsStreak++;
			m_recoverStreak = 0;
			if (m_lowFpsStreak >= 6 && s_currentFrameRate > lowerBound)
			{
				int step = Mathf.Max(1, Mathf.RoundToInt(s_currentFrameRate * 0.15f));
				int nextFrameRate = s_currentFrameRate - step;
				if (nextFrameRate < lowerBound)
				{
					nextFrameRate = lowerBound;
				}
				if (nextFrameRate != s_currentFrameRate)
				{
					s_currentFrameRate = nextFrameRate;
					Time.fixedDeltaTime = 1f / s_currentFrameRate;
				}
				m_lowFpsStreak = 0;
				m_frameRateCooldown = 0.5f;
			}
		}
		else if (m_smoothFps > upperThreshold && s_currentFrameRate < targetFrameRate)
		{
			m_recoverStreak++;
			m_lowFpsStreak = 0;
			if (m_recoverStreak >= 12)
			{
				int step = Mathf.Max(1, Mathf.RoundToInt(s_currentFrameRate * 0.15f));
				int nextFrameRate = s_currentFrameRate + step;
				if (nextFrameRate > targetFrameRate)
				{
					nextFrameRate = targetFrameRate;
				}
				if (nextFrameRate != s_currentFrameRate)
				{
					s_currentFrameRate = nextFrameRate;
					Time.fixedDeltaTime = 1f / s_currentFrameRate;
				}
				m_recoverStreak = 0;
				m_frameRateCooldown = 0.5f;
			}
		}
		else
		{
			m_lowFpsStreak = 0;
			m_recoverStreak = 0;
		}
	}
}
