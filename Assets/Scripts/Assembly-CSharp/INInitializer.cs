using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class INInitializer : MonoBehaviour
{
	[SerializeField]
	private List<GameObject> m_splashes;

	[SerializeField]
	private List<GameObject> m_prefabs;

	[SerializeField]
	private ResourceData m_resourceData;

	private bool m_initialized;

	private bool m_useAlphaAnimation;

	private float m_time;

	public bool Initialized => m_initialized;

	private void Awake()
	{
		m_useAlphaAnimation = true;
		m_time = 3f;
		Initialize().Forget();
	}

	private async UniTask Initialize()
	{
		for (int i = 0; i < m_splashes.Count; i++)
		{
			GameObject splash = Object.Instantiate(m_splashes[i], Vector3.zero, Quaternion.identity);
			await PlayAnimation(splash);
			Object.Destroy(splash);
		}
		INUnity.Initialize(m_resourceData);
		foreach (GameObject prefab in m_prefabs)
		{
			Object.Instantiate(prefab);
		}
		await UniTask.WaitUntil(() => INSettings.VersionSelected);
		m_initialized = true;
		await LoadMainMenu();
	}

	private async UniTask LoadMainMenu()
	{
		await UniTask.WaitUntil(() => SingletonSpawner.SpawnDone);
		await UniTask.WaitUntil(() => Bundle.initialized && !Bundle.checkingBundles && Singleton<GameConfigurationManager>.Instance.HasData);
		PostInitialize();
		Singleton<GameManager>.Instance.LoadMainMenu(showLoadingScreen: true);
	}

	private void PostInitialize()
	{
		if (INSettings.GetBool(INFeature.RuntimeGameData))
		{
			Object.Instantiate(INUnity.LoadGameObject("INPartFactoryManager"));
		}
		if (INSettings.GetBool(INFeature.ApplicationInterface))
		{
			Object.Instantiate(INUnity.LoadGameObject("INApplicationInterface"));
		}
		if (INSettings.GetBool(INFeature.CommandSystem))
		{
			new GameObject("INAddonManager").AddComponent<INAddonManager>();
		}
	}

	private async UniTask PlayAnimation(GameObject gameObject)
	{
		if (!m_useAlphaAnimation)
		{
			await UniTask.Delay((int)(m_time * 1000f), ignoreTimeScale: true);
			return;
		}
		CanvasRenderer canvasRenderer = gameObject.GetComponentInChildren<CanvasRenderer>();
		if (canvasRenderer != null)
		{
			await canvasRenderer.PlayFadeInAnimation(m_time / 3f, ignoreTimeScale: true);
			await UniTask.Delay((int)(m_time / 3f * 1000f), ignoreTimeScale: true);
			await canvasRenderer.PlayFadeOutAnimation(m_time / 3f, ignoreTimeScale: true);
		}
	}
}
