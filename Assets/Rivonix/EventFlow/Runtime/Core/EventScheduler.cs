using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Handles delayed and repeating events.
    /// Automatically created when first used.
    /// </summary>
    public class EventScheduler : MonoBehaviour
    {
        private static EventScheduler _instance;
        
        /// <summary>
        /// Singleton instance of the EventScheduler
        /// </summary>
        public static EventScheduler Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("EventScheduler");
                    _instance = go.AddComponent<EventScheduler>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private List<ScheduledEvent> scheduledEvents = new List<ScheduledEvent>();
        private List<RepeatingEvent> repeatingEvents = new List<RepeatingEvent>();
        
        private class ScheduledEvent
        {
            public IEvent eventData;
            public Type eventType;
            public float triggerTime;
        }
        
        private class RepeatingEvent
        {
            public IEvent eventData;
            public Type eventType;
            public float interval;
            public float nextTriggerTime;
            public int maxRepeats;
            public int currentRepeats;
            public string id;
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            float currentTime = Time.time;
            
            // Check scheduled events
            for (int i = scheduledEvents.Count - 1; i >= 0; i--)
            {
                if (currentTime >= scheduledEvents[i].triggerTime)
                {
                    TriggerEvent(scheduledEvents[i]);
                    scheduledEvents.RemoveAt(i);
                }
            }
            
            // Check repeating events
            for (int i = 0; i < repeatingEvents.Count; i++)
            {
                var re = repeatingEvents[i];
                if (currentTime >= re.nextTriggerTime)
                {
                    TriggerEvent(new ScheduledEvent
                    {
                        eventData = re.eventData,
                        eventType = re.eventType,
                        triggerTime = currentTime
                    });
                    
                    re.currentRepeats++;
                    re.nextTriggerTime = currentTime + re.interval;
                    
                    if (re.maxRepeats > 0 && re.currentRepeats >= re.maxRepeats)
                    {
                        repeatingEvents.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
        
        private void TriggerEvent(ScheduledEvent scheduled)
        {
            var method = typeof(EventBus).GetMethod("Trigger");
            var generic = method.MakeGenericMethod(scheduled.eventType);
            generic.Invoke(null, new object[] { scheduled.eventData });
        }
        
        /// <summary>
        /// Schedule an event to trigger after a delay
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        /// <param name="delay">Delay in seconds</param>
        public void Schedule<T>(T eventData, float delay) where T : IEvent
        {
            scheduledEvents.Add(new ScheduledEvent
            {
                eventData = eventData,
                eventType = typeof(T),
                triggerTime = Time.time + delay
            });
        }
        
        /// <summary>
        /// Schedule a repeating event
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="eventData">Event data</param>
        /// <param name="interval">Interval between triggers in seconds</param>
        /// <param name="maxRepeats">Maximum number of repeats (-1 for infinite)</param>
        /// <returns>ID that can be used to cancel the repeating event</returns>
        public string ScheduleRepeating<T>(T eventData, float interval, int maxRepeats = -1) where T : IEvent
        {
            string id = Guid.NewGuid().ToString();
            
            repeatingEvents.Add(new RepeatingEvent
            {
                eventData = eventData,
                eventType = typeof(T),
                interval = interval,
                nextTriggerTime = Time.time + interval,
                maxRepeats = maxRepeats,
                currentRepeats = 0,
                id = id
            });
            
            return id;
        }
        
        /// <summary>
        /// Cancel a repeating event by its ID
        /// </summary>
        /// <param name="id">ID returned from ScheduleRepeating</param>
        /// <returns>True if found and cancelled</returns>
        public bool CancelRepeating(string id)
        {
            for (int i = 0; i < repeatingEvents.Count; i++)
            {
                if (repeatingEvents[i].id == id)
                {
                    repeatingEvents.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Cancel all scheduled and repeating events
        /// </summary>
        public void CancelAll()
        {
            scheduledEvents.Clear();
            repeatingEvents.Clear();
        }
        
        /// <summary>
        /// Get count of pending scheduled events
        /// </summary>
        public int ScheduledCount => scheduledEvents.Count;
        
        /// <summary>
        /// Get count of active repeating events
        /// </summary>
        public int RepeatingCount => repeatingEvents.Count;
        
        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}