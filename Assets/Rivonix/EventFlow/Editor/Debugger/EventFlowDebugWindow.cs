#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Rivonix.EventFlow.Editor
{
    public class EventFlowDebugWindow : EditorWindow
    {
        private Vector2 _scroll;
        private Vector2 _historyScroll;
        private string  _search            = "";
        private bool    _showListeners     = true;
        private bool    _showHistory       = true;
        private bool    _showPipelines     = true;
        private bool    _showPermissions   = true;
        private bool    _showPerformance   = true;
        private bool    _showScheduler     = true;
        private int     _selectedTypeIndex = -1;
        private float   _refreshRate       = 0.5f;
        private double  _lastRefresh;

        private List<Type>                     _eventTypes      = new List<Type>();
        private Dictionary<Type, int>          _listenerCounts  = new Dictionary<Type, int>();
        private Dictionary<Type, List<string>> _listenerDetails = new Dictionary<Type, List<string>>();
        private Dictionary<Type, string>       _permStrings     = new Dictionary<Type, string>();

        private static FieldInfo _listenersField;
        private static FieldInfo _permissionsField;

        private static FieldInfo ListenersField
        {
            get
            {
                if (_listenersField == null)
                    _listenersField = typeof(EventBus).GetField("listeners",
                        BindingFlags.Static | BindingFlags.NonPublic);
                return _listenersField;
            }
        }

        private static FieldInfo PermissionsField
        {
            get
            {
                if (_permissionsField == null)
                    _permissionsField = typeof(GameStateManager).GetField("eventPermissions",
                        BindingFlags.Static | BindingFlags.NonPublic);
                return _permissionsField;
            }
        }

        [MenuItem("Tools/Rivonix/EventFlow/Debug Window")]
        public static void ShowWindow() => GetWindow<EventFlowDebugWindow>("EventFlow Debug");

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            titleContent = new GUIContent("EventFlow Debug",
                EditorGUIUtility.IconContent("d_UnityEditor.SceneView").image);
            Refresh();
        }

        private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

        private void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastRefresh > _refreshRate)
            {
                Refresh();
                _lastRefresh = EditorApplication.timeSinceStartup;
            }
        }

        private void Refresh()
        {
            RefreshListeners();
            RefreshPermissions();
            Repaint();
        }

        private void RefreshListeners()
        {
            _eventTypes.Clear();
            _listenerCounts.Clear();
            _listenerDetails.Clear();

            var dict = ListenersField?.GetValue(null) as System.Collections.IDictionary;
            if (dict == null) return;

            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                var eventType = entry.Key as Type;
                if (eventType == null) continue;

                var list  = entry.Value as System.Collections.IList;
                int count = list != null ? list.Count : 0;

                _eventTypes.Add(eventType);
                _listenerCounts[eventType] = count;

                var details = new List<string>();
                if (list != null && list.Count > 0)
                {
                    var handlerField = list[0].GetType().GetField("handler");
                    var scopeField   = list[0].GetType().GetField("scope");

                    foreach (var item in list)
                    {
                        string scope  = scopeField?.GetValue(item) as string ?? "global";
                        var    del    = handlerField?.GetValue(item) as Delegate;
                        string target = del != null
                            ? del.Method.DeclaringType.Name + "." + del.Method.Name
                            : "unknown";
                        details.Add("[" + scope + "]  " + target);
                    }
                }
                _listenerDetails[eventType] = details;
            }
        }

        private void RefreshPermissions()
        {
            _permStrings.Clear();

            var dict = PermissionsField?.GetValue(null) as System.Collections.IDictionary;
            if (dict == null) return;

            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                var eventType = entry.Key as Type;
                if (eventType == null) continue;

                var set   = entry.Value as System.Collections.IEnumerable;
                var parts = new List<string>();
                if (set != null)
                    foreach (var s in set) parts.Add(s.ToString());

                _permStrings[eventType] = string.Join(", ", parts);
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawHeader();
            DrawCurrentState();
            DrawPerformance();
            DrawEventListeners();
            DrawPipelines();
            DrawEventHistory();
            DrawScheduler();
            DrawStatePermissions();
            DrawControls();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60))) Refresh();
            GUILayout.Label("Rate:", GUILayout.Width(35));
            _refreshRate = EditorGUILayout.Slider(_refreshRate, 0.1f, 2f, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.Label("Search:", GUILayout.Width(45));
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(150));
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22))) _search = "";
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Rivonix EventFlow Debugger", EditorStyles.boldLabel);
            GUILayout.Label("Real-time event monitoring & diagnostics", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentState()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Game State", EditorStyles.boldLabel);
            var state = GameStateManager.CurrentState;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current:", GUILayout.Width(60));
            GUI.color = GetStateColor(state);
            EditorGUILayout.EnumPopup(state, GUILayout.Width(160));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Stack (top first):", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            foreach (var s in GameStateManager.StateStack) EditorGUILayout.LabelField("► " + s);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawPerformance()
        {
            EditorGUILayout.BeginVertical("box");
            _showPerformance = EditorGUILayout.Foldout(_showPerformance, "Performance", true);
            if (_showPerformance)
            {
                int histCount   = EventFlowDiagnostics.GetEventHistory().Count;
                int frameEvents = EventFlowDiagnostics.EventsThisFrame;
                int maxFrame    = EventFlowDiagnostics.MaxEventsPerFrame;
                int overflow    = EventFlowDiagnostics.OverflowQueueCount;

                var bar = EditorGUILayout.GetControlRect(false, 18);
                EditorGUI.ProgressBar(bar, Mathf.Clamp01(frameEvents / (float)maxFrame),
                    "This frame: " + frameEvents + " / " + maxFrame);

                EditorGUILayout.LabelField("Event types registered : " + _eventTypes.Count);
                EditorGUILayout.LabelField("Total listeners        : " + _listenerCounts.Values.Sum());
                EditorGUILayout.LabelField("History entries        : " + histCount);

                if (overflow > 0)
                {
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField("Overflow queue         : " + overflow + " pending");
                    GUI.color = Color.white;
                }

                EditorGUILayout.LabelField("Scheduled events       : " + EventScheduler.Instance.ScheduledCount);
                EditorGUILayout.LabelField("Repeating events       : " + EventScheduler.Instance.RepeatingCount);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEventListeners()
        {
            EditorGUILayout.BeginVertical("box");
            _showListeners = EditorGUILayout.Foldout(_showListeners, "Event Listeners", true);
            if (_showListeners)
            {
                if (_eventTypes.Count == 0)
                {
                    EditorGUILayout.HelpBox("No events registered yet.", MessageType.Info);
                }
                else
                {
                    var filtered = _eventTypes
                        .Where(t => string.IsNullOrEmpty(_search) ||
                                    t.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                        .OrderBy(t => t.Name)
                        .ToList();

                    for (int idx = 0; idx < filtered.Count; idx++)
                    {
                        Type t       = filtered[idx];
                        int  count   = _listenerCounts.ContainsKey(t) ? _listenerCounts[t] : 0;
                        bool expanded = _selectedTypeIndex == idx;

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button(expanded ? "v" : ">", EditorStyles.miniButton, GUILayout.Width(22)))
                            _selectedTypeIndex = expanded ? -1 : idx;
                        EditorGUILayout.LabelField(t.Name, GUILayout.Width(200));
                        var barRect = EditorGUILayout.GetControlRect(false, 18);
                        EditorGUI.ProgressBar(barRect, Mathf.Clamp01(count / 20f),
                            count + (count != 1 ? " listeners" : " listener"));
                        if (GUILayout.Button("Find", EditorStyles.miniButton, GUILayout.Width(44)))
                            FindEventInProject(t.Name);
                        EditorGUILayout.EndHorizontal();

                        if (expanded)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.LabelField("Full type: " + t.FullName, EditorStyles.miniLabel);
                            if (_listenerDetails.ContainsKey(t) && _listenerDetails[t].Count > 0)
                                foreach (var d in _listenerDetails[t])
                                    EditorGUILayout.LabelField("  " + d, EditorStyles.miniLabel);
                            else
                                EditorGUILayout.LabelField("  No detail available.", EditorStyles.miniLabel);
                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEventHistory()
        {
            EditorGUILayout.BeginVertical("box");
            _showHistory = EditorGUILayout.Foldout(_showHistory, "Recent Events", true);
            if (_showHistory)
            {
                _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.Height(180));
                var history = EventFlowDiagnostics.GetEventHistory();
                if (history.Count == 0)
                {
                    EditorGUILayout.HelpBox("No events recorded yet.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Frame", EditorStyles.boldLabel, GUILayout.Width(50));
                    EditorGUILayout.LabelField("Time",  EditorStyles.boldLabel, GUILayout.Width(56));
                    EditorGUILayout.LabelField("Event", EditorStyles.boldLabel, GUILayout.Width(160));
                    EditorGUILayout.LabelField("Data",  EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    for (int i = history.Count - 1; i >= 0; i--)
                    {
                        var log = history[i];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(log.frameCount.ToString(), GUILayout.Width(50));
                        EditorGUILayout.LabelField(log.time.ToString("F2") + "s", GUILayout.Width(56));
                        GUI.color = log.wasBlocked ? Color.yellow : Color.green;
                        EditorGUILayout.LabelField(
                            log.wasBlocked ? log.eventType + " [BLOCKED]" : log.eventType,
                            GUILayout.Width(160));
                        GUI.color = Color.white;
                        string data = log.eventData != null && log.eventData.Length > 60
                            ? log.eventData.Substring(0, 57) + "..."
                            : log.eventData ?? "";
                        EditorGUILayout.LabelField(data, EditorStyles.wordWrappedLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPipelines()
        {
            EditorGUILayout.BeginVertical("box");
            _showPipelines = EditorGUILayout.Foldout(_showPipelines, "Pipelines", true);
            if (_showPipelines)
            {
                var pipelines = EventFlowController.GetPipelines();
                if (pipelines.Count == 0)
                {
                    EditorGUILayout.HelpBox("No pipeline steps registered. Use EventFlow.AddStep<T>().", MessageType.Info);
                }
                else
                {
                    foreach (var kvp in pipelines.OrderBy(x => x.Key.Name))
                    {
                        if (!string.IsNullOrEmpty(_search) &&
                            kvp.Key.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                        var steps         = EventFlowController.GetPipelineSteps(kvp.Key);
                        var execInfo      = EventFlowController.GetPipelineExecutionInfo(kvp.Key);
                        int stepCount     = steps != null ? steps.Count : 0;
                        int listenerCount = EventBus.GetListenerCount(kvp.Key);

                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField(
                            kvp.Key.Name + "  (" + stepCount + " step" + (stepCount != 1 ? "s" : "") +
                            ", " + listenerCount + " listener" + (listenerCount != 1 ? "s" : "") + ")",
                            EditorStyles.boldLabel);

                        EditorGUILayout.LabelField("Executions: " + execInfo.ExecutionCount, EditorStyles.miniLabel);
                        if (!string.IsNullOrEmpty(execInfo.LastExecutedStepName))
                        {
                            string status = execInfo.LastExecutionFailed ? "FAILED"
                                : execInfo.LastDispatchSucceeded ? "dispatched" : "stopped";
                            EditorGUILayout.LabelField(
                                "Last: [" + execInfo.LastExecutedStepName + "] -> " + status,
                                EditorStyles.miniLabel);
                        }

                        if (steps != null)
                        {
                            foreach (var step in steps)
                            {
                                bool isLast = step.Name == execInfo.LastExecutedStepName;
                                if (isLast)
                                    GUI.color = execInfo.LastExecutionFailed
                                        ? new Color(1f, 0.5f, 0.5f)
                                        : new Color(0.6f, 1f, 0.6f);
                                EditorGUILayout.LabelField(
                                    "  [" + step.Order + "] " + step.Name +
                                    "  |  priority " + step.Priority +
                                    "  |  " + (step.Enabled ? "enabled" : "DISABLED"));
                                if (isLast) GUI.color = Color.white;
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawScheduler()
        {
            EditorGUILayout.BeginVertical("box");
            _showScheduler = EditorGUILayout.Foldout(_showScheduler, "Scheduler", true);
            if (_showScheduler)
            {
                int scheduled = EventScheduler.Instance.ScheduledCount;
                int repeating = EventScheduler.Instance.RepeatingCount;
                EditorGUILayout.LabelField("One-shot pending : " + scheduled);
                EditorGUILayout.LabelField("Repeating active : " + repeating);
                if (scheduled > 0 || repeating > 0)
                {
                    if (GUILayout.Button("Cancel All Scheduled Events"))
                    {
                        if (EditorUtility.DisplayDialog("Cancel All",
                            "Cancel all scheduled and repeating events?", "Yes", "No"))
                            EventScheduler.Instance.CancelAll();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawStatePermissions()
        {
            EditorGUILayout.BeginVertical("box");
            _showPermissions = EditorGUILayout.Foldout(_showPermissions, "State Permissions", true);
            if (_showPermissions)
            {
                if (_permStrings.Count == 0)
                {
                    EditorGUILayout.HelpBox("No event permissions configured. All events fire in all states.", MessageType.Info);
                }
                else
                {
                    foreach (var kvp in _permStrings.OrderBy(x => x.Key.Name))
                    {
                        if (!string.IsNullOrEmpty(_search) &&
                            kvp.Key.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kvp.Key.Name, GUILayout.Width(180));
                        EditorGUILayout.LabelField(kvp.Value);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(1f, 0.9f, 0.3f);
            if (GUILayout.Button("Clear History", GUILayout.Height(28)))
                EventFlowDiagnostics.ClearHistory();

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Clear All Listeners", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Warning",
                    "Remove ALL event listeners? This cannot be undone.", "Yes", "Cancel"))
                {
                    EventBus.ClearAll();
                    Refresh();
                }
            }

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Reset Game State", GUILayout.Height(28)))
                GameStateManager.Reset();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Register:    EventFlow.Register<T>(handler, scope)\n" +
                "Trigger:     EventFlow.Trigger(new MyEvent { ... })\n" +
                "State:       GameStateManager.PushState(GameState.Paused)\n" +
                "Scene clear: EventFlow.ClearListeners(\"SceneName\")",
                MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private static Color GetStateColor(GameStateManager.GameState state)
        {
            switch (state)
            {
                case GameStateManager.GameState.Playing:        return Color.green;
                case GameStateManager.GameState.Paused:         return Color.yellow;
                case GameStateManager.GameState.GameOver:       return Color.red;
                case GameStateManager.GameState.Loading:        return Color.cyan;
                case GameStateManager.GameState.MainMenu:       return new Color(0.5f, 0.7f, 1f);
                case GameStateManager.GameState.Bootstrapping:  return Color.gray;
                default:                                        return Color.white;
            }
        }

        private static void FindEventInProject(string typeName)
        {
            string[] guids = AssetDatabase.FindAssets("t:script " + typeName);
            foreach (string guid in guids)
            {
                string path    = AssetDatabase.GUIDToAssetPath(guid);
                string content = System.IO.File.ReadAllText(path);
                if (content.Contains("struct " + typeName) || content.Contains("class " + typeName))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
                    EditorUtility.DisplayDialog("Found", "Event defined in:\n" + path, "OK");
                    return;
                }
            }
            if (guids.Length > 0)
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(
                    AssetDatabase.GUIDToAssetPath(guids[0])));
            else
                EditorUtility.DisplayDialog("Not Found",
                    "No script found containing '" + typeName + "'", "OK");
        }
    }
}
#endif