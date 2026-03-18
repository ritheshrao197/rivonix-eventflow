using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Collects runtime diagnostics for EventFlow without coupling the dispatcher to editor tooling.
    /// </summary>
    public static class EventFlowDiagnostics
    {
        private static readonly List<EventRecord> eventHistory = new List<EventRecord>();
        private static int maxHistorySize = 100;
        private static int eventsThisFrame;
        private static int maxEventsPerFrame = 200;

        public class EventRecord
        {
            public string eventType;
            public string eventData;
            public float time;
            public int frameCount;
            public bool wasBlocked;
        }

        public static IReadOnlyList<EventRecord> EventHistory => eventHistory;
        public static int EventsThisFrame => eventsThisFrame;
        public static int MaxEventsPerFrame
        {
            get => maxEventsPerFrame;
            set => maxEventsPerFrame = Mathf.Max(1, value);
        }

        public static bool TryBeginEventDispatch()
        {
            eventsThisFrame++;
            if (eventsThisFrame > maxEventsPerFrame)
            {
                Debug.LogWarning($"[EventFlow] Too many events this frame ({eventsThisFrame}). Possible infinite loop?");
                return false;
            }

            return true;
        }

        public static void RecordEvent(Type eventType, string eventData, bool wasBlocked)
        {
            eventHistory.Add(new EventRecord
            {
                eventType = eventType.Name,
                eventData = eventData,
                time = Time.time,
                frameCount = Time.frameCount,
                wasBlocked = wasBlocked
            });

            while (eventHistory.Count > maxHistorySize)
            {
                eventHistory.RemoveAt(0);
            }
        }

        public static void ClearHistory()
        {
            eventHistory.Clear();
        }

        public static void ResetFrame()
        {
            eventsThisFrame = 0;
        }

        public static void ResetAll()
        {
            eventsThisFrame = 0;
            eventHistory.Clear();
        }
    }
}
