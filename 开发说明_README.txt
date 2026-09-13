================================================================
  BPLE 星海版 (新创unity星海) — 工程交接说明
  Unity 版本: 2021.3.45f2c1
================================================================

一、工程概述
────────────
本工程基于 BPLE 2022.1.9 反编译版本进行二次开发，
主要增加了"星海版"自定义功能（机械门参数、自定义背景、
音量分区控制等）。

二、Unity 编辑器要求
────────────────────
- Unity 2021.3.45f2c1（团结引擎 Tuanjie 版本）
  安装路径示例: C:\Program Files\Unity\Hub\Editor\2021.3.45f2c1
- 确保安装了 Android Build Support 模块（如需打包安卓）

三、首次打开步骤
────────────────
1. 打开 Unity Hub → Add → 选择本文件夹（bpxhgc）
2. Unity 会自动导入资源并生成 Library 目录（首次约5-15分钟）
3. 打开主场景: Assets/Scene/Scenes/UI/SplashScreen.unity
4. 如遇编译错误，先执行 Assets → Reimport All

四、打包说明
────────────
【安卓 APK】
- 菜单: Tools → Build → Build Android APK
- 脚本: Assets/Editor/BuildAndroidApk.cs
- 配置: Mono2x + ARMv7（32位），代码裁剪 Disabled
- 包名: com.star.tech.build
- 输出: Builds/新创unity星海XXX.apk
- 打包前会自动重建 AssetBundle（Tools → AssetBundles → Rebuild Android）

【Windows】
- 菜单: Tools → Build → Build Windows
- 脚本: Assets/Editor/BuildWindows.cs
- 输出: Builds/Windows/新创unity星海XXX.exe

【修改版本号】
- 编辑对应脚本中的 OutputApk / locationPathName 即可
- 打包前建议先手动 Rebuild AssetBundle

五、核心代码结构
────────────────
Assets/
├── Editor/
│   ├── BuildAndroidApk.cs    — 安卓打包脚本
│   └── BuildWindows.cs       — Windows 打包脚本
├── Scripts/
│   ├── INCustomBackgroundInterface.cs  — 自定义背景面板
│   ├── INSettingsInterface.cs          — 设置界面核心
│   ├── INSettings.cs                   — 设置系统（慎改启动逻辑）
│   ├── MusicManager.cs                 — 音乐管理（建造/运行音量）
│   ├── AudioManager.cs                 — 音效管理（SFX音量）
│   ├── MechanicalGate.cs               — 机械门逻辑
│   └── StarSeaSettings.cs              — 星海自定义参数
├── Scene/
│   └── Scenes/UI/
│       ├── SplashScreen.unity  — 启动场景
│       ├── MainMenu.unity      — 主菜单
│       └── LevelStub.unity     — 关卡
└── GameObject/
    └── AddonVideoPlayer.prefab — addon 视频播放器（勿删组件）

六、已知注意事项
────────────────
1. 【EventSystem 冲突】进入设置会同时出现2个 EventSystem，
   导致输入框无法输入。根因: INSettings.InitializeSettings() 每次
   都 DontDestroyOnLoad 新建 EventSystem + INVersionSelector.prefab
   内嵌一个 EventSystem。暂未修复，不要尝试在 InitializeSettings
   加防重守卫（会导致进不去设置）。

2. 【安卓打包】必须用 Mono2x + ARMv7，不要用 IL2CPP+ARM64，
   否则反射属性被裁剪导致设置界面空白。

3. 【安卓文件上传】无法唤出系统文件选择器，用路径输入框粘贴
   文件路径实现上传。

4. 【AddonVideoPlayer.prefab】上的 MeshRenderer/MeshFilter
   是原厂组件，不要移除，否则播放器异常。

5. 【本地化】INLocalizationData.json 中 key 必须存在且已导入，
   否则对应 UI 按钮/文本显示空白。

七、其他文档
────────────
- README.md               — 项目简介
- .gitignore              — Git 忽略规则
- Packages/manifest.json  — UPM 包依赖清单

================================================================
  打包日期: 2026-08-29
  如有问题请联系原开发者
================================================================
