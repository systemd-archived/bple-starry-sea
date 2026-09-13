# 输入框无法输入 —— 多 EventSystem 根因分析（存档）

> 归档日期：2026-08-26　状态：**暂不修复**（用户决定存档，后续排期再动工）
> 本文仅做只读诊断，**未改动任何代码 / 场景 / 资产**。

---

## 一、现象

- 在游戏内进入「设置」界面后，**所有 uGUI InputField 都无法键入文本**。
- Unity 控制台持续刷错误（每帧多条）：

```
There can be only one active Event System.
  EventSystem.cs:437
There are 2 event systems in the scene. Please ensure there is always exactly one event system in the scene
  EventSystem.cs:527
```

- 背景/自定义背景相关代码与此无关；回退背景代码不能解决（已确认）。

---

## 二、根因（本次实测精确定位）

项目里存在**两个不同来源各生成一个 EventSystem**，进入设置的一瞬间即为 2 个：

| # | 来源 | 文件 | 触发时机 | 是否持久 |
|---|------|------|----------|----------|
| ① | `INVersionSelector.prefab` **内嵌一个名为 `EventSystem` 的子对象**（含 `EventSystem` + `StandaloneInputModule`） | `Assets/GameObject/INVersionSelector.prefab`（对象 fileID `1985305603660343`） | 版本选择界面随 prefab 实例化即出现 | 随 prefab 实例存在 |
| ② | `INSettings.InitializeSettings()` 无条件 `DontDestroyOnLoad(Instantiate("EventSystem"))` | `Assets/Scripts/Assembly-CSharp/INSettings.cs:642` | 点「进入」版本按钮时 | 跨场景常驻 |

**调用链（来源②）：**
```
INVersionButton(type=-1):OnClick()
→ INVersionSelector.EnterVersion()          INVersionSelector.cs:67
→ INSettings.Initialize(m_version)          INSettings.cs:305
→ InitializeSettings()                      INSettings.cs:642
→ DontDestroyOnLoad( Instantiate( LoadGameObject("EventSystem") ) )
```

### 为什么"单次进设置"就已 2 个（不是"反复进出才累积"）
1. SplashScreen → 版本选择：随 `INVersionSelector.prefab` 实例化，**来源①** 出现。此刻只有它一个，版本按钮可点。
2. 用户点「进入」：`InitializeSettings()` 通过**另一个独立 prefab** `Assets/GameObject/EventSystem.prefab` 新建 **来源②** 并 `DontDestroyOnLoad`。
3. 从这一帧起场景内 **2 个活动 EventSystem** → uGUI `InputField` 依赖 `EventSystem.current` 路由键盘输入，两个打架 → 全部输入框失效。

关键证据：控制台在来源② 实例化那一帧立即抛出 `There can be only one active Event System`（说明 New 之前已存在来源①）。

### 排除项（已核查）
- 场景文件里没有任何放置的 EventSystem（`Assets/Scene` 下无 `m_Name: EventSystem`）。
- 全项目 C# 中只有 `INSettings.cs:642` 一处通过名字加载/实例化 `"EventSystem"`；其余 `EventSystem` 引用仅为组件类名（GuiManager、InputFieldFitter、ToggleSwitch 等）。
- `INPartFactoryManager.InitializeSettings()` 是另一类方法，仅注册零件，不生成 EventSystem。

---

## 三、为什么此前两种修复都失败（勿重复）

历史曾两次尝试，均导致「进不去设置」并被完全还原：
1. **在 `INSettings.InitializeSettings()` 加防重守卫**（已存在则不 New）——破坏设置入口。
2. **独立 `INEventSystemGuard.cs` 每帧销毁多余 EventSystem**——同样破坏设置入口。

**重要推论（本文实测推演）：**
只要 `INSettings.InitializeSettings():642` 仍在点「进入」时新建一个 EventSystem，那么**无论是否去掉来源①、是否另建持久 EventSystem，进入设置时都会有 2 个**，输入框依旧失效。

因此：**真正把 EventSystem 减到 1 个，必须消除来源①与来源② 其中之一；而唯一可靠的控制点就在 `INSettings.InitializeSettings()`（来源②）。** 凡是"不碰那处代码就指望修好"的方案，均不可行。

> ⚠️ 本项目 `execute_csharp_script` 因 `Assets/Plugins/UnityEngine.UI.dll` 与 `com.unity.ugui` asmdef 同名冲突而不可用（Roslyn 引用解析失败），运行时务必靠「控制台 Error 数量 + 手工 Play 实测」验证，不能靠 C# 脚本枚举。

---

## 四、后续修复方案（待用户授权，勿擅自实施）

以下任一方案都**必须经用户明确授权**再动（尤其触碰 `INSettings.cs` 的核心启动流程）。所有改动**先备份、可无限叠加、永不覆盖删除备份**，改资产后需 `unity_asset action=import` 重导入，再 Play 验证。

### 方案 A（改动最小、单点、可回退）——在 `INSettings.InitializeSettings()` 加防重守卫
- 改 `Assets/Scripts/Assembly-CSharp/INSettings.cs:642`：
  先检查 `UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>()` 是否存在，存在则**不再实例化**，仍继续执行其下 `INLocalization.Create()` / `INUserSettings.Load()` / `INContraptionDataManager.SetContraptionData()`。
- 前提：版本选择阶段需保留来源① 的 EventSystem（在点「进入」前它是唯一一个），以满足「唯一一个」。
- 风险：触碰核心启动文件；历史上同思路曾破坏设置入口 → 必须以 `GetComponent`/`FindObjectOfType` 精确、仅跳过实例化、保留其余副作用，并充分 Play 实测。

### 方案 B —— 去掉 `INVersionSelector.prefab` 内嵌 EventSystem + 确认来源②为唯一
- 改 prefab 资产（备份+reimport），并**必须**确保"点进入前确实有唯一持久 EventSystem"。推演确认：仅去内嵌而不处理来源②，会导致「进入设置时仍有 2 个」，输入框照旧失效；且版本选择阶段会暂时无 EventSystem。
- 因此方案 B 实质仍需配合方案 A 才能成立。

### 统一验证清单（无论哪个方案）
1. 备份所有将被改动的 `.cs` / `.prefab`（唯一增量命名）。
2. 改 C# → `unity_editor start_compilation_pipeline`（看是否 `error CS`）。
3. 改 prefab 资产 → `unity_asset action=import` 重导入。
4. 进 Play：SplashScreen → 版本选择界面的按钮**能点**。
5. 点「进入」→ 设置界面输入框**能输入**。
6. 控制台无 `There are N event systems` 报错。
7. 任一步不符即回退备份，勿硬撑。

---

## 五、结论一句话

输入框失效 = **进入设置时 EventSystem 变 2 个（版本选择 prefab 内嵌 1 个 + INSettings 进版本又 New 1 个）**；要修必须消除其中一处创建，唯一可靠控制点是 `INSettings.InitializeSettings()`。**现状未改动，待用户授权后再按方案 A/B 排期实施。**
