using System.Collections.Generic;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Extension methods for working with events
    /// </summary>
    public static class EventExtensions
    {
        private static Queue<IEvent> eventPool = new Queue<IEvent>();
        
        /// <summary>
        /// Get an event from the pool (reduces allocations)
        /// </summary>
        public static T GetFromPool<T>() where T : IEvent, new()
        {
            if (eventPool.Count > 0)
                return (T)eventPool.Dequeue();
            
            return new T();
        }
        
        /// <summary>
        /// Return an event to the pool
        /// </summary>
        public static void ReturnToPool(this IEvent eventData)
        {
            eventPool.Enqueue(eventData);
        }
        
        /// <summary>
        /// Trigger an event and return it to the pool
        /// </summary>
        public static void TriggerAndReturn<T>(this T eventData) where T : IEvent
        {
            EventBus.Trigger(eventData);
            eventData.ReturnToPool();
        }
    }
}