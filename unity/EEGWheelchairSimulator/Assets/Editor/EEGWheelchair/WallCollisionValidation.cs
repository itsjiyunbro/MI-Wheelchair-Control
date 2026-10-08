using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    public static class WallCollisionValidation
    {
        public static void ConfigureAndValidate()
        {
            WallCollisionSetup.Configure();
            var paths=WallCollisionSetup.Scenes.Concat(new[]{
                "Assets/Art/Prefabs/Environment/IndoorTestCourse.prefab",
                "Assets/Art/Prefabs/Environment/RehabJunction/Modules/WallPanel2m.prefab",
                "Assets/Art/Prefabs/Environment/RehabJunction/Modules/DecorativeDoor.prefab",
                "ProjectSettings/TagManager.asset"}).ToArray();
            var before=paths.Select(File.ReadAllText).ToArray();
            WallCollisionSetup.Configure();
            for(int i=0;i<paths.Length;i++)Require(before[i]==File.ReadAllText(paths[i]),"Setup not idempotent: "+paths[i]);
            foreach(var path in WallCollisionSetup.Scenes)
            {
                EditorSceneManager.OpenScene(path);CheckScene();
                // Reuse original centreline/camera checks without earlier setup or old no-collider assumptions.
                bool rehab=path.Contains("Rehab");
                var root=GameObject.Find(rehab?"RehabJunctionCourse":"IndoorTestCourse");
                Type type=rehab?typeof(RehabJunctionValidation):typeof(IndoorTestCourseSetup);
                type.GetMethod(rehab?"CheckRoutes":"CheckRouteAndPreview",BindingFlags.NonPublic|BindingFlags.Static)
                    .Invoke(null,new object[]{root});
                Debug.Log("WALL_SCENE_OK: "+path+"; colliders, sweep, stop, yaw escape, frame rate and original route.");
            }
            EditorSceneManager.OpenScene(WallCollisionSetup.Scenes[0]);
            WallCollisionPlayValidation.Begin();
        }
        public static void ValidateRehabPlay()
        {
            EditorSceneManager.OpenScene(WallCollisionSetup.Scenes[1]);
            WallCollisionPlayValidation.Begin();
        }
        static void CheckScene()
        {
            var movement=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var guard=movement.GetComponent<WheelchairCollisionGuard>();
            var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            int layer=LayerMask.NameToLayer(WallCollisionSetup.LayerName);
            Require(guard!=null && movement.GetComponents<WheelchairCollisionGuard>().Length==1,"Guard missing/duplicated.");
            Require(movement.GetComponent<Rigidbody>()==null,"Rigidbody introduced.");
            var data=new SerializedObject(guard);float radius=data.FindProperty("clearanceRadius").floatValue;
            Require(data.FindProperty("obstacleLayers").intValue==(1<<layer),"Wrong layer mask.");
            // Mesh bounds in world space conservatively cover every visual vertex, not the hidden placeholder.
            foreach(var mesh in movement.transform.Find("WheelchairVisual").GetComponentsInChildren<MeshFilter>())
            {
                var b=mesh.sharedMesh.bounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {
                    var p=mesh.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)))-movement.transform.position;
                    Require(new Vector2(p.x,p.z).magnitude<=radius,"Visual extends outside clearance circle: "+mesh.name);
                }
            }
            Require(!Physics.CheckSphere(movement.transform.position+Vector3.up*.2f,radius,1<<layer,QueryTriggerInteraction.Ignore),"Start overlaps a wall.");
            var walls=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.gameObject.layer==layer).ToArray();
            Require(walls.Length>=10 && walls.All(c=>c.enabled&&!c.isTrigger),"Wall/door collider setup missing.");
            Require(walls.All(c=>c.name.StartsWith("Wall_")||c.name=="WallBody"||c.name=="Panel"),"Non-wall decoration collides.");
            var initial=movement.transform.position;var rotation=movement.transform.rotation;
            sim.SetControlSource(WheelchairControlSource.Keyboard);sim.ResetSimulation();
            movement.ApplyInput(true,1,10);Require(movement.transform.position==initial && movement.transform.rotation==rotation,"STOPPED bypassed.");
            float? stopX=null;
            foreach(int fps in new[]{30,60,120})
            {
                sim.ResetSimulation();sim.StartSimulation();movement.transform.rotation=Quaternion.Euler(0,90,0);
                for(int i=0;i<fps*3;i++)movement.ApplyInput(true,0,1f/fps);
                var p=movement.transform.position;
                Require(p.x>.5f && p.x<1.5f && Mathf.Abs(p.y-initial.y)<.0001f,"Did not stop before side wall/door: "+p);
                Require(!movement.IsMoving,"Blocked command still reports FORWARD.");
                if(stopX.HasValue)Require(Mathf.Abs(stopX.Value-p.x)<.002f,"Collision depends on frame rate.");stopX=p.x;
                // The entire yaw sweep fits the same envelope; full turn and escape must remain possible.
                for(int i=0;i<360;i++)movement.ApplyInput(false,1,1f/60f);
                Require(Vector3.Distance(p,movement.transform.position)<.0001f,"Turning translated the chair.");
                movement.ApplyInput(false,1,3); // 180 degrees, face away from the wall.
                movement.ApplyInput(true,0,.5f);
                Require(movement.transform.position.x<p.x-.9f && movement.IsMoving,"Cannot leave wall after turning away.");
            }
            sim.ResetSimulation();sim.StartSimulation();movement.transform.rotation=Quaternion.Euler(0,45,0);
            movement.ApplyInput(true,0,10);Physics.SyncTransforms();
            Require(!Physics.CheckSphere(movement.transform.position+Vector3.up*.2f,radius,1<<layer,QueryTriggerInteraction.Ignore),"Diagonal sweep penetrated wall.");
            var test=new GameObject("CollisionQATemporaryThinWall");var box=test.AddComponent<BoxCollider>();
            test.layer=layer;test.transform.position=new Vector3(0,.7f,1.6f);box.size=new Vector3(5,2,.01f);
            try
            {
                sim.ResetSimulation();sim.StartSimulation();movement.ApplyInput(true,0,5);
                Require(movement.transform.position.z>0 && movement.transform.position.z<.5f,"Large step tunneled through thin wall.");
                // Triggers and non-obstacle layers must not block the existing ground/control graph.
                box.isTrigger=true;sim.ResetSimulation();sim.StartSimulation();movement.ApplyInput(true,0,1);
                Require(Mathf.Abs(movement.transform.position.z-2)<.001f,"Trigger blocked motion.");
                box.isTrigger=false;test.layer=0;sim.ResetSimulation();sim.StartSimulation();movement.ApplyInput(true,0,1);
                Require(Mathf.Abs(movement.transform.position.z-2)<.001f,"Default layer blocked motion.");
                test.layer=layer;test.transform.position=initial+Vector3.up*.2f;
                sim.ResetSimulation();sim.StartSimulation();movement.ApplyInput(true,0,5);
                Require(movement.transform.position==initial && !movement.IsMoving,"Overlapping cast origin tunneled.");
            }
            finally{UnityEngine.Object.DestroyImmediate(test);sim.ResetSimulation();Physics.SyncTransforms();}
            Require(movement.transform.position==initial && Quaternion.Angle(movement.transform.rotation,rotation)<.01f && !sim.IsRunning,"Reset changed.");
        }
        internal static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }

    [InitializeOnLoad]
    public static class WallCollisionPlayValidation
    {
        const string Key="EEG.WallCollisionQA";
        static IEnumerator checks;
        static int lastFrame=-1;
        static string runtimeError;
        static System.Net.Sockets.TcpClient peer;
        static Type WindowType=>typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeWindow");
        const BindingFlags WindowFlags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
        static void Resolution(uint w,uint h)=>WindowType.GetMethod("SetCustomRenderingResolution",WindowFlags)
            .Invoke(null,new object[]{w,h,"Collision QA"});
        static WallCollisionPlayValidation()
        {
            EditorApplication.playModeStateChanged+=OnPlay;
            if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;
        }
        public static void Begin()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("QA is batch-only.");
            if(!string.IsNullOrEmpty(RehabJunctionValidation.Argument("-collisionPreviewFolder")))
            {
                object[] previous={(uint)0,(uint)0};WindowType.GetMethod("GetRenderingResolution",WindowFlags).Invoke(null,previous);
                SessionState.SetInt(Key+"Width",(int)(uint)previous[0]);SessionState.SetInt(Key+"Height",(int)(uint)previous[1]);
            }
            SessionState.SetBool(Key,true);SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+180);
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        static void OnPlay(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {
                if(!string.IsNullOrEmpty(RehabJunctionValidation.Argument("-collisionPreviewFolder")))Resolution(1600,900);
                Application.logMessageReceived+=Log;checks=Run();lastFrame=-1;
            }
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Done",false))
            { SessionState.SetBool(Key+"Done",false);EditorApplication.Exit(SessionState.GetInt(Key+"Exit",1)); }
        }
        static void Log(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)runtimeError=message;}
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"Deadline",0))throw new TimeoutException("Collision QA timed out.");
                if(checks==null||!EditorApplication.isPlaying||Time.frameCount==lastFrame)return;
                lastFrame=Time.frameCount;if(!checks.MoveNext())Finish(null);
            }
            catch(Exception error){Finish(error);}
        }
        static void Finish(Exception error)
        {
            Application.logMessageReceived-=Log;peer?.Dispose();peer=null;
            // RunChecks is reused without its separate Begin/Finish lifecycle.
            var type=typeof(Step07PredictionValidation);
            foreach(string name in new[]{"keyboard","mouse"})
            {
                var device=type.GetField(name,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null) as InputDevice;
                if(device!=null && device.added)InputSystem.RemoveDevice(device);
            }
            var sender=type.GetField("sender",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null) as System.Diagnostics.Process;
            if(sender!=null){if(!sender.HasExited)sender.Kill();sender.Dispose();}
            (type.GetField("wire",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null) as IDisposable)?.Dispose();
            if(!string.IsNullOrEmpty(RehabJunctionValidation.Argument("-collisionPreviewFolder")))
                Resolution((uint)SessionState.GetInt(Key+"Width",1920),(uint)SessionState.GetInt(Key+"Height",1080));
            EditorApplication.update-=Tick;SessionState.SetBool(Key,false);SessionState.SetBool(Key+"Done",true);
            SessionState.SetInt(Key+"Exit",error==null?0:1);
            if(error!=null)Debug.LogException(error);else Debug.Log("WALL_PLAY_OK: old real keyboard/button/Python regression and blocked prediction versus actual movement passed.");
            EditorApplication.ExitPlaymode();
        }
        static IEnumerator Wait(Func<bool> ready,string message)
        {
            double end=EditorApplication.timeSinceStartup+12;
            while(!ready()){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(message);yield return null;}
        }
        static IEnumerator Run()
        {
            var regression=(IEnumerator)typeof(Step07PredictionValidation).GetMethod("RunChecks",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            while(regression.MoveNext())yield return regression.Current;
            var movement=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            var receiver=movement.GetComponent<PythonWheelchairReceiver>();
            var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();
            string Text(string name)=>hud.transform.Find("StatusPanel/"+name).GetComponent<Text>().text;
            sim.ResetSimulation();sim.SetControlSource(WheelchairControlSource.Python);
            var wait=Wait(()=>receiver.IsListening,"Listener missing");while(wait.MoveNext())yield return null;
            peer=new System.Net.Sockets.TcpClient();var connecting=peer.ConnectAsync("127.0.0.1",5055);
            wait=Wait(()=>connecting.IsCompleted&&receiver.IsConnected,"Peer connection failed");while(wait.MoveNext())yield return null;
            WallCollisionValidation.Require(!connecting.IsFaulted,"Connect failed.");
            bool rehab=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Rehab");
            movement.transform.SetPositionAndRotation(rehab?new Vector3(11.5f,.5f,10):new Vector3(0,.5f,0),Quaternion.Euler(0,90,0));
            UnityEngine.Object.FindFirstObjectByType<CameraFollow>().SnapToTarget();
            sim.StartSimulation();
            byte[] data=System.Text.Encoding.UTF8.GetBytes("{\"command\":\"FORWARD\",\"confidence\":0.87,\"timestamp\":1726362000.125,\"sequence\":100}\n");
            peer.GetStream().Write(data,0,data.Length);
            float startX=movement.transform.position.x;
            wait=Wait(()=>movement.transform.position.x>startX+.1f && !movement.IsMoving,"Held TCP command did not reach and stop at wall");
            while(wait.MoveNext())yield return null;
            for(int i=0;i<5;i++)yield return null;
            var blocked=movement.transform.position;
            WallCollisionValidation.Require(Text("PredictionText")=="PREDICTION: FORWARD" && Text("MoveText")=="MOVE: STOPPED"
                && Text("SteeringText")=="STEERING: STRAIGHT" && sim.IsRunning,"Prediction/actual movement conflated at wall.");
            for(int i=0;i<15;i++)yield return null;
            WallCollisionValidation.Require(Vector3.Distance(blocked,movement.transform.position)<.0001f,"Held command creeps through wall.");
            string folder=RehabJunctionValidation.Argument("-collisionPreviewFolder");
            if(!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
                string path=Path.Combine(folder,rehab?"Rehab-Blocked.png":"Main-Blocked.png");
                DateTime request=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);
                wait=Wait(()=>File.Exists(path)&&File.GetLastWriteTimeUtc(path)>=request,"Capture failed");
                while(wait.MoveNext())yield return null;
                Debug.Log("WALL_CAPTURE_OK: "+path);
            }
            sim.StopSimulation();sim.StartSimulation();
            for(int i=0;i<6;i++)yield return null;
            WallCollisionValidation.Require(receiver.CurrentCommand=="STOP" && !movement.IsMoving,"START replayed blocked command.");
            sim.ResetSimulation();
            WallCollisionValidation.Require(!sim.IsRunning && Vector3.Distance(movement.transform.position,new Vector3(0,.5f,0))<.001f,"Reset from wall failed.");
            peer.Dispose();peer=null;
            wait=Wait(()=>!receiver.IsConnected,"Disconnect failed");while(wait.MoveNext())yield return null;
            WallCollisionValidation.Require(runtimeError==null,"Runtime error: "+runtimeError);
            Debug.Log("WALL_PREDICTION_OK: FORWARD prediction persists while wall forces MOVE STOPPED; held command stable; STOP/START/RESET preserved.");
        }
    }
}
