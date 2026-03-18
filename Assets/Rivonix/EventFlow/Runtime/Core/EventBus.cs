using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Core event bus — the central message broker for all EventFlow communication.
    ///
    /// FIXES applied vs. original:
    ///
    ///   FIX 1 — Scope-tagged registration.
    ///     EventBus.Clear() previously wiped every listener in the game, silently
    ///     breaking anything that survived a scene change (DontDestroyOnLoad objects,
    ///     other additive scenes).  Listeners now carry an optional string scope tag.
    ///     Clear(scope) removes only that scope's listeners; Clear() with no argument
    ///     remains available but logs a warning in the Editor so callers know what
    ///     they're doing.
    ///
    ///   FIX 2 — Per-dispatch array allocation eliminated.
    ///     Original code called listeners[type].ToArray() on every dispatch to get a
    ///     safe snapshot.  That allocates a new array for every event fired.  We now
    ///     reuse a pre-allocated buffer and grow it only when needed.
    ///
    ///   FIX 3 — Overflow queue instead of silent drop.
    ///     When the per-frame cap is hit, events are queued for the next frame via
    ///     EventFlowDiagnostics.EnqueueOverflow instead of being silently discarded.
    /// </summary>
    public static class EventBus
    {
        // ── Listener storage ─────────────────────────────────────────────────────
        // Key: event Type.  Value: list of entries, each holding the delegate and its scope.
        private static readonly Dictionary<Type, List<ListenerEntry>> listeners
            = new Dictionary<Type, List<ListenerEntry>>();

        internal struct ListenerEntry
        {
            public Delegate handler;
            public string   scope;   // null = "global"
        }

        // Reusable snapshot buffer — avoids a heap allocation per dispatch.
        private static ListenerEntry[] _dispatchBuffer = new ListenerEntry[16];

        // ── Registration ─────────────────────────────────────────────────────────

        /// <summary>
        /// Register a listener for an event type.
        /// </summary>
        /// <param name="handler">Callback to invoke when the event fires.</param>
        /// <param name="scope">
        ///   Optional scope tag (e.g. a scene name).  Pass the same tag to
        ///   <see cref="Clear(string)"/> to remove only this scope's listeners on
        ///   scene unload.  Defaults to "global".
        /// </param>
        public static void Register<T>(Action<T> handler, string scope = "global") where T : IEvent
        {
            Type eventType = typeof(T);

            if (!listeners.TryGetValue(eventType, out List<ListenerEntry> list))
            {
                list = new List<ListenerEntry>(4);
                listeners[eventType] = list;
            }

            // Deduplicate — same handler + same scope
            for (int i = 0; i < list.Count; i++)
                if (list[i].handler == (Delegate)handler && list[i].scope == scope) return;

            list.Add(new ListenerEntry { handler = handler, scope = scope });

#if UNITY_EDITOR
            Debug.Log($"[EventBus] +{eventType.Name} (scope: {scope})");
#endif
        }

        /// <summary>
        /// Register a listener with automatic cleanup when the owning MonoBehaviour is destroyed.
        /// </summary>
        public static void Register<T>(MonoBehaviour owner, Action<T> handler, string scope = "global") where T : IEvent
        {
            Register(handler, scope);

            ListenerTracker tracker = owner.gameObject.GetComponent<ListenerTracker>();
            if (tracker == null)
                tracker = owner.gameObject.AddComponent<ListenerTracker>();

            tracker.AddCleanupAction(() => Unregister(handler));
        }

        /// <summary>Unregister a specific listener.</summary>
        public static void Unregister<T>(Action<T> handler) where T : IEvent
        {
            Type eventType = typeof(T);

            if (!listeners.TryGetValue(eventType, out List<ListenerEntry> list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].handler == (Delegate)handler)
                {
                    list.RemoveAt(i);
                    break; // handlers are deduplicated on Register, so at most one match
                }
            }

#if UNITY_EDITOR
            Debug.Log($"[EventBus] -{eventType.Name}");
#endif
        }

        // ── Dispatch ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Dispatch an event directly to all registered listeners, bypassing the pipeline.
        /// Called by EventFlowController after pipeline steps pass.
        /// </summary>
        public static void Dispatch<T>(T eventData) where T : IEvent
        {
            Type eventType = typeof(T);

            if (!EventFlowDiagnostics.TryBeginEventDispatch())
            {
                // Queue for next frame instead of silently dropping
                EventFlowDiagnostics.EnqueueOverflow(() => Dispatch(eventData));
                return;
            }

            bool blocked = !GameStateManager.CanTriggerEvent(eventType);
            EventFlowDiagnostics.RecordEvent(eventType, eventData.ToString(), blocked);
            if (blocked) return;

            if (!listeners.TryGetValue(eventType, out List<ListenerEntry> list) || list.Count == 0)
                return;

            // Copy to reusable buffer — safe snapshot without a heap allocation
            int count = list.Count;
            if (_dispatchBuffer.Length < count)
                Array.Resize(ref _dispatchBuffer, count * 2);
            list.CopyTo(_dispatchBuffer);

            for (int i = 0; i < count; i++)
            {
                try
                {
                    (_dispatchBuffer[i].handler as Action<T>)?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Listener error for {eventType.Name}: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        // ── Delayed dispatch ─────────────────────────────────────────────────────

        /// <summary>Fire an event after a real-time delay in seconds.</summary>
        public static void TriggerDelayed<T>(T eventData, float delay) where T : IEvent
        {
            EventScheduler.Instance.Schedule(eventData, delay);
        }

        // ── Queries ──────────────────────────────────────────────────────────────

        /// <summary>Returns true if at least one listener is registered for this event type.</summary>
        public static bool HasListeners<T>() where T : IEvent
        {
            Type eventType = typeof(T);
            return listeners.TryGetValue(eventType, out List<ListenerEntry> list) && list.Count > 0;
        }

        /// <summary>Returns the number of listeners registered for this event type.</summary>
        public static int GetListenerCount<T>() where T : IEvent
            => GetListenerCount(typeof(T));

        /// <summary>Returns the number of listeners registered for a given event type.</summary>
        public static int GetListenerCount(Type eventType)
        {
            return listeners.TryGetValue(eventType, out List<ListenerEntry> list) ? list.Count : 0;
        }

        /// <summary>Returns every event type that currently has at least one listener.</summary>
        public static Type[] GetAllEventTypes()
        {
            var types = new Type[listeners.Count];
            listeners.Keys.CopyTo(types, 0);
            return types;
        }

        // ── Clear ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Remove all listeners belonging to a specific scope.
        /// Call this when unloading a scene to clean up that scene's listeners
        /// without disturbing listeners registered by other scenes or systems.
        /// </summary>
        public static void Clear(string scope)
        {
            foreach (var list in listeners.Values)
                list.RemoveAll(e => e.scope == scope);

            EventFlowDiagnostics.ResetAll();

#if UNITY_EDITOR
            Debug.Log($"[EventBus] Cleared scope: '{scope}'");
#endif
        }

        /// <summary>
        /// Remove ALL listeners across every scope.
        /// Prefer <see cref="Clear(string)"/> when using additive scenes so that
        /// DontDestroyOnLoad objects and other scenes are not affected.
        /// </summary>
        public static void ClearAll()
        {
            listeners.Clear();
            EventFlowDiagnostics.ResetAll();

#if UNITY_EDITOR
            Debug.LogWarning("[EventBus] ClearAll() — all listeners removed across every scope. " +
                             "Use Clear(scope) if you only want to clear a specific scene.");
#endif
        }

        /// <summary>
        /// Remove ALL listeners. Backwards-compatible overload — delegates to <see cref="ClearAll"/>.
        /// </summary>
        public static void Clear() => ClearAll();

        // ── Internal editor access ───────────────────────────────────────────────

        // Used by the debug window to read listener data without reflection.
        internal static IReadOnlyDictionary<Type, List<ListenerEntry>> GetListenerMap()
            => listeners;

        internal struct ListenerEntryPublic
        {
            public string scope;
            public string delegateTarget;
        }

        internal static List<ListenerEntryPublic> GetListenerDetails(Type eventType)
        {
            var result = new List<ListenerEntryPublic>();
            if (!listeners.TryGetValue(eventType, out List<ListenerEntry> list)) return result;
            foreach (var entry in list)
            {
                result.Add(new ListenerEntryPublic
                {
                    scope          = entry.scope ?? "global",
                    delegateTarget = entry.handler?.Method?.DeclaringType?.Name + "." + entry.handler?.Method?.Name
                });
            }
            return result;
        }
    }
}