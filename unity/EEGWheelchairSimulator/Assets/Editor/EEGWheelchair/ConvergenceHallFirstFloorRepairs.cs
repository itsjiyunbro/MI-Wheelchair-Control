using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallFirstFloorRepairs
    {
        static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Static;
        static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        static string Output()
        {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-firstFloorAudit");return i>=0?args[i+1]:Path.Combine(Directory.GetCurrentDirectory(),"FirstFloorAudit.tsv");}
        public static void StairCheek(Transform parent,float x,Material material)
        {
            var outline=new[]{new Vector2(0,0),new Vector2(11.2f,0),new Vector2(11.2f,4.5f),new Vector2(6.7f,2.25f),new Vector2(4.5f,2.25f)};
            var v=new List<Vector3>();var tri=new List<int>();
            void Face(Vector3 a,Vector3 b,Vector3 c){int i=v.Count;v.Add(a);v.Add(b);v.Add(c);tri.Add(i);tri.Add(i+1);tri.Add(i+2);}
            Vector3 Point(int i,float side)=>new Vector3(x+side*.06f,outline[i].y,outline[i].x);
            foreach(int i in new[]{1,2,3}){Face(Point(0,-1),Point(i,-1),Point(i+1,-1));Face(Point(0,1),Point(i+1,1),Point(i,1));}
            for(int i=0;i<5;i++){int n=(i+1)%5;Face(Point(i,-1),Point(i,1),Point(n,1));Face(Point(i,-1),Point(n,1),Point(n,-1));}
            var mesh=new Mesh{name="1F_ClosedStairCheek"+x};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var g=ConvergenceHallClassroomProps.Group(parent,"WhiteStairCheek");g.gameObject.AddComponent<MeshFilter>().sharedMesh=ConvergenceHallClassroomProps.SaveMesh(mesh.name,mesh);g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        public static void RemoveOverlaps(GameObject basement,GameObject first)
        {
            var wrapper=new GameObject("OverlapRepairScope");var bp=basement.transform.parent;var fp=first.transform.parent;var renamed=new Dictionary<Transform,string>();
            try
            {
                basement.transform.SetParent(wrapper.transform,true);first.transform.SetParent(wrapper.transform,true);
                var blue=basement.transform.Find("BluebellStand");var upper=first.transform.Find("Details/UpperBluebell");
                foreach(var f in wrapper.GetComponentsInChildren<MeshFilter>())if(f.transform.IsChildOf(blue)||f.transform.IsChildOf(upper)){renamed[f.transform]=f.name;f.name="Wall_"+f.name;}
                var type=typeof(ConvergenceHallWallSurfaceCleanup);var ruleType=type.GetNestedType("Rule",BindingFlags.NonPublic);
                IList Pairs(){var surfaces=type.GetMethod("Scan",Private).Invoke(null,new object[]{wrapper});return (IList)type.GetMethod("WallPairs",Private).Invoke(null,new[]{surfaces});}
                bool Relevant(object p){var a=(MeshFilter)Field(Field(p,"a"),"filter");var b=(MeshFilter)Field(Field(p,"b"),"filter");return a.transform.IsChildOf(first.transform)||b.transform.IsChildOf(first.transform)||a.transform.IsChildOf(blue)||b.transform.IsChildOf(blue);}
                var pairs=Pairs().Cast<object>().Where(Relevant).ToArray();var rules=new Dictionary<MeshFilter,IList>();
                foreach(var p in pairs)
                {
                    var owner=Field(p,"owner");var loser=Field(p,"loser");
                    // Give the opaque enclosing wall priority over coplanar oak tread edges.
                    bool Wall(object surf)=>((string)Field(surf,"material")).Contains("White")&&((MeshFilter)Field(surf,"filter")).name.Contains("Wall");
                    if(Wall(loser)&&!Wall(owner)){var swap=owner;owner=loser;loser=swap;}
                    var filter=(MeshFilter)Field(loser,"filter");if(!rules.TryGetValue(filter,out var list)){list=(IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(ruleType));rules[filter]=list;}
                    foreach(var triangle in (IEnumerable)Field(owner,"triangles")){var rule=Activator.CreateInstance(ruleType);ruleType.GetField("sub").SetValue(rule,Field(loser,"sub"));ruleType.GetField("cutter").SetValue(rule,triangle);list.Add(rule);}
                }
                foreach(var entry in rules){string path="";for(var t=entry.Key.transform;t&&t!=wrapper.transform;t=t.parent)path=t.name+"["+t.GetSiblingIndex()+"] /"+path;var hash=System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(path));string name="FirstFloorRepair_"+BitConverter.ToString(hash).Replace("-","").Substring(0,12)+"_"+entry.Key.name;entry.Key.sharedMesh=(Mesh)type.GetMethod("SaveClipped",Private).Invoke(null,new object[]{entry.Key,entry.Value,name});entry.Key.GetComponent<Renderer>().enabled=entry.Key.sharedMesh.vertexCount>0;PrefabUtility.RecordPrefabInstancePropertyModifications(entry.Key);PrefabUtility.RecordPrefabInstancePropertyModifications(entry.Key.GetComponent<Renderer>());}
                var left=Pairs().Cast<object>().Where(Relevant).ToArray();int remaining=left.Length;foreach(var pair in left)Debug.Log("OVERLAP_REMAINS "+Field(pair,"area")+" "+Field(Field(pair,"a"),"path")+" / "+Field(Field(pair,"b"),"path"));if(remaining>0)throw new Exception("Coplanar Bluebell/1F faces remain: "+remaining);
                Debug.Log("FIRST_FLOOR_OVERLAPS_OK: removed "+pairs.Length+" overlapping surface pairs; remaining 0; collider geometry unchanged.");
            }
            finally{foreach(var n in renamed)n.Key.name=n.Value;basement.transform.SetParent(bp,true);first.transform.SetParent(fp,true);UnityEngine.Object.DestroyImmediate(wrapper);}
        }
        public static void AuditCurrent()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var basement=GameObject.Find("ConvergenceHallB1");var first=GameObject.Find("ConvergenceHall1F");
            var lines=new List<string>();
            foreach(string path in new[]{"CoreHalls/B109ElevatorHall","CoreHalls/B109Stairwell"})
            {
                var hall=basement.transform.Find(path);lines.Add("CORE "+path+" world="+hall.position+" rotation="+hall.eulerAngles);
                foreach(Transform child in hall)lines.Add(child.name+" active="+child.gameObject.activeSelf+" position="+child.localPosition+" scale="+child.localScale+" rotation="+child.localEulerAngles);
            }
            lines.AddRange(AuditBluebell(basement,first));File.WriteAllLines(Output(),lines);
            Debug.Log("FIRST_FLOOR_AUDIT_WRITTEN: "+Output());if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        public static List<string> AuditBluebell(GameObject basement,GameObject first)
        {
            var wrapper=new GameObject("AuditScope");var bp=basement.transform.parent;var fp=first.transform.parent;
            var renamed=new Dictionary<Transform,string>();
            try
            {
                basement.transform.SetParent(wrapper.transform,true);first.transform.SetParent(wrapper.transform,true);
                foreach(var filter in wrapper.GetComponentsInChildren<MeshFilter>())
                    if(filter.transform.IsChildOf(basement.transform.Find("BluebellStand"))||filter.transform.IsChildOf(first.transform.Find("Details/UpperBluebell")))
                    {renamed[filter.transform]=filter.name;filter.name="Wall_"+filter.name;}
                var type=typeof(ConvergenceHallWallSurfaceCleanup);
                var surfaces=type.GetMethod("Scan",Private).Invoke(null,new object[]{wrapper});
                var pairs=(IList)type.GetMethod("WallPairs",Private).Invoke(null,new[]{surfaces});
                var lines=new List<string>{"area_m2\tmaterial_a\tmaterial_b\tsurface_a\tsurface_b"};
                foreach(var pair in pairs)
                {
                    var a=Field(pair,"a");var b=Field(pair,"b");string ap=(string)Field(a,"path"),bpath=(string)Field(b,"path");
                    if(!ap.Contains("Bluebell")&&!bpath.Contains("Bluebell"))continue;
                    lines.Add(Field(pair,"area")+"\t"+Field(a,"material")+"\t"+Field(b,"material")+"\t"+ap+"\t"+bpath);
                }
                return lines;
            }
            finally
            {
                foreach(var pair in renamed)pair.Key.name=pair.Value;
                basement.transform.SetParent(bp,true);first.transform.SetParent(fp,true);UnityEngine.Object.DestroyImmediate(wrapper);
            }
        }
    }
}
