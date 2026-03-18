using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Internal component that tracks event listeners for automatic cleanup.
    /// Attached automatically when using EventBus.Register(owner, handler).
    /// </summary>
    public class ListenerTracker : MonoBehaviour
    {
        private List<TrackerEntry> trackedListeners = new List<TrackerEntry>();
        
        private struct TrackerEntry
        {
            public Type eventType;
            public Delegate handler;
        }
        
        public void AddListener(Type eventType, Delegate handler)
        {
            trackedListeners.Add(new TrackerEntry 
            { 
                eventType = eventType, 
                handler = handler 
            });
        }
        
        private void OnDestroy()
        {
            foreach (var entry in trackedListeners)
            {
                try
                {
                    var method = typeof(EventBus).GetMethod("Unregister");
                    var generic = method.MakeGenericMethod(entry.eventType);
                    generic.Invoke(null, new object[] { entry.handler });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error unregistering listener: {e.Message}");
                }
            }
            
            trackedListeners.Clear();
        }
    }
}