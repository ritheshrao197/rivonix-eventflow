using UnityEngine;
using Rivonix.EventFlow;

// /// <summary>
// /// Simple event definitions for basic examples
// /// </summary>

// // Player events
// public struct PlayerScoredEvent : IEvent
// {
//     public int points;
//     public string enemyTag;
//     public float comboMultiplier;
// }

// public struct PlayerDamagedEvent : IEvent
// {
//     public int damage;
//     public string source;
//     public Vector3 position;
// }

// public struct PlayerDiedEvent : IEvent
// {
//     public int finalScore;
//     public float playTime;
//     public string killedBy;
// }

// Game events
// public struct GameStartEvent : IEvent
// {
//     public int levelNumber;
//     public string levelName;
//     public int difficulty;
// }

// public struct GamePausedEvent : IEvent { }
// public struct GameResumedEvent : IEvent { }

// UI Events
public struct UIEvents
{
    public struct ButtonClickedEvent : IEvent
    {
        public string buttonName;
    }
    
    public struct MenuOpenedEvent : IEvent
    {
        public string menuName;
    }
    
    public struct MenuClosedEvent : IEvent
    {
        public string menuName;
    }
}

// // Gameplay Events
// public struct CollectiblePickupEvent : IEvent
// {
//     public string itemId;
//     public int value;
//     public Vector3 position;
// }

// public struct LevelCompleteEvent : IEvent
// {
//     public int levelNumber;
//     public float completionTime;
//     public int starsEarned;
// }

public struct EnemySpawnedEvent : IEvent
{
    public string enemyType;
    public Vector3 position;
    public int waveNumber;
}

public struct EnemyDefeatedEvent : IEvent
{
    public string enemyType;
    public Vector3 position;
    public int scoreValue;
}