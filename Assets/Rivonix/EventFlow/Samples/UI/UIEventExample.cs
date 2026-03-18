using UnityEngine;
using UnityEngine.UI;
using Rivonix.EventFlow;

/// <summary>
/// Example showing how to integrate events with UI
/// </summary>
public class UIEventExample : EventListenerBase
{
    [Header("UI References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text healthText;
    [SerializeField] private Text stateText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Slider healthSlider;
    
    [Header("Event Channels")]
    [SerializeField] private IntEventChannelSO scoreChannel;
    [SerializeField] private GameStateChangedEventChannelSO stateChannel;
    
    private int currentScore = 0;
    private int currentHealth = 100;
    
    private void Awake()
    {
        // Setup UI button listeners
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
            
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
            
        if (healthSlider != null)
            healthSlider.onValueChanged.AddListener(OnHealthSliderChanged);
    }
    
    protected override void RegisterEvents()
    {
        // Register for events
        EventBus.Register<PlayerScoredEvent>(OnPlayerScored);
        EventBus.Register<PlayerDamagedEvent>(OnPlayerDamaged);
        EventBus.Register<GameStateChangedEvent>(OnGameStateChanged);
        
        // Subscribe to ScriptableObject channels
        if (scoreChannel != null)
            scoreChannel.OnEventRaised.AddListener(OnScoreChannelRaised);
            
        if (stateChannel != null)
            stateChannel.OnEventRaised.AddListener(OnStateChannelRaised);
    }
    
    protected override void UnregisterEvents()
    {
        // Unsubscribe from ScriptableObject channels
        if (scoreChannel != null)
            scoreChannel.OnEventRaised.RemoveListener(OnScoreChannelRaised);
            
        if (stateChannel != null)
            stateChannel.OnEventRaised.RemoveListener(OnStateChannelRaised);
    }
    
    private void OnPlayerScored(PlayerScoredEvent e)
    {
        currentScore += e.points;
        UpdateUI();
    }
    
    private void OnPlayerDamaged(PlayerDamagedEvent e)
    {
        currentHealth = Mathf.Max(0, currentHealth - e.damage);
        UpdateUI();
    }
    
    private void OnGameStateChanged(GameStateChangedEvent e)
    {
        UpdateUI();
        
        // Enable/disable UI based on state
        bool interactive = (e.newState == GameStateManager.GameState.Playing);
        if (pauseButton != null)
            pauseButton.interactable = interactive;
    }
    
    private void OnScoreChannelRaised(int score)
    {
        Debug.Log($"[UI] Score channel raised: {score}");
        currentScore = score;
        UpdateUI();
    }
    
    private void OnStateChannelRaised(GameStateChangedEvent e)
    {
        Debug.Log($"[UI] State channel raised: {e.newState}");
        UpdateUI();
    }
    
    private void OnPauseClicked()
    {
        if (GameStateManager.CurrentState == GameStateManager.GameState.Playing)
        {
            GameStateManager.PushState(GameStateManager.GameState.Paused);
            EventBus.Trigger(new GamePausedEvent());
        }
        else if (GameStateManager.CurrentState == GameStateManager.GameState.Paused)
        {
            GameStateManager.PopState();
            EventBus.Trigger(new GameResumedEvent());
        }
    }
    
    private void OnResetClicked()
    {
        currentHealth = 100;
        currentScore = 0;
        UpdateUI();
        
        if (healthSlider != null)
            healthSlider.value = 100;
    }
    
    private void OnHealthSliderChanged(float value)
    {
        currentHealth = Mathf.RoundToInt(value);
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";
            
        if (healthText != null)
            healthText.text = $"Health: {currentHealth}";
            
        if (stateText != null)
            stateText.text = $"State: {GameStateManager.CurrentState}";
            
        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }
}

/// <summary>
/// Custom event channel for GameStateChangedEvent
/// </summary>
[CreateAssetMenu(fileName = "GameState Channel", menuName = "Rivonix/EventFlow/GameState Channel")]
public class GameStateChangedEventChannelSO : EventChannelSO<GameStateChangedEvent> { }