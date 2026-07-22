using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DevTools
{
    /// <summary>
    /// Lightweight, collapsible in-game console — mirrors the Unity Editor console
    /// (Log/Warning/Error tabs with counts, collapsed duplicates, tap-to-expand stack traces).
    /// Zero setup: bootstraps itself into every scene before the first scene loads.
    /// Built entirely from code (legacy UI + built-in font) so it has no asset/prefab dependencies.
    /// </summary>
    public class InGameLogger : MonoBehaviour
    {
        private enum ConsoleTab { All, Warnings, Errors }

        private class LogEntry
        {
            public string message;
            public string stackTrace;
            public LogType type;
            public int count = 1;
            public Text label;
            public Text stackLabel;
            public GameObject row;
        }

        private const int MaxEntries = 300;

        public static InGameLogger Instance { get; private set; }

        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private int _logCount, _warningCount, _errorCount;
        private ConsoleTab _activeTab = ConsoleTab.All;
        private bool _listDirty;
        private bool _panelOpen;

        private GameObject _panel;
        private GameObject _toggleButton;
        private Text _toggleLabel;
        private RectTransform _content;
        private ScrollRect _scrollRect;
        private readonly Dictionary<ConsoleTab, Button> _tabButtons = new Dictionary<ConsoleTab, Button>();
        private readonly Dictionary<ConsoleTab, Text> _tabLabels = new Dictionary<ConsoleTab, Text>();

        private static Font _builtinFont;
        private static Font BuiltinFont => _builtinFont != null
            ? _builtinFont
            : (_builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[InGameLogger]");
            DontDestroyOnLoad(go);
            go.AddComponent<InGameLogger>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildUI();
            SetPanelOpen(false);
        }

        private void OnEnable() => Application.logMessageReceived += HandleLog;
        private void OnDisable() => Application.logMessageReceived -= HandleLog;

        private void LateUpdate()
        {
            if (_listDirty)
            {
                _listDirty = false;
                RebuildVisibleList();
            }
        }

        // ── Log capture ─────────────────────────────────────────────────────────

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Warning: _warningCount++; break;
                case LogType.Log: _logCount++; break;
                default: _errorCount++; break; // Error, Exception, Assert
            }
            UpdateTabLabels();

            var last = _entries.Count > 0 ? _entries[_entries.Count - 1] : null;
            if (last != null && last.type == type && last.message == message)
            {
                last.count++;
                if (last.label != null) last.label.text = FormatMessage(last);
            }
            else
            {
                _entries.Add(new LogEntry { message = message, stackTrace = stackTrace, type = type });
                if (_entries.Count > MaxEntries) _entries.RemoveAt(0);
            }

            if (_panelOpen) _listDirty = true;
        }

        private static string FormatMessage(LogEntry e) =>
            e.count > 1 ? $"{e.message}   x{e.count}" : e.message;

        private static Color ColorFor(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return new Color(1f, 0.85f, 0.3f);
                case LogType.Log: return Color.white;
                default: return new Color(1f, 0.4f, 0.4f); // Error, Exception, Assert
            }
        }

        private static bool MatchesTab(LogType type, ConsoleTab tab)
        {
            switch (tab)
            {
                case ConsoleTab.Warnings: return type == LogType.Warning;
                case ConsoleTab.Errors: return type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
                default: return true;
            }
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public void Clear()
        {
            _entries.Clear();
            _logCount = _warningCount = _errorCount = 0;
            UpdateTabLabels();
            _listDirty = true;
        }

        public void TogglePanel() => SetPanelOpen(!_panelOpen);

        private void SetPanelOpen(bool open)
        {
            _panelOpen = open;
            _panel.SetActive(open);
            _toggleButton.transform.Find("Badge").gameObject.SetActive(!open);
            if (open) _listDirty = true;
        }

        private void SetTab(ConsoleTab tab)
        {
            _activeTab = tab;
            foreach (var kvp in _tabButtons)
                kvp.Value.image.color = kvp.Key == tab ? new Color(0.3f, 0.3f, 0.35f) : new Color(0.15f, 0.15f, 0.18f);
            _listDirty = true;
        }

        private void UpdateTabLabels()
        {
            if (_tabLabels.Count == 0) return;
            _tabLabels[ConsoleTab.All].text = $"All ({_logCount + _warningCount + _errorCount})";
            _tabLabels[ConsoleTab.Warnings].text = $"Warnings ({_warningCount})";
            _tabLabels[ConsoleTab.Errors].text = $"Errors ({_errorCount})";
            if (_toggleLabel != null) _toggleLabel.text = _errorCount > 0 ? $"!{_errorCount}" : (_warningCount > 0 ? $"?{_warningCount}" : "Log");
        }

        private void RebuildVisibleList()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            foreach (var entry in _entries)
            {
                if (!MatchesTab(entry.type, _activeTab)) continue;
                CreateRow(entry);
            }
        }

        private void CreateRow(LogEntry entry)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(Image));
            row.transform.SetParent(_content, false);
            row.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f); // invisible, but clickable
            var vlg = row.GetComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(6, 6, 4, 4);
            row.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.flexibleWidth = 1;

            entry.row = row;

            var msgGO = new GameObject("Message", typeof(RectTransform));
            msgGO.transform.SetParent(row.transform, false);
            var msgText = msgGO.AddComponent<Text>();
            msgText.font = BuiltinFont;
            msgText.fontSize = 24;
            msgText.color = ColorFor(entry.type);
            msgText.text = FormatMessage(entry);
            msgText.horizontalOverflow = HorizontalWrapMode.Wrap;
            msgText.verticalOverflow = VerticalWrapMode.Overflow;
            entry.label = msgText;

            var stackGO = new GameObject("StackTrace", typeof(RectTransform));
            stackGO.transform.SetParent(row.transform, false);
            var stackText = stackGO.AddComponent<Text>();
            stackText.font = BuiltinFont;
            stackText.fontSize = 18;
            stackText.color = new Color(0.75f, 0.75f, 0.75f);
            stackText.text = string.IsNullOrEmpty(entry.stackTrace) ? "(no stack trace)" : entry.stackTrace;
            stackText.horizontalOverflow = HorizontalWrapMode.Wrap;
            stackText.verticalOverflow = VerticalWrapMode.Overflow;
            entry.stackLabel = stackText;
            stackGO.SetActive(false);

            var button = row.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => stackGO.SetActive(!stackGO.activeSelf));

            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(row.transform, false);
            divider.GetComponent<Image>().color = new Color(1, 1, 1, 0.08f);
            divider.AddComponent<LayoutElement>().preferredHeight = 1;
        }

        // ── UI construction ─────────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            BuildToggleButton(canvasGO.transform);
            BuildPanel(canvasGO.transform);
            UpdateTabLabels();
        }

        private void BuildToggleButton(Transform parent)
        {
            _toggleButton = new GameObject("LoggerToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            _toggleButton.transform.SetParent(parent, false);
            var rt = (RectTransform)_toggleButton.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(140, 60);
            rt.anchoredPosition = new Vector2(-16, 16);
            _toggleButton.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
            _toggleButton.GetComponent<Button>().onClick.AddListener(TogglePanel);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(_toggleButton.transform, false);
            var labelRT = (RectTransform)labelGO.transform;
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
            _toggleLabel = labelGO.AddComponent<Text>();
            _toggleLabel.font = BuiltinFont;
            _toggleLabel.fontSize = 26;
            _toggleLabel.alignment = TextAnchor.MiddleCenter;
            _toggleLabel.color = Color.white;
            _toggleLabel.text = "Log";

            var badgeGO = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeGO.transform.SetParent(_toggleButton.transform, false);
            var badgeRT = (RectTransform)badgeGO.transform;
            badgeRT.anchorMin = badgeRT.anchorMax = new Vector2(1f, 1f);
            badgeRT.sizeDelta = new Vector2(14, 14);
            badgeRT.anchoredPosition = new Vector2(-4, -4);
            badgeGO.GetComponent<Image>().color = new Color(0.3f, 0.9f, 0.3f);
        }

        private void BuildPanel(Transform parent)
        {
            _panel = new GameObject("LoggerPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(parent, false);
            var panelRT = (RectTransform)_panel.transform;
            panelRT.anchorMin = new Vector2(0.02f, 0.05f);
            panelRT.anchorMax = new Vector2(0.98f, 0.7f);
            panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.95f);

            // Header bar: 3 tabs + Clear + Close
            var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            header.transform.SetParent(_panel.transform, false);
            var headerRT = (RectTransform)header.transform;
            headerRT.anchorMin = new Vector2(0f, 1f);
            headerRT.anchorMax = new Vector2(1f, 1f);
            headerRT.pivot = new Vector2(0.5f, 1f);
            headerRT.sizeDelta = new Vector2(0, 70);
            var hlg = header.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.spacing = 6;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;

            CreateTabButton(header.transform, ConsoleTab.All, "All (0)");
            CreateTabButton(header.transform, ConsoleTab.Warnings, "Warnings (0)");
            CreateTabButton(header.transform, ConsoleTab.Errors, "Errors (0)");

            CreateHeaderButton(header.transform, "Clear", Clear, 100);
            CreateHeaderButton(header.transform, "X", () => SetPanelOpen(false), 60);

            // Scroll view body
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
            scrollGO.transform.SetParent(_panel.transform, false);
            var scrollRT = (RectTransform)scrollGO.transform;
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(4, 4);
            scrollRT.offsetMax = new Vector2(-4, -74);
            scrollGO.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            scrollGO.GetComponent<Mask>().showMaskGraphic = false;
            _scrollRect = scrollGO.GetComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(scrollGO.transform, false);
            _content = (RectTransform)contentGO.transform;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            var clg = contentGO.GetComponent<VerticalLayoutGroup>();
            clg.childControlHeight = true;
            clg.childControlWidth = true;
            clg.childForceExpandHeight = false;
            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.content = _content;

            SetTab(ConsoleTab.All);
        }

        private void CreateTabButton(Transform parent, ConsoleTab tab, string initialLabel)
        {
            var go = new GameObject($"Tab_{tab}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().flexibleWidth = 1;
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SetTab(tab));
            _tabButtons[tab] = button;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = (RectTransform)labelGO.transform;
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
            var text = labelGO.AddComponent<Text>();
            text.font = BuiltinFont;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = initialLabel;
            _tabLabels[tab] = text;
        }

        private void CreateHeaderButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, float width)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredWidth = width;
            go.GetComponent<Image>().color = new Color(0.25f, 0.12f, 0.12f);
            go.GetComponent<Button>().onClick.AddListener(onClick);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = (RectTransform)labelGO.transform;
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
            var text = labelGO.AddComponent<Text>();
            text.font = BuiltinFont;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
        }
    }
}
