using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Add three rows to the existing HUD. Never invokes previous stage generators.
    public static class Step07PredictionSetup
    {
        [MenuItem("Tools/EEG Wheelchair/Step 7 - Connect Prediction HUD")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string path = "Assets/Scenes/MainScene.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(path);
            var parts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var hud = parts.OfType<WheelchairStatusUI>().Single();
            var receiver = parts.OfType<PythonWheelchairReceiver>().Single();
            var panel = (RectTransform)hud.transform.Find("StatusPanel");
            var template = panel.Find("ControlText").GetComponent<Text>();
            var data = new SerializedObject(hud);
            data.FindProperty("receiver").objectReferenceValue = receiver;
            string[] names = { "ConnectionText", "PredictionText", "ConfidenceText" };
            string[] fields = { "connectionText", "predictionText", "confidenceText" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform existing = panel.Find(names[i]);
                Text row;
                if (existing == null)
                {
                    row = UnityEngine.Object.Instantiate(template, panel);
                    row.name = names[i];
                    Undo.RegisterCreatedObjectUndo(row.gameObject, "Add prediction HUD row");
                }
                else row = existing.GetComponent<Text>() ?? throw new InvalidOperationException("Unexpected existing HUD object: " + names[i]);
                Undo.RecordObject(row.rectTransform, "Position prediction HUD row");
                row.rectTransform.anchoredPosition = new Vector2(18f, -201f - i * 29f);
                data.FindProperty(fields[i]).objectReferenceValue = row;
            }
            data.ApplyModifiedProperties();
            Undo.RecordObject(panel, "Expand prediction HUD");
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, 373f);
            foreach (string name in new[] { "StartButton", "StopButton", "ResetButton" })
            {
                var rect = (RectTransform)panel.Find(name);
                Undo.RecordObject(rect, "Move simulation button below prediction rows");
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -313f);
            }
            hud.RefreshDisplay();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("MainScene save failed.");
            if (Application.isBatchMode)
            {
                scene = EditorSceneManager.OpenScene(path);
                hud = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WheelchairStatusUI>(true)).Single();
                data = new SerializedObject(hud);
                foreach (string field in new[] { "receiver", "connectionText", "predictionText", "confidenceText" })
                    if (data.FindProperty(field).objectReferenceValue == null) throw new InvalidOperationException("Missing persisted reference: " + field);
            }
            Debug.Log("STEP07_SCENE_OK: prediction rows and references saved/reloaded; existing MainScene retained.");
        }

        public static void ConfigureAndValidate()
        {
            Configure();
            Step07PredictionValidation.Begin();
        }
    }
}
