using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EEGWheelchairSimulator.Editor
{
    // Optional authoring utility. The runtime components do not depend on this file.
    public static class Step02KeyboardSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Tools/EEG Wheelchair/Step 2 - Connect Keyboard Movement")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before configuring Step 2.");
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("MainScene must already exist. Step 2 does not recreate it.", ScenePath);
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Use the existing loaded scene when possible, including its current edits.
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject chair = FindChair(scene);
            if (chair.GetComponent<Rigidbody>() != null)
                throw new InvalidOperationException("A Rigidbody is present. Review physics before enabling Transform movement.");

            int rootCount = scene.rootCount;
            bool changed = false;
            if (chair.GetComponent<WheelchairMovement>() == null)
            {
                Undo.AddComponent<WheelchairMovement>(chair);
                changed = true;
            }
            if (chair.GetComponent<KeyboardWheelchairInput>() == null)
            {
                Undo.AddComponent<KeyboardWheelchairInput>(chair);
                changed = true;
            }

            if (changed)
                EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save MainScene.");

            // Reopen only in batch mode, so interactive use keeps the editor's Undo history.
            if (Application.isBatchMode)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            chair = FindChair(scene);
            if (scene.rootCount != rootCount
                || chair.GetComponents<WheelchairMovement>().Length != 1
                || chair.GetComponents<KeyboardWheelchairInput>().Length != 1
                || !chair.GetComponent<WheelchairMovement>().enabled
                || !chair.GetComponent<KeyboardWheelchairInput>().enabled)
                throw new InvalidOperationException("Step 2 component check failed. Inspect WheelchairPlaceholder.");

            if (Application.isBatchMode)
                CheckMovement();
            Debug.Log("STEP02_OK: Existing MainScene configured; movement and keyboard components verified.");
        }

        private static GameObject FindChair(Scene scene)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == "WheelchairPlaceholder")
                .Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected exactly one WheelchairPlaceholder. No objects were recreated.");
            return matches[0];
        }

        // A temporary object checks motion without moving or saving the scene's actual Cube.
        private static void CheckMovement()
        {
            var probe = new GameObject("Step02MovementCheck") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var movement = probe.AddComponent<WheelchairMovement>();
                Vector3 start = new Vector3(0f, 0.5f, 0f);
                foreach (int fps in new[] { 30, 120 })
                {
                    probe.transform.SetPositionAndRotation(start, Quaternion.identity);
                    for (int frame = 0; frame < fps; frame++)
                        movement.ApplyInput(true, 0f, 1f / fps);
                    Check(Vector3.Distance(probe.transform.position, start + Vector3.forward * 2f) < 0.001f,
                        "Forward distance must be 2 metres in one second at " + fps + " FPS.");

                    Vector3 stoppedPosition = probe.transform.position;
                    movement.ApplyInput(false, 0f, 1f);
                    Check(probe.transform.position == stoppedPosition, "Released input must stop translation.");

                    foreach (int direction in new[] { -1, 1 })
                    {
                        probe.transform.SetPositionAndRotation(start, Quaternion.identity);
                        for (int frame = 0; frame < fps; frame++)
                            movement.ApplyInput(false, direction, 1f / fps);
                        Check(Quaternion.Angle(probe.transform.rotation, Quaternion.Euler(0f, direction * 60f, 0f)) < 0.02f,
                            "Turning must be 60 degrees per second in both directions.");
                        Check(probe.transform.position == start, "In-place turning must not translate.");
                    }
                }

                probe.transform.SetPositionAndRotation(start, Quaternion.Euler(0f, 90f, 0f));
                movement.ApplyInput(true, 0f, 1f);
                Check(Vector3.Distance(probe.transform.position, start + Vector3.right * 2f) < 0.001f,
                    "Forward movement must follow the Cube's heading.");
                movement.ApplyInput(true, -1f, 1f / 60f);
                Check(Mathf.Approximately(probe.transform.position.y, start.y), "Combined motion must preserve Y.");
                Vector3 beforeDisabled = probe.transform.position;
                Quaternion rotationBeforeDisabled = probe.transform.rotation;
                movement.enabled = false;
                movement.ApplyInput(true, 1f, 1f);
                Check(probe.transform.position == beforeDisabled && probe.transform.rotation == rotationBeforeDisabled,
                    "A disabled movement component must not move or turn.");
                Debug.Log("STEP02_MOTION_OK: 30/120 FPS distance and turns, stop, local forward, fixed Y and disabled state checked.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        private static void Check(bool passed, string message)
        {
            if (!passed)
                throw new InvalidOperationException(message);
        }
    }
}
