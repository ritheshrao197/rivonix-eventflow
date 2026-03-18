using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Internal component attached to a GameObject by EventBus.Register(owner, handler).
    /// Automatically unregisters all tracked listeners when the GameObject is destroyed.
    ///
    /// FIX — Eliminated reflection in OnDestroy.
    ///   Original code used GetMethod("Unregister").MakeGenericMethod(type).Invoke(...)
    ///   for each listener, which is slow, fragile, and boxes the delegate.
    ///   We now store a pre-built Action that calls the correct generic Unregister
    ///   overload directly, captured at registration time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ListenerTracker : MonoBehaviour
    {
        // Each entry is a zero-argument Action that calls EventBus.Unregister<T>(handler)
        // with the correct generic type already baked in — no reflection needed at cleanup.
        private readonly List<Action> _unregisterActions = new List<Action>();

        /// <summary>
        /// Called by EventBus.Register to record a cleanup action for this listener.
        /// The Action captures the handler directly, so Unregister is a direct call.
        /// </summary>
        internal void AddCleanupAction(Action unregisterAction)
        {
            _unregisterActions.Add(unregisterAction);
        }

        private void OnDestroy()
        {
            foreach (Action unregister in _unregisterActions)
            {
                try { unregister(); }
                catch (Exception e)
                {
                    Debug.LogError($"[EventFlow] ListenerTracker cleanup error on {gameObject.name}: {e.Message}");
                }
            }
            _unregisterActions.Clear();
        }
    }
}