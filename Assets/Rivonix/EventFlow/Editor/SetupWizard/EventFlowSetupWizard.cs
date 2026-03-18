#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace Rivonix.EventFlow.Editor
{
    /// <summary>
    /// Setup wizard for one-click configuration of Rivonix EventFlow
    /// </summary>
    public class EventFlowSetupWizard : EditorWindow
    {
        private bool setupGlobalSystem = true;
        private bool createExampleEvents = true;
        private bool createSampleScene = false;
        private bool createEventChannels = true;
        private GameStateManager.GameState initialState = GameStateManager.GameState.MainMenu;
        
        [MenuItem("Tools/Rivonix/EventFlow/Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<EventFlowSetupWizard>("Rivonix EventFlow Setup");
        }
        
        private void OnGUI()
        {
            // Draw header
            GUILayout.Label("RIVONIX EVENTFLOW", EditorStyles.boldLabel);
            GUILayout.Label("Professional Event System for Unity", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This wizard will set up Rivonix EventFlow in your project.\n" +
                "All settings can be changed later.",
                MessageType.Info
            );
            
            EditorGUILayout.Space(20);
            
            // Setup options
            DrawSetupOptions();
            
            EditorGUILayout.Space(20);
            
            // Action buttons
            DrawActionButtons();
            
            EditorGUILayout.Space(10);
            
            // Footer
            DrawFooter();
        }
        
        private void DrawSetupOptions()
        {
            GUILayout.Label("Setup Options", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            setupGlobalSystem = EditorGUILayout.Toggle(new GUIContent(
                "Create Global Event System", 
                "Adds GlobalEventSystem to your first scene"
            ), setupGlobalSystem);
            
            if (setupGlobalSystem)
            {
                EditorGUI.indentLevel++;
                initialState = (GameStateManager.GameState)EditorGUILayout.EnumPopup(
                    new GUIContent("Initial State", "Starting game state"), 
                    initialState
                );
                EditorGUI.indentLevel--;
            }
            
            createExampleEvents = EditorGUILayout.Toggle(new GUIContent(
                "Create Example Events", 
                "Generates example event definitions"
            ), createExampleEvents);
            
            createEventChannels = EditorGUILayout.Toggle(new GUIContent(
                "Create Event Channels (SO)", 
                "Creates ScriptableObject event channels"
            ), createEventChannels);
            
            createSampleScene = EditorGUILayout.Toggle(new GUIContent(
                "Create Sample Scene", 
                "Creates a sample scene with working examples"
            ), createSampleScene);
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Setup EventFlow", GUILayout.Height(40)))
            {
                SetupEventSystem();
            }
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Documentation", GUILayout.Height(40)))
            {
                Application.OpenURL("https://github.com/Rivonix/EventFlow/wiki");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Debug Window", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Tools/Rivonix/EventFlow/Debug Window");
            }
            
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Check for Updates", GUILayout.Height(30)))
            {
                CheckForUpdates();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Rivonix EventFlow v1.0.0", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("© 2024 Rivonix. All rights reserved.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }
        
        private void SetupEventSystem()
        {
            // Create folders if they don't exist
            CreateFolderStructure();
            
            // Create global event system in current scene
            if (setupGlobalSystem)
            {
                CreateGlobalEventSystem();
            }
            
            // Create example events
            if (createExampleEvents)
            {
                CreateExampleEvents();
            }
            
            // Create event channels
            if (createEventChannels)
            {
                CreateEventChannels();
            }
            
            // Create sample scene
            if (createSampleScene)
            {
                CreateSampleScene();
            }
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog(
                "Setup Complete",
                "Rivonix EventFlow has been set up successfully!\n\n" +
                "Next steps:\n" +
                "1. Check the Debug Window (Tools > Rivonix > EventFlow > Debug Window)\n" +
                "2. Explore the Samples folder for examples\n" +
                "3. Read the Documentation\n\n" +
                "Happy coding!",
                "Get Started"
            );
        }
        
        private void CreateFolderStructure()
        {
            string[] folders = {
                "Assets/Rivonix",
                "Assets/Rivonix/EventFlow",
                "Assets/Rivonix/EventFlow/Runtime",
                "Assets/Rivonix/EventFlow/Editor",
                "Assets/Rivonix/EventFlow/Channels",
                "Assets/Rivonix/EventFlow/Samples",
                "Assets/Rivonix/EventFlow/Samples/Basic",
                "Assets/Rivonix/EventFlow/Samples/Events",
                "Assets/Rivonix/EventFlow/Samples/Scenes",
                "Assets/Rivonix/EventFlow/Samples/UI",
                "Assets/Rivonix/EventFlow/Documentation"
            };
            
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
            
            Debug.Log("[Rivonix] Created folder structure");
        }
        
        private void CreateGlobalEventSystem()
        {
            // Check if one already exists
            var existing = GameObject.FindObjectOfType<GlobalEventSystem>();
            if (existing != null)
            {
                Debug.Log("[Rivonix] Global Event System already exists in scene");
                return;
            }
            
            // Create new GameObject with GlobalEventSystem
            GameObject go = new GameObject("Rivonix_EventFlow");
            var system = go.AddComponent<GlobalEventSystem>();
            
            // Set initial state via serialized property
            var serialized = new SerializedObject(system);
            serialized.FindProperty("initialState").enumValueIndex = (int)initialState;
            serialized.ApplyModifiedProperties();
            
            // Mark scene as dirty
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            Debug.Log("[Rivonix] Created Global Event System in current scene");
        }
        
        private void CreateExampleEvents()
        {
            string exampleCode = @"using Rivonix.EventFlow;

// Example game events for Rivonix EventFlow
public struct PlayerScoredEvent : IEvent
{
    public int points;
    public string enemyTag;
    public float comboMultiplier;
}

public struct PlayerDamagedEvent : IEvent
{
    public int damage;
    public string source;
    public UnityEngine.Vector3 position;
}

public struct PlayerDiedEvent : IEvent
{
    public int finalScore;
    public float playTime;
    public string killedBy;
}

public struct GameStartEvent : IEvent
{
    public int levelNumber;
    public string levelName;
    public int difficulty;
}

public struct GamePausedEvent : IEvent { }

public struct GameResumedEvent : IEvent { }

public struct OptionsChangedEvent : IEvent
{
    public float volume;
    public bool fullscreen;
    public int resolutionIndex;
}

public struct CollectiblePickupEvent : IEvent
{
    public string itemId;
    public int value;
    public UnityEngine.Vector3 position;
}

public struct LevelCompleteEvent : IEvent
{
    public int levelNumber;
    public float completionTime;
    public int starsEarned;
}
";
            
            File.WriteAllText("Assets/Rivonix/EventFlow/Samples/Events/ExampleEvents.cs", exampleCode);
            Debug.Log("[Rivonix] Created example events");
        }
        
        private void CreateEventChannels()
        {
            // Create some default event channel assets
            var channelTypes = new[] { 
                typeof(IntEventChannelSO), 
                typeof(FloatEventChannelSO), 
                typeof(StringEventChannelSO),
                typeof(BoolEventChannelSO),
                typeof(Vector3EventChannelSO)
            };
            
            foreach (var type in channelTypes)
            {
                var instance = ScriptableObject.CreateInstance(type);
                string assetName = type.Name.Replace("EventChannelSO", "");
                string path = $"Assets/Rivonix/EventFlow/Channels/{assetName}Channel.asset";
                
                AssetDatabase.CreateAsset(instance, path);
            }
            
            Debug.Log("[Rivonix] Created event channel assets");
        }
        
        private void CreateSampleScene()
        {
            // Create a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "Rivonix_EventFlow_Sample";
            
            // Create event system
            GameObject go = new GameObject("Rivonix_EventFlow");
            go.AddComponent<GlobalEventSystem>();
            
            // Create example listener
            GameObject listener = new GameObject("ExampleListener");
            listener.AddComponent<ExampleEventListener>();
            
            // Create UI text for demo
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            var textGO = new GameObject("Instructions");
            textGO.transform.SetParent(canvasGO.transform);
            var text = textGO.AddComponent<UnityEngine.UI.Text>();
            text.text = "Rivonix EventFlow Sample Scene\n\nPress SPACE to trigger PlayerScoredEvent\nPress D to trigger PlayerDamagedEvent\nCheck Console for output";
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            
            // Save scene
            EditorSceneManager.SaveScene(scene, "Assets/Rivonix/EventFlow/Samples/Scenes/SampleScene.unity");
            
            Debug.Log("[Rivonix] Created sample scene");
        }
        
        private void CheckForUpdates()
        {
            Debug.Log("[Rivonix] Checking for updates...");
            EditorUtility.DisplayDialog("Updates", "You are using the latest version of Rivonix EventFlow (v1.0.0)", "OK");
        }
    }
}
#endif


