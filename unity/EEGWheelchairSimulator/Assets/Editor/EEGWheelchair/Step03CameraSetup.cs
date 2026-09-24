using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EEGWheelchairSimulator.Editor
{
    // Optional Editor-only setup; never runs Step 1 or Step 2 or recreates a scene.
    public static class Step03CameraSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Tools/EEG Wheelchair/Step 3 - Connect Follow Camera")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before configuring Step 3.");
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("The existing MainScene is required.", ScenePath);
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject chair = FindObject(scene, "WheelchairPlaceholder");
            GameObject cameraObject = FindObject(scene, "Main Camera");
            Camera camera = cameraObject.GetComponent<Camera>();
            if (camera == null || !camera.CompareTag("MainCamera"))
                throw new InvalidOperationException("Main Camera must contain the existing MainCamera-tagged Camera.");
            if (chair.GetComponent<WheelchairMovement>() == null || chair.GetComponent<KeyboardWheelchairInput>() == null)
                throw new InvalidOperationException("The existing Step 2 components are required and will not be recreated.");

            int rootCount = scene.rootCount;
            CameraFollow follow = cameraObject.GetComponent<CameraFollow>();
            bool added = follow == null;
            if (added)
                follow = Undo.AddComponent<CameraFollow>(cameraObject);
            var serialized = new SerializedObject(follow);
            SerializedProperty target = serialized.FindProperty("target");
            // Re-running preserves user tuning and an already assigned target.
            if (target.objectReferenceValue == null)
            {
                target.objectReferenceValue = chair.transform;
                serialized.ApplyModifiedProperties();
            }
            if (added)
            {
                Undo.RecordObject(camera.transform, "Set initial follow camera pose");
                follow.SnapToTarget();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save MainScene.");

            if (Application.isBatchMode)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            cameraObject = FindObject(scene, "Main Camera");
            follow = cameraObject.GetComponent<CameraFollow>();
            Check(scene.rootCount == rootCount && cameraObject.GetComponents<CameraFollow>().Length == 1
                && follow.enabled && new SerializedObject(follow).FindProperty("target").objectReferenceValue != null,
                "Follow camera component/target failed the saved scene check.");
            if (Application.isBatchMode)
                CheckFollow();
            Debug.Log("STEP03_OK: MainScene saved and reloaded; follow camera and target verified.");
        }

        private static GameObject FindObject(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected exactly one existing object: " + name);
            return matches[0];
        }

        // Simulate movement and camera updates on disposable objects, without editing the saved Cube.
        private static void CheckFollow()
        {
            var chair = new GameObject("Step03TargetCheck") { hideFlags = HideFlags.HideAndDontSave };
            var cameraObject = new GameObject("Step03CameraCheck") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                chair.transform.localScale = new Vector3(1f, 1f, 1.5f);
                var movement = chair.AddComponent<WheelchairMovement>();
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = 60f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                var follow = cameraObject.AddComponent<CameraFollow>();
                var serialized = new SerializedObject(follow);
                serialized.FindProperty("target").objectReferenceValue = chair.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                MethodInfo method = typeof(CameraFollow).GetMethod("Follow", BindingFlags.Instance | BindingFlags.NonPublic);
                var tick = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), follow, method);

                foreach (float aspect in new[] { 16f / 9f, 4f / 3f })
                foreach (int fps in new[] { 30, 120 })
                foreach (int turn in new[] { -1, 0, 1 })
                foreach (bool forward in new[] { false, true })
                {
                    camera.aspect = aspect;
                    chair.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, 0f), Quaternion.identity);
                    follow.SnapToTarget();
                    Check(Vector3.Distance(camera.transform.position, new Vector3(0f, 5f, -7f)) < 0.001f,
                        "Initial offset must ignore Cube scale.");
                    for (int frame = 0; frame < fps * 5; frame++)
                    {
                        movement.ApplyInput(forward, turn, 1f / fps);
                        tick(1f / fps);
                        // Check the whole Cube, not just its centre, against the camera's viewport.
                        foreach (int x in new[] { -1, 1 })
                        foreach (int y in new[] { -1, 1 })
                        foreach (int z in new[] { -1, 1 })
                        {
                            Vector3 corner = chair.transform.TransformPoint(new Vector3(x, y, z) * 0.5f);
                            Vector3 view = camera.WorldToViewportPoint(corner);
                            Check(view.z > camera.nearClipPlane && view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f,
                                "Cube left the viewport during simulated movement/turning.");
                        }
                        Check(Vector3.Dot(camera.transform.position - chair.transform.position, chair.transform.forward) < 0f,
                            "Camera must remain behind the target during default-speed movement.");
                    }
                    // Stop all target motion and allow the camera to settle for two seconds.
                    for (int frame = 0; frame < fps * 2; frame++)
                        tick(1f / fps);
                    Vector3 expected = chair.transform.position + chair.transform.rotation * new Vector3(0f, 4.5f, -7f);
                    Check(Vector3.Distance(camera.transform.position, expected) < 0.002f, "Camera did not settle after stopping.");
                    Quaternion look = Quaternion.LookRotation(chair.transform.position + Vector3.up * 0.5f - camera.transform.position);
                    Check(Quaternion.Angle(camera.transform.rotation, look) < 0.1f, "Camera rotation did not settle.");
                }
                serialized.Update();
                serialized.FindProperty("target").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Vector3 position = camera.transform.position;
                tick(1f / 60f);
                follow.SnapToTarget();
                Check(camera.transform.position == position, "Missing target must leave the camera still.");
                Debug.Log("STEP03_FOLLOW_OK: 30/120 FPS, 16:9/4:3, forward/left/right/combined motion, full Cube visibility, rear position, settling and missing target passed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(chair);
            }
        }

        private static void Check(bool passed, string message)
        {
            if (!passed)
                throw new InvalidOperationException(message);
        }
    }
}
