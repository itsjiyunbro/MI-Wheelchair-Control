using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallB1Revision
    {
        [MenuItem("Tools/EEG Wheelchair/Correct Convergence B1 Rooms And Chair Size")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                var rooms=root.transform.Find("RoomsAndCores");
                // Resize existing groups using their CURRENT floor bounds. Repeated runs
                // produce the same dimensions and preserve child identities/references.
                ConvergenceHallB1Notch.ApplyTo(root);
                Resize(rooms.Find("B113"),884,334,1054,441.66667f);
                Resize(rooms.Find("B114"),884,441.66667f,1054,549.33333f);
                Resize(rooms.Find("B115"),884,549.33333f,1054,657);
                Resize(rooms.Find("EastService"),1054,334,1110,549.33333f);
                Resize(rooms.Find("EastService2"),1054,549.33333f,1110,657);
                ConvergenceHallWallMaterials.ApplyTo(root);
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var scene=EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var visual=chair.transform.Find("WheelchairVisual");var scale=chair.transform.lossyScale;
            visual.localScale=new Vector3(.50f/scale.x,.50f/scale.y,.50f/scale.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
            var guard=new SerializedObject(chair.GetComponent<WheelchairCollisionGuard>());
            guard.FindProperty("clearanceRadius").floatValue=.58f;guard.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var r=GameObject.Find("ConvergenceHallB1").transform.Find("RoomsAndCores");
            Bounds Floor(string n)=>r.Find(n+"/RoomFloor").GetComponent<Renderer>().bounds;
            if(Floor("B111").max.x>Floor("B112").min.x+.001f)throw new Exception("B111/B112 still overlap.");
            var sizes=new[]{"B113","B114","B115"}.Select(n=>Floor(n).size).ToArray();
            if(sizes.Any(s=>Vector3.Distance(s,sizes[0])>.001f))throw new Exception("East rooms are unequal.");
            Debug.Log("B1_REVISION_OK: B112 overlap removed, B113/114/115 equal size "+sizes[0]+"; school chair scale .50, radius .58.");
        }
        static void Resize(Transform room,float x0,float y0,float x1,float y1)
        {
            Bounds b=room.Find("RoomFloor").GetComponent<Renderer>().bounds;
            Vector3 min=ConvergenceHallB1Setup.Plan(x0,y1),max=ConvergenceHallB1Setup.Plan(x1,y0);
            float sx=(max.x-min.x)/b.size.x,sz=(max.z-min.z)/b.size.z;
            Vector3 pos=room.position;pos.x=min.x+(pos.x-b.min.x)*sx;pos.z=min.z+(pos.z-b.min.z)*sz;
            var scale=room.localScale;room.localScale=new Vector3(scale.x*sx,scale.y,scale.z*sz);room.position=pos;
        }
        public static void ApplyAndValidate()
        {
            Apply();Apply(); // Includes a repeated-run geometry assertion.
            ConvergenceHallB1Validation.BuildAndValidate();
        }
    }
}


