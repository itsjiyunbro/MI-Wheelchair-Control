using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    public static class Step05SimulationSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Tools/EEG Wheelchair/Step 5 - Connect Simulation Controls")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before configuring Step 5.");
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("Existing MainScene is required.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var movement = roots.SelectMany(root => root.GetComponentsInChildren<WheelchairMovement>(true)).Single();
            var camera = roots.SelectMany(root => root.GetComponentsInChildren<CameraFollow>(true)).Single();
            var hud = roots.SelectMany(root => root.GetComponentsInChildren<WheelchairStatusUI>(true)).Single();
            var panel = hud.transform.Find("StatusPanel") as RectTransform;
            if (panel == null) throw new InvalidOperationException("Existing HUD panel is required.");
            var controllers = roots.SelectMany(root => root.GetComponentsInChildren<SimulationController>(true)).ToArray();
            if (controllers.Length > 0)
            {
                Validate(scene);
                Debug.Log("Step 5 is already connected. Existing settings were preserved.");
                return;
            }
            if (panel.Find("SimulationText") != null || panel.Find("StartButton") != null
                || panel.Find("StopButton") != null || panel.Find("ResetButton") != null)
                throw new InvalidOperationException("Conflicting simulation UI objects exist; nothing was replaced.");
            var systems = roots.SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).ToArray();
            if (systems.Length > 1 || systems.Any(system => system.GetComponent<StandaloneInputModule>() != null))
                throw new InvalidOperationException("Review existing EventSystems/Input Modules before adding controls.");
            const string actionsPath = "Assets/InputSystem_Actions.inputactions";
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath);
            var references = AssetDatabase.LoadAllAssetsAtPath(actionsPath).OfType<InputActionReference>().ToArray();
            InputActionReference Reference(string name) => references.FirstOrDefault(item => item.action != null && item.action.id == actions.FindAction("UI/" + name, true).id)
                ?? throw new InvalidOperationException("Persisted UI action reference not found: " + name);
            if (actions == null) throw new InvalidOperationException("Existing Input Actions asset is required.");
            var point = Reference("Point");
            var click = Reference("Click");

            var controllerObject = new GameObject("SimulationController");
            SceneManager.MoveGameObjectToScene(controllerObject, scene);
            Undo.RegisterCreatedObjectUndo(controllerObject, "Add simulation controls");
            var controller = controllerObject.AddComponent<SimulationController>();
            SetReference(controller, "movement", movement);
            SetReference(controller, "followCamera", camera);
            SetReference(movement, "simulation", controller);

            Undo.RecordObject(panel, "Expand status panel");
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, 286f);
            var template = panel.Find("ControlText").GetComponent<Text>();
            Text simulationText = UnityEngine.Object.Instantiate(template, panel);
            simulationText.name = "SimulationText";
            simulationText.rectTransform.anchoredPosition = new Vector2(18f, -56f);
            Undo.RegisterCreatedObjectUndo(simulationText.gameObject, "Add simulation status");
            string[] rows = { "ControlText", "MoveText", "SteeringText", "HeadingText" };
            for (int i = 0; i < rows.Length; i++)
            {
                var rect = panel.Find(rows[i]) as RectTransform;
                Undo.RecordObject(rect, "Move HUD row");
                rect.anchoredPosition = new Vector2(18f, -85f - i * 29f);
            }
            Button start = CreateButton(panel, template.font, "StartButton", "START", 18f, controller.StartSimulation, hud);
            Button stop = CreateButton(panel, template.font, "StopButton", "STOP", 122f, controller.StopSimulation, hud);
            Button reset = CreateButton(panel, template.font, "ResetButton", "RESET", 226f, controller.ResetSimulation, hud);
            SetReference(hud, "simulation", controller);
            SetReference(hud, "simulationText", simulationText);
            SetReference(hud, "startButton", start);
            SetReference(hud, "stopButton", stop);
            SetReference(hud, "resetButton", reset);
            if (hud.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(hud.gameObject);

            EventSystem eventSystem;
            if (systems.Length == 0)
            {
                var eventObject = new GameObject("EventSystem", typeof(EventSystem));
                SceneManager.MoveGameObjectToScene(eventObject, scene);
                Undo.RegisterCreatedObjectUndo(eventObject, "Add UI EventSystem");
                eventSystem = eventObject.GetComponent<EventSystem>();
                // Mouse-operated controls do not reuse the wheelchair's WASD/arrow navigation.
                eventSystem.sendNavigationEvents = false;
            }
            else eventSystem = systems[0];
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
                inputModule.actionsAsset = actions;
                inputModule.point = point;
                inputModule.leftClick = click;
                inputModule.rightClick = Reference("RightClick");
                inputModule.middleClick = Reference("MiddleClick");
                inputModule.scrollWheel = Reference("ScrollWheel");
                inputModule.move = Reference("Navigate");
                inputModule.submit = Reference("Submit");
                inputModule.cancel = Reference("Cancel");
                inputModule.trackedDevicePosition = Reference("TrackedDevicePosition");
                inputModule.trackedDeviceOrientation = Reference("TrackedDeviceOrientation");
            }
            hud.RefreshDisplay();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("MainScene save failed.");
            if (Application.isBatchMode) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Validate(scene);
            Debug.Log("STEP05_SCENE_OK: controller, movement gate, HUD/buttons, raycaster and InputSystemUIInputModule saved/reloaded.");
        }

        public static void ConfigureAndValidate()
        {
            Configure();
            Step05SimulationValidation.Begin();
        }

        private static void SetReference(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static Button CreateButton(Transform parent, Font font, string name, string label, float x, UnityAction action, WheelchairStatusUI hud)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Add simulation button");
            go.layer = 5;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -226f);
            rect.sizeDelta = new Vector2(96f, 36f);
            var image = go.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = new Color(0.15f, 0.3f, 0.4f);
            colors.highlightedColor = new Color(0.23f, 0.45f, 0.58f);
            colors.pressedColor = new Color(0.08f, 0.18f, 0.25f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.12f, 0.14f, 0.17f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            UnityEventTools.AddPersistentListener(button.onClick, action);
            UnityEventTools.AddPersistentListener(button.onClick, hud.RefreshDisplay);
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.layer = 5;
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            Text text = labelObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = label;
            return button;
        }

        private static void Validate(Scene scene)
        {
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var controller = components.OfType<SimulationController>().Single();
            var movement = components.OfType<WheelchairMovement>().Single();
            var hud = components.OfType<WheelchairStatusUI>().Single();
            var module = components.OfType<EventSystem>().Single().GetComponent<InputSystemUIInputModule>();
            if (new SerializedObject(movement).FindProperty("simulation").objectReferenceValue != controller
                || new SerializedObject(controller).FindProperty("movement").objectReferenceValue != movement
                || new SerializedObject(controller).FindProperty("followCamera").objectReferenceValue == null
                || module == null || !module.enabled || module.point == null || module.leftClick == null
                || components.OfType<StandaloneInputModule>().Any() || hud.GetComponent<GraphicRaycaster>() == null)
                throw new InvalidOperationException("Simulation/UI input references are incomplete.");
            var data = new SerializedObject(hud);
            if (data.FindProperty("simulation").objectReferenceValue != controller || data.FindProperty("simulationText").objectReferenceValue == null)
                throw new InvalidOperationException("Simulation HUD references are incomplete.");
            foreach (string field in new[] { "startButton", "stopButton", "resetButton" })
            {
                var button = data.FindProperty(field).objectReferenceValue as Button;
                if (button == null || button.onClick.GetPersistentEventCount() != 2 || button.onClick.GetPersistentTarget(0) != controller)
                    throw new InvalidOperationException("Simulation button event reference is incomplete: " + field);
            }
        }
    }
}
