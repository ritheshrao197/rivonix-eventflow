using UnityEngine;
using Rivonix.EventFlow;

/// <summary>
/// Advanced example showing game state management patterns
/// </summary>
public class GameStateExample : MonoBehaviour
{
    [Header("State Settings")]
    [SerializeField] private bool enableDebugLogging = true;
    
    private void Start()
    {
        // Configure event permissions for different game states
        SetupEventPermissions();
        
        // Subscribe to state changes
        GameStateManager.OnStateChanged += HandleStateChanged;
    }
    
    private void SetupEventPermissions()
    {
        // Only allow player actions in Playing state
        GameStateManager.AllowEventInStates<PlayerScoredEvent>(
            GameStateManager.GameState.Playing
        );
        
        GameStateManager.AllowEventInStates<PlayerDamagedEvent>(
            GameStateManager.GameState.Playing
        );
        
        // Allow UI events in multiple states
        GameStateManager.AllowEventInStates<UIEvents.ButtonClickedEvent>(
            GameStateManager.GameState.MainMenu,
            GameStateManager.GameState.Playing,
            GameStateManager.GameState.Paused,
            GameStateManager.GameState.Settings
        );
        
        // Allow system events everywhere
        GameStateManager.AllowEventInAllStates<GameStateChangedEvent>();
    }
    
    private void HandleStateChanged(GameStateManager.GameState previous, GameStateManager.GameState current)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"<color=yellow>Game State: {previous} -> {current}</color>");
        }
        
        // Handle state entry
        switch (current)
        {
            case GameStateManager.GameState.MainMenu:
                EnterMainMenu();
                break;
                
            case GameStateManager.GameState.Loading:
                EnterLoading();
                break;
                
            case GameStateManager.GameState.Playing:
                EnterPlaying();
                break;
                
            case GameStateManager.GameState.Paused:
                EnterPaused();
                break;
                
            case GameStateManager.GameState.GameOver:
                EnterGameOver();
                break;
        }
        
        // Handle state exit
        switch (previous)
        {
            case GameStateManager.GameState.Playing:
                ExitPlaying();
                break;
        }
    }
    
    private void EnterMainMenu()
    {
        Debug.Log("Entered Main Menu");
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void EnterLoading()
    {
        Debug.Log("Entered Loading");
        // Show loading screen
    }
    
    private void EnterPlaying()
    {
        Debug.Log("Entered Playing");
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void EnterPaused()
    {
        Debug.Log("Entered Paused");
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void EnterGameOver()
    {
        Debug.Log("Entered Game Over");
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void ExitPlaying()
    {
        Debug.Log("Exited Playing");
        // Save game, etc.
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));
        
        GUILayout.Label($"Current State: {GameStateManager.CurrentState}");
        GUILayout.Label($"Stack Depth: {GameStateManager.StateStack.Length}");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Play"))
            GameStateManager.SetState(GameStateManager.GameState.Playing);
            
        if (GUILayout.Button("Pause"))
            GameStateManager.PushState(GameStateManager.GameState.Paused);
            
        if (GUILayout.Button("Resume"))
            GameStateManager.PopState();
            
        if (GUILayout.Button("Main Menu"))
            GameStateManager.SetState(GameStateManager.GameState.MainMenu);
            
        if (GUILayout.Button("Game Over"))
            GameStateManager.SetState(GameStateManager.GameState.GameOver);
            
        GUILayout.EndArea();
    }
    
    private void OnDestroy()
    {
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }
}