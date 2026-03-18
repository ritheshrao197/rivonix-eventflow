using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Manages game states and controls which events can fire in each state.
    /// Uses a stack-based state machine for hierarchical states.
    /// </summary>
    public static class GameStateManager
    {
        /// <summary>
        /// Predefined game states. Extend this enum in your own code as needed.
        /// </summary>
        [Serializable]
        public enum GameState
        {
            None = 0,
            Bootstrapping = 1,
            MainMenu = 10,
            Loading = 20,
            Playing = 30,
            Paused = 31,
            Cutscene = 32,
            Dialogue = 33,
            GameOver = 40,
            Victory = 41,
            Settings = 50,
            Inventory = 51,
            Shop = 52,
            Multiplayer = 60
        }
        
        // State stack for hierarchical states
        private static Stack<GameState> stateStack = new Stack<GameState>();
        
        // Event permission storage
        private static Dictionary<Type, List<GameState>> eventPermissions = new Dictionary<Type, List<GameState>>();
        
        // Callbacks for state changes
        public static event Action<GameState, GameState> OnStateChanged;
        
        static GameStateManager()
        {
            // Initialize with default state
            stateStack.Push(GameState.Bootstrapping);
        }
        
        /// <summary>
        /// Current active game state (top of stack)
        /// </summary>
        public static GameState CurrentState => stateStack.Count > 0 ? stateStack.Peek() : GameState.None;
        
        /// <summary>
        /// Full state stack (for debugging)
        /// </summary>
        public static GameState[] StateStack => stateStack.ToArray();
        
        /// <summary>
        /// Configure which states an event can trigger in
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="allowedStates">States where this event is allowed</param>
        public static void AllowEventInStates<T>(params GameState[] allowedStates) where T : IEvent
        {
            Type eventType = typeof(T);
            
            if (!eventPermissions.ContainsKey(eventType))
                eventPermissions[eventType] = new List<GameState>();
            
            eventPermissions[eventType].Clear();
            eventPermissions[eventType].AddRange(allowedStates);
            
            #if UNITY_EDITOR
            Debug.Log($"[GameState] Event {eventType.Name} allowed in: {string.Join(", ", allowedStates)}");
            #endif
        }
        
        /// <summary>
        /// Allow an event in all states (remove restrictions)
        /// </summary>
        public static void AllowEventInAllStates<T>() where T : IEvent
        {
            Type eventType = typeof(T);
            
            if (eventPermissions.ContainsKey(eventType))
                eventPermissions.Remove(eventType);
        }
        
        /// <summary>
        /// Check if an event can trigger in the current state
        /// </summary>
        public static bool CanTriggerEvent(Type eventType)
        {
            // If no permissions defined, allow in all states
            if (!eventPermissions.ContainsKey(eventType))
                return true;
            
            var allowedStates = eventPermissions[eventType];
            return allowedStates.Contains(CurrentState) || allowedStates.Contains(GameState.None);
        }
        
        /// <summary>
        /// Push a new state onto the stack (suspend current state)
        /// </summary>
        /// <param name="newState">The new state to enter</param>
        public static void PushState(GameState newState)
        {
            GameState previousState = CurrentState;
            stateStack.Push(newState);
            
            OnStateChanged?.Invoke(previousState, newState);
            
            // Trigger event for other systems
            EventFlow.Trigger(new GameStateChangedEvent
            {
                previousState = previousState,
                newState = newState,
                isPush = true
            });
            
            #if UNITY_EDITOR
            Debug.Log($"[GameState] Pushed: {newState} (Stack: {stateStack.Count})");
            #endif
        }
        
        /// <summary>
        /// Pop the current state and return to the previous one
        /// </summary>
        public static void PopState()
        {
            if (stateStack.Count > 1)
            {
                GameState previousState = stateStack.Pop();
                GameState newState = CurrentState;
                
                OnStateChanged?.Invoke(previousState, newState);
                
                EventFlow.Trigger(new GameStateChangedEvent
                {
                    previousState = previousState,
                    newState = newState,
                    isPush = false
                });
                
                #if UNITY_EDITOR
                Debug.Log($"[GameState] Popped: {previousState} (Now: {newState})");
                #endif
            }
        }
        
        /// <summary>
        /// Set a single state (clears the stack)
        /// </summary>
        /// <param name="newState">The new state to enter</param>
        public static void SetState(GameState newState)
        {
            GameState previousState = CurrentState;
            stateStack.Clear();
            stateStack.Push(newState);
            
            OnStateChanged?.Invoke(previousState, newState);
            
            EventFlow.Trigger(new GameStateChangedEvent
            {
                previousState = previousState,
                newState = newState,
                isPush = false
            });
            
            #if UNITY_EDITOR
            Debug.Log($"[GameState] Set: {newState}");
            #endif
        }
        
        /// <summary>
        /// Check if the current state matches any of the provided states
        /// </summary>
        /// <param name="states">States to check against</param>
        /// <returns>True if current state is in the list</returns>
        public static bool IsInState(params GameState[] states)
        {
            foreach (var state in states)
            {
                if (CurrentState == state)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Reset the state manager (clears stack and permissions)
        /// </summary>
        public static void Reset()
        {
            stateStack.Clear();
            stateStack.Push(GameState.Bootstrapping);
            eventPermissions.Clear();
        }
    }
    
    /// <summary>
    /// Event fired when the game state changes
    /// </summary>
    public struct GameStateChangedEvent : IEvent
    {
        public GameStateManager.GameState previousState;
        public GameStateManager.GameState newState;
        public bool isPush; // True if pushed onto stack, false if set/popped
    }
}
