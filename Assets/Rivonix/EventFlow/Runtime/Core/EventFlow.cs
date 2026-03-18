using System;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Public facade for the EventFlow runtime API.
    /// </summary>
    public static class EventFlow
    {
        public static void Register<T>(Action<T> handler) where T : IEvent
        {
            EventBus.Register(handler);
        }

        public static void Register<T>(MonoBehaviour owner, Action<T> handler) where T : IEvent
        {
            EventBus.Register(owner, handler);
        }

        public static void Unregister<T>(Action<T> handler) where T : IEvent
        {
            EventBus.Unregister(handler);
        }

        public static void Trigger<T>(T eventData) where T : IEvent
        {
            EventBus.Trigger(eventData);
        }

        public static void TriggerDelayed<T>(T eventData, float delay) where T : IEvent
        {
            EventBus.TriggerDelayed(eventData, delay);
        }

        public static bool HasListeners<T>() where T : IEvent
        {
            return EventBus.HasListeners<T>();
        }

        public static int GetListenerCount<T>() where T : IEvent
        {
            return EventBus.GetListenerCount<T>();
        }

        public static Type[] GetAllEventTypes()
        {
            return EventBus.GetAllEventTypes();
        }

        public static void ClearListeners()
        {
            EventBus.Clear();
        }
    }
}
