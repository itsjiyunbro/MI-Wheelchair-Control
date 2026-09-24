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
    // Editor-only primitive authoring. No runtime component or physics is added.
    public static class WheelchairVisualSetup
    {
        private const string PrefabPath = "Assets/Art/Prefabs/WheelchairVisual.prefab";
        private const string MaterialFolder = "Assets/Art/Materials/Wheelchair";

        [MenuItem("Tools/EEG Wheelchair/Create Wheelchair Visual")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string scenePath = "Assets/Scenes/MainScene.unity";
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(scenePath);
            var movement = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<WheelchairMovement>(true)).Single();
            Transform root = movement.transform;
            Transform[] existing = root.Cast<Transform>().Where(t => t.name == "WheelchairVisual").ToArray();
            if (existing.Length > 1) throw new InvalidOperationException("Multiple visual children exist; review them before setup.");
            if (existing.Length == 1)
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existing[0].gameObject) != PrefabPath)
                    throw new InvalidOperationException("An unrelated WheelchairVisual already exists. It was preserved.");
                Debug.Log("WHEELCHAIR_VISUAL_EXISTS: existing instance, transforms, materials and edits preserved. No duplicate created.");
                return;
            }
            var cubeRenderer = root.GetComponent<MeshRenderer>();
            var ground = scene.GetRootGameObjects().Single(go => go.name == "Ground").GetComponent<Renderer>();
            Vector3 scale = root.lossyScale;
            if (cubeRenderer == null || ground == null || scale.x <= 0 || scale.y <= 0 || scale.z <= 0
                || Vector3.Dot(root.up, Vector3.up) < 0.9999f)
                throw new InvalidOperationException("Expected upright positive-scale wheelchair root and existing Ground renderer.");
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Materials");
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/Art/Prefabs");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                if (File.Exists(PrefabPath)) throw new InvalidOperationException("Unexpected asset at visual prefab path; nothing overwritten.");
                var frame = Material("Frame", new Color(0.10f, 0.13f, 0.17f), 0.35f, 0.3f);
                var cushion = Material("CushionBlue", new Color(0.025f, 0.23f, 0.52f), 0f, 0.22f);
                var tire = Material("Tire", new Color(0.028f, 0.035f, 0.044f), 0f, 0.12f);
                var metal = Material("Metal", new Color(0.50f, 0.57f, 0.64f), 0.65f, 0.4f);
                GameObject built = Build(frame, cushion, tire, metal);
                try { prefab = PrefabUtility.SaveAsPrefabAsset(built, PrefabPath); }
                finally { UnityEngine.Object.DestroyImmediate(built); }
                if (prefab == null) throw new InvalidOperationException("Visual prefab save failed.");
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            Undo.RegisterCreatedObjectUndo(instance, "Attach wheelchair visual");
            instance.name = "WheelchairVisual";
            instance.transform.localRotation = Quaternion.identity;
            // Prefab is authored in metres with its origin on the floor, +Z forward.
            // Preserve the controller root (1,1,1.5) and cancel its scale on this child only.
            instance.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            instance.transform.position = new Vector3(root.position.x, ground.bounds.max.y, root.position.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            Undo.RecordObject(cubeRenderer, "Hide original Cube renderer");
            cubeRenderer.enabled = false;
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("MainScene save failed.");
            Debug.Log("WHEELCHAIR_VISUAL_CREATED: visual-only prefab child attached; original root/components/collider retained.");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        private static Material Material(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new InvalidOperationException("Conflicting material path: " + path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Installed URP Lit shader was not found.");
            var result = new Material(shader) { name = name };
            result.SetColor("_BaseColor", color);
            result.SetFloat("_Metallic", metallic);
            result.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 scale, Material material, Quaternion rotation)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            // Remove only the newly generated primitive collider, never the root's existing collider.
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
            => Part(parent, name, PrimitiveType.Cube, position, size, material, Quaternion.identity);

        private static GameObject Wheel(Transform parent, string name, Vector3 position, float diameter, float width, Material material)
            => Part(parent, name, PrimitiveType.Cylinder, position, new Vector3(diameter, width / 2f, diameter), material, Quaternion.Euler(0, 0, 90));

        private static GameObject Build(Material frame, Material cushion, Material tire, Material metal)
        {
            var visual = new GameObject("WheelchairVisual");
            Transform root = visual.transform;
            Box(root, "Seat", new Vector3(0, 0.65f, 0.04f), new Vector3(0.82f, 0.12f, 0.78f), cushion);
            Box(root, "Backrest", new Vector3(0, 1.025f, -0.33f), new Vector3(0.82f, 0.70f, 0.10f), cushion);
            Box(root, "Footrest", new Vector3(0, 0.22f, 0.88f), new Vector3(0.79f, 0.065f, 0.30f), metal);
            Transform rails = new GameObject("Frame").transform;
            rails.SetParent(root, false);
            Box(rails, "RearAxle", new Vector3(0, 0.48f, -0.18f), new Vector3(1.06f, 0.065f, 0.065f), metal);
            Box(rails, "FrontCrossbar", new Vector3(0, 0.42f, 0.45f), new Vector3(0.86f, 0.065f, 0.065f), frame);
            foreach (int sign in new[] { -1, 1 })
            {
                string side = sign < 0 ? "Left" : "Right";
                Box(root, side + "Armrest", new Vector3(sign * 0.47f, 0.93f, 0.03f), new Vector3(0.10f, 0.09f, 0.70f), tire);
                Box(rails, side + "SeatRail", new Vector3(sign * 0.40f, 0.56f, 0.04f), new Vector3(0.065f, 0.065f, 0.83f), frame);
                Box(rails, side + "LowerRail", new Vector3(sign * 0.40f, 0.42f, 0.10f), new Vector3(0.065f, 0.065f, 0.96f), frame);
                Box(rails, side + "BackPost", new Vector3(sign * 0.40f, 0.91f, -0.40f), new Vector3(0.06f, 0.94f, 0.06f), frame);
                Box(rails, side + "Handle", new Vector3(sign * 0.40f, 1.35f, -0.53f), new Vector3(0.075f, 0.075f, 0.30f), tire);
                Box(rails, side + "ArmSupport", new Vector3(sign * 0.47f, 0.73f, 0.23f), new Vector3(0.055f, 0.35f, 0.055f), frame);
                Box(rails, side + "FrontPost", new Vector3(sign * 0.40f, 0.42f, 0.56f), new Vector3(0.055f, 0.35f, 0.055f), frame);
                Box(rails, side + "FootrestRail", new Vector3(sign * 0.34f, 0.27f, 0.72f), new Vector3(0.055f, 0.055f, 0.36f), frame);
                Box(rails, side + "CasterFork", new Vector3(sign * 0.45f, 0.24f, 0.64f), new Vector3(0.045f, 0.20f, 0.08f), metal);
                Wheel(root, side + "Wheel", new Vector3(sign * 0.53f, 0.48f, -0.18f), 0.96f, 0.10f, tire);
                // Flat concentric cylinders and six simple spokes read clearly at demo distance.
                Wheel(root, side + "WheelRim", new Vector3(sign * 0.584f, 0.48f, -0.18f), 0.79f, 0.014f, metal);
                Wheel(root, side + "WheelInset", new Vector3(sign * 0.596f, 0.48f, -0.18f), 0.67f, 0.014f, frame);
                Wheel(root, side + "WheelHub", new Vector3(sign * 0.613f, 0.48f, -0.18f), 0.13f, 0.026f, metal);
                for (int i = 0; i < 6; i++)
                {
                    Quaternion rotation = Quaternion.Euler(i * 60f, 0, 0);
                    Vector3 position = new Vector3(sign * 0.608f, 0.48f, -0.18f) + rotation * Vector3.up * 0.17f;
                    Part(root, side + "Spoke" + (i + 1), PrimitiveType.Cube, position,
                        new Vector3(0.015f, 0.32f, 0.025f), metal, rotation);
                }
                Wheel(root, "FrontWheel_" + (sign < 0 ? "L" : "R"), new Vector3(sign * 0.40f, 0.15f, 0.64f), 0.30f, 0.075f, tire);
                Wheel(root, side + "CasterHub", new Vector3(sign * 0.443f, 0.15f, 0.64f), 0.10f, 0.016f, metal);
            }
            return visual;
        }

        public static void ConfigureAndValidate()
        {
            Configure();
            // A second execution must do nothing, including preserving artist edits.
            string first = File.ReadAllText("Assets/Scenes/MainScene.unity");
            Configure();
            if (first != File.ReadAllText("Assets/Scenes/MainScene.unity")) throw new InvalidOperationException("Setup is not idempotent.");
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var movement = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<WheelchairMovement>(true)).Single();
            Transform visual = movement.transform.Find("WheelchairVisual");
            if (visual == null || movement.GetComponent<MeshRenderer>().enabled || visual.GetComponentsInChildren<Collider>().Length != 0
                || visual.GetComponentsInChildren<MonoBehaviour>().Length != 0 || visual.GetComponentsInChildren<Rigidbody>().Length != 0)
                throw new InvalidOperationException("Visual-only structure validation failed.");
            foreach (string name in new[] { "Seat", "Backrest", "LeftArmrest", "RightArmrest", "LeftWheel", "RightWheel", "FrontWheel_L", "FrontWheel_R", "Footrest", "Frame" })
                if (visual.Find(name) == null) throw new InvalidOperationException("Required visual part missing: " + name);
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Select(r => r.sharedMaterial).Distinct().Count() != 4) throw new InvalidOperationException("Expected four shared materials.");
            var ground = scene.GetRootGameObjects().Single(go => go.name == "Ground").GetComponent<Renderer>();
            foreach (string wheel in new[] { "LeftWheel", "RightWheel", "FrontWheel_L", "FrontWheel_R" })
                if (Mathf.Abs(visual.Find(wheel).GetComponent<Renderer>().bounds.min.y - ground.bounds.max.y) > 0.002f)
                    throw new InvalidOperationException("Wheel does not touch Ground: " + wheel);
            if (Vector3.Distance(visual.lossyScale, Vector3.one) > 0.001f || Vector3.Dot(visual.forward, movement.transform.forward) < 0.999f)
                throw new InvalidOperationException("Visual scale/forward alignment mismatch.");
            Debug.Log("VISUAL_ASSET_OK: prefab saved/reloaded; four shared materials; ground contact; root retained; no new colliders/runtime scripts; idempotent setup.");
            CaptureAssetPreview(visual);
            // Reuse only the existing regression validation, never earlier scene generators.
            Step07PredictionValidation.Begin();
        }

        private static void CaptureAssetPreview(Transform visual)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-assetPreviewPath");
            if (index < 0) return;
            Camera camera = Camera.main;
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            bool enabled = canvas.enabled;
            try
            {
                // Temporary front three-quarter inspection camera; never saved to MainScene.
                canvas.enabled = false;
                camera.transform.position = visual.position + new Vector3(2.5f, 2.1f, 3.0f);
                camera.transform.LookAt(visual.position + Vector3.up * 0.65f);
                typeof(Step04HudValidation).GetMethod("RenderPreview", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { canvas, args[index + 1] });
            }
            finally { camera.transform.SetPositionAndRotation(position, rotation); canvas.enabled = enabled; }
        }
    }
}
