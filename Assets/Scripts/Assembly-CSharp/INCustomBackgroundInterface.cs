using System;
using System.IO;
using Innovation;
using ShellFileDialogs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class INCustomBackgroundInterface : MonoBehaviour
{
	// 复用与 INPartAdjustInterface 相同的模板结构（Scroll View + 分组/项目模板 + 按钮组）
	[SerializeField]
	private GameObject m_content;

	[SerializeField]
	private GameObject m_itemTemplate;

	[SerializeField]
	private GameObject m_groupTemplate;

	[SerializeField]
	private UnityEngine.UI.Button m_saveButton;

	[SerializeField]
	private UnityEngine.UI.Button m_resetButton;

	[SerializeField]
	private UnityEngine.UI.Button m_fillButton;

	private static readonly string[] VideoExtensions = new string[] { ".mp4", ".mov", ".webm", ".m4v", ".avi", ".mkv", ".wmv" };

	private GameObject m_background;

	private Texture2D m_texture;

	private VideoPlayer m_videoPlayer;

	private ToggleSwitch m_enableToggle;

	private ToggleSwitch m_playPauseToggle;

	private ToggleSwitch m_loopToggle;

	private ToggleSwitch m_customScaleToggle;

	private InputField m_pathInput;

	private InputField m_volumeInput;

	private InputField m_buildVolumeInput;

	private InputField m_runVolumeInput;

	private InputField m_soundVolumeInput;

	private InputField m_scaleXInput;

	private InputField m_scaleYInput;

	private InputField m_offsetXInput;

	private InputField m_offsetYInput;

	private InputField m_zInput;

	private InputField m_rotationInput;

	private InputField m_opacityInput;

	private InputField m_playbackSpeedInput;

	private InputField m_seekTimeInput;

	private InputField m_progressInput;

	private Slider m_progressSlider;

	private bool m_isDragging;

	private float m_progressTimer;

	private ToggleSwitch m_absoluteToggle;

	private ToggleSwitch m_cameraAdaptiveToggle;

	private ToggleSwitch m_useLevelSelectionMusicToggle;

	private InputField m_entryMusicVolumeInput;

	private InputField m_absXInput;

	private InputField m_absYInput;

	private string m_lastPath;

	private bool m_isVideo;

	private float m_volume = 1f;

	// 建造界面 / 运行界面 音乐音量（0~1，默认 1；存于 UserSettings）
	private float m_buildVolume = 1f;

	private float m_runVolume = 1f;

	// 音效音量（0~1，默认 1；存于 UserSettings，经 AudioManager 生效，与音乐独立）
	private float m_soundVolume = 1f;

	// 使用关卡选择音乐（默认开：level_selection.ogg；关：用 MusicTheme）
	private bool m_useLevelSelectionMusic = true;

	// 刚进入游戏时主菜单音乐音量（0~1，默认 1）
	private float m_entryMusicVolume = 1f;

	// 播放循环开关：控制 VideoPlayer.isLooping（默认开）
	private bool m_loop = true;

	// 自定义缩放主开关：true 时 X/Y 拉伸倍数生效，false 时背景自动撑满屏幕
	private bool m_customScale = false;

	// X/Y 轴拉伸倍数（无限制，任意数值直接接受；默认 1=原始撑满）
	private float m_scaleX = 1f;

	private float m_scaleY = 1f;

	// 快速填充时暂存的原版 X/Y 拉伸（按"快速填充"前用户手动设的拉伸；再次按填充时用它恢复，而不是还原成 1,1）
	private float m_stashedScaleX = 1f;

	private float m_stashedScaleY = 1f;

	// X/Y 轴偏移（无限制，任意数值直接接受；默认 0=居中）
	private float m_offsetX = 0f;

	private float m_offsetY = 0f;

	// 背景旋转角度（度，无限制，默认 0=正立）
	private float m_rotation = 0f;

	// 背景透明度（0=全透明隐藏，1=完全不透明；默认 1）
	private float m_opacity = 1f;

	// 视频播放速度（倍速，默认 1）
	private float m_playbackSpeed = 1f;

	// 启用绝对坐标：true 时背景固定在全局世界坐标，且大小不随视野(FOV/距离)变化
	private bool m_useAbsolute = false;

	// 绝对坐标 X/Y（世界坐标；绝对坐标的 Z 复用"Z轴距离"字段）
	private float m_absX = 0f;

	private float m_absY = 0f;

	// 摄像机自适应：true(默认) 时背景大小随相机缩放；false 时固定大小不随相机缩放变化
	private bool m_cameraAdaptive = true;

	// 快捷填充开关状态：false=未填充；true=已按下"快速填充"(关闭摄像机自适应+启用绝对坐标)，再次按则恢复
	private bool m_fillApplied = false;

	// 背景距相机的 Z 轴距离（世界前向；绝对坐标模式也用它作为 Z），默认 19
	private float m_bgDistance = 4f;

	// 背景媒体（图片或视频）的宽高比，用于等比撑满参考
	private float m_mediaAspect = 0f;

	private const string TextureFileName = "uploaded.png";

	private const string RecordFileName = "CustomBackground.json";

	private static string StorageDir => Path.Combine(INUnity.SettingsPath, "CustomBackground");

	private static string TexturePath => Path.Combine(StorageDir, TextureFileName);

	private static string RecordPath => Path.Combine(INUnity.SettingsPath, RecordFileName);

	[Serializable]
	private class Record
	{
		public bool HasResource;

		public bool IsVideo;

		public bool Enabled;

		public string Path;

		public float Volume = 1f;

		public bool CustomScale;

		public float ScaleX = 1f;

		public float ScaleY = 1f;

		public float OffsetX;

		public float OffsetY;

		public float Z = 19f;

		public float Rotation;

		// 背景透明度（0~1，默认 1）
		public float Opacity = 1f;

		// 启用绝对坐标：背景固定在全局世界坐标，且大小不随视野变化
		public bool UseAbsolute;

		// 绝对坐标 X/Y（世界坐标；Z 复用"Z轴距离"字段 Z）
		public float AbsX;

		public float AbsY;

		// 摄像机自适应（默认开：大小随相机缩放；关：固定大小）
		public bool CameraAdaptive = true;

		public bool Loop = true;

		public bool UseLevelSelectionMusic = true;

		public float EntryMusicVolume = 1f;

		// 视频播放速度（倍速，默认 1）
		public float PlaybackSpeed = 1f;

		// 时间跳转（秒，默认 0=不跳转）
		public float SeekTime;
	}

	private void Awake()
	{
		m_saveButton.onClick.AddListener(UploadImage);
		m_resetButton.onClick.AddListener(ClearBackground);
	}

	private void Start()
	{
		SetButtonText(m_saveButton, "上传背景");
		SetButtonText(m_resetButton, "清除背景");
        SetButtonText(m_fillButton, "快速填充");
		// 加载建造/运行/音效界面音量设置(UserSettings)
		m_buildVolume = Mathf.Clamp01(UserSettings.GetFloat("BuildMusicVolume", 1f));
		m_runVolume = Mathf.Clamp01(UserSettings.GetFloat("InFlightMusicVolume", 1f));
		m_soundVolume = Mathf.Clamp01(UserSettings.GetFloat("SoundVolume", 1f));
		EnsureFillButton();
		GenerateGroup();
		LoadRecord();
	}

	private void Update()
	{
		// 播放时保持暂停控件状态与 VideoPlayer 同步（如视频播完自动停止）
		if (m_isVideo && m_videoPlayer != null && m_playPauseToggle != null)
		{
			bool playing = m_videoPlayer.isPlaying;
			if (playing != m_playPauseToggle.IsOn)
			{
				m_playPauseToggle.SetIsOnWithoutNotify(playing);
			}
			// 进度条每0.1秒更新（拖动时实时更新文本，松手后跳转）
			if (m_progressInput != null)
			{
				if (m_isDragging)
				{
					float dur = (float)m_videoPlayer.length;
					float cur = m_progressSlider.value * dur;
					float pct = m_progressSlider.value * 100f;
					m_progressInput.text = FormatTime(cur) + " / " + FormatTime(dur) + " (" + pct.ToString("F1") + "%)";
				}
				else
				{
					m_progressTimer += Time.unscaledDeltaTime;
					if (m_progressTimer >= 0.1f)
					{
						m_progressTimer = 0f;
						float cur = (float)m_videoPlayer.time;
						float dur = (float)m_videoPlayer.length;
						float pct = dur > 0f ? (cur / dur * 100f) : 0f;
						m_progressInput.text = FormatTime(cur) + " / " + FormatTime(dur) + " (" + pct.ToString("F1") + "%)";
						if (m_progressSlider != null)
						{
							m_progressSlider.SetValueWithoutNotify(dur > 0f ? cur / dur : 0f);
						}
					}
				}
			}
			VideoClip clip = m_videoPlayer.clip;
			if (clip != null && clip.height > 0)
			{
				float aspect = (float)clip.width / (float)clip.height;
				if (Mathf.Abs(aspect - m_mediaAspect) > 0.001f)
				{
					m_mediaAspect = aspect;
				}
			}
			else if (m_videoPlayer.height > 0)
			{
				// URL 源视频 clip 恒 null，用 VideoPlayer 画布宽高推原视频比例，
				// 保证自定义缩放 X=Y=1 时用真实视频比例(而非 camAspect 兜底→拉伸)，原版比例无拉伸。
				float aspect = (float)m_videoPlayer.width / (float)m_videoPlayer.height;
				if (Mathf.Abs(aspect - m_mediaAspect) > 0.001f)
				{
					m_mediaAspect = aspect;
				}
			}
			// 立即把最新视频比例同步到驱动组件读取的静态配置，
			// 否则 CustomBackgroundConfig.MediaAspect 仍是 0，驱动会误用 camAspect 把视频拉成方/相机比例。
			CustomBackgroundConfig.MediaAspect = m_mediaAspect;
			// 播放循环开关与 VideoPlayer 实时一致
			if (m_loopToggle != null && m_videoPlayer.isLooping != m_loop)
			{
				m_videoPlayer.isLooping = m_loop;
			}
		}
	}

	// 生成"一栏一栏"的界面：开关栏 / 路径输入栏 / 音量栏 / 画面缩放栏 / 时间跳转栏 / 放停栏
	// （把可输入的栏尽量放在前面，避免处于 Scroll View 滚动内容末尾的行被裁切到可视区外而点不到）
	private void GenerateGroup()
	{
		GameObject group = UnityEngine.Object.Instantiate(m_groupTemplate);
		group.SetActive(value: true);
		group.name = "SettingsGroup_CustomBackground";
		group.transform.SetParent(m_content.transform, worldPositionStays: false);
		Text groupName = group.transform.Find("GroupName")?.GetComponent<Text>();
		if (groupName != null)
		{
			groupName.text = "自定义背景（保存按钮是快捷填充）";
		}

		// 栏1：开启背景（开关）
		GameObject toggleItem = CreateItemRow(group.transform, "EnableBackground", ItemControl.Toggle, out ToggleSwitch toggle, out InputField _);
		if (toggle != null)
		{
			m_enableToggle = toggle;
			toggle.OnValueChanged.AddListener(OnToggleChanged);
		}
		SetItemLabel(toggleItem, "开启背景");

		// 栏2：背景路径（输入栏，支持图片或视频路径）
		GameObject pathItem = CreateItemRow(group.transform, "BackgroundPath", ItemControl.Input, out ToggleSwitch _, out InputField path);
		if (path != null)
		{
			m_pathInput = path;
			path.onEndEdit.AddListener(OnPathSubmitted);
		}
		SetItemLabel(pathItem, "背景路径");

		// 栏3：视频音量（输入栏，0~1）
		GameObject volumeItem = CreateItemRow(group.transform, "BackgroundVolume", ItemControl.Input, out ToggleSwitch _, out InputField volume);
		if (volume != null)
		{
			m_volumeInput = volume;
			volume.onEndEdit.AddListener(OnVolumeSubmitted);
		}
		SetItemLabel(volumeItem, "视频音量");

		// 栏4：透明度（输入栏，0~1；0=全透明隐藏，1=完全不透明）
		GameObject opacityItem = CreateItemRow(group.transform, "BackgroundOpacity", ItemControl.Input, out ToggleSwitch _, out InputField opacity);
		if (opacity != null)
		{
			m_opacityInput = opacity;
			opacity.onEndEdit.AddListener(OnOpacitySubmitted);
		}
		SetItemLabel(opacityItem, "透明度");

		// 栏4：自定义缩放（开关，开启后 X/Y 拉伸倍数生效）
		GameObject scaleToggleItem = CreateItemRow(group.transform, "CustomScale", ItemControl.Toggle, out ToggleSwitch scaleToggle, out InputField _);
		if (scaleToggle != null)
		{
			m_customScaleToggle = scaleToggle;
			scaleToggle.OnValueChanged.AddListener(OnCustomScaleChanged);
			scaleToggle.SetIsOnWithoutNotify(m_customScale);
		}
		SetItemLabel(scaleToggleItem, "自定义缩放");

		// 栏：摄像机自适应（开关，默认开：背景大小随相机缩放；关：固定大小不随相机缩放变化）
		GameObject camAdaptItem = CreateItemRow(group.transform, "CameraAdaptive", ItemControl.Toggle, out ToggleSwitch camAdapt, out InputField _);
		if (camAdapt != null)
		{
			m_cameraAdaptiveToggle = camAdapt;
			camAdapt.OnValueChanged.AddListener(OnCameraAdaptiveChanged);
			camAdapt.SetIsOnWithoutNotify(m_cameraAdaptive);
		}
		SetItemLabel(camAdaptItem, "摄像机自适应");

		// 栏5：X 轴拉伸（输入栏，无限制，默认 1）
		GameObject sxItem = CreateItemRow(group.transform, "ScaleX", ItemControl.Input, out ToggleSwitch _, out InputField sx);
		if (sx != null)
		{
			m_scaleXInput = sx;
			sx.onEndEdit.AddListener(OnScaleXSubmitted);
		}
		SetItemLabel(sxItem, "X轴拉伸(默认1)");

		// 栏6：Y 轴拉伸（输入栏，无限制，默认 1）
		GameObject syItem = CreateItemRow(group.transform, "ScaleY", ItemControl.Input, out ToggleSwitch _, out InputField sy);
		if (sy != null)
		{
			m_scaleYInput = sy;
			sy.onEndEdit.AddListener(OnScaleYSubmitted);
		}
		SetItemLabel(syItem, "Y轴拉伸(默认1)");

		// 栏7：X 轴偏移（输入栏，无限制，默认 0）
		GameObject oxItem = CreateItemRow(group.transform, "OffsetX", ItemControl.Input, out ToggleSwitch _, out InputField ox);
		if (ox != null)
		{
			m_offsetXInput = ox;
			ox.onEndEdit.AddListener(OnOffsetXSubmitted);
		}
		SetItemLabel(oxItem, "X轴偏移(默认0)");

		// 栏8：Y 轴偏移（输入栏，无限制，默认 0）
		GameObject oyItem = CreateItemRow(group.transform, "OffsetY", ItemControl.Input, out ToggleSwitch _, out InputField oy);
		if (oy != null)
		{
			m_offsetYInput = oy;
			oy.onEndEdit.AddListener(OnOffsetYSubmitted);
		}
		SetItemLabel(oyItem, "Y轴偏移(默认0)");

		// 栏：启用绝对坐标（开关，开启后背景固定在全局世界坐标，大小不随视野变化）
		GameObject absToggleItem = CreateItemRow(group.transform, "UseAbsolute", ItemControl.Toggle, out ToggleSwitch absToggle, out InputField _);
		if (absToggle != null)
		{
			m_absoluteToggle = absToggle;
			absToggle.OnValueChanged.AddListener(OnAbsoluteChanged);
			absToggle.SetIsOnWithoutNotify(m_useAbsolute);
		}
		SetItemLabel(absToggleItem, "启用绝对坐标");

		// 栏：绝对坐标X（输入栏，仅启用绝对坐标时生效）
		GameObject axItem = CreateItemRow(group.transform, "AbsX", ItemControl.Input, out ToggleSwitch _, out InputField ax);
		if (ax != null)
		{
			m_absXInput = ax;
			ax.onEndEdit.AddListener(OnAbsXSubmitted);
		}
		SetItemLabel(axItem, "绝对坐标X");

		// 栏：绝对坐标Y（输入栏，仅启用绝对坐标时生效）
		GameObject ayItem = CreateItemRow(group.transform, "AbsY", ItemControl.Input, out ToggleSwitch _, out InputField ay);
		if (ay != null)
		{
			m_absYInput = ay;
			ay.onEndEdit.AddListener(OnAbsYSubmitted);
		}
		SetItemLabel(ayItem, "绝对坐标Y");

		// 栏9：旋转角度（输入栏，度，无限制；图片/视频均生效）
		GameObject rotationItem = CreateItemRow(group.transform, "Rotation", ItemControl.Input, out ToggleSwitch _, out InputField rotation);
		if (rotation != null)
		{
			m_rotationInput = rotation;
			rotation.onEndEdit.AddListener(OnRotationSubmitted);
		}
		SetItemLabel(rotationItem, "旋转角度(度)");

		// 栏10：Z 轴距离（输入栏，距相机前向距离；绝对坐标模式下作为绝对坐标 Z）
		GameObject zItem = CreateItemRow(group.transform, "BackgroundZ", ItemControl.Input, out ToggleSwitch _, out InputField z);
		if (z != null)
		{
			m_zInput = z;
			z.onEndEdit.AddListener(OnZSubmitted);
		}
		SetItemLabel(zItem, "Z轴距离(默认4)");

		// 栏10：播放循环（开关，控制视频循环播放）
		GameObject loopItem = CreateItemRow(group.transform, "LoopPlayback", ItemControl.Toggle, out ToggleSwitch loop, out InputField _);
		if (loop != null)
		{
			m_loopToggle = loop;
			loop.OnValueChanged.AddListener(OnLoopChanged);
			loop.SetIsOnWithoutNotify(m_loop);
		}
		SetItemLabel(loopItem, "播放循环");

		// 栏：视频播放速度（输入栏，倍速，默认 1）
		GameObject speedItem = CreateItemRow(group.transform, "PlaybackSpeed", ItemControl.Input, out ToggleSwitch _, out InputField speed);
		if (speed != null)
		{
			m_playbackSpeedInput = speed;
			speed.onEndEdit.AddListener(OnPlaybackSpeedSubmitted);
			speed.text = m_playbackSpeed.ToString();
		}
		SetItemLabel(speedItem, "视频播放速度");

		// 栏：时间跳转（输入栏，秒，输入后视频跳转到该时间点）
		GameObject seekItem = CreateItemRow(group.transform, "SeekTime", ItemControl.Input, out ToggleSwitch _, out InputField seek);
		if (seek != null)
		{
			m_seekTimeInput = seek;
			seek.onEndEdit.AddListener(OnSeekTimeSubmitted);
			seek.text = "0";
		}
		SetItemLabel(seekItem, "跳转时间(秒,百分比,（时）分秒)");

		// 栏：播放进度（只读显示：当前时间 / 总时间 (百分比) + 进度条动画）
		GameObject progressItem = CreateItemRow(group.transform, "Progress", ItemControl.Input, out ToggleSwitch _, out InputField progress);
		if (progress != null)
		{
			m_progressInput = progress;
			progress.readOnly = true;
			progress.interactable = false;
			progress.text = "00:00.0 / 00:00.0 (0.0%)";
		}
		SetItemLabel(progressItem, "播放进度");
		// 在进度栏下方创建 Slider 作为进度条动画（只读，不接收输入）
		if (progress != null && progress.transform.parent != null)
		{
			GameObject sliderGo = new GameObject("ProgressBar");
			sliderGo.transform.SetParent(progress.transform.parent, false);
			RectTransform sliderRt = sliderGo.AddComponent<RectTransform>();
			sliderRt.anchorMin = new Vector2(0f, 0f);
			sliderRt.anchorMax = new Vector2(1f, 0f);
			sliderRt.pivot = new Vector2(0.5f, 1f);
			sliderRt.sizeDelta = new Vector2(0f, 12f);
			sliderRt.anchoredPosition = new Vector2(0f, -2f);
			Slider slider = sliderGo.AddComponent<Slider>();
			slider.interactable = true;
			slider.transition = UnityEngine.UI.Selectable.Transition.None;
			slider.direction = Slider.Direction.LeftToRight;
			slider.minValue = 0f;
			slider.maxValue = 1f;
			slider.value = 0f;
			// Background
			GameObject bgGo = new GameObject("Background");
			bgGo.transform.SetParent(sliderGo.transform, false);
			RectTransform bgRt = bgGo.AddComponent<RectTransform>();
			bgRt.anchorMin = Vector2.zero;
			bgRt.anchorMax = Vector2.one;
			bgRt.sizeDelta = Vector2.zero;
			Image bgImg = bgGo.AddComponent<Image>();
			bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
			// Fill Area
			GameObject fillArea = new GameObject("Fill Area");
			fillArea.transform.SetParent(sliderGo.transform, false);
			RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
			fillAreaRt.anchorMin = Vector2.zero;
			fillAreaRt.anchorMax = Vector2.one;
			fillAreaRt.sizeDelta = Vector2.zero;
			// Fill
			GameObject fillGo = new GameObject("Fill");
			fillGo.transform.SetParent(fillArea.transform, false);
			RectTransform fillRt = fillGo.AddComponent<RectTransform>();
			fillRt.anchorMin = Vector2.zero;
			fillRt.anchorMax = Vector2.one;
			fillRt.sizeDelta = Vector2.zero;
			Image fillImg = fillGo.AddComponent<Image>();
			fillImg.color = new Color(0.3f, 0.7f, 1f, 0.9f);
			slider.fillRect = fillRt;
			m_progressSlider = slider;
			slider.onValueChanged.AddListener(OnProgressSliderChanged);
			// 添加 EventTrigger 检测拖拽开始/结束
			EventTrigger trigger = sliderGo.AddComponent<EventTrigger>();
			EventTrigger.Entry pointerDown = new EventTrigger.Entry();
			pointerDown.eventID = EventTriggerType.PointerDown;
			pointerDown.callback.AddListener((data) => { m_isDragging = true; });
			trigger.triggers.Add(pointerDown);
			EventTrigger.Entry pointerUp = new EventTrigger.Entry();
			pointerUp.eventID = EventTriggerType.PointerUp;
			pointerUp.callback.AddListener((data) => { m_isDragging = false; SeekToSliderValue(); });
			trigger.triggers.Add(pointerUp);
		}

		// 栏11：播放/暂停（开关，仅视频有效；自动与 VideoPlayer 同步）
		GameObject playItem = CreateItemRow(group.transform, "PlayPause", ItemControl.Toggle, out ToggleSwitch play, out InputField _);
		if (play != null)
		{
			m_playPauseToggle = play;
			play.OnValueChanged.AddListener(OnPlayPauseChanged);
		}
		SetItemLabel(playItem, "播放/暂停");

		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);

		// 独立"音量设置"分组（建造/运行/音效三档音量，与自定义背景设置分开）
		GameObject volGroup = UnityEngine.Object.Instantiate(m_groupTemplate);
		volGroup.SetActive(value: true);
		volGroup.name = "SettingsGroup_VolumeSettings";
		volGroup.transform.SetParent(m_content.transform, worldPositionStays: false);
		Text volGroupName = volGroup.transform.Find("GroupName")?.GetComponent<Text>();
		if (volGroupName != null)
		{
			volGroupName.text = "音量设置";
		}

		// 建造界面音量（0~1）
		GameObject buildVolItem = CreateItemRow(volGroup.transform, "BuildVolume", ItemControl.Input, out ToggleSwitch _, out InputField buildVol);
		if (buildVol != null)
		{
			m_buildVolumeInput = buildVol;
			buildVol.onEndEdit.AddListener(OnBuildVolumeSubmitted);
			buildVol.text = m_buildVolume.ToString();
		}
		SetItemLabel(buildVolItem, "建造界面音量");

		// 运行界面音量（0~1）
		GameObject runVolItem = CreateItemRow(volGroup.transform, "RunVolume", ItemControl.Input, out ToggleSwitch _, out InputField runVol);
		if (runVol != null)
		{
			m_runVolumeInput = runVol;
			runVol.onEndEdit.AddListener(OnRunVolumeSubmitted);
			runVol.text = m_runVolume.ToString();
		}
		SetItemLabel(runVolItem, "运行界面音量");

		// 音效音量（0~1）
		GameObject sfxVolItem = CreateItemRow(volGroup.transform, "SoundVolume", ItemControl.Input, out ToggleSwitch _, out InputField sfxVol);
		if (sfxVol != null)
		{
			m_soundVolumeInput = sfxVol;
			sfxVol.onEndEdit.AddListener(OnSoundVolumeSubmitted);
			sfxVol.text = m_soundVolume.ToString();
		}
		SetItemLabel(sfxVolItem, "音效音量");

		// 使用关卡选择音乐（开关，默认开）
		GameObject useLevelMusicItem = CreateItemRow(volGroup.transform, "UseLevelSelectionMusic", ItemControl.Toggle, out ToggleSwitch useLevelMusic, out InputField _);
		if (useLevelMusic != null)
		{
			m_useLevelSelectionMusicToggle = useLevelMusic;
			useLevelMusic.OnValueChanged.AddListener(OnUseLevelSelectionMusicChanged);
			useLevelMusic.SetIsOnWithoutNotify(m_useLevelSelectionMusic);
		}
		SetItemLabel(useLevelMusicItem, "关卡选择音乐");

		// 进入游戏音量（0~1）
		GameObject entryVolItem = CreateItemRow(volGroup.transform, "EntryMusicVolume", ItemControl.Input, out ToggleSwitch _, out InputField entryVol);
		if (entryVol != null)
		{
			m_entryMusicVolumeInput = entryVol;
			entryVol.onEndEdit.AddListener(OnEntryMusicVolumeSubmitted);
			entryVol.text = m_entryMusicVolume.ToString();
		}
		SetItemLabel(entryVolItem, "进入游戏音量(0~1)");

		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)volGroup.transform);
	}

	private enum ItemControl
	{
		Toggle,
		Input
	}

	private GameObject CreateItemRow(Transform group, string name, ItemControl control, out ToggleSwitch toggle, out InputField input)
	{
		GameObject item = UnityEngine.Object.Instantiate(m_itemTemplate);
		item.SetActive(value: true);
		item.name = "SettingsItem_" + name;
		item.transform.SetParent(group, worldPositionStays: false);

		toggle = item.transform.Find("ToggleSwitch")?.GetComponent<ToggleSwitch>();
		input = item.transform.Find("InputField")?.GetComponent<InputField>();

		if (toggle != null)
		{
			toggle.gameObject.SetActive(control == ItemControl.Toggle);
		}
		if (input != null)
		{
			input.gameObject.SetActive(control == ItemControl.Input);
			if (control == ItemControl.Input)
			{
				input.readOnly = false;
				input.interactable = true;
			}
		}
		return item;
	}

	private static void SetItemLabel(GameObject item, string label)
	{
		Text nameText = item?.transform.Find("Name")?.GetComponent<Text>();
		if (nameText != null)
		{
			nameText.text = label;
		}
	}

	private static void SetButtonText(UnityEngine.UI.Button button, string text)
	{
		if (button == null)
		{
			return;
		}
		Text t = button.transform.Find("Text")?.GetComponent<Text>();
		if (t != null)
		{
			t.text = text;
		}
	}

	// 确保存在"快捷填充"按钮：若 prefab 未手动放置，则在按钮组里克隆"上传背景"按钮作为快捷填充按钮。
	// 按钮组为手动锚点布局(无 LayoutGroup)，克隆后需把新按钮平移到"上传背景"左侧，避免重合。
	private void EnsureFillButton()
	{
		if (m_fillButton == null && m_saveButton != null && m_saveButton.transform.parent != null)
		{
			GameObject clone = UnityEngine.Object.Instantiate(m_saveButton.gameObject, m_saveButton.transform.parent);
			clone.name = "FillButton";
			clone.SetActive(value: true);
			RectTransform src = m_saveButton.transform as RectTransform;
			RectTransform rt = clone.transform as RectTransform;
			if (src != null && rt != null)
			{
				rt.anchorMin = src.anchorMin;
				rt.anchorMax = src.anchorMax;
				rt.pivot = src.pivot;
				rt.sizeDelta = src.sizeDelta;
				// 复制"上传背景"同锚定位，再向左平移一个按钮宽度加间距
				rt.anchoredPosition = new Vector2(src.anchoredPosition.x - src.sizeDelta.x - 50f, src.anchoredPosition.y);
			}
			m_fillButton = clone.GetComponent<UnityEngine.UI.Button>();
		}
		if (m_fillButton != null)
		{
			m_fillButton.onClick.RemoveAllListeners();
			m_fillButton.onClick.AddListener(FillShortcut);
			// 固定显示"快速填充"：克隆自"上传背景/保存"按钮，会带本地化组件覆盖文字，先禁用其 UITextLocale。
			DisableLocaleOnButton(m_fillButton);
			SetButtonText(m_fillButton, "快速填充");
		}
	}

	// 禁用按钮 Text 子物体上的本地化组件(UITextLocale)，保证 SetButtonText 设置的文字不被覆盖。
	private static void DisableLocaleOnButton(UnityEngine.UI.Button button)
	{
		if (button == null)
		{
			return;
		}
		UITextLocale locale = button.GetComponentInChildren<UITextLocale>(true);
		if (locale != null)
		{
			locale.enabled = false;
		}
	}

	// 快速填充（开关式）：首次按下→关闭摄像机自适应 + 启用绝对坐标 + 把背景当前尺寸填入 X/Y 拉伸、
	// 当前世界位置填入绝对坐标 X/Y(不改 Z)；再次按下→恢复(开启摄像机自适应 + 关闭绝对坐标)并把 X/Y 拉伸还原为 1,1。
	private void FillShortcut()
	{
		if (m_background == null)
		{
			return;
		}
		m_fillApplied = !m_fillApplied;
		if (m_fillApplied)
		{
			// 按下：先把当前(原版) X/Y 拉伸暂存以便恢复，再关闭摄像机自适应 + 启用绝对坐标，
			// 然后把背景当前实渲染尺寸(localScale)填入 X/Y 拉伸、当前世界位置填入绝对坐标 X/Y（不改 Z）。
			m_stashedScaleX = m_scaleX;
			m_stashedScaleY = m_scaleY;
			m_cameraAdaptive = false;
			m_useAbsolute = true;
			Vector3 ls = m_background.transform.localScale;
			m_scaleX = ls.x;
			m_scaleY = ls.y;
			Vector3 pos = m_background.transform.position;
			m_absX = pos.x;
			m_absY = pos.y;
		}
		else
		{
			// 再次按下：恢复（开启摄像机自适应 + 关闭绝对坐标）并把 X/Y 拉伸还原为按快速填充前暂存的原版值
			m_cameraAdaptive = true;
			m_useAbsolute = false;
			m_scaleX = m_stashedScaleX;
			m_scaleY = m_stashedScaleY;
		}
		if (m_cameraAdaptiveToggle != null)
		{
			m_cameraAdaptiveToggle.SetIsOnWithoutNotify(m_cameraAdaptive);
		}
		if (m_absoluteToggle != null)
		{
			m_absoluteToggle.SetIsOnWithoutNotify(m_useAbsolute);
		}
		if (m_scaleXInput != null)
		{
			m_scaleXInput.text = m_scaleX.ToString();
		}
		if (m_scaleYInput != null)
		{
			m_scaleYInput.text = m_scaleY.ToString();
		}
		if (m_absXInput != null)
		{
			m_absXInput.text = m_absX.ToString();
		}
		if (m_absYInput != null)
		{
			m_absYInput.text = m_absY.ToString();
		}
		PushScaleConfig();
		SaveRecord();
	}

	private void LoadRecord()
	{
		try
		{
			if (File.Exists(RecordPath))
			{
				Record record = null;
				using (StreamReader reader = new StreamReader(RecordPath))
				{
					record = Json.Deserialize<Record>(reader);
				}
				if (record != null && record.HasResource && !string.IsNullOrEmpty(record.Path) && File.Exists(record.Path))
				{
					m_isVideo = record.IsVideo;
					m_volume = record.Volume;
					m_customScale = record.CustomScale;
					m_scaleX = record.ScaleX;
					m_scaleY = record.ScaleY;
					m_offsetX = record.OffsetX;
					m_offsetY = record.OffsetY;
					m_bgDistance = record.Z;
					m_rotation = record.Rotation;
					m_opacity = Mathf.Clamp01(record.Opacity);
					m_useAbsolute = record.UseAbsolute;
					m_absX = record.AbsX;
					m_absY = record.AbsY;
					m_cameraAdaptive = record.CameraAdaptive;
					m_loop = record.Loop;
				m_playbackSpeed = record.PlaybackSpeed;
				float seekTime = record.SeekTime;
				// 音乐设置：从 Record 读取，同时同步到 UserSettings 供 MusicManager 读取
					m_useLevelSelectionMusic = record.UseLevelSelectionMusic;
					m_entryMusicVolume = record.EntryMusicVolume;
					m_playbackSpeed = record.PlaybackSpeed;
					UserSettings.SetBool("UseLevelSelectionMusic", m_useLevelSelectionMusic);
					UserSettings.SetFloat("EntryMusicVolume", m_entryMusicVolume);
					Debug.Log("[CustomBG] LoadRecord: UseLevelSelectionMusic=" + m_useLevelSelectionMusic + ", EntryMusicVolume=" + m_entryMusicVolume);
					if (m_useLevelSelectionMusicToggle != null)
					{
						m_useLevelSelectionMusicToggle.SetIsOnWithoutNotify(m_useLevelSelectionMusic);
					}
					if (m_entryMusicVolumeInput != null)
					{
						m_entryMusicVolumeInput.text = m_entryMusicVolume.ToString();
					}
					if (m_loopToggle != null)
					{
						m_loopToggle.SetIsOnWithoutNotify(m_loop);
					}
					if (m_playbackSpeedInput != null)
					{
						m_playbackSpeedInput.text = m_playbackSpeed.ToString();
					}
					if (m_seekTimeInput != null)
					{
						m_seekTimeInput.text = seekTime.ToString();
					}
					if (m_customScaleToggle != null)
					{
						m_customScaleToggle.SetIsOnWithoutNotify(m_customScale);
					}
					if (m_absoluteToggle != null)
					{
						m_absoluteToggle.SetIsOnWithoutNotify(m_useAbsolute);
					}
					if (m_cameraAdaptiveToggle != null)
					{
						m_cameraAdaptiveToggle.SetIsOnWithoutNotify(m_cameraAdaptive);
					}
					if (m_absXInput != null)
					{
						m_absXInput.text = m_absX.ToString();
					}
					if (m_absYInput != null)
					{
						m_absYInput.text = m_absY.ToString();
					}
					if (m_zInput != null)
					{
						m_zInput.text = m_bgDistance.ToString();
					}
					if (m_pathInput != null)
					{
						m_pathInput.text = record.Path;
					}
					if (m_volumeInput != null)
					{
						m_volumeInput.text = m_volume.ToString();
					}
					if (m_scaleXInput != null)
					{
						m_scaleXInput.text = m_scaleX.ToString();
					}
					if (m_scaleYInput != null)
					{
						m_scaleYInput.text = m_scaleY.ToString();
					}
					if (m_offsetXInput != null)
					{
						m_offsetXInput.text = m_offsetX.ToString();
					}
					if (m_offsetYInput != null)
					{
						m_offsetYInput.text = m_offsetY.ToString();
					}
					if (m_rotationInput != null)
					{
						m_rotationInput.text = m_rotation.ToString();
					}
					if (m_opacityInput != null)
					{
						m_opacityInput.text = m_opacity.ToString();
					}
					if (m_enableToggle != null)
					{
						m_enableToggle.SetIsOnWithoutNotify(record.Enabled);
					}
					if (m_isVideo)
					{
						LoadVideoFromFile(record.Path);
					}
					else
					{
						LoadTextureFromFile(record.Path);
					}
					// 直接按设置应用到背景
					PushScaleConfig();
					// 自动关闭再打开背景，强制 addon 重新创建背景对象并应用所有设置。
					// 跳过初始 RefreshBackground，避免创建两个 addon 对象（Destroy 是延迟的）。
					if (record.Enabled && m_enableToggle != null)
					{
						Debug.Log("[CustomBG] LoadRecord: Enabled=true, Invoke DelayedOpenBackground 0.03s");
						m_enableToggle.SetIsOnWithoutNotify(true);
						Invoke(nameof(DelayedOpenBackground), 0.03f);
					}
					else
					{
						Debug.Log("[CustomBG] LoadRecord: 跳过, Enabled=" + record.Enabled);
						RefreshBackground();
					}
					return;
				}
			}
		}
		catch
		{
		}
		if (m_enableToggle != null)
		{
			m_enableToggle.SetIsOnWithoutNotify(value: false);
		}
	}

	private void SaveRecord()
	{
		try
		{
			Record record = new Record
			{
				HasResource = (m_texture != null || m_videoPlayer != null || !string.IsNullOrEmpty(m_lastPath)),
				IsVideo = m_isVideo,
				Enabled = m_enableToggle != null && m_enableToggle.IsOn,
				Path = m_lastPath,
				Volume = m_volume,
				CustomScale = m_customScale,
				ScaleX = m_scaleX,
				ScaleY = m_scaleY,
				OffsetX = m_offsetX,
				OffsetY = m_offsetY,
				Z = m_bgDistance,
				Rotation = m_rotation,
				Opacity = m_opacity,
				UseAbsolute = m_useAbsolute,
				AbsX = m_absX,
				AbsY = m_absY,
				CameraAdaptive = m_cameraAdaptive,
				Loop = m_loop,
				UseLevelSelectionMusic = m_useLevelSelectionMusic,
				EntryMusicVolume = m_entryMusicVolume,
				PlaybackSpeed = m_playbackSpeed,
				SeekTime = (m_seekTimeInput != null && float.TryParse(m_seekTimeInput.text, out float st)) ? st : 0f
			};
			using StreamWriter writer = new StreamWriter(RecordPath);
			Json.Serialize(writer, record);
		}
		catch
		{
		}
	}

	private static bool IsVideoFile(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		string ext = Path.GetExtension(path);
		foreach (string v in VideoExtensions)
		{
			if (string.Equals(ext, v, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private void OnPathSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		text = text.Trim();
		if (!File.Exists(text))
		{
			return;
		}
		try
		{
			m_isVideo = IsVideoFile(text);
			if (m_isVideo)
			{
				m_lastPath = text;
				LoadVideoFromFile(text);
			}
			else
			{
				Directory.CreateDirectory(StorageDir);
				File.Copy(text, TexturePath, overwrite: true);
				LoadTextureFromFile(TexturePath);
			}
			RefreshBackground();
			SaveRecord();
		}
		catch
		{
		}
	}

	private void OnVolumeSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			m_volume = Mathf.Clamp01(value);
			if (m_videoPlayer != null)
			{
				ApplyVideoVolume();
			}
			SaveRecord();
		}
	}

	private void OnBuildVolumeSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			m_buildVolume = Mathf.Clamp01(value);
			if (m_buildVolumeInput != null)
			{
				m_buildVolumeInput.text = m_buildVolume.ToString();
			}
			UserSettings.SetFloat("BuildMusicVolume", m_buildVolume);
			if (MusicManager.Instance != null)
			{
				MusicManager.Instance.ApplyMusicMuteSetting();
			}
		}
	}

	private void OnRunVolumeSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			m_runVolume = Mathf.Clamp01(value);
			if (m_runVolumeInput != null)
			{
				m_runVolumeInput.text = m_runVolume.ToString();
			}
			UserSettings.SetFloat("InFlightMusicVolume", m_runVolume);
			if (MusicManager.Instance != null)
			{
				MusicManager.Instance.ApplyMusicMuteSetting();
			}
		}
	}

	private void OnSoundVolumeSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			m_soundVolume = Mathf.Clamp01(value);
			if (m_soundVolumeInput != null)
			{
				m_soundVolumeInput.text = m_soundVolume.ToString();
			}
			UserSettings.SetFloat("SoundVolume", m_soundVolume);
			if (AudioManager.Instance != null)
			{
				AudioManager.Instance.ApplySoundVolume();
			}
		}
	}

	private void OnUseLevelSelectionMusicChanged(bool on)
	{
		m_useLevelSelectionMusic = on;
		UserSettings.SetBool("UseLevelSelectionMusic", m_useLevelSelectionMusic);
		Debug.Log("[CustomBG] UseLevelSelectionMusic=" + m_useLevelSelectionMusic);
		SaveRecord();
	}

	private void OnEntryMusicVolumeSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			m_entryMusicVolume = Mathf.Clamp01(value);
			if (m_entryMusicVolumeInput != null)
			{
				m_entryMusicVolumeInput.text = m_entryMusicVolume.ToString();
			}
			UserSettings.SetFloat("EntryMusicVolume", m_entryMusicVolume);
			Debug.Log("[CustomBG] EntryMusicVolume=" + m_entryMusicVolume);
			SaveRecord();
		}
	}

	private void OnCustomScaleChanged(bool on)
	{
		m_customScale = on;
		PushScaleConfig();
		SaveRecord();
	}

	private void DelayedOpenBackground()
	{
		Debug.Log("[CustomBG] DelayedOpenBackground: 开启背景 (IsOn=" + (m_enableToggle != null && m_enableToggle.IsOn) + ")");
		if (m_enableToggle != null)
		{
			m_enableToggle.SetIsOnWithoutNotify(true);
			OnToggleChanged(true);
			Debug.Log("[CustomBG] DelayedOpenBackground: 完成 (m_background=" + (m_background != null) + ")");
		}
	}

	private void OnCameraAdaptiveChanged(bool on)
	{
		m_cameraAdaptive = on;
		PushScaleConfig();
		SaveRecord();
	}

	private void OnScaleXSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// X 轴拉伸倍数，无限制，任意数值直接接受
			m_scaleX = value;
			if (m_scaleXInput != null)
			{
				m_scaleXInput.text = m_scaleX.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnScaleYSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// Y 轴拉伸倍数，无限制，任意数值直接接受
			m_scaleY = value;
			if (m_scaleYInput != null)
			{
				m_scaleYInput.text = m_scaleY.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnOffsetXSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// X 轴偏移，无限制，任意数值直接接受
			m_offsetX = value;
			if (m_offsetXInput != null)
			{
				m_offsetXInput.text = m_offsetX.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnOffsetYSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// Y 轴偏移，无限制，任意数值直接接受
			m_offsetY = value;
			if (m_offsetYInput != null)
			{
				m_offsetYInput.text = m_offsetY.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnZSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// 任意数值直接接受，驱动侧会对非正数做安全兜底(Mathf.Max(0.1f,…))
			m_bgDistance = value;
			if (m_zInput != null)
			{
				m_zInput.text = m_bgDistance.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnRotationSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// 旋转角度，无限制，任意数值直接接受
			m_rotation = value;
			if (m_rotationInput != null)
			{
				m_rotationInput.text = m_rotation.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnOpacitySubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// 透明度，0~1（0=全透明隐藏，1=完全不透明）
			m_opacity = Mathf.Clamp01(value);
			if (m_opacityInput != null)
			{
				m_opacityInput.text = m_opacity.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnAbsoluteChanged(bool on)
	{
		// 仅切换绝对坐标模式，不自动填充任何值：绝对坐标 X/Y、Z轴距离、缩放均由用户手动设置。
		m_useAbsolute = on;
		PushScaleConfig();
		// 更新 addon 的 LocationMode：绝对坐标时禁用自动定位，非绝对时恢复
		if (m_background != null)
		{
			var bg = m_background.GetComponent<AddonBackground>();
			if (bg != null) bg.LocationMode = on ? LocationMode.None : LocationMode.CameraAndScreen;
			var vp = m_background.GetComponent<AddonVideoPlayer>();
			if (vp != null) vp.LocationMode = on ? LocationMode.None : LocationMode.CameraAndScreen;
		}
		SaveRecord();
	}

	private void OnAbsXSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// 绝对坐标 X（世界坐标）
			m_absX = value;
			if (m_absXInput != null)
			{
				m_absXInput.text = m_absX.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnAbsYSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			// 绝对坐标 Y（世界坐标）
			m_absY = value;
			if (m_absYInput != null)
			{
				m_absYInput.text = m_absY.ToString();
			}
			PushScaleConfig();
			SaveRecord();
		}
	}

	private void OnLoopChanged(bool loop)
	{
		m_loop = loop;
		if (m_videoPlayer != null)
		{
			m_videoPlayer.isLooping = loop;
		}
		SaveRecord();
	}

	private void OnPlaybackSpeedSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (float.TryParse(text, out float value))
		{
			m_playbackSpeed = value;
			if (m_playbackSpeedInput != null)
			{
				m_playbackSpeedInput.text = m_playbackSpeed.ToString();
			}
			if (m_videoPlayer != null)
			{
				m_videoPlayer.playbackSpeed = m_playbackSpeed;
			}
			SaveRecord();
		}
	}

	private void OnProgressSliderChanged(float value)
	{
	}

	private void SeekToSliderValue()
	{
		if (m_videoPlayer == null || !m_videoPlayer.isPrepared || m_progressSlider == null)
		{
			return;
		}
		float dur = (float)m_videoPlayer.length;
		float targetTime = Mathf.Clamp01(m_progressSlider.value) * dur;
		m_videoPlayer.time = targetTime;
	}

	private void OnSeekTimeSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		text = text.Trim();
		// -1 表示不操作
		if (text == "-1")
		{
			return;
		}
		if (m_videoPlayer == null || !m_videoPlayer.isPrepared)
		{
			return;
		}
		text = text.Trim();
		float dur = (float)m_videoPlayer.length;
		float targetTime = -1f;

		// 百分比模式：以 % 或 ％ 结尾
		if (text.EndsWith("%") || text.EndsWith("％"))
		{
			string numStr = text.Substring(0, text.Length - 1);
			if (float.TryParse(numStr, out float pct))
			{
				targetTime = dur * Mathf.Clamp01(pct / 100f);
			}
		}
		// 时分秒模式：包含英文冒号或中文冒号
		else if (text.Contains(":") || text.Contains("："))
		{
			string normalized = text.Replace("：", ":");
			string[] parts = normalized.Split(':');
			if (parts.Length == 3)
			{
				// H:M:S
				if (float.TryParse(parts[0], out float h) &&
				    float.TryParse(parts[1], out float m) &&
				    float.TryParse(parts[2], out float s))
				{
					targetTime = h * 3600f + m * 60f + s;
				}
			}
			else if (parts.Length == 2)
			{
				// M:S
				if (float.TryParse(parts[0], out float m) &&
				    float.TryParse(parts[1], out float s))
				{
					targetTime = m * 60f + s;
				}
			}
		}
		// 纯秒数模式
		else
		{
			if (float.TryParse(text, out float value))
			{
				targetTime = value;
			}
		}

		if (targetTime >= 0f)
		{
			m_videoPlayer.time = Mathf.Clamp(targetTime, 0f, dur);
		}
		SaveRecord();
	}

	private static string FormatTime(float totalSeconds)
	{
		if (totalSeconds < 0f) totalSeconds = 0f;
		int mins = (int)(totalSeconds / 60f);
		float secs = totalSeconds - mins * 60f;
		return mins.ToString("D2") + ":" + secs.ToString("F1").PadLeft(4, '0');
	}

	private void OnPlayPauseChanged(bool shouldPlay)
	{
		if (m_videoPlayer == null)
		{
			return;
		}
		if (shouldPlay)
		{
			m_videoPlayer.Play();
		}
		else
		{
			m_videoPlayer.Pause();
		}
	}

	private void UploadImage()
	{
		Filter[] filters = new Filter[1]
		{
			new Filter("Image/Video", "*.png;*.jpg;*.jpeg;*.bmp;*.tga;*.mp4;*.mov;*.webm;*.m4v;*.avi")
		};
		string path = FileOpenDialog.ShowSingleSelectDialog(IntPtr.Zero, string.Empty, Application.dataPath, string.Empty, filters, 0);
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return;
		}
		try
		{
			m_isVideo = IsVideoFile(path);
			if (m_isVideo)
			{
				m_lastPath = path;
				LoadVideoFromFile(path);
			}
			else
			{
				Directory.CreateDirectory(StorageDir);
				File.Copy(path, TexturePath, overwrite: true);
				LoadTextureFromFile(TexturePath);
			}
			if (m_pathInput != null)
			{
				m_pathInput.text = path;
			}
			RefreshBackground();
			SaveRecord();
		}
		catch
		{
		}
	}

	private void ClearBackground()
	{
		DestroyBackgroundObject();
		if (m_enableToggle != null)
		{
			m_enableToggle.SetIsOnWithoutNotify(value: false);
		}
		if (m_pathInput != null)
		{
			m_pathInput.text = string.Empty;
		}
		m_texture = null;
		m_videoPlayer = null;
		m_lastPath = null;
		m_isVideo = false;
		m_customScale = false;
		m_scaleX = 1f;
		m_scaleY = 1f;
		m_stashedScaleX = 1f;
		m_stashedScaleY = 1f;
		m_offsetX = 0f;
		m_offsetY = 0f;
		m_mediaAspect = 0f;
		m_bgDistance = 4f;
		m_rotation = 0f;
		m_opacity = 1f;
		m_playbackSpeed = 1f;
		if (m_seekTimeInput != null)
		{
			m_seekTimeInput.text = "0";
		}
		m_useAbsolute = false;
		m_absX = 0f;
		m_absY = 0f;
		m_cameraAdaptive = true;
		if (m_customScaleToggle != null)
		{
			m_customScaleToggle.SetIsOnWithoutNotify(m_customScale);
		}
		if (m_absoluteToggle != null)
		{
			m_absoluteToggle.SetIsOnWithoutNotify(m_useAbsolute);
		}
		if (m_cameraAdaptiveToggle != null)
		{
			m_cameraAdaptiveToggle.SetIsOnWithoutNotify(m_cameraAdaptive);
		}
		if (m_scaleXInput != null)
		{
			m_scaleXInput.text = m_scaleX.ToString();
		}
		if (m_scaleYInput != null)
		{
			m_scaleYInput.text = m_scaleY.ToString();
		}
		if (m_offsetXInput != null)
		{
			m_offsetXInput.text = m_offsetX.ToString();
		}
		if (m_offsetYInput != null)
		{
			m_offsetYInput.text = m_offsetY.ToString();
		}
		if (m_zInput != null)
		{
			m_zInput.text = m_bgDistance.ToString();
		}
		if (m_rotationInput != null)
		{
			m_rotationInput.text = m_rotation.ToString();
		}
		if (m_opacityInput != null)
		{
			m_opacityInput.text = m_opacity.ToString();
		}
		if (m_absXInput != null)
		{
			m_absXInput.text = m_absX.ToString();
		}
		if (m_absYInput != null)
		{
			m_absYInput.text = m_absY.ToString();
		}
		PushScaleConfig();
		SaveRecord();
	}

	private void LoadTextureFromFile(string filePath)
	{
		m_texture = INAddonManager.LoadTexture(File.ReadAllBytes(filePath), "CustomBackgroundTexture");
		m_videoPlayer = null;
		m_lastPath = filePath;
		if (m_texture != null && m_texture.height > 0)
		{
			m_mediaAspect = (float)m_texture.width / (float)m_texture.height;
		}
	}

	// 记录视频路径，不立即创建（实际创建统一走 RefreshBackground->CreateVideoBackground）
	private void LoadVideoFromFile(string filePath)
	{
		m_texture = null;
		m_lastPath = filePath;
	}

	// 统一使用 addon 播放器：视频用 INAddonManager.CreateVideoPlayer，图片用 CreateBackground。
	// 自定义缩放由挂在背景身上的 CustomBackgroundScale 驱动组件在每帧 LateUpdate 兜底覆盖，
	// 该组件跟随后台对象存续(不随设置面板关闭而停)，保证退出设置界面后缩放仍生效。
	private VideoPlayer CreateVideoBackground(string filePath)
	{
		if (INAddonManager.Instance == null)
		{
			return null;
		}
		try
		{
			AddonVideoPlayer avp = INAddonManager.Instance.CreateVideoPlayer(filePath, LocationMode.CameraAndScreen);
			VideoPlayer vp = avp.Player;
			m_background = avp.gameObject;
			EnsureScaleDriver(m_background);
			m_videoPlayer = vp;
			m_isVideo = true;
			if (vp != null)
			{
				vp.isLooping = m_loop;
				vp.playbackSpeed = m_playbackSpeed;
			}
			if (m_loopToggle != null)
			{
				m_loopToggle.SetIsOnWithoutNotify(m_loop);
			}
			ApplyVideoVolume();
			vp.playbackSpeed = m_playbackSpeed;
			// 如果有保存的跳转时间，视频准备就绪后跳转
			if (m_seekTimeInput != null && float.TryParse(m_seekTimeInput.text, out float seekVal) && seekVal > 0f)
			{
				vp.prepareCompleted += (VideoPlayer vp2) => { vp2.time = seekVal; };
			}
			return vp;
		}
		catch
		{
			return null;
		}
	}

	// 图片背景：addon CreateBackground 直接由 addon 撑满屏幕
	private void CreateImageBackground()
	{
		if (INAddonManager.Instance == null)
		{
			return;
		}
		try
		{
			AddonBackground bg = INAddonManager.Instance.CreateBackground(m_texture, null, LocationMode.CameraAndScreen);
			m_background = bg.gameObject;
			EnsureScaleDriver(m_background);
			m_videoPlayer = null;
			m_isVideo = false;
		}
		catch
		{
		}
	}

	// 给 addon 背景挂上持久缩放驱动组件并同步当前配置，避免设置面板关闭后缩放失效
	private void EnsureScaleDriver(GameObject go)
	{
		if (go == null)
		{
			return;
		}
		if (go.GetComponent<CustomBackgroundScale>() == null)
		{
			go.AddComponent<CustomBackgroundScale>();
		}
		// 绝对坐标时禁用 addon 自动定位（避免覆盖我们设置的位置）；
		// 非绝对坐标时保留 addon 的 CameraAndScreen 模式（自定义缩放关闭时 addon 会自动撑满屏幕）。
		var bg = go.GetComponent<AddonBackground>();
		if (bg != null)
		{
			bg.LocationMode = CustomBackgroundConfig.UseAbsolute ? LocationMode.None : LocationMode.CameraAndScreen;
		}
		var vp = go.GetComponent<AddonVideoPlayer>();
		if (vp != null)
		{
			vp.LocationMode = CustomBackgroundConfig.UseAbsolute ? LocationMode.None : LocationMode.CameraAndScreen;
		}
		PushScaleConfig();
	}

	// 把当前缩放配置写入共享静态配置，持久驱动组件每秒读取应用
	private void PushScaleConfig()
	{
		CustomBackgroundConfig.Enabled = m_customScale;
		CustomBackgroundConfig.BackgroundEnabled = m_enableToggle != null && m_enableToggle.IsOn;
		CustomBackgroundConfig.ScaleX = m_scaleX;
		CustomBackgroundConfig.ScaleY = m_scaleY;
		CustomBackgroundConfig.OffsetX = m_offsetX;
		CustomBackgroundConfig.OffsetY = m_offsetY;
		CustomBackgroundConfig.Dist = m_bgDistance;
		CustomBackgroundConfig.Rotate = m_rotation;
		CustomBackgroundConfig.Opacity = m_opacity;
		CustomBackgroundConfig.UseAbsolute = m_useAbsolute;
		CustomBackgroundConfig.AbsX = m_absX;
		CustomBackgroundConfig.AbsY = m_absY;
		CustomBackgroundConfig.CameraAdaptive = m_cameraAdaptive;
		CustomBackgroundConfig.MediaAspect = m_mediaAspect;
	}

	private void ApplyVideoVolume()
	{
		if (m_videoPlayer == null)
		{
			return;
		}
		try
		{
			m_videoPlayer.SetDirectAudioVolume(0, m_volume);
		}
		catch
		{
		}
	}

	private void OnToggleChanged(bool enabled)
	{
		PushScaleConfig();
		RefreshBackground();
		SaveRecord();
	}

	private void RefreshBackground()
	{
		DestroyBackgroundObject();
		if (m_enableToggle != null && !m_enableToggle.IsOn)
		{
			return;
		}
		if (m_isVideo && !string.IsNullOrEmpty(m_lastPath) && File.Exists(m_lastPath))
		{
			CreateVideoBackground(m_lastPath);
		}
		else if (m_texture != null)
		{
			CreateImageBackground();
		}
	}

	private void DestroyBackgroundObject()
	{
		if (m_background != null)
		{
			UnityEngine.Object.Destroy(m_background);
			m_background = null;
		}
		m_videoPlayer = null;
	}

	private void OnDestroy()
	{
		DestroyBackgroundObject();
	}
}

// 自定义背景缩放配置的共享静态状态：设置界面写入，持久驱动组件读取应用。
// 这样即使设置面板被禁用/销毁，缩放仍由挂在背景对象上的组件持续驱动。
public static class CustomBackgroundConfig
{
	// 自定义缩放开关（决定是否覆盖尺寸）
	public static bool Enabled;

	// 背景是否开启（开启背景 开关；关闭时透明度强制 0 隐藏）
	public static bool BackgroundEnabled;

	public static float ScaleX = 1f;

	public static float ScaleY = 1f;

	// X/Y 轴偏移（屏幕比例语义，默认 0=居中；驱动组件乘视野换算为相机相对位移）
	public static float OffsetX;

	public static float OffsetY;

	public static float Dist = 4f;

	// 背景旋转角度（度，默认 0=正立）
	public static float Rotate;

	// 背景透明度（0~1，默认 1；0=全透明隐藏）
	public static float Opacity = 1f;

	// 启用绝对坐标：背景固定在全局世界坐标，且大小不随视野(FOV/距离)变化
	public static bool UseAbsolute;

	// 绝对坐标 X/Y（世界坐标；Z 复用 Dist）
	public static float AbsX;

	public static float AbsY;

	// 摄像机自适应（默认开：大小随相机缩放；关：固定大小不随相机缩放变化）
	public static bool CameraAdaptive = true;

	// 0 表示未知(用屏幕宽高比)，由驱动组件回退处理
	public static float MediaAspect;
}

// 挂在 addon 背景 GameObject 上的持久缩放驱动组件。
// 它跟随后台对象存续(不随设置界面关闭而停)，每帧 LateUpdate 覆盖 addon 的 LocationMode，
// 保证"自定义缩放"开启时 X/Y 拉伸 + Z 距离在退出设置界面后仍然生效。
public class CustomBackgroundScale : MonoBehaviour
{
	private void LateUpdate()
	{
		Transform t = transform;
		if (t == null)
		{
			return;
		}
		// 透明度：自定义背景关闭时强制 alpha=0（隐藏），否则用用户设置的透明度 Opacity(0~1)。
		ApplyOpacity();

		Camera cam = Camera.main;
		float d = Mathf.Max(0.1f, CustomBackgroundConfig.Dist);
		float camAspect = (cam != null) ? cam.aspect : 1f;
		float viewH;
		if (cam != null && cam.orthographic)
		{
			viewH = 2f * cam.orthographicSize;
		}
		else
		{
			float fov = (cam != null) ? cam.fieldOfView : 60f;
			viewH = 2f * d * Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f);
		}

		if (CustomBackgroundConfig.UseAbsolute)
		{
			// 绝对坐标：固定在全局世界坐标(绝对坐标 X/Y + Z轴距离 Dist)，不随摄像机旋转——仅叠加用户旋转角。
			t.position = new Vector3(CustomBackgroundConfig.AbsX, CustomBackgroundConfig.AbsY, d);
			t.rotation = Quaternion.Euler(0f, 0f, CustomBackgroundConfig.Rotate);
		}
		else if (cam == null)
		{
			// 无相机兜底：退回本地绝对坐标（多数场景有相机，不会走这里）
			t.localPosition = new Vector3(CustomBackgroundConfig.OffsetX, CustomBackgroundConfig.OffsetY, d);
		}
		else
		{
			// 位置：X/Y 跟随摄像机偏移，Z 固定为世界坐标（不随摄像机移动）。
			float viewW = viewH * camAspect;
			Vector3 fwd = cam.transform.forward;
			Vector3 up = cam.transform.up;
			Vector3 right = cam.transform.right;
			Vector3 camRelative = cam.transform.position
				+ right * (CustomBackgroundConfig.OffsetX * viewW)
				+ up * (CustomBackgroundConfig.OffsetY * viewH);
			t.position = new Vector3(camRelative.x, camRelative.y, d);
			t.rotation = Quaternion.LookRotation(fwd, up) * Quaternion.Euler(0f, 0f, CustomBackgroundConfig.Rotate);
		}

		// 尺寸：由"摄像机自适应"开关决定。
		if (CustomBackgroundConfig.CameraAdaptive)
		{
			// 开启(默认)：大小随相机缩放。自定义缩放开启时用视野高度换算(拉伸)以跟随缩放；
			// 关闭自定义缩放时交给 addon 自动撑满(同样随相机缩放)。
			if (CustomBackgroundConfig.Enabled)
			{
				float mediaAspect = (CustomBackgroundConfig.MediaAspect > 0f) ? CustomBackgroundConfig.MediaAspect : camAspect;
				t.localScale = new Vector3(viewH * mediaAspect * CustomBackgroundConfig.ScaleX, viewH * CustomBackgroundConfig.ScaleY, 1f);
			}
		}
		else
		{
			// 关闭：固定大小，不随摄像机缩放变化(直接用 X/Y 拉伸倍数作为固定尺寸)。
			t.localScale = new Vector3(CustomBackgroundConfig.ScaleX, CustomBackgroundConfig.ScaleY, 1f);
		}
	}

	// 修改背景所有渲染器材质颜色的 alpha：
	// 关闭背景时透明度强制 0（隐藏）；开启时用 CustomBackgroundConfig.Opacity(0~1)。
	private void ApplyOpacity()
	{
		float alpha = CustomBackgroundConfig.BackgroundEnabled
			? Mathf.Clamp01(CustomBackgroundConfig.Opacity)
			: 0f;
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		if (renderers == null)
		{
			return;
		}
		foreach (Renderer r in renderers)
		{
			if (r == null)
			{
				continue;
			}
			Material mat = r.material;
			if (mat == null)
			{
				continue;
			}
			Color c = mat.color;
			if (Mathf.Abs(c.a - alpha) > 0.0001f)
			{
				c.a = alpha;
				mat.color = c;
			}
		}
	}
}
