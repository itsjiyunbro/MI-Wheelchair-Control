using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallB1Notch
    {
        // Six plan vertices, clockwise: (501,334),(640,334),(640,367),
        // (632,367),(632,438),(501,438). Concave corner receives B112.
        public static void ApplyTo(GameObject root)
        {
            var rooms=root.transform.Find("RoomsAndCores");var b112=rooms.Find("B112");
            var b=b112.Find("RoomFloor").GetComponent<Renderer>().bounds;
            float left=ConvergenceHallB1Setup.Plan(632,0).x,right=ConvergenceHallB1Setup.Plan(710,0).x;
            float sx=(right-left)/b.size.x;var p=b112.position;p.x=left+(p.x-b.min.x)*sx;
            var s=b112.localScale;s.x*=sx;b112.localScale=s;b112.position=p;
            var room=rooms.Find("B111");
            Floor(room,"RoomFloor",501,334,632,438);
            Floor(room,"RoomFloorExtension",632,334,640,367);
            Edge(room,"West",501,334,501,438,Vector3.right);
            Edge(room,"North",501,334,640,334,Vector3.back);
            Edge(room,"EastUpper",640,334,640,367,Vector3.left);
            Edge(room,"SouthNotch",632,367,640,367,Vector3.forward);
            Edge(room,"East",632,367,632,438,Vector3.left);
            Edge(room,"South",501,438,632,438,Vector3.forward);
        }
        static void Floor(Transform room,string name,float x0,float y0,float x1,float y1)
        {
            var t=room.Find(name);
            if(t==null){t=UnityEngine.Object.Instantiate(room.Find("RoomFloor").gameObject,room).transform;t.name=name;}
            t.localPosition=ConvergenceHallB1Setup.Plan((x0+x1)/2,(y0+y1)/2,.003f);
            t.localScale=new Vector3((x1-x0)*ConvergenceHallB1Setup.MetresPerPlanUnit,.006f,(y1-y0)*ConvergenceHallB1Setup.MetresPerPlanUnit);
        }
        static void Edge(Transform room,string name,float x0,float y0,float x1,float y1,Vector3 inward)
        {
            var t=room.Find(name);
            if(t==null){t=UnityEngine.Object.Instantiate(room.Find("East").gameObject,room).transform;t.name=name;}
            var a=ConvergenceHallB1Setup.Plan(x0,y0);var b=ConvergenceHallB1Setup.Plan(x1,y1);float length=(b-a).magnitude;
            t.localPosition=(a+b)/2+inward*.06f;t.localRotation=Quaternion.LookRotation((b-a).normalized);t.localScale=Vector3.one;
            var c=t.GetComponent<BoxCollider>();c.size=new Vector3(.12f,2.8f,length);
            foreach(string n in new[]{"LowerWall","TopCap","Skirting"}){var part=t.Find(n);var scale=part.localScale;scale.z=length;part.localScale=scale;}
        }
        [MenuItem("Tools/EEG Wheelchair/Correct B111 Notch And B112 Gap")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                ApplyTo(root);ConvergenceHallWallMaterials.ApplyTo(root);Validate(root);
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            Validate(GameObject.Find("ConvergenceHallB1"));
        }
        static void Validate(GameObject root)
        {
            var rooms=root.transform.Find("RoomsAndCores");var room=rooms.Find("B111");
            var other=rooms.Find("B112/RoomFloor").GetComponent<Renderer>().bounds;
            var floors=room.Cast<Transform>().Where(t=>t.name.StartsWith("RoomFloor")).Select(t=>t.GetComponent<Renderer>().bounds).ToArray();
            foreach(var f in floors)
                if(Mathf.Min(f.max.x,other.max.x)-Mathf.Max(f.min.x,other.min.x)>.001f&&Mathf.Min(f.max.z,other.max.z)-Mathf.Max(f.min.z,other.min.z)>.001f)
                    throw new Exception("B111/B112 footprint overlap.");
            if(room.Cast<Transform>().Count(t=>t.GetComponent<BoxCollider>()!=null)!=6)throw new Exception("Expected six boundary walls.");
            var adjacent=rooms.Find("SunkenVoid/RoomFloor").GetComponent<Renderer>().bounds;
            if(Mathf.Abs(other.min.x-adjacent.max.x)>.001f)throw new Exception("B112 side gap remains.");
            float expected=((131*104)+(8*33))*Mathf.Pow(ConvergenceHallB1Setup.MetresPerPlanUnit,2);
            if(Mathf.Abs(floors.Sum(f=>f.size.x*f.size.z)-expected)>.002f)throw new Exception("Hexagonal floor area mismatch.");
            Debug.Log("B111_NOTCH_OK: six walls, two joined floor rectangles; no B112 overlap, side gap removed.");
        }
        public static void ApplyAndValidate()
        {
            Apply();int first=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            Apply();if(GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length!=first)throw new Exception("Duplicate geometry on repeated run.");
            ConvergenceHallB1Validation.BuildAndValidate();
        }
    }
}
