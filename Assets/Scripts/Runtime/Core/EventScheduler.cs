// using System;
// using System.Collections.Generic;
// using UnityEngine;

// namespace Rivonix.EventFlow
// {
//     /// <summary>
//     /// Handles delayed and repeating events.
//     /// Automatically created as a DontDestroyOnLoad singleton on first use.
//     ///
//     /// FIX — Eliminated reflection in TriggerEvent.
//     ///   Original code used GetMethod("Trigger").MakeGenericMethod(type).Invoke(...)
//     ///   on every scheduled fire.  This is slow, not compile-time safe, and breaks if
//     ///   the method is renamed.  We now capture a typed Action delegate when the event
//     ///   is scheduled, so firing is a direct call with zero overhead.
//     /// </summary>
//     public class EventScheduler : MonoBehaviour
//     {
//         // ── Singleton ────────────────────────────────────────────────────────────

//         private static EventScheduler _instance;

//         public static EventScheduler Instance
//         {
//             get
//             {
//                 if (_instance == null)
//                 {
//                     var go = new GameObject("[EventFlow] Scheduler");
//                     _instance = go.AddComponent<EventScheduler>();
//                     DontDestroyOnLoad(go);
//                 }
//                 return _instance;
//             }
//         }

//         // ── Data types ───────────────────────────────────────────────────────────

//         private class ScheduledEvent
//         {
//             public Action  dispatch;    // pre-captured typed delegate — no reflection
//             public float   triggerTime;
//         }

//         private class RepeatingEvent
//         {
//             public Action  dispatch;
//             public float   interval;
//             public float   nextTriggerTime;
//             public int     maxRepeats;   // -1 = infinite
//             public int     currentRepeats;
//             public string  id;
//         }

//         // ── State ────────────────────────────────────────────────────────────────

//         private readonly List<ScheduledEvent>  _scheduled  = new List<ScheduledEvent>();
//         private readonly List<RepeatingEvent>  _repeating  = new List<RepeatingEvent>();

//         // ── Unity lifecycle ──────────────────────────────────────────────────────

//         private void Awake()
//         {
//             if (_instance != null && _instance != this) { Destroy(gameObject); return; }
//             _instance = this;
//             DontDestroyOnLoad(gameObject);
//         }

//         private void Update()
//         {
//             float now = Time.time;

//             // One-shot scheduled events — iterate backwards so we can remove safely
//             for (int i = _scheduled.Count - 1; i >= 0; i--)
//             {
//                 if (now >= _scheduled[i].triggerTime)
//                 {
//                     _scheduled[i].dispatch.Invoke();   // direct call, no reflection
//                     _scheduled.RemoveAt(i);
//                 }
//             }

//             // Repeating events
//             for (int i = _repeating.Count - 1; i >= 0; i--)
//             {
//                 RepeatingEvent re = _repeating[i];
//                 if (now < re.nextTriggerTime) continue;

//                 re.dispatch.Invoke();                   // direct call, no reflection
//                 re.currentRepeats++;
//                 re.nextTriggerTime = now + re.interval;

//                 if (re.maxRepeats > 0 && re.currentRepeats >= re.maxRepeats)
//                     _repeating.RemoveAt(i);
//             }
//         }

//         private void OnDestroy()
//         {
//             if (_instance == this) _instance = null;
//         }

//         // ── Public API ───────────────────────────────────────────────────────────

//         /// <summary>
//         /// Schedule an event to fire through the full EventFlow pipeline after a delay.
//         /// </summary>
//         public void Schedule<T>(T eventData, float delay) where T : IEvent
//         {
//             _scheduled.Add(new ScheduledEvent
//             {
//                 dispatch    = () => EventFlow.Trigger(eventData),   // captured generic — no reflection
//                 triggerTime = Time.time + delay
//             });
//         }

//         /// <summary>
//         /// Schedule an event to fire repeatedly at a fixed interval.
//         /// </summary>
//         /// <param name="eventData">Event data snapshot sent on every trigger.</param>
//         /// <param name="interval">Seconds between triggers.</param>
//         /// <param name="maxRepeats">Stop after this many triggers. -1 = infinite.</param>
//         /// <returns>ID string you can pass to <see cref="CancelRepeating"/> to stop early.</returns>
//         public string ScheduleRepeating<T>(T eventData, float interval, int maxRepeats = -1) where T : IEvent
//         {
//             string id = Guid.NewGuid().ToString();
//             _repeating.Add(new RepeatingEvent
//             {
//                 dispatch        = () => EventFlow.Trigger(eventData),
//                 interval        = interval,
//                 nextTriggerTime = Time.time + interval,
//                 maxRepeats      = maxRepeats,
//                 currentRepeats  = 0,
//                 id              = id
//             });
//             return id;
//         }

//         /// <summary>Cancel a specific repeating event by its ID.</summary>
//         /// <returns>True if the event was found and cancelled.</returns>
//         public bool CancelRepeating(string id)
//         {
//             for (int i = 0; i < _repeating.Count; i++)
//             {
//                 if (_repeating[i].id == id)
//                 {
//                     _repeating.RemoveAt(i);
//                     return true;
//                 }
//             }
//             return false;
//         }

//         /// <summary>Cancel all pending one-shot and repeating events.</summary>
//         public void CancelAll()
//         {
//             _scheduled.Clear();
//             _repeating.Clear();
//         }

//         /// <summary>Number of pending one-shot events.</summary>
//         public int ScheduledCount  => _scheduled.Count;

//         /// <summary>Number of active repeating events.</summary>
//         public int RepeatingCount  => _repeating.Count;
//     }
// }