using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallFurnitureScale
    {
        static Dictionary<string,int[]> Counts(GameObject root)=>ConvergenceHallClassroomInteriors.Rooms.ToDictionary(name=>name,name=>
        {var room=root.transform.Find("ClassroomInteriors/"+name);var seating=room.Find("Seating");return new[]{seating.Cast<Transform>().Count(t=>t.name.StartsWith("Desk_")),seating.Cast<Transform>().Count(t=>t.name.StartsWith("Chair_"))};});
        static string Key(Vector3 p)=>Mathf.RoundToInt(p.x*1000)+","+Mathf.RoundToInt(p.y*1000)+","+Mathf.RoundToInt(p.z*1000);
        static bool ContainsNear(HashSet<string> points,Vector3 p)
        {
            int x=Mathf.RoundToInt(p.x*1000),y=Mathf.RoundToInt(p.y*1000),z=Mathf.RoundToInt(p.z*1000);
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)for(int dz=-1;dz<=1;dz++)if(points.Contains((x+dx)+","+(y+dy)+","+(z+dz)))return true;
            return false;
        }
        static void Validate(GameObject root,Dictionary<string,int[]> original)
        {
            int deskCount=0,chairCount=0;
            foreach(string name in ConvergenceHallClassroomInteriors.Rooms)
            {
                var room=root.transform.Find("ClassroomInteriors/"+name);var seating=room.Find("Seating");var desks=seating.Cast<Transform>().Where(t=>t.name.StartsWith("Desk_")).ToArray();var chairs=seating.Cast<Transform>().Where(t=>t.name.StartsWith("Chair_")).Concat(new[]{room.Find("TeacherChair")}).ToArray();
                if(desks.Length!=original[name][0]||chairs.Length-1!=original[name][1])throw new Exception("Furniture count changed: "+name);
                var rendered=room.Find("CombinedVisual").GetComponent<MeshFilter>().sharedMesh;var actual=new HashSet<string>(rendered.vertices.Select(Key));
                foreach(var item in desks.Concat(chairs))
                {
                    if(Vector3.Distance(item.localScale,Vector3.one*.95f)>.0001f)throw new Exception("Furniture scale mismatch: "+name+"/"+item.name);
                    var source=item.Find("CombinedVisual").GetComponent<MeshFilter>().sharedMesh;
                    foreach(var vertex in source.vertices.Where((p,i)=>i%79==0))
                    {var expected=room.InverseTransformPoint(item.Find("CombinedVisual").TransformPoint(vertex));if(!ContainsNear(actual,expected))throw new Exception("Combined render mesh has stale furniture size: "+name+"/"+item.name);}
                    if(item.name.StartsWith("Desk_"))
                    {
                        var size=Vector3.Scale(source.bounds.size,item.localScale);
                        if(Mathf.Abs(size.x-1.425f)>.002f||Mathf.Abs(size.z-.5225f)>.002f)throw new Exception("Scaled desk dimensions mismatch");
                        var hit=item.GetComponent<BoxCollider>();var localSize=Vector3.Scale(hit.size,item.localScale);
                        if(Mathf.Abs(localSize.x-1.425f)>.002f||Mathf.Abs(localSize.z-.5605f)>.002f)throw new Exception("Desk collision did not shrink");
                    }
                    else if(Mathf.Abs(Vector3.Scale(item.GetComponent<BoxCollider>().size,item.localScale).x-.5225f)>.002f)throw new Exception("Chair collision did not shrink");
                }
                if(Vector3.Distance(room.Find("Lectern").localScale,Vector3.one)>.001f)throw new Exception("Lectern scale changed");
                deskCount+=desks.Length;chairCount+=chairs.Length-1;
            }
            if(root.transform.Find("ClassroomInteriors/B112/Seating").Cast<Transform>().Count(t=>t.name.StartsWith("Desk_"))!=21)throw new Exception("B112 seven-row layout changed");
            ConvergenceHallWallSurfaceCleanup.Validate(root);
            Debug.Log($"FURNITURE_SCALE_OK: {deskCount} desks and {chairCount} student chairs plus 12 teacher chairs at 95%; desks 1.425m x 0.5225m; colliders and combined meshes scaled; room counts/layout and B112 wall alignment retained.");
        }
        public static void ApplyAndCapture()
        {
            var prefab=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);Dictionary<string,int[]> original;
            try{original=Counts(prefab);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            ConvergenceHallClassroomInteriors.Apply();Validate(GameObject.Find("ConvergenceHallB1"),original);
            SessionState.SetString("EEG.ConvergenceQA.FocusRoom","B106,B112,B109");SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
