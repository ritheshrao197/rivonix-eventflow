using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Collects runtime diagnostics for EventFlow without coupling the dispatcher to editor tooling.
    ///
    /// FIX 1 — Silent drop replaced with overflow queue.
    ///   Old code silently returned false when the per-frame cap was hit, meaning events were
    ///   fired by the caller but never reached any listener — an invisible bug.
    ///   Now excess events are queued and drained next frame. A warning fires only when the
    ///   queue grows past a secondary threshold, signalling a genuine event storm.
    ///
    /// FIX 2 — O(n) ring-buffer RemoveAt(0) replaced with a circular index.
    ///   Removing the first element of a List shifts every subsequent element, which is O(n)
    ///   and happens on every event once the history is full.  We now track a head index and
    ///   overwrite in place — always O(1).
    /// </summary>
    public static class EventFlowDiagnostics
    {
        // ── History ring buffer ──────────────────────────────────────────────────
        private static EventRecord[] historyBuffer = new EventRecord[100];
        private static int           historyHead;   // next write position
        private static int           historyCount;  // how many slots are filled

        private static int maxHistorySize = 100;

        // ── Per-frame accounting ─────────────────────────────────────────────────
        private static int eventsThisFrame;
        private static int maxEventsPerFrame = 200;

        // ── Overflow queue (replaces silent drop) ────────────────────────────────
        private static readonly Queue<Action> overflowQueue    = new Queue<Action>();
        private const           int           OverflowWarnSize = 500;

        // ────────────────────────────────────────────────────────────────────────

        public class EventRecord
        {
            public string eventType;
            public string eventData;
            public float  time;
            public int    frameCount;
            public bool   wasBlocked;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public static int EventsThisFrame    => eventsThisFrame;
        public static int OverflowQueueCount => overflowQueue.Count;

        public static int MaxEventsPerFrame
        {
            get => maxEventsPerFrame;
            set => maxEventsPerFrame = Mathf.Max(1, value);
        }

        public static int MaxHistorySize
        {
            get => maxHistorySize;
            set
            {
                value = Mathf.Max(1, value);
                if (value != maxHistorySize)
                {
                    maxHistorySize = value;
                    historyBuffer  = new EventRecord[maxHistorySize];
                    historyHead    = 0;
                    historyCount   = 0;
                }
            }
        }

        /// <summary>
        /// Returns a snapshot of recent events in chronological order (oldest → newest).
        /// Allocates a new list each call; use only in editor/debug contexts.
        /// </summary>
        public static List<EventRecord> GetEventHistory()
        {
            var result = new List<EventRecord>(historyCount);
            if (historyCount < maxHistorySize)
            {
                // Buffer not yet wrapped — elements are 0..historyHead-1
                for (int i = 0; i < historyCount; i++)
                    result.Add(historyBuffer[i]);
            }
            else
            {
                // Buffer has wrapped — oldest element is at historyHead
                for (int i = 0; i < maxHistorySize; i++)
                    result.Add(historyBuffer[(historyHead + i) % maxHistorySize]);
            }
            return result;
        }

        /// <summary>
        /// Called by EventBus before each dispatch.
        /// Returns true when the event should proceed this frame.
        /// Returns false and enqueues a retry when the per-frame cap is reached.
        /// </summary>
        internal static bool TryBeginEventDispatch()
        {
            eventsThisFrame++;
            return eventsThisFrame <= maxEventsPerFrame;
        }

        /// <summary>
        /// Enqueue a dispatch action to run next frame when the cap is hit.
        /// </summary>
        internal static void EnqueueOverflow(Action dispatch)
        {
            overflowQueue.Enqueue(dispatch);
            if (overflowQueue.Count >= OverflowWarnSize)
            {
                Debug.LogWarning(
                    $"[EventFlow] Overflow queue has {overflowQueue.Count} pending events. " +
                    "This may indicate an event storm or an event triggering itself recursively.");
            }
        }

        /// <summary>
        /// Records an event in the ring buffer.
        /// </summary>
        internal static void RecordEvent(Type eventType, string eventData, bool wasBlocked)
        {
            var record = historyBuffer[historyHead] ?? new EventRecord();
            record.eventType  = eventType.Name;
            record.eventData  = eventData;
            record.time       = Time.time;
            record.frameCount = Time.frameCount;
            record.wasBlocked = wasBlocked;
            historyBuffer[historyHead] = record;

            historyHead = (historyHead + 1) % maxHistorySize;
            if (historyCount < maxHistorySize) historyCount++;
        }

        public static void ClearHistory()
        {
            Array.Clear(historyBuffer, 0, historyBuffer.Length);
            historyHead  = 0;
            historyCount = 0;
        }

        /// <summary>
        /// Called once per frame by GlobalEventSystem.Update.
        /// Resets the per-frame counter and drains up to maxEventsPerFrame overflow events.
        /// </summary>
        internal static void ResetFrame()
        {
            eventsThisFrame = 0;
            DrainOverflow();
        }

        internal static void ResetAll()
        {
            eventsThisFrame = 0;
            overflowQueue.Clear();
            ClearHistory();
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private static void DrainOverflow()
        {
            int budget = maxEventsPerFrame;
            while (overflowQueue.Count > 0 && budget-- > 0)
            {
                overflowQueue.Dequeue().Invoke();
            }
        }
    }
}