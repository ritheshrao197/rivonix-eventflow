using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Rivonix.EventFlow
{
    /// <summary>
    /// Coordinates event pipeline execution before events reach the dispatcher.
    /// </summary>
    public static class EventFlowController
    {
        private static readonly Dictionary<Type, object> pipelines = new Dictionary<Type, object>();
        private static readonly ReadOnlyDictionary<Type, object> readonlyPipelines =
            new ReadOnlyDictionary<Type, object>(pipelines);

        public static void Execute<T>(T eventData) where T : IEvent
        {
            if (pipelines.TryGetValue(typeof(T), out object pipelineObject))
            {
                var pipeline = (EventPipeline<T>)pipelineObject;
                if (!pipeline.TryProcess(ref eventData))
                {
                    return;
                }
            }

            EventBus.Dispatch(eventData);
        }

        public static void AddStep<T>(EventStep<T> step) where T : IEvent
        {
            AddStep(step.Method.Name, step, 0, true);
        }

        public static void AddStep<T>(string name, EventStep<T> step, int priority = 0, bool enabled = true) where T : IEvent
        {
            GetOrCreatePipeline<T>().AddStep(name, step, priority, enabled);
        }

        public static IReadOnlyDictionary<Type, object> GetPipelines()
        {
            return readonlyPipelines;
        }

        public static IReadOnlyList<PipelineStepInfo> GetPipelineSteps(Type eventType)
        {
            if (!pipelines.TryGetValue(eventType, out object pipelineObject))
            {
                return null;
            }

            if (pipelineObject is IEventPipelineInfo pipelineInfo)
            {
                return pipelineInfo.GetStepInfos();
            }

            return Array.Empty<PipelineStepInfo>();
        }

        private static EventPipeline<T> GetOrCreatePipeline<T>() where T : IEvent
        {
            Type eventType = typeof(T);
            if (!pipelines.TryGetValue(eventType, out object pipelineObject))
            {
                pipelineObject = new EventPipeline<T>();
                pipelines[eventType] = pipelineObject;
            }

            return (EventPipeline<T>)pipelineObject;
        }

        private interface IEventPipelineInfo
        {
            IReadOnlyList<PipelineStepInfo> GetStepInfos();
        }

        private sealed class EventPipeline<T> : IEventPipelineInfo where T : IEvent
        {
            private readonly List<PipelineStep<T>> steps = new List<PipelineStep<T>>();
            private readonly List<PipelineStepInfo> stepInfos = new List<PipelineStepInfo>();

            public void AddStep(string name, EventStep<T> step, int priority, bool enabled)
            {
                if (step != null)
                {
                    steps.Add(new PipelineStep<T>(name, step, priority, enabled));
                    steps.Sort((left, right) => left.Priority.CompareTo(right.Priority));
                    RebuildStepInfos();
                }
            }

            public bool TryProcess(ref T eventData)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    PipelineStep<T> step = steps[i];
                    if (!step.Enabled)
                    {
                        continue;
                    }

                    try
                    {
                        if (step.Action(ref eventData) == FlowResult.Stop)
                        {
                            return false;
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[EventFlow] Pipeline step '{step.Name}' failed for {typeof(T).Name}: {exception.Message}\n{exception.StackTrace}");
                        return false;
                    }
                }

                return true;
            }

            public IReadOnlyList<PipelineStepInfo> GetStepInfos()
            {
                return stepInfos;
            }

            private void RebuildStepInfos()
            {
                stepInfos.Clear();

                for (int i = 0; i < steps.Count; i++)
                {
                    PipelineStep<T> step = steps[i];
                    stepInfos.Add(new PipelineStepInfo(step.Name, step.Priority, step.Enabled, i + 1));
                }
            }
        }

        private readonly struct PipelineStep<TStepEvent> where TStepEvent : IEvent
        {
            public PipelineStep(string name, EventStep<TStepEvent> action, int priority, bool enabled)
            {
                Name = string.IsNullOrWhiteSpace(name) ? action.Method.Name : name;
                Action = action;
                Priority = priority;
                Enabled = enabled;
            }

            public string Name { get; }
            public EventStep<TStepEvent> Action { get; }
            public int Priority { get; }
            public bool Enabled { get; }
        }
    }

    public readonly struct PipelineStepInfo
    {
        public PipelineStepInfo(string name, int priority, bool enabled, int order)
        {
            Name = name;
            Priority = priority;
            Enabled = enabled;
            Order = order;
        }

        public string Name { get; }
        public int Priority { get; }
        public bool Enabled { get; }
        public int Order { get; }
    }
}
