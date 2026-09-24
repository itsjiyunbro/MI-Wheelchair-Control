using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Explicit batch-only Play Mode verification; inert during ordinary Editor use.
    [InitializeOnLoad]
    public static class Step05SimulationValidation
    {
        private const string ActiveKey = "EEG.Step05.ValidationActive";
        private const string FinishedKey = "EEG.Step05.ValidationFinished";
        private static IEnumerator checks;
        private static int lastFrame = -1;
        private static Keyboard keyboard;
        private static Mouse mouse;
        private static string runtimeError;

        static Step05SimulationValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
        }

        public static void Begin()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Validation requires batch mode.");
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FinishedKey, false);
            SessionState.SetFloat("EEG.Step05.Deadline", (float)EditorApplication.timeSinceStartup + 150f);
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ActiveKey, false))
            {
                Application.logMessageReceived += OnLog;
                checks = RunChecks();
                lastFrame = -1;
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(FinishedKey, false))
            {
                SessionState.SetBool(FinishedKey, false);
                EditorApplication.Exit(SessionState.GetInt("EEG.Step05.Result", 1));
            }
        }

        private static void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = message;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            try
            {
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat("EEG.Step05.Deadline", 0f))
                    throw new TimeoutException("Step 5 Play Mode validation timed out.");
                if (checks == null || !EditorApplication.isPlaying || Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                if (!checks.MoveNext()) Finish(null);
            }
            catch (Exception error) { Finish(error); }
        }

        private static void Finish(Exception error)
        {
            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= Tick;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetBool(FinishedKey, true);
            SessionState.SetInt("EEG.Step05.Result", error == null ? 0 : 1);
            if (error != null) Debug.LogException(error);
            else Debug.Log("STEP05_PLAY_OK: real Play Mode keyboard input, Input System pointer clicks, stop gate, reset, HUD/buttons and camera passed.");
            EditorApplication.ExitPlaymode();
        }

        private static IEnumerator RunChecks()
        {
            // These are temporary test devices/settings and are never saved as project assets.
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            // A hidden batch Editor has no focused Game View; route test events to its player loop.
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            var components = SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var simulation = components.OfType<SimulationController>().Single();
            var movement = components.OfType<WheelchairMovement>().Single();
            var hud = components.OfType<WheelchairStatusUI>().Single();
            var camera = components.OfType<CameraFollow>().Single();
            Transform panel = hud.transform.Find("StatusPanel");
            Button start = panel.Find("StartButton").GetComponent<Button>();
            Button stop = panel.Find("StopButton").GetComponent<Button>();
            Button reset = panel.Find("ResetButton").GetComponent<Button>();
            Text State(string name) => panel.Find(name).GetComponent<Text>();
            Vector3 initialPosition = movement.transform.position;
            Quaternion initialRotation = movement.transform.rotation;
            for (int i = 0; i < 4; i++) yield return null;
            Check(!simulation.IsRunning && !movement.IsMoving && movement.TurnInput == 0f, "Initial state is not stopped.");
            Check(State("SimulationText").text == "SIMULATION: STOPPED" && start.interactable && !stop.interactable && reset.interactable,
                "Initial HUD/button states are incorrect.");
            Keys(Key.W, Key.A);
            for (int i = 0; i < 8; i++) yield return null;
            CheckPose(movement.transform, initialPosition, initialRotation, "Initial stopped keyboard gate failed.");
            // A direct non-keyboard producer must also be blocked.
            movement.ApplyInput(true, 1f, 1f);
            CheckPose(movement.transform, initialPosition, initialRotation, "Direct command bypassed stopped simulation.");
            Keys();

            IEnumerator click = Click(start);
            while (click.MoveNext()) yield return click.Current;
            Check(simulation.IsRunning && !start.interactable && stop.interactable, "START pointer click did not start simulation.");
            Keys(Key.W);
            for (int i = 0; i < 10; i++) yield return null;
            Check(Vector3.Distance(movement.transform.position, initialPosition) > 0.001f && State("MoveText").text == "MOVE: FORWARD",
                "Running W input failed.");
            foreach (var input in new[] { new[] { Key.A }, new[] { Key.D }, new[] { Key.W, Key.A }, new[] { Key.W, Key.D }, new[] { Key.A, Key.D } })
            {
                Keys(input);
                for (int i = 0; i < 5; i++) yield return null;
                bool forward = input.Contains(Key.W);
                string steering = input.Contains(Key.A) == input.Contains(Key.D) ? "STRAIGHT" : input.Contains(Key.A) ? "LEFT" : "RIGHT";
                Check(State("MoveText").text == "MOVE: " + (forward ? "FORWARD" : "STOPPED")
                    && State("SteeringText").text == "STEERING: " + steering, "Keyboard combination HUD failed.");
                Vector3 view = camera.GetComponent<Camera>().WorldToViewportPoint(movement.transform.position);
                Check(view.z > 0f && view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f, "Camera lost target.");
            }
            Keys(Key.W, Key.D);
            click = Click(stop);
            while (click.MoveNext()) yield return click.Current;
            Vector3 stoppedPosition = movement.transform.position;
            Quaternion stoppedRotation = movement.transform.rotation;
            Check(!simulation.IsRunning && !movement.IsMoving && movement.TurnInput == 0f, "STOP did not clear motion immediately.");
            for (int i = 0; i < 8; i++) yield return null;
            CheckPose(movement.transform, stoppedPosition, stoppedRotation, "Held input moved the Cube after STOP.");
            Check(State("MoveText").text == "MOVE: STOPPED" && State("SteeringText").text == "STEERING: STRAIGHT", "Stopped HUD is stale.");
            Keys();
            click = Click(start);
            while (click.MoveNext()) yield return click.Current;
            // Exercise distant reset in addition to keyboard movement, without changing the initial snapshot.
            movement.ApplyInput(true, 1f, 5f);
            Keys(Key.W, Key.A);
            for (int i = 0; i < 8; i++) yield return null;
            click = Click(reset);
            while (click.MoveNext()) yield return click.Current;
            CheckPose(movement.transform, initialPosition, initialRotation, "RESET did not restore the runtime start pose.");
            Check(!simulation.IsRunning && !movement.IsMoving && movement.TurnInput == 0f, "RESET did not leave simulation stopped.");
            var cameraData = new SerializedObject(camera);
            Vector3 expectedCamera = initialPosition + Quaternion.Euler(0f, initialRotation.eulerAngles.y, 0f)
                * cameraData.FindProperty("offset").vector3Value;
            Check(Vector3.Distance(camera.transform.position, expectedCamera) < 0.002f, "Camera did not snap on RESET.");
            for (int i = 0; i < 8; i++) yield return null;
            CheckPose(movement.transform, initialPosition, initialRotation, "Held W moved the Cube after RESET.");
            Check(State("SimulationText").text == "SIMULATION: STOPPED" && start.interactable && !stop.interactable && reset.interactable,
                "RESET HUD/button state is wrong.");
            Keys();
            CheckCustomInitialPose();
            Canvas.ForceUpdateCanvases();
            foreach (Text text in hud.GetComponentsInChildren<Text>())
                Check(text.preferredWidth <= text.rectTransform.rect.width && text.preferredHeight <= text.rectTransform.rect.height,
                    "UI text does not fit: " + text.name);
            string[] args = Environment.GetCommandLineArgs();
            int previewIndex = Array.IndexOf(args, "-hudPreviewPath");
            if (previewIndex >= 0 && previewIndex + 1 < args.Length)
            {
                // Reuse only the existing offscreen preview helper; no earlier setup is executed.
                Type previewType = Type.GetType("EEGWheelchairSimulator.Editor.Step04HudValidation, Assembly-CSharp-Editor");
                previewType?.GetMethod("RenderPreview", BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, new object[] { hud.GetComponent<Canvas>(), args[previewIndex + 1] });
            }
            Check(runtimeError == null, "Runtime Console error: " + runtimeError);
        }

        private static void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        }

        private static IEnumerator Click(Button button)
        {
            Check(button.interactable, "Test attempted to click disabled button: " + button.name);
            Canvas.ForceUpdateCanvases();
            var rect = button.transform as RectTransform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            // Actual InputSystem events go through EventSystem raycasting and persistent Button.onClick.
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            for (int i = 0; i < 2; i++) yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
            for (int i = 0; i < 2; i++) yield return null;
            LogPointer(button, point, "pressed");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            for (int i = 0; i < 3; i++) yield return null;
            LogPointer(button, point, "released");
        }

        private static void LogPointer(Button button, Vector2 point, string phase)
        {
            var system = EventSystem.current;
            var module = system.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var hits = new System.Collections.Generic.List<RaycastResult>();
            system.RaycastAll(new PointerEventData(system) { position = point }, hits);
            Debug.Log($"STEP05_POINTER {phase}: target={button.name} point={point} screen={Screen.width}x{Screen.height} device={mouse.position.ReadValue()} pressed={mouse.leftButton.isPressed} module={system.currentInputModule} focused={Application.isFocused} pointEnabled={module.point.action.enabled} clickEnabled={module.leftClick.action.enabled} actionPoint={module.point.action.ReadValue<Vector2>()} actionClick={module.leftClick.action.ReadValue<float>()} hits={string.Join(",", hits.Select(hit => hit.gameObject.name))}");
        }

        private static void CheckCustomInitialPose()
        {
            var go = new GameObject("Step05CustomPoseCheck");
            go.SetActive(false);
            try
            {
                go.transform.SetPositionAndRotation(new Vector3(3f, 0.75f, -2f), Quaternion.Euler(0f, 37f, 0f));
                var movement = go.AddComponent<WheelchairMovement>();
                var simulation = go.AddComponent<SimulationController>();
                var movementData = new SerializedObject(movement);
                movementData.FindProperty("simulation").objectReferenceValue = simulation;
                movementData.ApplyModifiedPropertiesWithoutUndo();
                var simulationData = new SerializedObject(simulation);
                simulationData.FindProperty("movement").objectReferenceValue = movement;
                simulationData.ApplyModifiedPropertiesWithoutUndo();
                go.SetActive(true);
                simulation.StartSimulation();
                movement.ApplyInput(true, -1f, 2f);
                simulation.ResetSimulation();
                CheckPose(go.transform, new Vector3(3f, 0.75f, -2f), Quaternion.Euler(0f, 37f, 0f), "Reset used a hardcoded pose.");
                Check(!simulation.IsRunning, "Custom pose reset must stop simulation.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        private static void CheckPose(Transform target, Vector3 position, Quaternion rotation, string message)
        {
            Check(Vector3.Distance(target.position, position) < 0.001f && Quaternion.Angle(target.rotation, rotation) < 0.05f, message);
        }

        private static void Check(bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException(message);
        }
    }
}
