using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
            GetOrCreatePipeline<T>().AddStep(step);
        }

        public static IReadOnlyDictionary<Type, object> GetPipelines()
        {
            return readonlyPipelines;
        }

        public static List<string> GetPipelineSteps(Type eventType)
        {
            if (!pipelines.TryGetValue(eventType, out object pipelineObject))
            {
                return null;
            }

            if (pipelineObject is IEventPipelineInfo pipelineInfo)
            {
                return new List<string>(pipelineInfo.GetStepNames());
            }

            return new List<string>();
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
            IReadOnlyList<string> GetStepNames();
        }

        private sealed class EventPipeline<T> : IEventPipelineInfo where T : IEvent
        {
            private readonly List<EventStep<T>> steps = new List<EventStep<T>>();

            public void AddStep(EventStep<T> step)
            {
                if (step != null)
                {
                    steps.Add(step);
                }
            }

            public bool TryProcess(ref T eventData)
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    if (steps[i](ref eventData) == FlowResult.Stop)
                    {
                        return false;
                    }
                }

                return true;
            }

            public IReadOnlyList<string> GetStepNames()
            {
                List<string> names = new List<string>(steps.Count);

                for (int i = 0; i < steps.Count; i++)
                {
                    names.Add(steps[i].Method.Name);
                }

                return names;
            }
        }
    }
}
