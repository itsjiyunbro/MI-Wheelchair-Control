using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EEGWheelchairSimulator.Editor
{
    // Additive setup only. Never runs Step 1-5 or recreates MainScene/UI.
    public static class Step06SocketSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Tools/EEG Wheelchair/Step 6 - Connect Python Test Receiver")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath);
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var movement = components.OfType<WheelchairMovement>().Single();
            var simulation = components.OfType<SimulationController>().Single();
            var receiver = components.OfType<PythonWheelchairReceiver>().SingleOrDefault();
            if (receiver == null) receiver = Undo.AddComponent<PythonWheelchairReceiver>(movement.gameObject);
            if (receiver.gameObject != movement.gameObject) throw new InvalidOperationException("Receiver must be on the existing wheelchair.");
            var data = new SerializedObject(receiver);
            data.FindProperty("simulation").objectReferenceValue = simulation;
            data.FindProperty("movement").objectReferenceValue = movement;
            data.ApplyModifiedProperties();
            // The Python console owns focus during manual testing.
            PlayerSettings.runInBackground = true;
            components.OfType<WheelchairStatusUI>().Single().RefreshDisplay();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("MainScene save failed.");
            if (Application.isBatchMode)
            {
                scene = EditorSceneManager.OpenScene(ScenePath);
                receiver = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PythonWheelchairReceiver>(true)).Single();
                data = new SerializedObject(receiver);
                if (data.FindProperty("simulation").objectReferenceValue == null || data.FindProperty("movement").objectReferenceValue == null)
                    throw new InvalidOperationException("Receiver references did not survive scene reload.");
            }
            Debug.Log("STEP06_SCENE_OK: existing MainScene saved/reloaded with receiver references; existing controls and UI retained.");
        }

        public static void ConfigureAndValidate()
        {
            Configure();
            Step06SocketValidation.Begin();
        }
    }
}
