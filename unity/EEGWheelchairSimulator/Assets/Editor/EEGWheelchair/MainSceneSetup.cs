using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EEGWheelchairSimulator.Editor
{
    // Step 1 only. No runtime scripts depend on this optional Editor utility.
    public static class MainSceneSetup
    {
        private const string SourceScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string MaterialsFolder = "Assets/Materials/Step01";

        [MenuItem("Tools/EEG Wheelchair/Step 1 - Create MainScene")]
        public static void CreateMainScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before creating MainScene.");

            // Never overwrite a scene that may contain the user's later work.
            if (File.Exists(MainScenePath) || File.Exists(MainScenePath + ".meta"))
            {
                Debug.Log("MainScene already exists. Nothing was changed. Open it from Assets/Scenes.");
                return;
            }

            if (!File.Exists(SourceScenePath))
                throw new FileNotFoundException("The URP source scene was not found.", SourceScenePath);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("The installed URP Lit shader could not be found.");

            // Interactive use respects unsaved work. Batch use runs in a separate, closed project.
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!AssetDatabase.CopyAsset(SourceScenePath, MainScenePath))
                throw new InvalidOperationException("Could not copy SampleScene to MainScene.");

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            Camera camera = scene.GetRootGameObjects()
                .Select(root => root.GetComponent<Camera>()).Single(component => component != null);

            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Step01");
            Material groundMaterial = GetOrCreateMaterial("Ground", shader, new Color(0.42f, 0.48f, 0.44f));
            Material chairMaterial = GetOrCreateMaterial("WheelchairPlaceholder", shader, new Color(0.06f, 0.35f, 0.8f));

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            // Unity's built-in Plane is 10 x 10 units, so this produces a 40 x 40 floor.
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chair.name = "WheelchairPlaceholder";
            chair.transform.position = new Vector3(0f, 0.5f, 0f);
            chair.transform.localScale = new Vector3(1f, 1f, 1.5f);
            chair.GetComponent<Renderer>().sharedMaterial = chairMaterial;
            // +Z is forward. Motion and its input source will be added in a later step.

            camera.transform.position = new Vector3(0f, 7f, -10f);
            camera.transform.LookAt(chair.transform.position, Vector3.up);
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            if (!EditorSceneManager.SaveScene(scene, MainScenePath))
                throw new InvalidOperationException("MainScene could not be saved.");
            AssetDatabase.SaveAssets();

            // Reload the saved asset to check that the generated scene persisted correctly.
            scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            Debug.Log("STEP01_OK: MainScene saved and reloaded; 5 roots, Ground, Cube, Camera, Light and Volume verified.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static Material GetOrCreateMaterial(string name, Shader shader, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            if (File.Exists(path) || File.Exists(path + ".meta"))
                throw new InvalidOperationException("An existing asset cannot be reused as a material: " + path);

            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ValidateScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var ground = roots.Single(root => root.name == "Ground");
            var chair = roots.Single(root => root.name == "WheelchairPlaceholder");
            var camera = roots.Single(root => root.name == "Main Camera").GetComponent<Camera>();
            bool valid = roots.Length == 5
                && roots.Any(root => root.name == "Directional Light" && root.GetComponent<Light>() != null)
                && roots.Any(root => root.name == "Global Volume")
                && ground.GetComponent<MeshCollider>() != null
                && chair.GetComponent<BoxCollider>() != null
                && Mathf.Approximately(ground.GetComponent<Renderer>().bounds.size.x, 40f)
                && Mathf.Approximately(ground.GetComponent<Renderer>().bounds.size.z, 40f)
                && Mathf.Abs(chair.GetComponent<Renderer>().bounds.min.y) < 0.001f
                && camera != null && camera.CompareTag("MainCamera")
                && camera.GetComponent<AudioListener>() != null
                && ground.GetComponent<Renderer>().sharedMaterial != null
                && chair.GetComponent<Renderer>().sharedMaterial != null;
            if (!valid)
                throw new InvalidOperationException("MainScene failed the Step 1 structure check. Inspect the generated scene.");
        }
    }
}
