using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Optional batch validation, separate from the runtime HUD and the creation menu.
    public static class Step04HudValidation
    {
        public static void Run()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run these checks in batch mode with the project closed.");
            Scene scene = SceneManager.GetActiveScene();
            var source = scene.GetRootGameObjects().Single(root => root.name == "StatusCanvas");
            var chair = new GameObject("Step04StateCheck") { hideFlags = HideFlags.HideAndDontSave };
            GameObject clone = UnityEngine.Object.Instantiate(source);
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var movement = chair.AddComponent<WheelchairMovement>();
                var hud = clone.GetComponent<WheelchairStatusUI>();
                var serialized = new SerializedObject(hud);
                serialized.FindProperty("movement").objectReferenceValue = movement;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Text moveText = clone.transform.Find("StatusPanel/MoveText").GetComponent<Text>();
                Text steeringText = clone.transform.Find("StatusPanel/SteeringText").GetComponent<Text>();
                Text headingText = clone.transform.Find("StatusPanel/HeadingText").GetComponent<Text>();
                Text controlText = clone.transform.Find("StatusPanel/ControlText").GetComponent<Text>();
                Action<bool, float> verify = (forward, turn) =>
                {
                    movement.ApplyInput(forward, turn, 1f / 60f);
                    hud.RefreshDisplay();
                    Check(moveText.text == (forward ? "MOVE: FORWARD" : "MOVE: STOPPED"), "Incorrect MOVE label.");
                    Check(steeringText.text == (turn < 0 ? "STEERING: LEFT" : turn > 0 ? "STEERING: RIGHT" : "STEERING: STRAIGHT"),
                        "Incorrect STEERING label.");
                };
                hud.RefreshDisplay();
                Check(moveText.text == "MOVE: STOPPED" && steeringText.text == "STEERING: STRAIGHT", "Initial state must be stopped/straight.");
                foreach (bool forward in new[] { false, true })
                foreach (float turn in new[] { -1f, 0f, 1f })
                    verify(forward, turn);
                verify(false, 0f);
                foreach (float yaw in new[] { 0f, 90f, 270f, -90f, 359.9f })
                {
                    chair.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    hud.RefreshDisplay();
                    int expected = yaw == 359.9f ? 0 : yaw == -90f ? 270 : (int)yaw;
                    Check(headingText.text == "HEADING: " + expected + "\u00B0", "Heading must wrap into 0..359.");
                }
                movement.ApplyInput(true, 1f, 0f);
                hud.RefreshDisplay();
                Check(!movement.IsMoving && movement.TurnInput == 0f, "Zero elapsed time must report no applied motion.");
                movement.ApplyInput(true, 1f, 1f / 60f);
                movement.enabled = false;
                hud.RefreshDisplay();
                Check(moveText.text == "MOVE: STOPPED" && steeringText.text == "STEERING: STRAIGHT", "Disabled movement must report stopped.");
                movement.enabled = true;
                movement.ApplyInput(true, 1f, 1f / 60f);
                typeof(WheelchairMovement).GetField("lastInputFrame", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(movement, Time.frameCount - 1);
                hud.RefreshDisplay();
                Check(moveText.text == "MOVE: STOPPED" && steeringText.text == "STEERING: STRAIGHT", "Prior-frame input must not persist in the HUD.");
                var movementData = new SerializedObject(movement);
                movementData.FindProperty("moveSpeed").floatValue = 0f;
                movementData.FindProperty("turnSpeed").floatValue = 0f;
                movementData.ApplyModifiedPropertiesWithoutUndo();
                movement.ApplyInput(true, 1f, 1f / 60f);
                Check(!movement.IsMoving && movement.TurnInput == 0f, "Zero speed must report no applied motion.");
                movementData.FindProperty("moveSpeed").floatValue = 2f;
                movementData.FindProperty("turnSpeed").floatValue = 60f;
                movementData.ApplyModifiedPropertiesWithoutUndo();
                foreach (int fps in new[] { 30, 120 })
                {
                    chair.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, 0f), Quaternion.identity);
                    for (int i = 0; i < fps; i++) movement.ApplyInput(true, 0f, 1f / fps);
                    Check(Vector3.Distance(chair.transform.position, new Vector3(0f, 0.5f, 2f)) < 0.001f, "Existing forward movement changed.");
                }
                CheckCamera(movement);
                serialized.Update();
                serialized.FindProperty("controlSourceLabel").stringValue = "TEST SOURCE";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                hud.RefreshDisplay();
                Check(controlText.text == "CONTROL: TEST SOURCE", "CONTROL must use the single configurable label.");
                serialized.FindProperty("movement").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                hud.RefreshDisplay();
                Check(moveText.text == "MOVE: STOPPED" && steeringText.text == "STEERING: STRAIGHT" && headingText.text == "HEADING: --",
                    "Missing movement must not display stale state.");
                Canvas.ForceUpdateCanvases();
                foreach (Text text in clone.GetComponentsInChildren<Text>())
                    Check(text.preferredWidth <= text.rectTransform.rect.width && text.preferredHeight <= text.rectTransform.rect.height,
                        "Text does not fit: " + text.name);
                Debug.Log("STEP04_STATUS_OK: state combinations, release, heading wrap, disabled/stale/zero-speed states, source label and text fit passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
                UnityEngine.Object.DestroyImmediate(chair);
            }
            string[] args = Environment.GetCommandLineArgs();
            int previewIndex = Array.IndexOf(args, "-hudPreviewPath");
            if (previewIndex >= 0 && previewIndex + 1 < args.Length)
                RenderPreview(source.GetComponent<Canvas>(), args[previewIndex + 1]);
        }

        private static void CheckCamera(WheelchairMovement movement)
        {
            var cameraObject = new GameObject("Step04CameraCheck") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.aspect = 16f / 9f;
                camera.fieldOfView = 60f;
                var follow = cameraObject.AddComponent<CameraFollow>();
                var serialized = new SerializedObject(follow);
                serialized.FindProperty("target").objectReferenceValue = movement.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var tick = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), follow,
                    typeof(CameraFollow).GetMethod("Follow", BindingFlags.Instance | BindingFlags.NonPublic));
                follow.SnapToTarget();
                foreach (float turn in new[] { -1f, 1f })
                for (int frame = 0; frame < 180; frame++)
                {
                    movement.ApplyInput(true, turn, 1f / 60f);
                    tick(1f / 60f);
                    Vector3 viewport = camera.WorldToViewportPoint(movement.transform.position);
                    Check(viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f,
                        "Existing camera lost the target during combined motion.");
                }
                Debug.Log("STEP04_REGRESSION_OK: 30/120 FPS forward distance and existing camera combined-motion tracking passed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(cameraObject); }
        }

        private static void RenderPreview(Canvas canvas, string path)
        {
            Camera camera = Camera.main;
            var renderTexture = new RenderTexture(1600, 900, 24);
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            RenderMode previousMode = canvas.renderMode;
            Camera previousCamera = canvas.worldCamera;
            float previousPlane = canvas.planeDistance;
            try
            {
                // Temporarily render the same HUD through the camera for an offscreen QA image.
                // The saved scene remains Screen Space - Overlay.
                camera.targetTexture = renderTexture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = renderTexture });
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
                texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log("STEP04_PREVIEW_OK: " + path);
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousPlane;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void Check(bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException(message);
        }
    }
}
