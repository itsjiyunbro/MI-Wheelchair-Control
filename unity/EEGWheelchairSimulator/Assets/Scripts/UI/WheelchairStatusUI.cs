using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

namespace EEGWheelchairSimulator
{
    [DisallowMultipleComponent]
    public sealed class WheelchairStatusUI : MonoBehaviour
    {
        [SerializeField] private WheelchairMovement movement;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private Text simulationText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Text controlText;
        [SerializeField] private Text moveText;
        [SerializeField] private Text steeringText;
        [SerializeField] private Text headingText;
        [SerializeField] private PythonWheelchairReceiver receiver;
        [SerializeField] private Text connectionText;
        [SerializeField] private Text predictionText;
        [SerializeField] private Text confidenceText;

        private bool hasDisplayed;
        private string lastControl;
        private bool lastMoving;
        private int lastSteering;
        private int lastHeading;
        private bool lastRunning;
        private string lastConnection, lastPrediction;
        private int lastConfidenceTenths = -1;

        private void OnEnable()
        {
            hasDisplayed = false;
        }

        private void LateUpdate()
        {
            RefreshDisplay();
        }

        // Also used by the optional Editor setup to populate the saved initial labels.
        public void RefreshDisplay()
        {
            if (controlText == null || moveText == null || steeringText == null || headingText == null)
                return;

            bool moving = movement != null && movement.IsMoving;
            float turn = movement != null ? movement.TurnInput : 0f;
            int steering = turn < 0f ? -1 : turn > 0f ? 1 : 0;
            // Rounding 359.9 degrees must wrap to 0, never display 360.
            int heading = movement != null ? Mathf.RoundToInt(movement.HeadingDegrees) % 360 : -1;
            bool running = simulation != null && simulation.IsRunning;
            string controlSourceLabel = simulation != null && simulation.ControlSource == WheelchairControlSource.Python
                ? "PYTHON" : "KEYBOARD";

            if (simulationText != null && (!hasDisplayed || lastRunning != running))
                simulationText.text = running ? "SIMULATION: RUNNING" : "SIMULATION: STOPPED";
            bool controllerAvailable = simulation != null && simulation.isActiveAndEnabled;
            if (startButton != null) startButton.interactable = controllerAvailable && !running;
            if (stopButton != null) stopButton.interactable = controllerAvailable && running;
            if (resetButton != null) resetButton.interactable = controllerAvailable;

            if (!hasDisplayed || lastControl != controlSourceLabel)
                controlText.text = "CONTROL: " + controlSourceLabel;
            if (!hasDisplayed || lastMoving != moving)
                moveText.text = moving ? "MOVE: FORWARD" : "MOVE: STOPPED";
            if (!hasDisplayed || lastSteering != steering)
                steeringText.text = steering < 0 ? "STEERING: LEFT" : steering > 0 ? "STEERING: RIGHT" : "STEERING: STRAIGHT";
            if (!hasDisplayed || lastHeading != heading)
                headingText.text = heading >= 0 ? "HEADING: " + heading + "\u00B0" : "HEADING: --";

            bool python = simulation != null && simulation.ControlSource == WheelchairControlSource.Python;
            bool connected = python && receiver != null && receiver.IsConnected;
            bool hasPrediction = connected && receiver.HasPrediction;
            string connection = !python ? "-" : connected ? "CONNECTED" : "WAITING";
            string prediction = hasPrediction ? receiver.LastReceivedCommand : "-";
            int confidenceTenths = hasPrediction ? (int)System.Math.Round(receiver.LastConfidence * 1000) : -1;
            if (connectionText != null && (!hasDisplayed || lastConnection != connection))
                connectionText.text = "CONNECTION: " + connection;
            if (predictionText != null && (!hasDisplayed || lastPrediction != prediction))
                predictionText.text = "PREDICTION: " + prediction;
            if (confidenceText != null && (!hasDisplayed || lastConfidenceTenths != confidenceTenths))
                confidenceText.text = confidenceTenths < 0 ? "CONFIDENCE: -"
                    : "CONFIDENCE: " + (confidenceTenths / 10.0).ToString("F1", CultureInfo.InvariantCulture) + "%";
            lastConnection = connection;
            lastPrediction = prediction;
            lastConfidenceTenths = confidenceTenths;

            hasDisplayed = true;
            lastControl = controlSourceLabel;
            lastMoving = moving;
            lastSteering = steering;
            lastHeading = heading;
            lastRunning = running;
        }
    }
}
