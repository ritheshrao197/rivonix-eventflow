using Rivonix.EventFlow;

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
