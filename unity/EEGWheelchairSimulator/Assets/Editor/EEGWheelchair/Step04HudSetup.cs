using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Optional authoring tool. No runtime code depends on this Editor script.
    public static class Step04HudSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Tools/EEG Wheelchair/Step 4 - Create Status HUD")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before configuring Step 4.");
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException("The existing MainScene is required.", ScenePath);
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WheelchairMovement movement = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<WheelchairMovement>(true)).Single();
            GameObject[] existing = scene.GetRootGameObjects().Where(root => root.name == "StatusCanvas").ToArray();
            if (existing.Length > 0)
            {
                if (existing.Length != 1 || existing[0].GetComponent<WheelchairStatusUI>() == null)
                    throw new InvalidOperationException("An unrelated StatusCanvas already exists. Nothing was replaced.");
                ValidateReferences(existing[0].GetComponent<WheelchairStatusUI>(), movement);
                Debug.Log("Status HUD already exists. Existing layout and settings were preserved.");
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                throw new InvalidOperationException("Unity's built-in UI font was not found.");

            GameObject canvasObject = new GameObject("StatusCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.SetActive(false);
            canvasObject.layer = 5;
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create status HUD");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform panel = CreateRect("StatusPanel", canvasObject.transform, new Vector2(24f, -24f), new Vector2(340f, 196f));
            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.055f, 0.085f, 0.9f);
            background.raycastTarget = false;
            Text title = CreateText("Title", panel, font, 14f, "EEG WHEELCHAIR DEMO");
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.72f, 0.9f, 1f);
            Text control = CreateText("ControlText", panel, font, 56f, "");
            Text move = CreateText("MoveText", panel, font, 85f, "");
            Text steering = CreateText("SteeringText", panel, font, 114f, "");
            Text heading = CreateText("HeadingText", panel, font, 143f, "");

            WheelchairStatusUI hud = canvasObject.AddComponent<WheelchairStatusUI>();
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("movement").objectReferenceValue = movement;
            serialized.FindProperty("controlText").objectReferenceValue = control;
            serialized.FindProperty("moveText").objectReferenceValue = move;
            serialized.FindProperty("steeringText").objectReferenceValue = steering;
            serialized.FindProperty("headingText").objectReferenceValue = heading;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            canvasObject.SetActive(true);
            hud.RefreshDisplay();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("MainScene could not be saved.");

            if (Application.isBatchMode)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                hud = scene.GetRootGameObjects().Single(root => root.name == "StatusCanvas").GetComponent<WheelchairStatusUI>();
                movement = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WheelchairMovement>(true)).Single();
            }
            ValidateReferences(hud, movement);
            Debug.Log("STEP04_SCENE_OK: MainScene saved; HUD and all state/text references verified.");
        }

        // Batch-only checks are separate so re-running setup never resets user layout.
        public static void ConfigureAndValidate()
        {
            Configure();
            Step04HudValidation.Run();
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.layer = 5;
            var rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text CreateText(string name, Transform parent, Font font, float top, string value)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(18f, -top), new Vector2(304f, 28f));
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.95f, 0.97f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static void ValidateReferences(WheelchairStatusUI hud, WheelchairMovement movement)
        {
            var serialized = new SerializedObject(hud);
            if (serialized.FindProperty("movement").objectReferenceValue != movement)
                throw new InvalidOperationException("HUD must reference the existing WheelchairMovement.");
            foreach (string field in new[] { "controlText", "moveText", "steeringText", "headingText" })
            {
                var text = serialized.FindProperty(field).objectReferenceValue as Text;
                if (text == null || text.font == null)
                    throw new InvalidOperationException("HUD text/font reference is missing: " + field);
            }
            if (hud.GetComponent<Canvas>().renderMode != RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("HUD must use a screen-space overlay Canvas.");
        }
    }
}
