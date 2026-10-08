using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    // Explicit batch validation only. Never adds runtime behaviours to either scene.
    public static class RehabJunctionValidation
    {
        public static void BuildAndValidate()
        {
            Require(Application.isBatchMode,"Run QA in a separate batch Editor.");
            string original=File.ReadAllText("Assets/Scenes/MainScene.unity");
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            string graph=References();
            RehabJunctionSetup.Create();
            string generated=File.ReadAllText(RehabJunctionSetup.ScenePath);
            RehabJunctionSetup.Create();
            Require(generated==File.ReadAllText(RehabJunctionSetup.ScenePath),"Repeat generation modified scene.");
            Require(original==File.ReadAllText("Assets/Scenes/MainScene.unity"),"MainScene changed.");
            Require(graph==References(),"Copied control/HUD/camera references differ from MainScene.");
            var scene=SceneManager.GetActiveScene();
            var root=scene.GetRootGameObjects().Single(g=>g.name=="RehabJunctionCourse");
            Require(!scene.GetRootGameObjects().Any(g=>g.name=="IndoorTestCourse"),"Old environment remains active.");
            Require(root.GetComponentsInChildren<Rigidbody>(true).Length==0,"Environment added Rigidbody behaviour.");
            Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root)==RehabJunctionSetup.PrefabPath,"Course prefab disconnected.");
            foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                Require(r.sharedMaterial!=null && r.sharedMaterial.shader!=null,"Missing material.");
                Require(r.bounds.min.x>=-20 && r.bounds.max.x<=20 && r.bounds.min.z>=-20 && r.bounds.max.z<=20,"Course outside Ground.");
            }
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            Require(chair.transform.Find("WheelchairVisual")!=null,"Wheelchair visual missing.");
            Require(!chair.GetComponent<MeshRenderer>().enabled,"Placeholder renderer unexpectedly enabled.");
            Require(Vector3.Distance(chair.transform.position,new Vector3(0,.5f,0))<.001f,"Start changed.");
            Require(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1,"Duplicate EventSystem.");
            Canvas.ForceUpdateCanvases();
            foreach(var text in root.GetComponentsInChildren<Text>())
                Require(text.preferredWidth<=text.rectTransform.rect.width && text.preferredHeight<=text.rectTransform.rect.height,"Sign text overflow: "+text.text);
            // Check nested prefab reloading independent of the scene instance.
            var loaded=PrefabUtility.LoadPrefabContents(RehabJunctionSetup.PrefabPath);
            try { Require(loaded.GetComponentsInChildren<Renderer>().Length==root.GetComponentsInChildren<Renderer>().Length,"Prefab reload differs."); }
            finally { PrefabUtility.UnloadPrefabContents(loaded); }
            CheckRoutes(root);
            EditorSceneManager.OpenScene(RehabJunctionSetup.ScenePath); // Discard temporary QA poses/state.
            Require(graph==References(),"Scene reload lost control references.");
            Debug.Log("REHAB_SCENE_OK: save/reload, nested prefabs, reference graph, original MainScene, idempotence, start, bounds and sign fit.");
            // Existing real keyboard/pointer/Python integration tests on the new scene; no setup reruns.
            Step07PredictionValidation.Begin();
        }

        static string References()
        {
            string ObjectKey(UnityEngine.Object obj)
            {
                if(obj==null)return "null";
                if(EditorUtility.IsPersistent(obj))return AssetDatabase.GetAssetPath(obj)+"/"+obj.name;
                var t=obj is Component c?c.transform:(obj as GameObject)?.transform;
                if(t==null)return obj.GetType().Name+"/"+obj.name;
                string path=t.name; while(t.parent!=null){t=t.parent;path=t.name+"/"+path;}
                return path+"/"+obj.GetType().FullName;
            }
            var rows=new List<string>();
            foreach(var component in SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(g=>g.name!="IndoorTestCourse" && g.name!="RehabJunctionCourse")
                .SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
            {
                Require(component!=null,"Missing script in control graph.");
                var data=new SerializedObject(component);var it=data.GetIterator();
                while(it.Next(true))
                    if(it.propertyType==SerializedPropertyType.ObjectReference && !it.propertyPath.StartsWith("m_Prefab"))
                        rows.Add(ObjectKey(component)+":"+it.propertyPath+"="+ObjectKey(it.objectReferenceValue));
            }
            rows.Sort(StringComparer.Ordinal);return string.Join("\n",rows);
        }
        static void CheckRoutes(GameObject root)
        {
            var movement=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var simulation=UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            var camera=follow.GetComponent<Camera>();
            var tick=typeof(CameraFollow).GetMethod("Follow",BindingFlags.Instance|BindingFlags.NonPublic);
            var blockers=root.GetComponentsInChildren<Renderer>().Where(r=>r.bounds.max.y>.35f).ToArray();
            int frames=0;
            void Frame(bool forward,float turn)
            {
                movement.ApplyInput(forward,turn,1f/60f);tick.Invoke(follow,new object[]{1f/60f});
                Vector3 p=movement.transform.position;
                Require(Mathf.Abs(p.y-.5f)<.001f,"Chair Y changed.");
                Vector3 target=p+Vector3.up*.5f, dir=target-camera.transform.position;
                var ray=new Ray(camera.transform.position,dir.normalized);
                foreach(var r in blockers)
                    Require(!r.bounds.IntersectRay(ray,out float d)||d>dir.magnitude,
                        "Camera occluded by "+r.name+" at "+p+" yaw "+movement.HeadingDegrees);
                var v=camera.WorldToViewportPoint(target);
                Require(v.z>0 && v.x>.2f && v.x<.95f && v.y>.05f && v.y<.95f,"Chair outside usable view.");
                frames++;
            }
            foreach(int side in new[]{-1,1})
            {
                simulation.ResetSimulation();simulation.SetControlSource(WheelchairControlSource.Keyboard);
                simulation.StartSimulation();follow.SnapToTarget();
                for(int i=0;i<300;i++)Frame(true,0);
                for(int i=0;i<90;i++)Frame(false,side);
                for(int i=0;i<345;i++)Frame(true,0);
                Require(Vector3.Distance(movement.transform.position,new Vector3(side*11.5f,.5f,10))<.02f,"Goal waypoint missed.");
            }
            simulation.ResetSimulation();
            Require(Vector3.Distance(movement.transform.position,new Vector3(0,.5f,0))<.001f && !simulation.IsRunning,"Reset pose/state failed.");
            Debug.Log("REHAB_ROUTES_OK: "+frames+" movement/camera samples; both centreline routes and 90-degree turns; no geometry occludes chair centre.");
        }
        public static string Argument(string key)
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);
            return i>=0 && i+1<args.Length?args[i+1]:null;
        }
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    }

    // Captures real Play Mode Game View with the existing overlay HUD. Inert in normal use.
    [InitializeOnLoad]
    public static class RehabJunctionCapture
    {
        const string Key="EEG.RehabJunctionCapture";
        static int stage,frames,lastFrame=-1;
        static bool requested;
        static double deadline;
        static DateTime requestedAt;
        static readonly string[] Names={"Start","Approach","JunctionLeft","GoalA","JunctionRight","GoalB","Overview"};
        static Type WindowType=>typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeWindow");
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
        static void Resolution(uint w,uint h)=>WindowType.GetMethod("SetCustomRenderingResolution",Flags).Invoke(null,new object[]{w,h,"Rehab QA"});
        static RehabJunctionCapture()
        {
            if(SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update+=Tick;}
        }
        public static void Begin()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Capture requires batch mode.");
            EditorSceneManager.OpenScene(RehabJunctionSetup.ScenePath);
            object[] previous={(uint)0,(uint)0};WindowType.GetMethod("GetRenderingResolution",Flags).Invoke(null,previous);
            SessionState.SetInt(Key+"Width",(int)(uint)previous[0]);SessionState.SetInt(Key+"Height",(int)(uint)previous[1]);
            SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();
        }
        static void Finish(int result)
        {
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
            Resolution((uint)SessionState.GetInt(Key+"Width",1920),(uint)SessionState.GetInt(Key+"Height",1080));
            EditorApplication.Exit(result);
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Rehab Game View capture timed out.");
                if(!EditorApplication.isPlaying||Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
                if(frames==0)
                {
                    Resolution(1600,900);
                    var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
                    Vector3[] poses={new Vector3(0,.5f,0),new Vector3(0,.5f,6),new Vector3(0,.5f,10),new Vector3(-11.5f,.5f,10),new Vector3(0,.5f,10),new Vector3(11.5f,.5f,10),new Vector3(0,.5f,0)};
                    float[] yaws={0,0,270,270,90,90,0};
                    chair.transform.SetPositionAndRotation(poses[stage],Quaternion.Euler(0,yaws[stage],0));
                    var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();follow.SnapToTarget();
                    if(stage==6)
                    {
                        follow.enabled=false;var cam=follow.GetComponent<Camera>();
                        cam.transform.SetPositionAndRotation(new Vector3(0,29,-17),Quaternion.Euler(51,0,0));
                        cam.orthographic=true;cam.orthographicSize=13;
                        UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                    }
                }
                if(++frames<25)return;
                string folder=RehabJunctionValidation.Argument("-rehabPreviewFolder");Directory.CreateDirectory(folder);
                string path=Path.Combine(folder,Names[stage]+".png");
                if(!requested){requestedAt=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);requested=true;return;}
                if(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<requestedAt)return;
                Debug.Log("REHAB_CAPTURE_OK: "+path);stage++;frames=0;requested=false;
                if(stage==Names.Length)Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
    }
}
