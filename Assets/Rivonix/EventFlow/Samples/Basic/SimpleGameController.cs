using UnityEngine;
using Rivonix.EventFlow;

/// <summary>
/// Simple game controller showing how to use GameStateManager
/// </summary>
public class SimpleGameController : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int startingLevel = 1;
    [SerializeField] private string gameName = "My Game";
    
    private void Start()
    {
        // Configure event permissions
        GameStateManager.AllowEventInStates<PlayerScoredEvent>(
            GameStateManager.GameState.Playing
        );
        
        GameStateManager.AllowEventInStates<PlayerDamagedEvent>(
            GameStateManager.GameState.Playing
        );
        
        GameStateManager.AllowEventInStates<GamePausedEvent>(
            GameStateManager.GameState.Playing,
            GameStateManager.GameState.MainMenu
        );
        
        // Start the game
        GameStateManager.SetState(GameStateManager.GameState.MainMenu);
        
        // Trigger game start after 1 second
        EventFlow.TriggerDelayed(new GameStartEvent
        {
            levelNumber = startingLevel,
            levelName = "Tutorial",
            difficulty = 1
        }, 1.0f);
    }
    
    private void Update()
    {
        // Global pause handling
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandlePause();
        }
    }
    
    private void HandlePause()
    {
        if (GameStateManager.CurrentState == GameStateManager.GameState.Playing)
        {
            // Pause the game
            GameStateManager.PushState(GameStateManager.GameState.Paused);
            EventFlow.Trigger(new GamePausedEvent());
            Time.timeScale = 0f;
        }
        else if (GameStateManager.CurrentState == GameStateManager.GameState.Paused)
        {
            // Unpause
            GameStateManager.PopState();
            EventFlow.Trigger(new GameResumedEvent());
            Time.timeScale = 1f;
        }
    }
    
    private void OnDestroy()
    {
        // Make sure time scale is reset
        Time.timeScale = 1f;
    }
}
