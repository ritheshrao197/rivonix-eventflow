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
            EventFlowController.Execute(eventData);
        }

        public static void TriggerDirect<T>(T eventData) where T : IEvent
        {
            EventBus.Dispatch(eventData);
        }

        public static void TriggerDelayed<T>(T eventData, float delay) where T : IEvent
        {
            EventBus.TriggerDelayed(eventData, delay);
        }

        public static void AddStep<T>(EventStep<T> step) where T : IEvent
        {
            EventFlowController.AddStep(step);
        }

        public static void AddStep<T>(string name, EventStep<T> step, int priority = 0, bool enabled = true) where T : IEvent
        {
            EventFlowController.AddStep(name, step, priority, enabled);
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
