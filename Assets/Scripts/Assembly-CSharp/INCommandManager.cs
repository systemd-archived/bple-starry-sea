using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Innovation;
using Innovation.Script;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class INCommandManager : Singleton<INCommandManager>
{
	[Serializable]
	public class CommandSettings
	{
		public Version Version { get; set; }

		public string EntryPoint { get; set; }

		public string CurrentThemeName { get; set; }

		public Dictionary<string, CommandTheme> Themes { get; set; }

		public CommandTheme GetCurrentTheme()
		{
			Themes.TryGetValue(CurrentThemeName, out var value);
			return value;
		}

		public void UpdateVersion()
		{
			Version = INUnity.Version;
		}
	}

	[Serializable]
	public class CommandTheme
	{
		public HexColor BackgroundColor { get; set; }

		public int TextSize { get; set; }

		public HexColor TextColor { get; set; }

		public HexColor ExceptionColor { get; set; }

		public HexColor CodeKeywordColor { get; set; }

		public HexColor CodePunctuationColor { get; set; }

		public HexColor CodeOperatorColor { get; set; }

		public HexColor CodeStringColor { get; set; }

		public HexColor CodeNumberColor { get; set; }

		public HexColor CodeCommentColor { get; set; }
	}

	public class ScriptWriter : TextWriter
	{
		private bool m_changed;

		private StringBuilder m_builder;

		private Encoding m_encoding;

		private static string s_newLine = "\n";

		public bool IsChanged => m_changed;

		public StringBuilder Builder => m_builder;

		public override Encoding Encoding => m_encoding;

		public override string NewLine => s_newLine;

		public ScriptWriter()
		{
			m_builder = new StringBuilder();
			m_encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
		}

		public override void Write(char value)
		{
			WriteInternal(value);
		}

		public override void Write(string value)
		{
			WriteInternal(value);
		}

		public override void WriteLine()
		{
			WriteInternal(s_newLine);
		}

		public override void WriteLine(char value)
		{
			WriteInternal(value);
			WriteLine();
		}

		public override void WriteLine(string value)
		{
			WriteInternal(value);
			WriteLine();
		}

		public void Clear()
		{
			m_changed = true;
			m_builder.Clear();
		}

		private void WriteInternal(char value)
		{
			m_changed = true;
			m_builder.Append(value);
		}

		private void WriteInternal(string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				m_changed = true;
				m_builder.Append(value);
			}
		}

		public override string ToString()
		{
			return m_builder.ToString();
		}
	}

	private class SyntaxHighlighter
	{
		public readonly struct Span
		{
			public readonly int Start;

			public readonly int End;

			public readonly HexColor Color;

			public Span(int start, int end, HexColor color)
			{
				Start = start;
				End = end;
				Color = color;
			}
		}

		private List<Span> m_spans;

		public List<Span> Spans => m_spans;

		public SyntaxHighlighter()
		{
			m_spans = new List<Span>();
		}

		public void Handle(IEnumerable<SyntaxToken> tokens, CommandTheme theme)
		{
			foreach (SyntaxToken token in tokens)
			{
				HandleToken(token, theme);
			}
		}

		private void HandleToken(SyntaxToken token, CommandTheme theme)
		{
			SyntaxKind syntaxKind = token.Kind();
			HexColor color = default(HexColor);
			if (SyntaxFacts.IsKeywordKind(syntaxKind) || (syntaxKind == SyntaxKind.IdentifierToken && token.Text == "var"))
			{
				color = theme.CodeKeywordColor;
			}
			else if (SyntaxFacts.IsPunctuation(syntaxKind))
			{
				color = ((!SyntaxFacts.IsPrefixUnaryExpressionOperatorToken(syntaxKind) && !SyntaxFacts.IsBinaryExpressionOperatorToken(syntaxKind) && !SyntaxFacts.IsAssignmentExpressionOperatorToken(syntaxKind)) ? theme.CodePunctuationColor : theme.CodeOperatorColor);
			}
			else
			{
				switch (syntaxKind)
				{
				case SyntaxKind.CharacterLiteralToken:
				case SyntaxKind.StringLiteralToken:
					color = theme.CodeStringColor;
					break;
				case SyntaxKind.NumericLiteralToken:
					color = theme.CodeNumberColor;
					break;
				}
			}
			SyntaxTriviaList.Enumerator enumerator = token.LeadingTrivia.GetEnumerator();
			while (enumerator.MoveNext())
			{
				SyntaxTrivia current = enumerator.Current;
				HandleTrivia(current, theme);
			}
			TextSpan span = token.Span;
			if (span.Length > 0 && color.RGBA != 0)
			{
				m_spans.Add(new Span(span.Start, span.End, color));
			}
			enumerator = token.TrailingTrivia.GetEnumerator();
			while (enumerator.MoveNext())
			{
				SyntaxTrivia current2 = enumerator.Current;
				HandleTrivia(current2, theme);
			}
		}

		private void HandleTrivia(SyntaxTrivia trivia, CommandTheme theme)
		{
			SyntaxKind syntaxKind = trivia.Kind();
			if (syntaxKind == SyntaxKind.SingleLineCommentTrivia || syntaxKind == SyntaxKind.MultiLineCommentTrivia)
			{
				TextSpan span = trivia.Span;
				m_spans.Add(new Span(span.Start, span.End, theme.CodeCommentColor));
			}
		}

		public void Clear()
		{
			m_spans.Clear();
		}
	}

	private class CustomLogHandler : ILogHandler
	{
		private ILogHandler m_defaultHandler;

		public CustomLogHandler(ILogHandler defaultHandler)
		{
			m_defaultHandler = defaultHandler;
		}

		public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
		{
			m_defaultHandler.LogFormat(logType, context, format, args);
		}

		public void LogException(Exception exception, UnityEngine.Object context)
		{
			m_defaultHandler.LogException(exception, context);
		}

		public void CustomLog(string text)
		{
			if (!(Singleton<INCommandManager>.Instance == null))
			{
				Singleton<INCommandManager>.Instance.Writer.Write(text);
			}
		}
	}

	[SerializeField]
	private GameObject m_mainPage;

	[SerializeField]
	private GameObject m_noticePage;

	[SerializeField]
	private ScrollRect m_scrollView;

	[SerializeField]
	private UnityEngine.UI.Button m_submitButton;

	[SerializeField]
	private InputField m_inputField;

	[SerializeField]
	private UnityEngine.UI.Button m_noticeButton;

	[SerializeField]
	private UnityEngine.UI.Button m_copyButton;

	[SerializeField]
	private SelectableText m_text;

	[SerializeField]
	private Text m_defaultText;

	[SerializeField]
	private Image m_background;

	private ScriptEngine m_engine;

	private ScriptWriter m_writer;

	private SyntaxHighlighter m_highlighter;

	private CommandSettings m_settings;

	private StringBuilder m_stringBuilder;

	private static ScriptOptions m_options;

	public ScriptEngine Engine => m_engine;

	public ScriptWriter Writer => m_writer;

	private static IEnumerable<string> GetUsings()
	{
		yield return "System";
		yield return "UnityEngine";
		yield return "UnityEngine.Video";
		yield return "Innovation";
		yield return "Innovation.BP";
	}

	private static IEnumerable<AssemblyReference> GetReferences()
	{
		return from data in GetReferencePaths().Select(LoadFileWithMaybe)
			where data.HasValue
			select new AssemblyByteReference((byte[])data);
	}

	private static IEnumerable<string> GetReferencePaths()
	{
		string[] array = new string[29]
		{
			"mscorlib", "netstandard", "System.Private.CoreLib", "System", "System.Collections", "System.Collections.Concurrent", "System.Console", "System.Diagnostics.Debug", "System.Diagnostics.Process", "System.Diagnostics.StackTrace",
			"System.Globalization", "System.IO", "System.IO.FileSystem", "System.IO.FileSystem.Primitives", "System.Reflection", "System.Reflection.Extensions", "System.Reflection.Primitives", "System.Runtime", "System.Runtime.Extensions", "System.Runtime.InteropServices",
			"System.Text.Encoding", "System.Text.Encoding.CodePages", "System.Text.Encoding.Extensions", "System.Text.RegularExpressions", "System.Threading", "System.Threading.Tasks", "System.Threading.Tasks.Parallel", "System.Threading.Thread", "System.ValueTuple"
		};
		string systemDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
		string[] array2 = array;
		foreach (string text in array2)
		{
			yield return Path.Combine(systemDirectory, text + ".dll");
		}
		string unityDirectory = Path.GetDirectoryName(typeof(UnityEngine.Object).Assembly.Location);
		yield return Path.Combine(unityDirectory, "UnityEngine.AndroidJNIModule.dll");
		yield return Path.Combine(unityDirectory, "UnityEngine.AssetBundleModule.dll");
		yield return Path.Combine(unityDirectory, "UnityEngine.AudioModule.dll");
		yield return Path.Combine(unityDirectory, "UnityEngine.CoreModule.dll");
		yield return Path.Combine(unityDirectory, "UnityEngine.PhysicsModule.dll");
		yield return Path.Combine(unityDirectory, "UnityEngine.UIModule.dll");
		yield return Path.Combine(unityDirectory, "UnityEngine.UnityWebRequestModule.dll");
		yield return Path.Combine(unityDirectory, "UnityEngine.VideoModule.dll");
		yield return typeof(Graphic).Assembly.Location;
		yield return typeof(BP).Assembly.Location;
	}

	private static byte[] LoadFile(string path)
	{
		UnityWebRequest unityWebRequest = UnityWebRequest.Get(new Uri(path));
		unityWebRequest.SendWebRequest();
		while (!unityWebRequest.isDone)
		{
		}
		byte[] result = ((unityWebRequest.result == UnityWebRequest.Result.Success) ? unityWebRequest.downloadHandler.data : null);
		unityWebRequest.Dispose();
		return result;
	}

	private static Maybe<byte[]> LoadFileWithMaybe(string path)
	{
		UnityWebRequest unityWebRequest = UnityWebRequest.Get(new Uri(path));
		unityWebRequest.SendWebRequest();
		while (!unityWebRequest.isDone)
		{
		}
		Maybe<byte[]> result = ((unityWebRequest.result == UnityWebRequest.Result.Success) ? Maybe<byte[]>.Just(unityWebRequest.downloadHandler.data) : Maybe<byte[]>.Nothing());
		unityWebRequest.Dispose();
		return result;
	}

	private static async Task<byte[]> LoadFileAsync(string path)
	{
		Uri uri = new Uri(path);
		UnityWebRequest request = UnityWebRequest.Get(uri);
		request.SendWebRequest();
		byte[] result = ((request.result == UnityWebRequest.Result.Success) ? request.downloadHandler.data : null);
		request.Dispose();
		return result;
	}

	public ScriptEngine CreateEngine()
	{
		if (m_options == null)
		{
			m_options = new ScriptOptions(SourceCodeKind.Script, LanguageVersion.Latest, OptimizationLevel.Release, GetReferences(), GetUsings());
		}
		return new ScriptEngine(m_options);
	}

	private void Awake()
	{
		SetAsPersistant();
		m_defaultText.text = "INNOVATION " + INUnity.VersionText + " COMMAND-LINE INTERFACE";
		m_text.SelectionChanged.AddListener(OnSelectionChanged);
		m_submitButton.onClick.AddListener(OnSubmitButtonClicked);
		m_noticeButton.onClick.AddListener(OpenNoticePage);
		m_copyButton.onClick.AddListener(OnCopyButtonClicked);
		m_inputField.onSubmit.AddListener(OnInputFieldSubmit);
		m_noticePage.transform.Find("Content").Find("BackButton").GetComponent<UnityEngine.UI.Button>()
			.onClick.AddListener(CloseNoticePage);
		m_writer = new ScriptWriter();
		m_highlighter = new SyntaxHighlighter();
		m_stringBuilder = new StringBuilder();
		m_engine = CreateEngine();
		Task.Run(() => CreateEngine().Run(";"));
		LoadSettings(save: true);
		ILogHandler logHandler = Debug.unityLogger.logHandler;
		Debug.unityLogger.logHandler = new CustomLogHandler(logHandler);
	}

	private void Start()
	{
		string path = INAddonManager.ResolvePath(m_settings.EntryPoint);
		if (File.Exists(path))
		{
			string code = File.ReadAllText(path);
			Execute(code, printCode: true);
		}
	}

	public void Clear()
	{
		m_text.Text = string.Empty;
		m_text.UnselectableTextRanges.Clear();
		m_writer.Clear();
	}

	private void LoadSettings()
	{
		LoadSettings(save: false);
	}

	private void LoadSettings(bool save)
	{
		string path = INUnity.SettingsPath + "/INCommandSettings.json";
		LoadDefaultSettings();
		if (File.Exists(path))
		{
			try
			{
				using StreamReader reader = new StreamReader(path);
				CommandSettings commandSettings = Json.Deserialize<CommandSettings>(reader);
				if (commandSettings != null && commandSettings.Version >= INUnity.Version)
				{
					m_settings = commandSettings;
					m_settings.UpdateVersion();
				}
			}
			catch
			{
			}
		}
		SetButtonAlpha();
		SetButtonPosition();
		SetBackgroundColor();
		SetTextSize();
		SetTextColor();
	}

	private void LoadDefaultSettings()
	{
		m_settings = Json.Deserialize<CommandSettings>(INUnity.LoadTextAsset("INCommandDefaultSettings").text);
		m_settings.UpdateVersion();
	}

	private void SaveSettings()
	{
		string settingsPath = INUnity.SettingsPath;
		Directory.CreateDirectory(settingsPath);
		using StreamWriter writer = new StreamWriter(settingsPath + "/INCommandSettings.json");
		Json.Serialize(writer, m_settings);
	}

	private void OnSelectionChanged(SelectableText.SelectionChangedEventArgs eventData)
	{
		if (eventData.StartIndex != -1 && eventData.EndIndex != -1)
		{
			RectTransform rectTransform = (RectTransform)m_scrollView.transform;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.ScreenPoint, null, out var localPoint);
			if (localPoint.y > rectTransform.rect.yMax)
			{
				m_scrollView.verticalNormalizedPosition = Math.Clamp(m_scrollView.verticalNormalizedPosition + 0.01f, 0f, 1f);
			}
			else if (localPoint.y < rectTransform.rect.yMin)
			{
				m_scrollView.verticalNormalizedPosition = Math.Clamp(m_scrollView.verticalNormalizedPosition - 0.01f, 0f, 1f);
			}
		}
	}

	private void OnInputFieldSubmit(string input)
	{
		m_inputField.text = string.Empty;
		Execute(input, printCode: true);
	}

	private void OnSubmitButtonClicked()
	{
		string text = m_inputField.text;
		m_inputField.text = string.Empty;
		Execute(text, printCode: true);
	}

	private void OnCopyButtonClicked()
	{
		m_text.CopySelectedText();
	}

	private void OpenNoticePage()
	{
		CanvasGroup component = m_noticePage.GetComponent<CanvasGroup>();
		component.alpha = 1f;
		component.blocksRaycasts = true;
	}

	private void CloseNoticePage()
	{
		CanvasGroup component = m_noticePage.GetComponent<CanvasGroup>();
		component.alpha = 0f;
		component.blocksRaycasts = false;
	}

	public void Execute(string code, bool printCode)
	{
		Execute(code, printCode, m_engine);
	}

	public void Execute(string code, bool printCode, ScriptEngine engine)
	{
		if (string.IsNullOrEmpty(code) || code == "\n")
		{
			return;
		}
		if (TryHandleAddonImportCommand(code))
		{
			return;
		}
		if (TryHandleAddonImportCommand(code))
		{
			return;
		}
		if (TryHandleViewportCommand(code))
		{
			return;
		}
		if (TryHandleSaveSlotCommand(code))
		{
			return;
		}
		code = code.Replace("\t", "    ");
		ParseResult parseResult = engine.Parse(code);
		if (parseResult.Exception != null)
		{
			if (printCode)
			{
				m_writer.WriteLine(HandleCode(code, color: false, ImmutableArray<SyntaxToken>.Empty));
			}
			WriteError(parseResult.Exception);
			return;
		}
		if (printCode)
		{
			IEnumerable<SyntaxToken> items = parseResult.SyntaxTree.GetRoot().DescendantTokens();
			m_writer.WriteLine(HandleCode(code, color: true, ImmutableArray.CreateRange(items)));
		}
		ExecutionResult executionResult = m_engine.Run(parseResult.SyntaxTree);
		if (executionResult.Exception != null)
		{
			WriteError(executionResult.Exception);
		}
		if (executionResult.ReturnValue != null)
		{
			string value;
			try
			{
				value = executionResult.ReturnValue.ToString();
			}
			catch
			{
				value = executionResult.ReturnValue.GetType().ToString();
			}
			m_writer.WriteLine(value);
		}
		void WriteError(Exception exception)
		{
			string text = exception.GetType().ToString();
			string message = exception.Message;
			if (!string.IsNullOrEmpty(message))
			{
				text = text + ": " + message;
			}
			string text2 = "<color=" + m_settings.GetCurrentTheme().ExceptionColor.ToString() + ">";
			string text3 = "</color>";
			int length = m_writer.Builder.Length;
			int num = m_writer.Builder.Length + text2.Length + text.Length;
			m_text.AddUnselectableTextRange(length, length + text2.Length - 1);
			m_text.AddUnselectableTextRange(num, num + text3.Length - 1);
			m_writer.WriteLine(text2 + text + text3);
		}
	}

	private bool TryHandleAddonImportCommand(string code)
	{
		string trimmed = code.Trim();
		if (trimmed.StartsWith("/addon f imp ", StringComparison.Ordinal))
		{
			string folderPath = trimmed.Substring(13).Trim();
			return HandleAddonFolderImport(folderPath);
		}
		if (trimmed.StartsWith("/导入插件文件夹 ", StringComparison.Ordinal))
		{
			string folderPath = trimmed.Substring(8).Trim();
			return HandleAddonFolderImport(folderPath);
		}
		if (trimmed.StartsWith("/addons imp ", StringComparison.Ordinal))
		{
			string args = trimmed.Substring(12).Trim();
			return HandleAddonMultiImport(args);
		}
		if (trimmed.StartsWith("/导入多个插件 ", StringComparison.Ordinal))
		{
			string args = trimmed.Substring(7).Trim();
			return HandleAddonMultiImport(args);
		}
		if (trimmed.StartsWith("/addon imp ", StringComparison.Ordinal))
		{
			string filePath = trimmed.Substring(11).Trim();
			return HandleAddonSingleImport(filePath);
		}
		if (trimmed.StartsWith("/导入插件 ", StringComparison.Ordinal))
		{
			string filePath = trimmed.Substring(5).Trim();
			return HandleAddonSingleImport(filePath);
		}
		return false;
	}

	private bool HandleAddonSingleImport(string filePath)
	{
		if (string.IsNullOrEmpty(filePath))
		{
			m_writer.WriteLine("格式: /addon imp 文件路径 或 /导入插件 文件路径");
			return true;
		}
		if (!filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
		{
			m_writer.WriteLine("仅支持 .zip 格式的插件包。");
			return true;
		}
		if (!File.Exists(filePath))
		{
			m_writer.WriteLine("文件不存在: " + filePath);
			return true;
		}
		try
		{
			INAddonManager.Instance.PackageManager.ImportExternalPackage(filePath).Forget();
			m_writer.WriteLine("✅ 导入成功: " + Path.GetFileName(filePath));
		}
		catch (Exception ex)
		{
			m_writer.WriteLine("❌ 导入失败: " + ex.Message);
		}
		return true;
	}

	private bool HandleAddonMultiImport(string args)
	{
		if (string.IsNullOrEmpty(args))
		{
			m_writer.WriteLine("格式: /addons imp 路径1|路径2|... 或 /导入多个插件 路径1|路径2|...");
			return true;
		}
		string[] paths = args.Split('|');
		int success = 0;
		int fail = 0;
		foreach (string raw in paths)
		{
			string filePath = raw.Trim();
			if (string.IsNullOrEmpty(filePath)) continue;
			if (!filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			{
				m_writer.WriteLine("跳过非 .zip 文件: " + filePath);
				fail++;
				continue;
			}
			if (!File.Exists(filePath))
			{
				m_writer.WriteLine("文件不存在: " + filePath);
				fail++;
				continue;
			}
			try
			{
				INAddonManager.Instance.PackageManager.ImportExternalPackage(filePath).Forget();
				m_writer.WriteLine("✅ 导入成功: " + Path.GetFileName(filePath));
				success++;
			}
			catch (Exception ex)
			{
				m_writer.WriteLine("❌ 导入失败 [" + Path.GetFileName(filePath) + "]: " + ex.Message);
				fail++;
			}
		}
		m_writer.WriteLine("导入完成: 成功 " + success + ", 失败 " + fail);
		return true;
	}

	private bool HandleAddonFolderImport(string folderPath)
	{
		if (string.IsNullOrEmpty(folderPath))
		{
			m_writer.WriteLine("格式: /addon f imp 文件夹路径 或 /导入插件文件夹 文件夹路径");
			return true;
		}
		if (!Directory.Exists(folderPath))
		{
			m_writer.WriteLine("文件夹不存在: " + folderPath);
			return true;
		}
		string[] zipFiles = Directory.GetFiles(folderPath, "*.zip", SearchOption.TopDirectoryOnly);
		if (zipFiles.Length == 0)
		{
			m_writer.WriteLine("文件夹内没有找到 .zip 插件包: " + folderPath);
			return true;
		}
		int success = 0;
		int fail = 0;
		foreach (string zipFile in zipFiles)
		{
			try
			{
				INAddonManager.Instance.PackageManager.ImportExternalPackage(zipFile).Forget();
				m_writer.WriteLine("✅ 导入成功: " + Path.GetFileName(zipFile));
				success++;
			}
			catch (Exception ex)
			{
				m_writer.WriteLine("❌ 导入失败 [" + Path.GetFileName(zipFile) + "]: " + ex.Message);
				fail++;
			}
		}
		m_writer.WriteLine("文件夹导入完成: 共 " + zipFiles.Length + " 个, 成功 " + success + ", 失败 " + fail);
		return true;
	}

	private bool TryHandleViewportCommand(string code)
	{
		string trimmed = code.Trim();
		float x = 0f, y = 0f, z = 0f;
		bool isPos = false, isSize = false;
		if (trimmed.StartsWith("/vp ", StringComparison.Ordinal) || trimmed.StartsWith("/视野位置 ", StringComparison.Ordinal))
		{
			isPos = true;
			int argStart = trimmed.StartsWith("/vp ", StringComparison.Ordinal) ? 4 : 5;
			string arg = trimmed.Substring(argStart).Trim();
			string[] parts = arg.Split(',');
			if (parts.Length >= 2 && float.TryParse(parts[0].Trim(), out x) && float.TryParse(parts[1].Trim(), out y))
			{
			}
			else
			{
				m_writer.WriteLine("格式: /vp x,y 或 /视野位置 x,y");
				return true;
			}
		}
		else if (trimmed.StartsWith("/vs ", StringComparison.Ordinal) || trimmed.StartsWith("/视野大小 ", StringComparison.Ordinal))
		{
			isSize = true;
			int argStart = trimmed.StartsWith("/vs ", StringComparison.Ordinal) ? 4 : 5;
			string arg = trimmed.Substring(argStart).Trim();
			if (!float.TryParse(arg, out z))
			{
				m_writer.WriteLine("格式: /vs z 或 /视野大小 z");
				return true;
			}
		}
		if (!isPos && !isSize)
		{
			return false;
		}
		Camera cam = Camera.main;
		if (cam == null)
		{
			m_writer.WriteLine("未找到主相机。");
			return true;
		}
		if (isPos)
		{
			Vector3 pos = cam.transform.position;
			pos.x = x;
			pos.y = y;
			cam.transform.position = pos;
			m_writer.WriteLine("视野位置已设置: (" + x + ", " + y + ")");
		}
		if (isSize)
		{
			cam.orthographicSize = z;
			m_writer.WriteLine("视野大小已设置: " + z);
		}
		return true;
	}

	private bool TryHandleSaveSlotCommand(string code)
	{
		string trimmed = code.Trim();
		int slotIndex = -1;
		if (trimmed.StartsWith("/sa ", StringComparison.Ordinal))
		{
			string arg = trimmed.Substring(4).Trim();
			if (int.TryParse(arg, out slotIndex) && slotIndex >= 0)
			{
			}
			else
			{
				slotIndex = -1;
			}
		}
		else if (trimmed.StartsWith("/切换存档 ", StringComparison.Ordinal))
		{
			string arg2 = trimmed.Substring(5).Trim();
			if (int.TryParse(arg2, out slotIndex) && slotIndex >= 0)
			{
			}
			else
			{
				slotIndex = -1;
			}
		}
		if (slotIndex < 0)
		{
			return false;
		}
		LevelManager lm = WPFMonoBehaviour.levelManager;
		if (lm == null || !lm.m_sandbox || lm.CurrentGameMode == null)
		{
			m_writer.WriteLine("只能在沙盒/建造界面切换存档。");
			return true;
		}
		lm.CurrentGameMode.LoadContraptionSlot(slotIndex - 1);
		GameProgress.SetInt(SchematicButton.LastLoadedSlotKey, slotIndex - 1);
		m_writer.WriteLine("已切换到存档 " + (slotIndex - 1));
		return true;
	}

	private string HandleCode(string code, bool color, ImmutableArray<SyntaxToken> tokens)
	{
		List<(int, string)> list = new List<(int, string)>();
		if (color)
		{
			CommandTheme currentTheme = m_settings.GetCurrentTheme();
			m_highlighter.Handle(tokens, currentTheme);
			List<SyntaxHighlighter.Span> spans = m_highlighter.Spans;
			for (int num = spans.Count - 1; num >= 0; num--)
			{
				SyntaxHighlighter.Span span = spans[num];
				list.Add((span.Start * 3 + 2, "<color=" + span.Color.ToString() + ">"));
				list.Add((span.End * 3 + 1, "</color>"));
			}
			m_highlighter.Clear();
		}
		int i = 0;
		int num2 = 0;
		for (; i < code.Length; i++)
		{
			if (code[i] == '\n' || i == code.Length - 1)
			{
				string item = ((num2 == 0) ? "<color=#FFFFFFB3><b>></b></color> " : "<color=#FFFFFFB3><b>·</b></color> ");
				list.Add((num2 * 3, item));
				num2 = i + 1;
			}
		}
		int num3 = 0;
		int length = m_writer.Builder.Length;
		StringBuilder stringBuilder = m_stringBuilder;
		stringBuilder.Append(code);
		list.Sort(((int, string) x, (int, string) y) => x.Item1.CompareTo(y.Item1));
		foreach (var item2 in list)
		{
			int num4 = item2.Item1 / 3 + num3;
			int length2 = item2.Item2.Length;
			num3 += length2;
			stringBuilder.Insert(num4, item2.Item2);
			m_text.AddUnselectableTextRange(length + num4, length + num4 + length2 - 1);
		}
		string result = stringBuilder.ToString();
		stringBuilder.Clear();
		return result;
	}

	private void SetButtonAlpha()
	{
	}

	private void SetButtonPosition()
	{
	}

	private void SetBackgroundColor()
	{
		Color color = (Color)m_settings.GetCurrentTheme().BackgroundColor;
		m_background.color = color;
	}

	private void SetTextSize()
	{
		int textSize = m_settings.GetCurrentTheme().TextSize;
		m_text.TextComponent.fontSize = textSize;
	}

	private void SetTextColor()
	{
		Color color = (Color)m_settings.GetCurrentTheme().TextColor;
		m_text.TextComponent.color = color;
	}

	private void Update()
	{
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.Return))
		{
			string text = m_inputField.text;
			m_inputField.text = string.Empty;
			Execute(text, printCode: true);
		}
		if (m_writer.IsChanged)
		{
			string text2 = m_writer.ToString();
			m_text.Text = text2;
		}
	}
}
