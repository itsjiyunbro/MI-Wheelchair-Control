using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    // Additive upgrade of existing assets. Does not run any scene/course generation step.
    public static class WallCollisionSetup
    {
        public const string LayerName = "WheelchairObstacle";
        public static readonly string[] Scenes = {
            "Assets/Scenes/MainScene.unity", "Assets/Scenes/RehabJunctionScene.unity"
        };
        const string Indoor = "Assets/Art/Prefabs/Environment/IndoorTestCourse.prefab";
        const string Modules = "Assets/Art/Prefabs/Environment/RehabJunction/Modules/";

        [MenuItem("Tools/EEG Wheelchair/Enable Wall Collision In Both Maps")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (var path in Scenes.Concat(new[] { Indoor, Modules+"WallPanel2m.prefab", Modules+"DecorativeDoor.prefab" }))
                if (!File.Exists(path)) throw new IOException("Expected existing asset: " + path);
            var originalSetup = EditorSceneManager.GetSceneManagerSetup();
            int layer = EnsureLayer();
            try
            {
                EditPrefab(Indoor, root => {
                    foreach (Transform wall in root.transform.Find("Walls"))
                        if (wall.name.StartsWith("Wall_", StringComparison.Ordinal)) Solid(wall.gameObject,layer);
                    foreach (Transform room in root.transform.Find("Doors")) Solid(room.Find("Panel").gameObject,layer);
                });
                EditPrefab(Modules+"WallPanel2m.prefab", root => Solid(root.transform.Find("WallBody").gameObject,layer));
                EditPrefab(Modules+"DecorativeDoor.prefab", root => Solid(root.transform.Find("Panel").gameObject,layer));
                foreach (string path in Scenes)
                {
                    var scene=EditorSceneManager.OpenScene(path);
                    var movement=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WheelchairMovement>(true)).Single();
                    var guard=movement.GetComponent<WheelchairCollisionGuard>();
                    bool added=guard==null;
                    if(added) guard=movement.gameObject.AddComponent<WheelchairCollisionGuard>();
                    var data=new SerializedObject(guard);
                    data.FindProperty("obstacleLayers").intValue=1<<layer;
                    if(added)
                    {
                        data.FindProperty("clearanceRadius").floatValue=1.15f;
                        data.FindProperty("queryHeight").floatValue=.2f;
                        data.FindProperty("skinWidth").floatValue=.025f;
                    }
                    data.ApplyModifiedPropertiesWithoutUndo();
                    var moveData=new SerializedObject(movement);
                    moveData.FindProperty("collisionGuard").objectReferenceValue=guard;
                    moveData.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed: "+path);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("WALL_SETUP_OK: obstacle colliders and shared movement guard in both existing maps; no duplicate guard/colliders.");
            }
            finally { if(originalSetup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(originalSetup); }
        }
        static int EnsureLayer()
        {
            int existing=LayerMask.NameToLayer(LayerName);if(existing>=0)return existing;
            var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers=tags.FindProperty("layers");
            for(int i=8;i<32;i++)
                if(string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    layers.GetArrayElementAtIndex(i).stringValue=LayerName;
                    tags.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();return i;
                }
            throw new InvalidOperationException("No unused user layer; no layer overwritten.");
        }
        static void Solid(GameObject go,int layer)
        {
            var colliders=go.GetComponents<Collider>();
            if(colliders.Length>1 || (colliders.Length==1 && !(colliders[0] is BoxCollider)))
                throw new InvalidOperationException("Unexpected collider configuration on "+go.name);
            var box=go.GetComponent<BoxCollider>();
            if(box==null)
            {
                box=go.AddComponent<BoxCollider>();
                var bounds=go.GetComponent<MeshFilter>().sharedMesh.bounds;
                box.center=bounds.center;box.size=bounds.size;
            }
            box.isTrigger=false;box.enabled=true;go.layer=layer;
        }
        static void EditPrefab(string path,Action<GameObject> edit)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try { edit(root);PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
