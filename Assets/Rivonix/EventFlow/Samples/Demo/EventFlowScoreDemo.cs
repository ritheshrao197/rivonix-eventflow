using UnityEngine;

namespace Rivonix.EventFlow.Samples
{
    /// <summary>
    /// Demo event used to showcase pipeline processing.
    /// </summary>
    public struct DemoScoreEvent : IEvent
    {
        public int points;
    }

    /// <summary>
    /// Playable score-processing demo for the EventFlow pipeline.
    /// </summary>
    public class EventFlowScoreDemo : MonoBehaviour
    {
        [SerializeField] private int basePoints = 100;
        [SerializeField] private int multiplier = 2;
        [SerializeField] private int maxScore = 1000;

        private static EventFlowScoreDemo activeInstance;
        private static bool pipelineInitialized;

        private int totalScore;
        private int lastInput;
        private int lastOutput;
        private string lastStatus = "Press SPACE to trigger the pipeline.";

        private void Awake()
        {
            activeInstance = this;

            if (!pipelineInitialized)
            {
                EventFlow.AddStep<DemoScoreEvent>(Validate);
                EventFlow.AddStep<DemoScoreEvent>(ApplyMultiplier);
                EventFlow.AddStep<DemoScoreEvent>(ClampScore);
                pipelineInitialized = true;
            }
        }

        private void OnEnable()
        {
            EventFlow.Register<DemoScoreEvent>(OnScore);
        }

        private void OnDisable()
        {
            EventFlow.Unregister<DemoScoreEvent>(OnScore);
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TriggerScore(basePoints);
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                TriggerScore(-25);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                totalScore = 0;
                lastInput = 0;
                lastOutput = 0;
                lastStatus = "Demo reset.";
            }
        }

        private void TriggerScore(int points)
        {
            lastInput = points;
            lastOutput = 0;
            lastStatus = $"Triggered DemoScoreEvent with {points} points.";
            EventFlow.Trigger(new DemoScoreEvent { points = points });
        }

        private static FlowResult Validate(ref DemoScoreEvent eventData)
        {
            if (eventData.points <= 0)
            {
                if (activeInstance != null)
                {
                    activeInstance.lastStatus = $"Validate stopped the event because {eventData.points} is not positive.";
                }

                return FlowResult.Stop;
            }

            return FlowResult.Continue;
        }

        private static FlowResult ApplyMultiplier(ref DemoScoreEvent eventData)
        {
            int multiplierValue = activeInstance != null ? activeInstance.multiplier : 2;
            eventData.points *= multiplierValue;
            return FlowResult.Continue;
        }

        private static FlowResult ClampScore(ref DemoScoreEvent eventData)
        {
            int maxScoreValue = activeInstance != null ? activeInstance.maxScore : 1000;
            eventData.points = Mathf.Min(eventData.points, maxScoreValue);
            return FlowResult.Continue;
        }

        private void OnScore(DemoScoreEvent eventData)
        {
            lastOutput = eventData.points;
            totalScore += eventData.points;
            lastStatus = $"Pipeline completed. Final points: {eventData.points}.";
            Debug.Log($"[EventFlow Demo] Final Score: {eventData.points}");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20f, 20f, 520f, 180f), GUI.skin.box);
            GUILayout.Label("EventFlow Score Pipeline Demo", GUI.skin.label);
            GUILayout.Label("SPACE: valid event | BACKSPACE: blocked event | R: reset");
            GUILayout.Space(8f);
            GUILayout.Label($"Last Input: {lastInput}");
            GUILayout.Label($"Last Output: {lastOutput}");
            GUILayout.Label($"Total Score: {totalScore}");
            GUILayout.Label($"Status: {lastStatus}");
            GUILayout.EndArea();
        }
    }
}
