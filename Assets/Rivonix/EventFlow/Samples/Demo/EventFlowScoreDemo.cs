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
                EventFlow.AddStep<DemoScoreEvent>("Validate Score", Validate, 10);
                EventFlow.AddStep<DemoScoreEvent>("Apply Multiplier", ApplyMultiplier, 20);
                EventFlow.AddStep<DemoScoreEvent>("Clamp Score", ClampScore, 30);
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
            GUILayout.BeginArea(new Rect(20f, 20f, 540f, 220f), GUI.skin.window);

            // Title
            GUILayout.Label("<b>Rivonix EventFlow - Score Pipeline Demo</b>", GetRichLabelStyle(16));

            GUILayout.Space(6f);

            // Controls
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("<b>Controls</b>", GetRichLabelStyle(13));
            GUILayout.Label("SPACE  → Valid Event");
            GUILayout.Label("BACKSPACE → Blocked Event");
            GUILayout.Label("R → Reset");
            GUILayout.EndVertical();

            GUILayout.Space(6f);

            // Runtime Data
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("<b>Runtime Data</b>", GetRichLabelStyle(13));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Last Input:", GUILayout.Width(120));
            GUILayout.Label(lastInput.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Last Output:", GUILayout.Width(120));
            GUILayout.Label(lastOutput.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Score:", GUILayout.Width(120));
            GUILayout.Label(totalScore.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Status:", GUILayout.Width(120));
            GUILayout.Label(GetStatusText(), GetStatusStyle());
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.EndArea();
        }
        private GUIStyle GetRichLabelStyle(int fontSize)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.fontSize = fontSize;
            style.fontStyle = FontStyle.Bold;
            return style;
        }

        private GUIStyle GetStatusStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;

            switch (lastStatus)
            {
                case "Blocked":
                    style.normal.textColor = Color.red;
                    break;
                case "Processed":
                    style.normal.textColor = Color.green;
                    break;
                default:
                    style.normal.textColor = Color.white;
                    break;
            }

            return style;
        }

        private string GetStatusText()
        {
            return lastStatus;
        }
    }
}

