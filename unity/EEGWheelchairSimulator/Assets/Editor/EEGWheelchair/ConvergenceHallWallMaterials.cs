using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    // Visual authoring only. Room footprints define adjacency; shared partitions and
    // external faces stay opaque. A partially exposed side is split at room boundaries.
    public static class ConvergenceHallWallMaterials
    {
        const string Folder="Assets/Art/Materials/ConvergenceHall/";
        [MenuItem("Tools/EEG Wheelchair/Apply Convergence Corridor Glass Walls")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try{ApplyTo(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            // References to this prefab update automatically. No scene reconstruction.
            var scene=EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
        }
        public static void ApplyTo(GameObject root)
        {
            ConvergenceHallWallSurfaceCleanup.Restore(root);
            ConvergenceHallBluebell.ApplyTo(root);
            ConvergenceHallRoomCurves.Prepare(root);
            var white=AssetDatabase.LoadAssetAtPath<Material>(Folder+"WarmWhite.mat");
            var blue=AssetDatabase.LoadAssetAtPath<Material>(Folder+"FrostedTeal.mat");
            var metal=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Metal.mat");
            if(!white||!blue||!metal)throw new InvalidOperationException("Existing school materials required.");
            var rooms=root.transform.Find("RoomsAndCores").Cast<Transform>().Where(t=>t.gameObject.activeSelf).ToArray();
            var footprints=rooms.SelectMany(r=>r.Cast<Transform>().Where(t=>t.name.StartsWith("RoomFloor")))
                .Select(t=>t.GetComponent<Renderer>().bounds).ToList();
            footprints.Add(root.transform.Find("BluebellStand/StandCollision").GetComponent<Renderer>().bounds);
            Bounds exterior=root.transform.Find("Floors/B1Footprint").GetComponent<Renderer>().bounds;
            int spans=0;
            foreach(var room in rooms)
            {
                bool eligible=(room.name.StartsWith("B1")&&room.name!="B112")||room.name=="Lounge";
                foreach(var edge in room.Cast<Transform>().Where(t=>t.GetComponent<BoxCollider>()!=null&&t.Find("LowerWall")!=null))
                {
                    string side=edge.name;
                    edge.Find("LowerWall").GetComponent<Renderer>().sharedMaterial=white;
                    foreach(Transform c in edge)if(c.name=="Mullion")c.gameObject.SetActive(false);
                    var group=edge.Find("CorridorGlazing");
                    if(group==null){group=new GameObject("CorridorGlazing").transform;group.SetParent(edge,false);}
                    foreach(Transform c in group)c.gameObject.SetActive(false);
                    if(!eligible)continue;
                    bool vertical=side.StartsWith("West")||side.StartsWith("East");
                    Vector3 normal=side.StartsWith("West")?Vector3.left:side.StartsWith("East")?Vector3.right:side.StartsWith("North")?Vector3.forward:Vector3.back;
                    float half=edge.GetComponent<BoxCollider>().size.z/2;
                    var endA=edge.TransformPoint(new Vector3(0,0,-half));var endB=edge.TransformPoint(new Vector3(0,0,half));
                    float min=vertical?Mathf.Min(endA.z,endB.z):Mathf.Min(endA.x,endB.x);
                    float max=vertical?Mathf.Max(endA.z,endB.z):Mathf.Max(endA.x,endB.x);
                    Vector3 boundary=edge.position+normal*(.06f*Mathf.Abs(edge.lossyScale.x));
                    var cuts=new List<float>{min,max};
                    foreach(var other in footprints)
                    {
                        cuts.Add(Mathf.Clamp(vertical?other.min.z:other.min.x,min,max));
                        cuts.Add(Mathf.Clamp(vertical?other.max.z:other.max.x,min,max));
                    }
                    cuts.Sort();int index=0;
                    for(int i=1;i<cuts.Count;i++)
                    {
                        float a=cuts[i-1],z=cuts[i];if(z-a<.003f)continue;
                        Vector3 face=vertical?new Vector3(boundary.x,0,(a+z)/2):new Vector3((a+z)/2,0,boundary.z);
                        Vector3 probe=face+normal*.025f;
                        bool Inside(Bounds box)=>probe.x>box.min.x+.001f&&probe.x<box.max.x-.001f&&probe.z>box.min.z+.001f&&probe.z<box.max.z-.001f;
                        if(!Inside(exterior)||footprints.Any(Inside))continue;
                        float localZ=edge.InverseTransformPoint(face).z;
                        float length=(z-a)/Mathf.Abs(edge.lossyScale.z);
                        // Thin visual skin only; existing wall and all colliders remain in place.
                        Part(group,"Glass"+index,new Vector3(0,.53f,localZ),new Vector3(.124f,1.06f,length-.002f),blue);
                        int bars=Mathf.Max(1,Mathf.CeilToInt((z-a)/.9f));
                        for(int j=0;j<=bars;j++)Part(group,"Frame"+index+"_"+j,new Vector3(0,.53f,localZ-length/2+length*j/bars),new Vector3(.136f,1.06f,.022f),metal);
                        index++;spans++;
                    }
                }
            }
            ConvergenceHallArchitecture.ApplyTo(root);
            Debug.Log("B1_GLASS_OK: "+spans+" corridor-facing spans; partitions/exterior warm white; original wall transforms/colliders retained.");
        }
        static void Part(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var t=parent.Find(name);
            if(t==null)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());t=go.transform;t.SetParent(parent,false);
            }
            t.localPosition=position;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=material;t.gameObject.SetActive(true);
        }
        public static void ApplyAndCapture()
        {
            Apply();
            int Count()=>GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            int first=Count();Apply();if(Count()!=first)throw new Exception("Repeated styling generated duplicates.");
            var root=GameObject.Find("ConvergenceHallB1");
            foreach(var c in root.GetComponentsInChildren<Collider>(true))
                if(c.transform.name.StartsWith("Glass")||c.transform.name.StartsWith("Frame"))throw new Exception("Decoration acquired collider.");
            SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}


