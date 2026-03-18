using UnityEngine;
using Rivonix.EventFlow;

/// <summary>
/// Example event listener showing how to use the Rivonix EventFlow system
/// </summary>
public class ExampleEventListener : EventListenerBase
{
    [Header("Settings")]
    [SerializeField] private string playerName = "Hero";
    [SerializeField] private int health = 100;
    
    private int score = 0;
    
    protected override void RegisterEvents()
    {
        // Register for various events
        EventFlow.Register<PlayerScoredEvent>(OnPlayerScored);
        EventFlow.Register<PlayerDamagedEvent>(OnPlayerDamaged);
        EventFlow.Register<PlayerDiedEvent>(OnPlayerDied);
        EventFlow.Register<GameStateChangedEvent>(OnGameStateChanged);
        
        Debug.Log("[Example] Registered for events");
    }
    
    protected override void UnregisterEvents()
    {
        // No need to manually unregister if using EventListenerBase!
        // The system handles it automatically
        Debug.Log("[Example] Unregistered from events");
    }
    
    private void OnPlayerScored(PlayerScoredEvent e)
    {
        score += e.points;
        Debug.Log($"[Example] Player scored {e.points} points! Total: {score} (Combo: {e.comboMultiplier}x)");
    }
    
    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        health -= e.damage;
        Debug.Log($"[Example] Player took {e.damage} damage from {e.source}! Health: {health}");
        
        if (health <= 0)
        {
            // Trigger death event
            EventFlow.Trigger(new PlayerDiedEvent
            {
                finalScore = score,
                playTime = Time.time,
                killedBy = e.source
            });
        }
    }
    
    private void OnPlayerDied(PlayerDiedEvent e)
    {
        Debug.Log($"[Example] GAME OVER! Score: {e.finalScore}, Time: {e.playTime:F2}s, Killed by: {e.killedBy}");
    }
    
    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        Debug.Log($"[Example] Game state changed: {e.previousState} -> {e.newState} (Push: {e.isPush})");
        
        // Disable input when not playing
        enabled = (e.newState == GameStateManager.GameState.Playing);
    }
    
    private void Update()
    {
        Debug.Log($"[Example] Update called in state: {GameStateManager.CurrentState}");    
        // Example: Trigger events based on input
       if (Input.GetKeyDown(KeyCode.Space))
        {
            EventFlow.Trigger(new PlayerScoredEvent
            {
                points = 100,
                enemyTag = "Enemy",
                comboMultiplier = 1.5f
            });
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            EventFlow.Trigger(new PlayerDamagedEvent
            {
                damage = 10,
                source = "Enemy",
                position = transform.position
            });
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Toggle pause
            if (GameStateManager.CurrentState == GameStateManager.GameState.Playing)
            {
                GameStateManager.PushState(GameStateManager.GameState.Paused);
                EventFlow.Trigger(new GamePausedEvent());
            }
            else if (GameStateManager.CurrentState == GameStateManager.GameState.Paused)
            {
                GameStateManager.PopState();
                EventFlow.Trigger(new GameResumedEvent());
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Reset health
            health = 100;
            Debug.Log("[Example] Health reset to 100");
        }
    }
    
    private void OnGUI()
    {
        // Simple GUI to show state
        GUILayout.Label($"Player: {playerName}");
        GUILayout.Label($"Health: {health}");
        GUILayout.Label($"Score: {score}");
        GUILayout.Label($"Game State: {GameStateManager.CurrentState}");
    }
}
