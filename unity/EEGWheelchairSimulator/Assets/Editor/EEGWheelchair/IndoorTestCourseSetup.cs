using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EEGWheelchairSimulator.Editor
{
    // Static visual environment only. No movement, collision, camera or UI behaviour.
    public static class IndoorTestCourseSetup
    {
        private const string PrefabPath = "Assets/Art/Prefabs/Environment/IndoorTestCourse.prefab";
        private const string MaterialPath = "Assets/Art/Materials/Environment";
        // Non-overlapping floor rectangles: entry, two wide turning bays, connector and exit.
        private static readonly Rect[] Floors = {
            Rect.MinMaxRect(-2.5f, -4, 2.5f, 3), Rect.MinMaxRect(-4, 3, 4, 11),
            Rect.MinMaxRect(-5, 4.5f, -4, 9.5f), Rect.MinMaxRect(-13, 3, -5, 11),
            Rect.MinMaxRect(-11.5f, 11, -6.5f, 18)
        };
        private struct Edge { public bool Vertical; public float Fixed, A, B; }

        [MenuItem("Tools/EEG Wheelchair/Create Indoor Test Course")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string path = "Assets/Scenes/MainScene.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(path);
            if (scene.GetRootGameObjects().Any(go => go.name == "IndoorTestCourse"))
            {
                Debug.Log("INDOOR_EXISTS: existing course preserved; no duplicate or overwrite.");
                return;
            }
            var movement = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<WheelchairMovement>(true)).Single();
            var ground = scene.GetRootGameObjects().Single(go => go.name == "Ground").GetComponent<Renderer>();
            // This course is deliberately fitted to the inspected scene, never relocate the wheelchair.
            if (Vector3.Distance(movement.transform.position, new Vector3(0, 0.5f, 0)) > 0.001f
                || Quaternion.Angle(movement.transform.rotation, Quaternion.identity) > 0.1f
                || Mathf.Abs(ground.bounds.max.y) > 0.001f || ground.bounds.size.x < 39.9f || ground.bounds.size.z < 39.9f)
                throw new InvalidOperationException("Scene start/Ground differs from the inspected layout. Existing objects were not moved.");
            Folder(MaterialPath); Folder("Assets/Art/Prefabs/Environment");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                if (File.Exists(PrefabPath)) throw new InvalidOperationException("Conflicting course asset; not overwritten.");
                GameObject built = Build();
                try { prefab = PrefabUtility.SaveAsPrefabAsset(built, PrefabPath); }
                finally { UnityEngine.Object.DestroyImmediate(built); }
                if (prefab == null) throw new InvalidOperationException("Course prefab save failed.");
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Add indoor test course");
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("MainScene save failed.");
            Debug.Log("INDOOR_CREATED: separate environment prefab attached; original scene objects retained.");
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Material Mat(string name, Color color)
        {
            string path = MaterialPath + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new InvalidOperationException("Conflicting material asset: " + path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader missing.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", 0.18f);
            AssetDatabase.CreateAsset(material, path); return material;
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, float yaw = 0)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = size; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Text(Transform parent, string name, string value, Vector3 position, Quaternion rotation, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.Text));
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localRotation = rotation;
            // World Space UI uses depth testing and maintains its own dynamic font atlas.
            // This is a non-interactive environment sign, independent of StatusCanvas.
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(600, 160);
            rect.localScale = Vector3.one * (size / 60f);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var text = go.GetComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = 96; text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; text.raycastTarget = false; text.supportRichText = false;
        }

        private static GameObject Build()
        {
            Material floor = Mat("Floor", new Color(0.69f, 0.74f, 0.77f));
            Material wall = Mat("Wall", new Color(0.84f, 0.88f, 0.89f));
            Material door = Mat("Door", new Color(0.21f, 0.34f, 0.43f));
            Material accent = Mat("Accent", new Color(0.065f, 0.24f, 0.29f));
            Material start = Mat("Start", new Color(0.06f, 0.49f, 0.32f));
            Material goal = Mat("Goal", new Color(0.94f, 0.46f, 0.08f));
            var root = new GameObject("IndoorTestCourse");
            Transform floors = Group(root.transform, "Floors"), walls = Group(root.transform, "Walls");
            Transform doors = Group(root.transform, "Doors"), signs = Group(root.transform, "Signs");
            Transform startArea = Group(root.transform, "StartArea"), goalArea = Group(root.transform, "GoalArea");
            Transform props = Group(root.transform, "Props");
            for (int i = 0; i < Floors.Length; i++)
            {
                Rect r = Floors[i];
                // 8 mm above the retained Ground: no coplanar surfaces, no controller height change.
                Box(floors, "Floor_" + i, new Vector3(r.center.x, -0.017f, r.center.y), new Vector3(r.width, 0.05f, r.height), floor);
            }
            BuildWalls(walls, wall, accent);
            Door(doors, "Room01", new Vector3(2.5f, 0, 0), -1, door, accent, floor);
            Door(doors, "Room02", new Vector3(4, 0, 8.6f), -1, door, accent, floor);
            Door(doors, "Room03", new Vector3(-11.5f, 0, 14), 1, door, accent, floor);
            Box(startArea, "StartPad", new Vector3(0, 0.014f, 0), new Vector3(3.5f, 0.008f, 2), start);
            Box(startArea, "StartLabelBase", new Vector3(0, 0.014f, 2.7f), new Vector3(2.2f, 0.008f, 0.65f), start);
            Text(startArea, "StartText", "START", new Vector3(0, 0.021f, 2.7f), Quaternion.Euler(90, 0, 0), 0.22f);
            Box(goalArea, "GoalPad", new Vector3(-9, 0.014f, 15), new Vector3(3.5f, 0.008f, 2.5f), goal);
            Text(goalArea, "GoalFloorText", "GOAL", new Vector3(-9, 0.021f, 15), Quaternion.Euler(90, 0, 0), 0.26f);
            Sign(signs, "LeftSign", "01  LEFT", new Vector3(0, 1.75f, 10.97f), 0, accent);
            Sign(signs, "RightSign", "02  RIGHT", new Vector3(-12.97f, 1.75f, 7), -90, accent);
            Sign(signs, "GoalSign", "GOAL", new Vector3(-9, 1.75f, 17.97f), 0, goal);
            // Clear centreline marks and turn targets. No trigger or goal logic.
            foreach (float z in new[] { 3.9f, 5.0f }) Arrow(signs, new Vector3(0, 0.018f, z), 0, accent);
            Arrow(signs, new Vector3(0, 0.018f, 7), -90, accent);
            foreach (float x in new[] { -2f, -4.5f, -7f }) Arrow(signs, new Vector3(x, 0.018f, 7), -90, accent);
            Arrow(signs, new Vector3(-9, 0.018f, 7), 0, accent);
            foreach (float z in new[] { 9f, 11.3f, 13f }) Arrow(signs, new Vector3(-9, 0.018f, z), 0, accent);
            // Two small static props outside the centre driving line.
            Box(props, "BenchSeat", new Vector3(3.25f, 0.48f, 5), new Vector3(0.55f, 0.12f, 1.6f), door);
            Box(props, "BenchBack", new Vector3(3.5f, 0.84f, 5), new Vector3(0.10f, 0.68f, 1.6f), door);
            foreach (float z in new[] { 4.45f, 5.55f }) Box(props, "BenchLeg", new Vector3(3.25f, 0.21f, z), new Vector3(0.45f, 0.42f, 0.08f), accent);
            Box(props, "PlanterBase", new Vector3(-12.3f, 0.32f, 9.8f), new Vector3(0.6f, 0.64f, 0.6f), door);
            Box(props, "Plant", new Vector3(-12.3f, 0.91f, 9.8f), new Vector3(0.52f, 0.65f, 0.52f), start, 25);
            return root;
        }

        private static void Door(Transform parent, string name, Vector3 at, int inward, Material door, Material frame, Material handle)
        {
            Transform root = Group(parent, name);
            Box(root, "Frame", at + new Vector3(inward * 0.022f, 1.12f, 0), new Vector3(0.06f, 2.24f, 1.46f), frame);
            Box(root, "Panel", at + new Vector3(inward * 0.058f, 1.06f, 0), new Vector3(0.035f, 2.08f, 1.23f), door);
            Box(root, "Handle", at + new Vector3(inward * 0.105f, 1.03f, -0.40f), new Vector3(0.07f, 0.055f, 0.22f), handle);
        }

        private static void Sign(Transform parent, string name, string label, Vector3 at, float yaw, Material material)
        {
            Quaternion rot = Quaternion.Euler(0, yaw, 0);
            Box(parent, name + "Board", at, new Vector3(2.1f, 0.60f, 0.035f), material, yaw);
            Text(parent, name + "Text", label, at + rot * Vector3.back * 0.022f, rot, 0.18f);
        }

        private static void Arrow(Transform parent, Vector3 at, float yaw, Material material)
        {
            Transform arrow = Group(parent, "RouteArrow"); arrow.localPosition = at; arrow.localRotation = Quaternion.Euler(0, yaw, 0);
            Box(arrow, "Shaft", new Vector3(0, 0, -0.08f), new Vector3(0.09f, 0.006f, 0.68f), material);
            Box(arrow, "HeadL", new Vector3(-0.14f, 0, 0.22f), new Vector3(0.09f, 0.006f, 0.40f), material, 45);
            Box(arrow, "HeadR", new Vector3(0.14f, 0, 0.22f), new Vector3(0.09f, 0.006f, 0.40f), material, -45);
        }

        private static void BuildWalls(Transform parent, Material wall, Material accent)
        {
            float[] xs = Floors.SelectMany(r => new[] { r.xMin, r.xMax }).Distinct().OrderBy(v => v).ToArray();
            float[] zs = Floors.SelectMany(r => new[] { r.yMin, r.yMax }).Distinct().OrderBy(v => v).ToArray();
            var cells = new bool[xs.Length - 1, zs.Length - 1];
            for (int x = 0; x < xs.Length - 1; x++) for (int z = 0; z < zs.Length - 1; z++)
                cells[x, z] = Floors.Any(r => r.Contains(new Vector2((xs[x] + xs[x + 1]) / 2, (zs[z] + zs[z + 1]) / 2)));
            bool Inside(int x, int z) => x >= 0 && z >= 0 && x < xs.Length - 1 && z < zs.Length - 1 && cells[x, z];
            var edges = new List<Edge>();
            for (int x = 0; x < xs.Length - 1; x++) for (int z = 0; z < zs.Length - 1; z++)
            {
                if (!cells[x, z]) continue;
                if (!Inside(x - 1, z)) edges.Add(new Edge { Vertical = true, Fixed = xs[x] - 0.10f, A = zs[z], B = zs[z + 1] });
                if (!Inside(x + 1, z)) edges.Add(new Edge { Vertical = true, Fixed = xs[x + 1] + 0.10f, A = zs[z], B = zs[z + 1] });
                if (!Inside(x, z - 1)) edges.Add(new Edge { Vertical = false, Fixed = zs[z] - 0.10f, A = xs[x], B = xs[x + 1] });
                if (!Inside(x, z + 1)) edges.Add(new Edge { Vertical = false, Fixed = zs[z + 1] + 0.10f, A = xs[x], B = xs[x + 1] });
            }
            int id = 0;
            foreach (var group in edges.GroupBy(e => new { e.Vertical, e.Fixed }))
            {
                Edge[] sorted = group.OrderBy(e => e.A).ToArray();
                float a = sorted[0].A, b = sorted[0].B;
                for (int i = 1; i <= sorted.Length; i++)
                {
                    if (i < sorted.Length && Mathf.Abs(sorted[i].A - b) < 0.001f) { b = sorted[i].B; continue; }
                    bool vertical = group.Key.Vertical;
                    Vector3 pos = vertical ? new Vector3(group.Key.Fixed, 1.3f, (a + b) / 2) : new Vector3((a + b) / 2, 1.3f, group.Key.Fixed);
                    Vector3 size = vertical ? new Vector3(0.2f, 2.6f, b - a) : new Vector3(b - a, 2.6f, 0.2f);
                    Box(parent, "Wall_" + id, pos, size, wall);
                    pos.y = 0.09f; size.y = 0.18f;
                    if (vertical) size.x = 0.24f; else size.z = 0.24f;
                    Box(parent, "Trim_" + id++, pos, size, accent);
                    if (i < sorted.Length) { a = sorted[i].A; b = sorted[i].B; }
                }
            }
        }

        public static void ConfigureAndValidate()
        {
            Configure();
            string sceneText = File.ReadAllText("Assets/Scenes/MainScene.unity");
            Configure();
            if (sceneText != File.ReadAllText("Assets/Scenes/MainScene.unity")) throw new InvalidOperationException("Course setup is not idempotent.");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var course = scene.GetRootGameObjects().Single(go => go.name == "IndoorTestCourse");
            if (course.GetComponentsInChildren<Collider>().Length != 0
                || course.GetComponentsInChildren<MonoBehaviour>().Any(c => !(c is UnityEngine.UI.Text)))
                throw new InvalidOperationException("Environment must be visual only.");
            foreach (Renderer renderer in course.GetComponentsInChildren<Renderer>())
                if (renderer.bounds.min.x < -20 || renderer.bounds.max.x > 20 || renderer.bounds.min.z < -20 || renderer.bounds.max.z > 20)
                    throw new InvalidOperationException("Environment exceeds Ground bounds: " + renderer.name);
            CheckRouteAndPreview(course);
            Debug.Log("INDOOR_SCENE_OK: prefab and scene reload, no duplicates, within 40x40 Ground, no added collision/runtime behaviour.");
            Step07PredictionValidation.Begin(); // Validation only; never executes an earlier setup.
        }

        private static void CheckRouteAndPreview(GameObject course)
        {
            var movement = UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var simulation = UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            var follow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            Camera camera = follow.GetComponent<Camera>();
            Vector3 start = movement.transform.position, cameraPosition = camera.transform.position;
            Quaternion startRotation = movement.transform.rotation, cameraRotation = camera.transform.rotation;
            Bounds[] walls = course.transform.Find("Walls").GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Wall_")).Select(r => r.bounds).ToArray();
            MethodInfo tick = typeof(CameraFollow).GetMethod("Follow", BindingFlags.NonPublic | BindingFlags.Instance);
            int samples = 0;
            void Frame(bool forward, float turn)
            {
                movement.ApplyInput(forward, turn, 1f / 60f);
                tick.Invoke(follow, new object[] { 1f / 60f });
                Vector3 target = movement.transform.position + Vector3.up * 0.5f;
                Vector3 direction = target - camera.transform.position;
                var ray = new Ray(camera.transform.position, direction.normalized);
                foreach (Bounds wall in walls)
                    if (wall.IntersectRay(ray, out float distance) && distance < direction.magnitude)
                        throw new InvalidOperationException("Wall occludes centreline route camera at " + movement.transform.position + " yaw=" + movement.HeadingDegrees);
                samples++;
            }
            try
            {
                simulation.StartSimulation(); follow.SnapToTarget();
                Preview("Indoor-Start.png");
                for (int i = 0; i < 210; i++) Frame(true, 0);
                for (int i = 0; i < 90; i++) Frame(false, -1);
                Preview("Indoor-Left.png");
                for (int i = 0; i < 270; i++) Frame(true, 0);
                for (int i = 0; i < 90; i++) Frame(false, 1);
                Preview("Indoor-Right.png");
                for (int i = 0; i < 240; i++) Frame(true, 0);
                Preview("Indoor-Goal.png");
                if (Vector3.Distance(movement.transform.position, new Vector3(-9, 0.5f, 15)) > 0.02f)
                    throw new InvalidOperationException("Course waypoint traversal failed.");
                Debug.Log("INDOOR_ROUTE_OK: " + samples + " simulated 60Hz movement/follow-camera samples, both turns and GOAL; no wall intersects camera-to-target line.");
                simulation.ResetSimulation();
                camera.transform.position = new Vector3(-4.5f, 27, 7);
                camera.transform.rotation = Quaternion.Euler(90, 0, 0);
                bool ortho = camera.orthographic; float size = camera.orthographicSize;
                try { camera.orthographic = true; camera.orthographicSize = 13; Preview("Indoor-Overview.png"); }
                finally { camera.orthographic = ortho; camera.orthographicSize = size; }
            }
            finally
            {
                simulation.StopSimulation(); movement.transform.SetPositionAndRotation(start, startRotation);
                camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            }
        }

        private static void Preview(string name)
        {
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-coursePreviewFolder");
            if (index < 0) return;
            // The environment also contains world-space Canvases. Only the existing HUD
            // may be temporarily converted by the offscreen HUD rendering helper.
            var canvas = UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>();
            bool active = canvas.enabled;
            try
            {
                canvas.enabled = false;
                typeof(Step04HudValidation).GetMethod("RenderPreview", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { canvas, Path.Combine(args[index + 1], name) });
            }
            finally { canvas.enabled = active; }
        }
    }
}
