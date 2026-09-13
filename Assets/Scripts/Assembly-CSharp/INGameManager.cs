using Innovation;
using UnityEngine;

public class INGameManager : MonoBehaviour
{
	private Vector2Int m_resolution;

	public static INGameManager Instance { get; private set; }

	public void SetFullScreen(bool fullScreen)
	{
		if (Screen.fullScreen != fullScreen)
		{
			if (fullScreen)
			{
				Resolution currentResolution = Screen.currentResolution;
				m_resolution = new Vector2Int(Screen.width, Screen.height);
				Screen.SetResolution(currentResolution.width, currentResolution.height, fullscreen: true);
			}
			else
			{
				Screen.SetResolution(m_resolution.x, m_resolution.y, fullscreen: false);
			}
		}
	}

	private void Awake()
	{
		Instance = this;
		Object.DontDestroyOnLoad(this);
		Resolution currentResolution = Screen.currentResolution;
		m_resolution = new Vector2Int(currentResolution.width, currentResolution.height);
		BP.CoreService = new CoreService();
		BP.AddonService = new AddonService();
		BP.PartService = new PartService();
		Json.Service = new JsonService();
	}

	private void Update()
	{
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.F))
		{
			bool flag = !Screen.fullScreen;
			INSettingsInterface.Instance?.SetValue("GeneralSettings_FullScreen", flag);
		}
	}
}
