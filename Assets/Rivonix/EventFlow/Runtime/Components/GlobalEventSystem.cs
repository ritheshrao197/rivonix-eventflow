using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Main entry point for the EventFlow system.
    /// Place this in your first scene to ensure the event system is initialized.
    /// </summary>
    public class GlobalEventSystem : MonoBehaviour
    {
        [Header("Initialization")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [Tooltip("If false, you must manually call GameStateManager.SetState()")]
        [SerializeField] private bool setInitialState = true;
        
        [Header("Default State")]
        [SerializeField] private GameStateManager.GameState initialState = GameStateManager.GameState.MainMenu;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private KeyCode openDebugWindowKey = KeyCode.F12;
        
        private static GlobalEventSystem instance;
        
        /// <summary>
        /// Check if the global event system has been initialized
        /// </summary>
        public static bool IsInitialized => instance != null;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
            
            // Initialize systems
            InitializeEventSystem();
        }
        
        private void Start()
        {
            // Set initial game state if requested
            if (setInitialState)
            {
                GameStateManager.SetState(initialState);
            }
        }
        
        private void InitializeEventSystem()
        {
            // Trigger EventScheduler creation
            var scheduler = EventScheduler.Instance;
            
            if (enableDebugLogging)
            {
                Debug.Log("[GlobalEventSystem] Rivonix EventFlow initialized");
                Debug.Log($"[GlobalEventSystem] Version 1.0.0");
            }
        }
        
        private void Update()
        {
            // Reset frame counter for event bus
            EventBus.OnFrameEnd();
            
            // Debug window shortcut
            #if UNITY_EDITOR
            if (Input.GetKeyDown(openDebugWindowKey))
            {
                OpenDebugWindow();
            }
            #endif
        }
        
        #if UNITY_EDITOR
        private void OpenDebugWindow()
        {
            var windowType = System.Type.GetType("Rivonix.EventFlow.Editor.EventFlowDebugWindow, Rivonix-EventFlow.Editor");
            if (windowType != null)
            {
                UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Rivonix/EventFlow/Debug Window");
            }
            else
            {
                Debug.LogWarning("Debug window not available. Make sure you're in the Editor and the scripts are compiled.");
            }
        }
        #endif
        
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
        
        /// <summary>
        /// Manually trigger a cleanup of all event listeners
        /// </summary>
        public static void Cleanup()
        {
            EventBus.Clear();
        }
    }
}