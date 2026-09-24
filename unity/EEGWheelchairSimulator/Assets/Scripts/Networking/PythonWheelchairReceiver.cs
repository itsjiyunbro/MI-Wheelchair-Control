using System;
using System.Globalization;
using UnityEngine;

namespace EEGWheelchairSimulator
{
    // Temporary development protocol / v0.2 (v0.1 compatible); not the final model protocol.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WheelchairMovement))]
    public sealed class PythonWheelchairReceiver : MonoBehaviour
    {
        [SerializeField] private SimulationController simulation;
        [SerializeField] private WheelchairMovement movement;
        [SerializeField, Range(1, 65535)] private int port = 5055;
        [SerializeField] private bool logReceivedMessages = true;

        [Serializable]
        private sealed class Packet
        {
            public string command;
            public double confidence = double.NaN;
            public double timestamp = double.NaN;
            // Missing sequence is accepted for v0.1. NaN is an internal missing-field sentinel.
            public double sequence = double.NaN;
        }

        private TcpNdjsonListener transport;
        private string activeCommand = "STOP";
        private int observedConnection;
        private bool observedConnected, observedListening;
        private string observedWarning;

        // Main-thread snapshot only: HUD never reads the worker's state directly.
        public bool IsListening { get; private set; }
        public bool IsConnected { get; private set; }
        public bool HasPrediction => LastReceivedCommand != null;
        public string CurrentCommand => activeCommand;
        public string LastReceivedCommand { get; private set; }
        public double LastConfidence { get; private set; }
        public double LastTimestamp { get; private set; }
        public long? LastSequence { get; private set; }
        public int ReceivedCount { get; private set; }

        private void OnEnable()
        {
            if (movement == null) movement = GetComponent<WheelchairMovement>();
            if (simulation == null)
            {
                Debug.LogWarning("Python receiver needs the existing SimulationController reference.", this);
                return;
            }
            simulation.InputStateCleared += OnInputStateCleared;
            OnInputStateCleared();
        }

        private void OnDisable()
        {
            if (simulation != null) simulation.InputStateCleared -= OnInputStateCleared;
            ClearCommand();
            CloseListener();
        }

        private void ClearCommand()
        {
            activeCommand = "STOP";
            if (movement != null && movement.ControlSource == WheelchairControlSource.Python)
                movement.ClearInputState();
        }

        private void OnInputStateCleared()
        {
            ClearCommand();
            if (!isActiveAndEnabled || simulation.ControlSource != WheelchairControlSource.Python)
            {
                CloseListener();
                return;
            }
            if (transport == null)
            {
                transport = new TcpNdjsonListener(Mathf.Clamp(port, 1, 65535));
                observedConnection = 0;
                observedConnected = observedListening = false;
                observedWarning = null;
            }
            transport.InvalidateCommands();
        }

        private void CloseListener()
        {
            transport?.Dispose();
            transport = null;
            IsListening = IsConnected = false;
            ClearPrediction();
        }

        private void ClearPrediction()
        {
            LastReceivedCommand = null;
            LastConfidence = LastTimestamp = 0;
            LastSequence = null;
        }

        private void ObserveState(TcpNdjsonListener.State state)
        {
            if (state.Connection != observedConnection || !state.Connected)
            {
                ClearCommand();
                ClearPrediction();
            }
            IsListening = state.Listening;
            IsConnected = state.Connected;
            if (state.Listening && !observedListening)
                Debug.Log($"[TCP v0.2 temporary] WAITING at 127.0.0.1:{port}", this);
            if (state.Connected != observedConnected || state.Connection != observedConnection)
                Debug.Log(state.Connected ? "[TCP v0.2 temporary] CONNECTED" : "[TCP v0.2 temporary] Disconnected; command/prediction cleared, WAITING.", this);
            if (state.Warning != null && state.Warning != observedWarning)
                Debug.LogWarning("[TCP v0.2 temporary] " + state.Warning, this);
            observedConnection = state.Connection;
            observedConnected = state.Connected;
            observedListening = state.Listening;
            observedWarning = state.Warning;
        }

        private void Update()
        {
            if (simulation == null || simulation.ControlSource != WheelchairControlSource.Python || transport == null) return;
            var state = transport.Snapshot;
            ObserveState(state);

            // All JSON parsing, logging and component access happens on Unity's main thread.
            for (int i = 0; i < 64 && transport.TryDequeue(out var message); i++)
            {
                state = transport.Snapshot;
                ObserveState(state);
                if (message.Epoch != state.Epoch || message.Connection != state.Connection || !state.Connected) continue;
                if (!TryParse(message.Line, out Packet packet)) continue;
                LastReceivedCommand = packet.command;
                LastConfidence = packet.confidence;
                LastTimestamp = packet.timestamp;
                LastSequence = double.IsNaN(packet.sequence) ? (long?)null : (long)packet.sequence;
                ReceivedCount++;
                if (logReceivedMessages)
                    Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "[TCP v0.2 temporary] command={0}, confidence={1}, timestamp={2:F3}, sequence={3}; simulation={4}",
                        packet.command, packet.confidence, packet.timestamp, LastSequence?.ToString() ?? "- (v0.1)",
                        simulation.IsRunning ? "RUNNING" : "STOPPED (display only)"), this);
                if (simulation.IsRunning) activeCommand = packet.command;
            }
            ObserveState(transport.Snapshot);
            if (!simulation.IsRunning || !IsConnected) ClearCommand();
            float turn = activeCommand == "LEFT" ? -1f : activeCommand == "RIGHT" ? 1f : 0f;
            movement.ApplyInput(activeCommand == "FORWARD", turn, Time.deltaTime, WheelchairControlSource.Python);
        }

        private bool TryParse(string line, out Packet packet)
        {
            packet = new Packet();
            try
            {
                string json = line.Trim();
                if (!json.StartsWith("{", StringComparison.Ordinal) || !json.EndsWith("}", StringComparison.Ordinal))
                    throw new ArgumentException("Expected one JSON object per line.");
                JsonUtility.FromJsonOverwrite(json, packet);
                if (double.IsNaN(packet.confidence) || double.IsInfinity(packet.confidence)
                    || double.IsNaN(packet.timestamp) || double.IsInfinity(packet.timestamp))
                    throw new ArgumentException("confidence and timestamp must be present and finite numbers.");
                if (packet.confidence < 0 || packet.confidence > 1)
                    throw new ArgumentException("confidence must be in [0, 1]. This is format validation, not a movement threshold.");
                if (!double.IsNaN(packet.sequence) && (double.IsInfinity(packet.sequence) || packet.sequence < 1
                    || packet.sequence > 9007199254740991d || Math.Floor(packet.sequence) != packet.sequence))
                    throw new ArgumentException("Optional sequence must be a positive, exactly representable integer.");
                if (packet.command != "LEFT" && packet.command != "RIGHT" && packet.command != "FORWARD" && packet.command != "STOP")
                    throw new ArgumentException("Unknown or missing command; allowed: LEFT, RIGHT, FORWARD, STOP.");
                return true;
            }
            catch (ArgumentException error)
            {
                Debug.LogWarning("[TCP v0.2 temporary] Ignored message: " + error.Message, this);
                return false;
            }
        }
    }
}
