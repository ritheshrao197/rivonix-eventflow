using UnityEngine;
using UnityEngine.Events;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// ScriptableObject-based event channel for visual scripting and inspector workflows.
    /// Allows designers to hook up events in the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "New Event Channel", menuName = "Rivonix/EventFlow/Event Channel", order = 1)]
    public class EventChannelSO : ScriptableObject
    {
        [Header("Event Settings")]
        [SerializeField] private string eventDescription;
        [SerializeField] private bool debugMode;
        
        [Header("Unity Event (Inspector connections)")]
        public UnityEvent OnEventRaised;
        
        /// <summary>
        /// Raise the event, triggering all connected UnityEvent listeners
        /// </summary>
        public void RaiseEvent()
        {
            if (debugMode)
                Debug.Log($"[EventChannel] {name} raised");
                
            OnEventRaised?.Invoke();
            
            // Also trigger through EventFlow for code listeners
            EventFlow.Trigger(new ScriptableObjectEvent { channelName = name });
        }
        
        private void OnEnable()
        {
            if (debugMode)
                Debug.Log($"[EventChannel] {name} enabled");
        }
    }
    
    /// <summary>
    /// Generic version with a parameter
    /// </summary>
    /// <typeparam name="T">The type of parameter</typeparam>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        [Header("Event Settings")]
        [SerializeField] private string eventDescription;
        [SerializeField] private bool debugMode;
        
        public UnityEvent<T> OnEventRaised;
        
        public void RaiseEvent(T value)
        {
            if (debugMode)
                Debug.Log($"[EventChannel] {name} raised with value: {value}");
                
            OnEventRaised?.Invoke(value);
        }
    }
    
    /// <summary>
    /// Concrete implementations for common types
    /// </summary>
    [CreateAssetMenu(fileName = "Int Event Channel", menuName = "Rivonix/EventFlow/Int Event Channel", order = 2)]
    public class IntEventChannelSO : EventChannelSO<int> { }
    
    [CreateAssetMenu(fileName = "Float Event Channel", menuName = "Rivonix/EventFlow/Float Event Channel", order = 3)]
    public class FloatEventChannelSO : EventChannelSO<float> { }
    
    [CreateAssetMenu(fileName = "String Event Channel", menuName = "Rivonix/EventFlow/String Event Channel", order = 4)]
    public class StringEventChannelSO : EventChannelSO<string> { }
    
    [CreateAssetMenu(fileName = "Bool Event Channel", menuName = "Rivonix/EventFlow/Bool Event Channel", order = 5)]
    public class BoolEventChannelSO : EventChannelSO<bool> { }
    
    [CreateAssetMenu(fileName = "Vector3 Event Channel", menuName = "Rivonix/EventFlow/Vector3 Event Channel", order = 6)]
    public class Vector3EventChannelSO : EventChannelSO<Vector3> { }
    
    /// <summary>
    /// Internal event for ScriptableObject channels
    /// </summary>
    public struct ScriptableObjectEvent : IEvent
    {
        public string channelName;
    }
}
