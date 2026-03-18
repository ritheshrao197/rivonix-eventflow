#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;

namespace Rivonix.EventFlow.Editor
{
    /// <summary>
    /// Debug window for monitoring events in real-time
    /// </summary>
    public class EventFlowDebugWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private Vector2 historyScrollPosition;
        private string searchFilter = "";
        private bool showListeners = true;
        private bool showHistory = true;
        private bool showStatePermissions = true;
        private bool showPerformance = true;
        private bool showScheduler = true;
        private int selectedEventIndex = -1;
        private float refreshRate = 0.5f;
        private double lastRefreshTime;
        
        // Cached data
        private List<Type> eventTypes = new List<Type>();
        private Dictionary<Type, int> listenerCounts = new Dictionary<Type, int>();
        
        [MenuItem("Tools/Rivonix/EventFlow/Debug Window")]
        public static void ShowWindow()
        {
            GetWindow<EventFlowDebugWindow>("Rivonix EventFlow Debug");
        }
        
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            RefreshData();
            titleContent = new GUIContent("EventFlow Debug", EditorGUIUtility.IconContent("d_UnityEditor.SceneView").image);
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnEditorUpdate()
        {
            // Refresh data periodically
            if (EditorApplication.timeSinceStartup - lastRefreshTime > refreshRate)
            {
                RefreshData();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }
        
        private void RefreshData()
        {
            // Use reflection to get listener data from EventBus
            var busType = typeof(EventBus);
            var listenersField = busType.GetField("listeners", BindingFlags.Static | BindingFlags.NonPublic);
            
            if (listenersField != null)
            {
                var listeners = listenersField.GetValue(null) as Dictionary<Type, List<Delegate>>;
                
                if (listeners != null)
                {
                    eventTypes.Clear();
                    listenerCounts.Clear();
                    
                    foreach (var kvp in listeners)
                    {
                        if (kvp.Key != null)
                        {
                            eventTypes.Add(kvp.Key);
                            listenerCounts[kvp.Key] = kvp.Value?.Count ?? 0;
                        }
                    }
                }
            }
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawHeader();
            DrawCurrentState();
            DrawPerformance();
            DrawEventListeners();
            DrawEventHistory();
            DrawScheduler();
            DrawStatePermissions();
            DrawControls();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshData();
            }
            
            GUILayout.Label("Refresh:", GUILayout.Width(50));
            refreshRate = EditorGUILayout.Slider(refreshRate, 0.1f, 2f, GUILayout.Width(100));
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label("Search:", GUILayout.Width(45));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));
            
            if (GUILayout.Button("Clear Filter", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                searchFilter = "";
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");
            
            GUILayout.Label("RIVONIX EVENTFLOW DEBUGGER", EditorStyles.boldLabel);
            GUILayout.Label("Real-time event monitoring and diagnostics", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCurrentState()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Current Game State", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("State:", GUILayout.Width(60));
            
            var currentState = GameStateManager.CurrentState;
            
            // Color code based on state
            GUI.color = GetStateColor(currentState);
            EditorGUILayout.EnumPopup(currentState, GUILayout.Width(150));
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            // Show state stack
            EditorGUILayout.LabelField("State Stack:", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            foreach (var state in GameStateManager.StateStack)
            {
                EditorGUILayout.LabelField($"► {state}");
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPerformance()
        {
            if (!showPerformance) return;
            
            EditorGUILayout.BeginVertical("box");
            
            showPerformance = EditorGUILayout.Foldout(showPerformance, "Performance", true);
            
            if (showPerformance)
            {
                var progressRect = EditorGUILayout.GetControlRect(false, 20);
                float eventLoad = Mathf.Clamp01(EventBus.GetEventHistory().Count / 50f);
                EditorGUI.ProgressBar(progressRect, eventLoad, $"Events in history: {EventBus.GetEventHistory().Count}");
                
                EditorGUILayout.LabelField($"Total Event Types: {eventTypes.Count}");
                EditorGUILayout.LabelField($"Total Listeners: {listenerCounts.Values.Sum()}");
                EditorGUILayout.LabelField($"Scheduled Events: {EventScheduler.Instance.ScheduledCount}");
                EditorGUILayout.LabelField($"Repeating Events: {EventScheduler.Instance.RepeatingCount}");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawEventListeners()
        {
            EditorGUILayout.BeginVertical("box");
            
            showListeners = EditorGUILayout.Foldout(showListeners, "Event Listeners", true);
            
            if (showListeners)
            {
                if (eventTypes.Count == 0)
                {
                    EditorGUILayout.HelpBox("No events registered yet. Trigger some events to see them here.", MessageType.Info);
                }
                else
                {
                    foreach (Type eventType in eventTypes.OrderBy(x => x.Name))
                    {
                        if (!string.IsNullOrEmpty(searchFilter) && 
                            !eventType.Name.ToLower().Contains(searchFilter.ToLower()))
                            continue;
                        
                        int count = listenerCounts.ContainsKey(eventType) ? listenerCounts[eventType] : 0;
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        // Select button
                        if (GUILayout.Button("►", EditorStyles.miniButton, GUILayout.Width(20)))
                        {
                            selectedEventIndex = selectedEventIndex == eventTypes.IndexOf(eventType) ? -1 : eventTypes.IndexOf(eventType);
                        }
                        
                        // Event name
                        EditorGUILayout.LabelField(eventType.Name, GUILayout.Width(200));
                        
                        // Listener count with progress bar
                        var rect = EditorGUILayout.GetControlRect(false, 18);
                        float percent = count / 20f;
                        EditorGUI.ProgressBar(rect, percent, $"{count} listener{(count != 1 ? "s" : "")}");
                        
                        // Jump to code button
                        if (GUILayout.Button("Find", EditorStyles.miniButton, GUILayout.Width(50)))
                        {
                            FindEventInProject(eventType.Name);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // Show expanded details
                        if (selectedEventIndex == eventTypes.IndexOf(eventType))
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.LabelField("Event Type:", eventType.FullName);
                            EditorGUILayout.LabelField("Listeners:", EditorStyles.miniLabel);
                            
                            // Try to show where listeners are registered
                            EditorGUILayout.HelpBox("Detailed listener info requires additional reflection. Check the EventBus class for implementation.", MessageType.Info);
                            
                            // Option to test trigger
                            if (GUILayout.Button("Test Trigger This Event", GUILayout.Width(150)))
                            {
                                Debug.Log($"[Debug] Manually triggered {eventType.Name}");
                                // This would need more complex reflection to create an instance
                            }
                            
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
            
            showHistory = EditorGUILayout.Foldout(showHistory, "Recent Events", true);
            
            if (showHistory)
            {
                historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition, GUILayout.Height(200));
                
                var history = EventBus.GetEventHistory();
                
                if (history == null || history.Count == 0)
                {
                    EditorGUILayout.HelpBox("No events triggered yet. Trigger some events to see them here.", MessageType.Info);
                }
                else
                {
                    // Column headers
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Frame", EditorStyles.boldLabel, GUILayout.Width(50));
                    EditorGUILayout.LabelField("Time", EditorStyles.boldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField("Event", EditorStyles.boldLabel, GUILayout.Width(150));
                    EditorGUILayout.LabelField("Data", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(2);
                    
                    foreach (var log in history)
                    {
                        string color = log.wasBlocked ? "orange" : "lime";
                        string blockedText = log.wasBlocked ? " [BLOCKED]" : "";
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        // Frame
                        EditorGUILayout.LabelField($"{log.frameCount}", GUILayout.Width(50));
                        
                        // Time
                        EditorGUILayout.LabelField($"{log.time:F2}s", GUILayout.Width(60));
                        
                        // Event name with color
                        GUI.color = log.wasBlocked ? Color.yellow : Color.green;
                        EditorGUILayout.LabelField($"{log.eventType}{blockedText}", GUILayout.Width(150));
                        GUI.color = Color.white;
                        
                        // Data (truncated)
                        string data = log.eventData;
                        if (data.Length > 50)
                            data = data.Substring(0, 47) + "...";
                        EditorGUILayout.LabelField(data, EditorStyles.wordWrappedLabel);
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawScheduler()
        {
            if (!showScheduler) return;
            
            EditorGUILayout.BeginVertical("box");
            
            showScheduler = EditorGUILayout.Foldout(showScheduler, "Event Scheduler", true);
            
            if (showScheduler)
            {
                EditorGUILayout.LabelField($"Scheduled Events: {EventScheduler.Instance.ScheduledCount}");
                EditorGUILayout.LabelField($"Repeating Events: {EventScheduler.Instance.RepeatingCount}");
                
                if (EventScheduler.Instance.ScheduledCount > 0 || EventScheduler.Instance.RepeatingCount > 0)
                {
                    if (GUILayout.Button("Cancel All Scheduled Events"))
                    {
                        if (EditorUtility.DisplayDialog("Cancel All", "Cancel all scheduled and repeating events?", "Yes", "No"))
                        {
                            EventScheduler.Instance.CancelAll();
                        }
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatePermissions()
        {
            EditorGUILayout.BeginVertical("box");
            
            showStatePermissions = EditorGUILayout.Foldout(showStatePermissions, "Event Permissions by State", true);
            
            if (showStatePermissions)
            {
                EditorGUILayout.HelpBox("Configure which events can trigger in which states. Use GameStateManager.AllowEventInStates<T>() in code.", MessageType.Info);
                
                // Simple permission viewer
                if (eventTypes.Count > 0)
                {
                    foreach (Type eventType in eventTypes.Take(5))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(eventType.Name, GUILayout.Width(150));
                        
                        // Check if there are permissions for this event
                        var permissionsField = typeof(GameStateManager).GetField("eventPermissions", BindingFlags.Static | BindingFlags.NonPublic);
                        if (permissionsField != null)
                        {
                            var permissions = permissionsField.GetValue(null) as Dictionary<Type, List<GameStateManager.GameState>>;
                            if (permissions != null && permissions.ContainsKey(eventType))
                            {
                                string states = string.Join(", ", permissions[eventType]);
                                EditorGUILayout.LabelField(states);
                            }
                            else
                            {
                                EditorGUILayout.LabelField("Allowed in all states", EditorStyles.miniLabel);
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (eventTypes.Count > 5)
                    {
                        EditorGUILayout.LabelField($"... and {eventTypes.Count - 5} more events");
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
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Clear History", GUILayout.Height(30)))
            {
                // Clear history via reflection
                var historyField = typeof(EventBus).GetField("eventHistory", BindingFlags.Static | BindingFlags.NonPublic);
                if (historyField != null)
                {
                    var history = historyField.GetValue(null) as List<EventBus.EventLog>;
                    history?.Clear();
                }
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear All Listeners", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Warning", 
                    "This will remove ALL event listeners. Are you sure?", 
                    "Yes", "Cancel"))
                {
                    EventBus.Clear();
                    RefreshData();
                }
            }
            
            GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button("Reset Game State", GUILayout.Height(30)))
            {
                GameStateManager.Reset();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Use EventBus.Register<T>() to listen for events\n" +
                "Use EventBus.Trigger<T>() to fire events\n" +
                "Use GameStateManager.PushState() to change states",
                MessageType.Info
            );
            
            EditorGUILayout.EndVertical();
        }
        
        private Color GetStateColor(GameStateManager.GameState state)
        {
            switch (state)
            {
                case GameStateManager.GameState.Playing:
                    return Color.green;
                case GameStateManager.GameState.Paused:
                    return Color.yellow;
                case GameStateManager.GameState.GameOver:
                    return Color.red;
                case GameStateManager.GameState.Loading:
                    return Color.cyan;
                case GameStateManager.GameState.MainMenu:
                    return Color.blue;
                case GameStateManager.GameState.Bootstrapping:
                    return Color.gray;
                default:
                    return Color.white;
            }
        }
        
        private void FindEventInProject(string eventTypeName)
        {
            // Search for scripts that might contain this event
            string[] guids = AssetDatabase.FindAssets($"t:script {eventTypeName}");
            
            if (guids.Length > 0)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    
                    // Check if the file contains the event type
                    string content = System.IO.File.ReadAllText(path);
                    if (content.Contains($"struct {eventTypeName}") || content.Contains($"class {eventTypeName}"))
                    {
                        EditorGUIUtility.PingObject(asset);
                        EditorUtility.DisplayDialog("Found", $"Event found in: {path}", "OK");
                        return;
                    }
                }
                
                // If we didn't find a matching struct, just ping the first script
                string firstPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var firstAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(firstPath);
                EditorGUIUtility.PingObject(firstAsset);
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", $"Could not find any script containing {eventTypeName}", "OK");
            }
        }
    }
}
#endif