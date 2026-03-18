using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Core event bus that handles all event communication in the game.
    /// This is a static class that acts as a central message broker.
    /// </summary>
    public static class EventBus
    {
        // Storage for all event listeners
        private static Dictionary<Type, List<Delegate>> listeners = new Dictionary<Type, List<Delegate>>();
        
        // Event history for debugging
        private static List<EventLog> eventHistory = new List<EventLog>();
        private static int maxHistorySize = 100;
        
        // Performance monitoring
        private static int eventsThisFrame = 0;
        private static int maxEventsPerFrame = 200;
        
        /// <summary>
        /// Log entry for debug viewing
        /// </summary>
        public class EventLog
        {
            public string eventType;
            public string eventData;
            public float time;
            public int frameCount;
            public bool wasBlocked;
        }
        
        /// <summary>
        /// Get the event history for debugging
        /// </summary>
        public static List<EventLog> GetEventHistory() => eventHistory;
        
        /// <summary>
        /// Register a listener for an event type
        /// </summary>
        /// <typeparam name="T">The event type to listen for</typeparam>
        /// <param name="handler">The callback function to invoke when event triggers</param>
        public static void Register<T>(Action<T> handler) where T : IEvent
        {
            Type eventType = typeof(T);
            
            if (!listeners.ContainsKey(eventType))
                listeners[eventType] = new List<Delegate>();
            
            if (!listeners[eventType].Contains(handler))
            {
                listeners[eventType].Add(handler);
                
                #if UNITY_EDITOR
                Debug.Log($"[EventBus] Registered listener for {eventType.Name}");
                #endif
            }
        }
        
        /// <summary>
        /// Register a listener with automatic cleanup when the MonoBehaviour is destroyed
        /// </summary>
        /// <typeparam name="T">The event type to listen for</typeparam>
        /// <param name="owner">The MonoBehaviour that owns this listener</param>
        /// <param name="handler">The callback function to invoke when event triggers</param>
        public static void Register<T>(MonoBehaviour owner, Action<T> handler) where T : IEvent
        {
            Register(handler);
            
            // Auto-cleanup when owner is destroyed
            var tracker = owner.gameObject.GetComponent<ListenerTracker>();
            if (tracker == null)
                tracker = owner.gameObject.AddComponent<ListenerTracker>();
                
            tracker.AddListener(typeof(T), handler);
        }
        
        /// <summary>
        /// Unregister a listener
        /// </summary>
        /// <typeparam name="T">The event type</typeparam>
        /// <param name="handler">The callback function to remove</param>
        public static void Unregister<T>(Action<T> handler) where T : IEvent
        {
            Type eventType = typeof(T);
            
            if (listeners.ContainsKey(eventType))
            {
                listeners[eventType].Remove(handler);
                
                #if UNITY_EDITOR
                Debug.Log($"[EventBus] Unregistered listener for {eventType.Name}");
                #endif
            }
        }
        
        /// <summary>
        /// Trigger an event immediately, invoking all registered listeners
        /// </summary>
        /// <typeparam name="T">The event type to trigger</typeparam>
        /// <param name="eventData">The event data to pass to listeners</param>
        public static void Trigger<T>(T eventData) where T : IEvent
        {
            Type eventType = typeof(T);
            
            // Performance check
            eventsThisFrame++;
            if (eventsThisFrame > maxEventsPerFrame)
            {
                Debug.LogWarning($"[EventBus] Too many events this frame ({eventsThisFrame}). Possible infinite loop?");
                return;
            }
            
            // Check game state permissions
            bool blocked = !GameStateManager.CanTriggerEvent(eventType);
            
            // Log for debugging
            #if UNITY_EDITOR
            LogEvent(eventType, eventData.ToString(), blocked);
            #endif
            
            if (blocked)
                return;
            
            // Trigger all listeners
            if (listeners.ContainsKey(eventType))
            {
                // Copy list to prevent modification during iteration
                var handlers = listeners[eventType].ToArray();
                
                foreach (var handler in handlers)
                {
                    try
                    {
                        (handler as Action<T>)?.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventBus] Error in listener for {eventType.Name}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Trigger an event after a delay
        /// </summary>
        /// <typeparam name="T">The event type to trigger</typeparam>
        /// <param name="eventData">The event data</param>
        /// <param name="delay">Delay in seconds</param>
        public static void TriggerDelayed<T>(T eventData, float delay) where T : IEvent
        {
            EventScheduler.Instance.Schedule(eventData, delay);
        }
        
        /// <summary>
        /// Check if an event type has any listeners
        /// </summary>
        public static bool HasListeners<T>() where T : IEvent
        {
            Type eventType = typeof(T);
            return listeners.ContainsKey(eventType) && listeners[eventType].Count > 0;
        }
        
        /// <summary>
        /// Get the number of listeners for an event type
        /// </summary>
        public static int GetListenerCount<T>() where T : IEvent
        {
            Type eventType = typeof(T);
            return listeners.ContainsKey(eventType) ? listeners[eventType].Count : 0;
        }
        
        /// <summary>
        /// Get all registered event types
        /// </summary>
        public static Type[] GetAllEventTypes()
        {
            var types = new Type[listeners.Keys.Count];
            listeners.Keys.CopyTo(types, 0);
            return types;
        }
        
        /// <summary>
        /// Get listener count for a specific event type
        /// </summary>
        public static int GetListenerCount(Type eventType)
        {
            return listeners.ContainsKey(eventType) ? listeners[eventType].Count : 0;
        }
        
        /// <summary>
        /// Clear all listeners (useful for scene changes)
        /// </summary>
        public static void Clear()
        {
            listeners.Clear();
            eventsThisFrame = 0;
            
            #if UNITY_EDITOR
            Debug.Log("[EventBus] All listeners cleared");
            #endif
        }
        
        /// <summary>
        /// Reset frame counter (called by GlobalEventSystem)
        /// </summary>
        public static void OnFrameEnd()
        {
            eventsThisFrame = 0;
        }
        
        #if UNITY_EDITOR
        private static void LogEvent(Type eventType, string data, bool blocked)
        {
            eventHistory.Add(new EventLog
            {
                eventType = eventType.Name,
                eventData = data,
                time = Time.time,
                frameCount = Time.frameCount,
                wasBlocked = blocked
            });
            
            // Trim history
            while (eventHistory.Count > maxHistorySize)
                eventHistory.RemoveAt(0);
        }
        #endif
    }
}