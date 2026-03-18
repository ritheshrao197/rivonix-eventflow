using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Base class for MonoBehaviours that listen to events.
    /// Handles automatic registration/unregistration.
    /// </summary>
    public abstract class EventListenerBase : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            RegisterEvents();
        }
        
        protected virtual void OnDisable()
        {
            UnregisterEvents();
        }
        
        protected virtual void OnDestroy()
        {
            UnregisterEvents();
        }
        
        /// <summary>
        /// Override this to register your event listeners
        /// Example: EventFlow.Register<PlayerDiedEvent>(OnPlayerDied);
        /// </summary>
        protected abstract void RegisterEvents();
        
        /// <summary>
        /// Override this to unregister your event listeners
        /// Example: EventFlow.Unregister<PlayerDiedEvent>(OnPlayerDied);
        /// </summary>
        protected abstract void UnregisterEvents();
        
        /// <summary>
        /// Helper method to trigger events from this component
        /// </summary>
        protected void TriggerEvent<T>(T eventData) where T : IEvent
        {
            EventFlow.Trigger(eventData);
        }
        
        /// <summary>
        /// Helper method to trigger delayed events
        /// </summary>
        protected void TriggerEventDelayed<T>(T eventData, float delay) where T : IEvent
        {
            EventFlow.TriggerDelayed(eventData, delay);
        }
        
        /// <summary>
        /// Helper method to check if an event has listeners
        /// </summary>
        protected bool HasListeners<T>() where T : IEvent
        {
            return EventFlow.HasListeners<T>();
        }
    }
}
