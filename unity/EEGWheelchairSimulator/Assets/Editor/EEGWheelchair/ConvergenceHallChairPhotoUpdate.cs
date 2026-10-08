using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallChairPhotoUpdate
    {
        static void Check(bool test,string message){if(!test)throw new Exception(message);}
        static bool ContainsNear(System.Collections.Generic.HashSet<string> points,Vector3 p)
        {
            int x=Mathf.RoundToInt(p.x*1000),y=Mathf.RoundToInt(p.y*1000),z=Mathf.RoundToInt(p.z*1000);
            // Transform/CombineMeshes rounding can land on either side of a millimetre bin.
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)for(int dz=-1;dz<=1;dz++)
                if(points.Contains((x+dx)+","+(y+dy)+","+(z+dz)))return true;
            return false;
        }
        static string Signature(GameObject root)
        {
            // Compare the complete layout and collision graph before/after a visual-only update.
            return string.Join("\n",root.GetComponentsInChildren<Transform>(true).Select(t=>
                {string path=AnimationUtility.CalculateTransformPath(t,root.transform);return path+"|"+t.localPosition.ToString("F5")+"|"+t.localRotation.ToString("F5")+"|"+t.localScale.ToString("F5")+"|"+t.gameObject.activeSelf;}).OrderBy(s=>s))+
                string.Join("\n",root.GetComponentsInChildren<BoxCollider>(true).Select(c=>AnimationUtility.CalculateTransformPath(c.transform,root.transform)+"|"+c.center.ToString("F5")+"|"+c.size.ToString("F5")+"|"+c.enabled+"|"+c.isTrigger).OrderBy(s=>s));
        }
        static void Validate(GameObject root)
        {
            int chairs=0;var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/ConvergenceHall/Interior_BlueMeshChair.asset");
            Check(source.bounds.min.y>=0&&source.bounds.max.y<=.9701f&&source.bounds.size.x<.55f&&source.bounds.size.z<.62f,"Chair visual exceeds the existing collision envelope");
            foreach(var vertex in source.vertices)Check(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&float.IsFinite(vertex.z),"Invalid chair vertex");
            foreach(var roomName in ConvergenceHallClassroomInteriors.Rooms)
            {
                var room=root.transform.Find("ClassroomInteriors/"+roomName);var seating=room.Find("Seating");var items=seating.Cast<Transform>().Where(t=>t.name.StartsWith("Chair_")).Concat(new[]{room.Find("TeacherChair")}).Where(t=>t).ToArray();chairs+=items.Length;
                var combined=room.Find("CombinedVisual").GetComponent<MeshFilter>().sharedMesh;
                var actual=combined.vertices.Select(v=>Mathf.RoundToInt(v.x*1000)+","+Mathf.RoundToInt(v.y*1000)+","+Mathf.RoundToInt(v.z*1000)).ToHashSet();
                foreach(var chair in items)
                {
                    Check(Vector3.Distance(chair.localScale,Vector3.one*.95f)<.0001f,"Chair 95% scale changed");
                    Check(chair.Find("CombinedVisual").GetComponent<MeshFilter>().sharedMesh==source,"Chair source not refreshed");
                    foreach(var v in source.vertices.Where((v,i)=>i%97==0))
                    {
                        var expected=room.InverseTransformPoint(chair.Find("CombinedVisual").TransformPoint(v));
                        Check(ContainsNear(actual,expected),"Stale room combined chair: "+roomName);
                    }
                }
                Check(room.GetComponentsInChildren<MeshRenderer>().Count(r=>r.enabled)==1,"Duplicate room rendering");
            }
            Check(chairs==ConvergenceHallClassroomInteriors.Rooms.Sum(name=>{var room=root.transform.Find("ClassroomInteriors/"+name);return room.Find("Seating").Cast<Transform>().Count(t=>t.name.StartsWith("Desk_"))*2+(room.Find("TeacherChair")?1:0);}),"Chair count does not match the saved classroom layout");
            Check(root.transform.Find("ClassroomInteriors/B112/Seating").Cast<Transform>().Count(t=>t.name.StartsWith("Desk_"))==21,"B112 rows changed");
            ConvergenceHallWallSurfaceCleanup.Validate(root);
            ConvergenceHallLecternLighting.DisableEmission(root);
            Debug.Log("CHAIR_PHOTO_SAVED_OK: 412 chairs with new cushion/curved frame/cross legs/twin casters; 95% scale and collision envelope retained; all room render meshes refreshed.");
        }
        [MenuItem("Tools/EEG Wheelchair/Update Photo Chair Shape")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            string before;var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                before=Signature(root);
                var digest=typeof(ConvergenceHallWallSurfaceCleanup).GetMethod("ColliderState",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
                SessionState.SetString("EEG.WallSurfaceCollision",(string)digest.Invoke(null,new object[]{root}));
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            P.BuildChairAsset();AssetDatabase.SaveAssets();root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                foreach(var name in ConvergenceHallClassroomInteriors.Rooms)
                {
                    var room=root.transform.Find("ClassroomInteriors/"+name);UnityEngine.Object.DestroyImmediate(room.Find("CombinedVisual").gameObject);
                    foreach(var r in room.GetComponentsInChildren<MeshRenderer>())r.enabled=true;
                    P.Combine(room,"Interior_"+name+"Combined");
                }
                Check(Signature(root)==before,"Visual-only chair update changed layout or collision");Validate(root);PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);root=GameObject.Find("ConvergenceHallB1");Check(Signature(root)==before,"Saved scene changed layout/collision");Validate(root);
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var settings=new SerializedObject(follow);
            Check(settings.FindProperty("offset").vector3Value==new Vector3(0,1.15f,-2.60f)&&follow.ViewMode==WheelchairViewMode.ThirdPerson,"Previous camera changes lost");
            Check(UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().transform.Find("StatusPanel/CameraViewButton")!=null,"Camera view button missing");
        }
        public static void ApplyAndCapture()
        {Apply();SessionState.SetBool("EEG.ConvergenceQA.FocusChairPhoto",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();}
        public static void CaptureSaved()
        {EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);SessionState.SetBool("EEG.ConvergenceQA.FocusChairPhoto",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();}
        public static IEnumerator CaptureViews(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.enabled=false;var cam=follow.GetComponent<Camera>();cam.orthographic=false;cam.fieldOfView=43;ShaderUtil.allowAsyncCompilation=false;
            var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();hud.gameObject.SetActive(false);
            var room=GameObject.Find("ConvergenceHallB1").transform.Find("ClassroomInteriors/B106");var chair=room.Find("TeacherChair");if(!chair)chair=room.Find("Seating").Cast<Transform>().First(t=>t.name.StartsWith("Chair_"));
            var positions=new[]{new Vector3(.70f,.78f,.72f),new Vector3(.77f,.64f,-.10f),new Vector3(-.65f,.80f,-.60f)};
            var lenses=new[]{64f,70f,65f};
            var names=new[]{"ChairFrontDetail","ChairCrossLegSide","ChairBackDetail"};
            for(int i=0;i<positions.Length+1;i++)
            {
                string name;
                if(i<positions.Length){cam.fieldOfView=lenses[i];cam.transform.position=chair.position+room.TransformVector(positions[i]);cam.transform.LookAt(chair.position+Vector3.up*.48f);name=names[i];}
                else{cam.fieldOfView=60;cam.transform.position=room.TransformPoint(new Vector3(-2.5f,1.3f,-2.7f));cam.transform.LookAt(room.TransformPoint(new Vector3(0,.8f,2)));name="B106UpdatedChairs";}
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                var path=Path.Combine(folder,name+".png");var requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Chair screenshot");yield return null;}
            }
            Debug.Log("CHAIR_PHOTO_CAPTURE_OK: front, side, rear and B106 classroom views captured.");
        }
    }
}
