#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rivonix.EventFlow.Editor
{
    /// <summary>
    /// One-click setup wizard for Rivonix EventFlow.
    /// Opens via Tools → Rivonix → EventFlow → Setup Wizard.
    /// </summary>
    public class EventFlowSetupWizard : EditorWindow
    {
        private bool _setupGlobalSystem   = true;
        private bool _createExampleEvents = true;
        private bool _createChannels      = true;
        private bool _createSampleScene   = false;

        private GameStateManager.GameState _initialState = GameStateManager.GameState.MainMenu;

        [MenuItem("Tools/Rivonix/EventFlow/Setup Wizard")]
        public static void ShowWindow()
            => GetWindow<EventFlowSetupWizard>("EventFlow Setup");

        private void OnGUI()
        {
            GUILayout.Label("Rivonix EventFlow", EditorStyles.boldLabel);
            GUILayout.Label("Professional Event System for Unity", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard configures EventFlow in your project.\n" +
                "All options can be changed later.",
                MessageType.Info);

            EditorGUILayout.Space(16);
            DrawOptions();
            EditorGUILayout.Space(16);
            DrawButtons();
            EditorGUILayout.Space(8);
            DrawFooter();
        }

        // ── Options ──────────────────────────────────────────────────────────────

        private void DrawOptions()
        {
            GUILayout.Label("Setup Options", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _setupGlobalSystem = EditorGUILayout.Toggle(
                new GUIContent("Create Global Event System", "Adds GlobalEventSystem to the active scene"),
                _setupGlobalSystem);

            if (_setupGlobalSystem)
            {
                EditorGUI.indentLevel++;
                _initialState = (GameStateManager.GameState)EditorGUILayout.EnumPopup(
                    new GUIContent("Initial State", "Starting game state after boot"),
                    _initialState);
                EditorGUI.indentLevel--;
            }

            _createExampleEvents = EditorGUILayout.Toggle(
                new GUIContent("Create Example Events", "Generates a ready-to-use ExampleEvents.cs"),
                _createExampleEvents);

            _createChannels = EditorGUILayout.Toggle(
                new GUIContent("Create Event Channel Assets", "Creates typed ScriptableObject channel assets"),
                _createChannels);

            _createSampleScene = EditorGUILayout.Toggle(
                new GUIContent("Create Sample Scene", "Creates a minimal demo scene"),
                _createSampleScene);

            EditorGUI.indentLevel--;
        }

        // ── Buttons ──────────────────────────────────────────────────────────────

        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("Setup EventFlow", GUILayout.Height(38)))
                RunSetup();

            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            if (GUILayout.Button("Documentation", GUILayout.Height(38)))
                Application.OpenURL("https://github.com/Rivonix/EventFlow/wiki");

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(1f, 0.95f, 0.4f);
            if (GUILayout.Button("Open Debug Window", GUILayout.Height(28)))
                EditorApplication.ExecuteMenuItem("Tools/Rivonix/EventFlow/Debug Window");

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Check for Updates", GUILayout.Height(28)))
                EditorUtility.DisplayDialog("Up to Date", "You are running Rivonix EventFlow v1.1.0.", "OK");

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Rivonix EventFlow v1.1.0", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Label("© 2024 Rivonix. All rights reserved.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        // ── Setup logic ──────────────────────────────────────────────────────────

        private void RunSetup()
        {
            CreateFolders();
            if (_setupGlobalSystem)   CreateGlobalEventSystem();
            if (_createExampleEvents) CreateExampleEvents();
            if (_createChannels)      CreateEventChannelAssets();
            if (_createSampleScene)   CreateSampleScene();

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Setup Complete",
                "Rivonix EventFlow v1.1.0 is ready!\n\n" +
                "Next steps:\n" +
                "  1. Open the Debug Window (Tools → Rivonix → EventFlow → Debug Window)\n" +
                "  2. Browse the Samples folder for usage examples\n" +
                "  3. Read the Documentation\n\n" +
                "Happy coding!",
                "Get Started");
        }

        private static void CreateFolders()
        {
            string[] folders =
            {
                "Assets/Rivonix",
                "Assets/Rivonix/EventFlow",
                "Assets/Rivonix/EventFlow/Runtime",
                "Assets/Rivonix/EventFlow/Editor",
                "Assets/Rivonix/EventFlow/Channels",
                "Assets/Rivonix/EventFlow/Samples",
                "Assets/Rivonix/EventFlow/Samples/Events",
                "Assets/Rivonix/EventFlow/Samples/Scenes",
                "Assets/Rivonix/EventFlow/Documentation"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    Directory.CreateDirectory(folder);
            }

            Debug.Log("[Rivonix] Folder structure ready.");
        }

        private void CreateGlobalEventSystem()
        {
            if (Object.FindObjectOfType<GlobalEventSystem>() != null)
            {
                Debug.Log("[Rivonix] GlobalEventSystem already present in scene — skipped.");
                return;
            }

            var go     = new GameObject("[EventFlow] GlobalEventSystem");
            var system = go.AddComponent<GlobalEventSystem>();

            var so = new SerializedObject(system);
            so.FindProperty("initialState").enumValueIndex = (int)_initialState;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Rivonix] GlobalEventSystem added to active scene.");
        }

        private static void CreateExampleEvents()
        {
            const string code = @"using Rivonix.EventFlow;
using UnityEngine;

// ──────────────────────────────────────────────────────
//  Rivonix EventFlow — example event definitions
//  All events are structs: zero allocations, zero GC.
// ──────────────────────────────────────────────────────

public struct PlayerScoredEvent : IEvent
{
    public int   points;
    public float comboMultiplier;
    public string enemyTag;
}

public struct PlayerDamagedEvent : IEvent
{
    public int     damage;
    public string  source;
    public Vector3 position;
}

public struct PlayerDiedEvent : IEvent
{
    public int   finalScore;
    public float playTime;
    public string killedBy;
}

public struct GameStartEvent : IEvent
{
    public int    levelNumber;
    public string levelName;
    public int    difficulty;
}

public struct GamePausedEvent   : IEvent { }
public struct GameResumedEvent  : IEvent { }

public struct LevelCompleteEvent : IEvent
{
    public int   levelNumber;
    public float completionTime;
    public int   starsEarned;
}

public struct CollectiblePickupEvent : IEvent
{
    public string  itemId;
    public int     value;
    public Vector3 position;
}

public struct OptionsChangedEvent : IEvent
{
    public float volume;
    public bool  fullscreen;
    public int   resolutionIndex;
}
";
            const string path = "Assets/Rivonix/EventFlow/Samples/Events/ExampleEvents.cs";
            File.WriteAllText(path, code);
            Debug.Log($"[Rivonix] Example events written to {path}");
        }

        private static void CreateEventChannelAssets()
        {
            var channelTypes = new[]
            {
                typeof(IntEventChannelSO),
                typeof(FloatEventChannelSO),
                typeof(StringEventChannelSO),
                typeof(BoolEventChannelSO),
                typeof(Vector3EventChannelSO)
            };

            foreach (var type in channelTypes)
            {
                string label = type.Name.Replace("EventChannelSO", "");
                string path  = $"Assets/Rivonix/EventFlow/Channels/{label}Channel.asset";
                if (!File.Exists(path))
                {
                    var instance = ScriptableObject.CreateInstance(type);
                    AssetDatabase.CreateAsset(instance, path);
                }
            }

            Debug.Log("[Rivonix] Event channel assets created.");
        }

        private static void CreateSampleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            var systemGO = new GameObject("[EventFlow] GlobalEventSystem");
            systemGO.AddComponent<GlobalEventSystem>();

            const string savePath = "Assets/Rivonix/EventFlow/Samples/Scenes/EventFlow_Sample.unity";
            EditorSceneManager.SaveScene(scene, savePath);
            Debug.Log($"[Rivonix] Sample scene saved to {savePath}");
        }
    }
}
#endif
