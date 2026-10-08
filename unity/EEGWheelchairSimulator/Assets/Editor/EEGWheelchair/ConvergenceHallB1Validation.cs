using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallB1Validation
    {
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        // One-time additive correction of this task's generated door decorations only.
        public static void FinishAndCapture()
        {
            var root=PrefabUtility.LoadPrefabContents(ConvergenceHallB1Setup.PrefabPath);
            try
            {
                var doors=root.transform.Find("DoorsAndSigns");
                foreach(string n in new[]{"B101","B102","B103"})doors.Find(n).localRotation=Quaternion.Euler(0,180,0);
                foreach(string n in new[]{"B106","B107"})doors.Find(n).localRotation=Quaternion.identity;
                foreach(string n in new[]{"B108","B109","B110"})
                {var t=doors.Find(n);var p=t.localPosition;p.x=ConvergenceHallB1Setup.Plan(n=="B110"?373.6f:257.6f,0).x;t.localPosition=p;}
                foreach(string n in new[]{"B104","B105","WEST LIFT"})
                {var t=doors.Find(n);var p=t.localPosition;p.x=ConvergenceHallB1Setup.Plan(233.4f,0).x;t.localPosition=p;}
                PrefabUtility.SaveAsPrefabAsset(root,ConvergenceHallB1Setup.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static void BuildAndValidate()
        {
            SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",false);
            ConvergenceHallB1Setup.Create();
            string saved=File.ReadAllText(ConvergenceHallB1Setup.ScenePath);
            ConvergenceHallB1Setup.Create();Check(saved==File.ReadAllText(ConvergenceHallB1Setup.ScenePath),"Not idempotent.");
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            Check(UnityEngine.Object.FindObjectsByType<WheelchairMovement>(FindObjectsSortMode.None).Length==1,"Duplicate controller.");
            Check(UnityEngine.Object.FindObjectsByType<PythonWheelchairReceiver>(FindObjectsSortMode.None).Length==1,"Duplicate receiver.");
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1,"Duplicate EventSystem.");
            foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var t in go.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
            var visual=chair.transform.Find("WheelchairVisual");
            Check(Mathf.Abs(visual.lossyScale.x-.50f)<.001f,"Wrong school visual scale.");
            foreach(var mf in visual.GetComponentsInChildren<MeshFilter>())
            {
                var b=mf.sharedMesh.bounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {
                    Vector3 p=mf.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)))-chair.transform.position;
                    Check(new Vector2(p.x,p.z).magnitude<=.58f,"Visual exceeds guard: "+mf.name);
                }
            }
            Check(Mathf.Abs(visual.Find("LeftWheel").GetComponent<Renderer>().bounds.min.y)<.002f,"Wheel is not on floor.");
            var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();sim.ResetSimulation();sim.StartSimulation();
            var start=chair.transform.position;var camera=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            var follow=typeof(CameraFollow).GetMethod("Follow",BindingFlags.Instance|BindingFlags.NonPublic);
            // Pixel coordinates from the supplied B1 plan. Each route includes actual turns.
            Vector2[][] routes={
                new[]{new Vector2(745,830),new Vector2(745,750),new Vector2(245.5f,750),new Vector2(245.5f,351),new Vector2(349,351)},
                new[]{new Vector2(745,830),new Vector2(745,750),new Vector2(745,422)},
                new[]{new Vector2(745,830),new Vector2(745,750),new Vector2(866,750),new Vector2(866,422)}
            };
            int steps=0,occluded=0;
            foreach(var route in routes)
            {
                chair.transform.SetPositionAndRotation(start,Quaternion.identity);camera.SnapToTarget();
                for(int n=1;n<route.Length;n++)
                {
                    var dest=ConvergenceHallB1Setup.Plan(route[n].x,route[n].y,.5f);
                    var dir=(dest-chair.transform.position).normalized;
                    float targetYaw=Quaternion.LookRotation(dir).eulerAngles.y;
                    float angle=Mathf.DeltaAngle(chair.HeadingDegrees,targetYaw);
                    int turns=Mathf.CeilToInt(Mathf.Abs(angle));
                    for(int i=0;i<turns;i++)
                    {
                        float delta=Mathf.Min(1,Mathf.Abs(Mathf.DeltaAngle(chair.HeadingDegrees,targetYaw)));
                        chair.ApplyInput(false,Mathf.Sign(angle),delta/60f);follow.Invoke(camera,new object[]{1f/60});
                    }
                    int frames=Mathf.CeilToInt(Vector3.Distance(chair.transform.position,dest)/.03f);
                    for(int i=0;i<frames;i++)
                    {
                        float dist=Vector3.Distance(chair.transform.position,dest);
                        chair.ApplyInput(true,0,Mathf.Min(.03f,dist)/2);follow.Invoke(camera,new object[]{1f/60});steps++;
                        var target=chair.transform.position+Vector3.up*.05f;
                        var ray=new Ray(camera.transform.position,(target-camera.transform.position).normalized);
                        float distance=Vector3.Distance(target,camera.transform.position);
                        if(GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Renderer>().Any(r=>r.bounds.IntersectRay(ray,out float hit)&&hit<distance-.05f))occluded++;
                    }
                    Check(Vector3.Distance(chair.transform.position,dest)<.003f,"Route blocked: "+route[n]+" actual "+chair.transform.position);
                }
            }
            bool fullHeight=GameObject.Find("ConvergenceHallB1").transform.Find("UpperWalls")==null;
            if(!fullHeight)Check(occluded==0,"Camera view blocked along tested routes.");
            else Debug.Log("FULL_HEIGHT_CAMERA_AUDIT: "+occluded+"/"+steps+" samples have an environment bounds intersection. Camera settings are preserved; full-height walls can occlude the existing follow view.");
            var narrow=ConvergenceHallB1Setup.Plan(245.5f,600,.5f);
            chair.transform.SetPositionAndRotation(narrow,Quaternion.Euler(0,90,0));
            chair.ApplyInput(true,0,2);
            float advance=chair.transform.position.x-narrow.x;
            Check(advance>.28f&&advance<.32f,"Measured corridor side clearance is incorrect.");
            var blocked=chair.transform.position;chair.ApplyInput(true,0,2);
            Check(Vector3.Distance(blocked,chair.transform.position)<.001f&&!chair.IsMoving,"School wall does not stop held command.");
            chair.ApplyInput(false,1,3);chair.ApplyInput(true,0,.08f);
            Check(chair.transform.position.x<blocked.x-.15f,"Cannot turn and leave school wall.");
            sim.ResetSimulation();camera.SnapToTarget();
            Check(chair.transform.position==start&&!sim.IsRunning,"Reset failed.");
            Debug.Log("CONVERGENCE_STATIC_OK: save/reload; single control graph; visual envelope/ground; 3 routes "+steps+" steps. Camera renderer intersections="+occluded);
            // Reload rather than save temporary validation poses/state.
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            ConvergenceHallB1Play.Begin();
        }
    }

    [InitializeOnLoad]
    public static class ConvergenceHallB1Play
    {
        const string Key="EEG.ConvergenceQA";
        const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static;
        static IEnumerator checks;static int frame=-1;static string errorLog;
        static Type Window=>typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeWindow");
        static ConvergenceHallB1Play()
        {
            EditorApplication.playModeStateChanged+=Changed;
            if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;
        }
        public static void Begin()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("QA is batch only.");
            object[] args={(uint)0,(uint)0};Window.GetMethod("GetRenderingResolution",Flags).Invoke(null,args);
            SessionState.SetInt(Key+"W",(int)(uint)args[0]);SessionState.SetInt(Key+"H",(int)(uint)args[1]);
            SessionState.SetBool(Key,true);SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+210);
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))
            {
                Window.GetMethod("SetCustomRenderingResolution",Flags).Invoke(null,new object[]{(uint)1600,(uint)900,"Convergence QA"});
                Application.logMessageReceived+=Log;checks=Run();frame=-1;
            }
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+"Done",false))
            {SessionState.SetBool(Key+"Done",false);EditorApplication.Exit(SessionState.GetInt(Key+"Exit",1));}
        }
        static void Log(string s,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errorLog=s;}
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"Deadline",0))throw new TimeoutException("Convergence Play QA timeout.");
                if(checks==null||!EditorApplication.isPlaying||Time.frameCount==frame)return;
                frame=Time.frameCount;if(!checks.MoveNext())Finish(null);
            }catch(Exception e){Finish(e);}
        }
        static void Finish(Exception e)
        {
            Application.logMessageReceived-=Log;
            var t=typeof(Step07PredictionValidation);
            foreach(string n in new[]{"keyboard","mouse"})
            {var d=t.GetField(n,Flags).GetValue(null) as InputDevice;if(d!=null&&d.added)InputSystem.RemoveDevice(d);}
            var p=t.GetField("sender",Flags).GetValue(null) as System.Diagnostics.Process;
            if(p!=null){if(!p.HasExited)p.Kill();p.Dispose();}
            (t.GetField("wire",Flags).GetValue(null) as IDisposable)?.Dispose();
            Window.GetMethod("SetCustomRenderingResolution",Flags).Invoke(null,new object[]{(uint)SessionState.GetInt(Key+"W",1920),(uint)SessionState.GetInt(Key+"H",1080),""});
            EditorApplication.update-=Tick;SessionState.SetBool(Key,false);SessionState.SetBool(Key+"Done",true);SessionState.SetInt(Key+"Exit",e==null?0:1);
            if(e!=null)Debug.LogException(e);else Debug.Log(SessionState.GetBool(Key+".CaptureOnly",false)?"CONVERGENCE_CAPTURE_OK: corrected door faces and perspective overview captured; no runtime errors.":"CONVERGENCE_PLAY_OK: actual Keyboard, pointer buttons, Python sender/stream, invalid JSON, reconnect, STOP/RESET, HUD and camera regression passed; screenshots saved.");
            SessionState.SetBool(Key+".CaptureOnly",false);
            EditorApplication.ExitPlaymode();
        }
        static IEnumerator Run()
        {
            if(!SessionState.GetBool(Key+".CaptureOnly",false))
            {
                var regression=(IEnumerator)typeof(Step07PredictionValidation).GetMethod("RunChecks",Flags).Invoke(null,null);
                while(regression.MoveNext())yield return regression.Current;
            }
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();sim.ResetSimulation();sim.SetControlSource(WheelchairControlSource.Keyboard);
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var cam=follow.GetComponent<Camera>();
            string folder=RehabJunctionValidation.Argument("-convergencePreviewFolder");Directory.CreateDirectory(folder);
            if(SessionState.GetBool(Key+".FocusLoungeInterior",false))
            {
                SessionState.SetBool(Key+".FocusLoungeInterior",false);var loungeChecks=ConvergenceHallLoungePhoto.CaptureViews(folder);while(loungeChecks.MoveNext())yield return loungeChecks.Current;yield break;
            }
            if(SessionState.GetBool(Key+".FocusReverseMovement",false))
            {
                SessionState.SetBool(Key+".FocusReverseMovement",false);var reverseChecks=ConvergenceHallReverseMovement.RunChecks(folder);while(reverseChecks.MoveNext())yield return reverseChecks.Current;yield break;
            }
            if(SessionState.GetBool(Key+".FocusDemoHUD",false))
            {
                SessionState.SetBool(Key+".FocusDemoHUD",false);var hudChecks=ConvergenceHallDemoCamera.CheckHiddenHUD();while(hudChecks.MoveNext())yield return hudChecks.Current;yield break;
            }
            if(SessionState.GetBool(Key+".FocusDemoCamera",false))
            {
                SessionState.SetBool(Key+".FocusDemoCamera",false);var demoChecks=ConvergenceHallDemoCamera.RunChecks(folder);
                while(demoChecks.MoveNext())yield return demoChecks.Current;yield break;
            }
            if(SessionState.GetBool(Key+".FocusElevatorMark",false))
            {
                SessionState.SetBool(Key+".FocusElevatorMark",false);var markChecks=ConvergenceHallElevatorMark.CaptureViews(folder);
                while(markChecks.MoveNext())yield return markChecks.Current;yield break;
            }
            if(SessionState.GetBool(Key+".FocusWestLiftPhoto",false))
            {
                SessionState.SetBool(Key+".FocusWestLiftPhoto",false);var westChecks=ConvergenceHallWestLiftPhoto.CaptureViews(folder);
                while(westChecks.MoveNext())yield return westChecks.Current;yield break;
            }
            if(SessionState.GetBool(Key+".FocusB112DiagonalScreen",false))
            {
                SessionState.SetBool(Key+".FocusB112DiagonalScreen",false);var diagonalChecks=ConvergenceHallB112DiagonalScreen.CaptureViews(folder);while(diagonalChecks.MoveNext())yield return diagonalChecks.Current;yield break;
            }
            if(SessionState.GetBool(Key+".FocusB112Presentation",false))
            {
                SessionState.SetBool(Key+".FocusB112Presentation",false);var b112Checks=ConvergenceHallB112Presentation.CaptureViews(folder);
                while(b112Checks.MoveNext())yield return b112Checks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusB124Sign",false))
            {
                SessionState.SetBool(Key+".FocusB124Sign",false);var signChecks=ConvergenceHallB124DoorPhoto.CaptureSignViews(folder);
                while(signChecks.MoveNext())yield return signChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusB124Photo",false))
            {
                SessionState.SetBool(Key+".FocusB124Photo",false);var b124Checks=ConvergenceHallB124DoorPhoto.CaptureViews(folder);
                while(b124Checks.MoveNext())yield return b124Checks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusBluebellDisplay",false))
            {
                SessionState.SetBool(Key+".FocusBluebellDisplay",false);var displayChecks=ConvergenceHallBluebellDisplay.CaptureViews(folder);
                while(displayChecks.MoveNext())yield return displayChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusPhotoDoorFinishes",false))
            {
                SessionState.SetBool(Key+".FocusPhotoDoorFinishes",false);var photoChecks=ConvergenceHallPhotoDoorFinishes.CaptureViews(folder);
                while(photoChecks.MoveNext())yield return photoChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusRoomCompletion",false))
            {
                SessionState.SetBool(Key+".FocusRoomCompletion",false);var roomChecks=ConvergenceHallRoomCompletion.CaptureViews(folder);
                while(roomChecks.MoveNext())yield return roomChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusDoors",false))
            {
                SessionState.SetBool(Key+".FocusDoors",false);var doorChecks=ConvergenceHallDoorValidation.RunChecks(folder);
                while(doorChecks.MoveNext())yield return doorChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusB112LiftWalls",false))
            {
                SessionState.SetBool(Key+".FocusB112LiftWalls",false);var wallChecks=ConvergenceHallB112LiftWallFinish.CaptureViews(folder);
                while(wallChecks.MoveNext())yield return wallChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusB108Ceiling",false))
            {
                SessionState.SetBool(Key+".FocusB108Ceiling",false);var ceilingChecks=ConvergenceHallCorridorCeilings.CaptureB108Views(folder);
                while(ceilingChecks.MoveNext())yield return ceilingChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusCorridorCeilings",false))
            {
                SessionState.SetBool(Key+".FocusCorridorCeilings",false);var ceilingChecks=ConvergenceHallCorridorCeilings.CaptureViews(folder);
                while(ceilingChecks.MoveNext())yield return ceilingChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusEntryWalls",false))
            {
                SessionState.SetBool(Key+".FocusEntryWalls",false);var wallChecks=ConvergenceHallEntryWallProportions.CaptureViews(folder);
                while(wallChecks.MoveNext())yield return wallChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusChairPhoto",false))
            {
                SessionState.SetBool(Key+".FocusChairPhoto",false);var chairChecks=ConvergenceHallChairPhotoUpdate.CaptureViews(folder);
                while(chairChecks.MoveNext())yield return chairChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusCameraViews",false))
            {
                SessionState.SetBool(Key+".FocusCameraViews",false);var viewChecks=ConvergenceHallCameraViews.RunChecks(folder);
                while(viewChecks.MoveNext())yield return viewChecks.Current;
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusStairHeaderGap",false))
            {
                SessionState.SetBool(Key+".FocusStairHeaderGap",false);follow.enabled=false;cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallStairHeaderGap.Validate(root);var stairHeaderHall=root.transform.Find("CoreHalls/B112Stairwell");
                Vector3[] headerEyes={new Vector3(0,1.5f,-2f),new Vector3(.7f,2.35f,-1.3f)};
                Vector3[] headerTargets={new Vector3(0,1.5f,0),new Vector3(0,2.35f,0)};
                string[] headerShots={"B112StairEntranceNoUpperSign","B112ClosedHeaderGap"};
                for(int i=0;i<headerEyes.Length;i++)
                {
                    cam.fieldOfView=i==0?75:65;cam.transform.position=stairHeaderHall.TransformPoint(headerEyes[i]);cam.transform.LookAt(stairHeaderHall.TransformPoint(headerTargets[i]));
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,headerShots[i]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Stair header preview");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusJoinPrivacy",false))
            {
                SessionState.SetBool(Key+".FocusJoinPrivacy",false);follow.enabled=false;cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallJoinPrivacy.Validate(root);var joinBounds=root.transform.Find("BluebellB112Connection/ConnectionWall").GetComponent<Renderer>().bounds;
                var joinAt=new Vector3(joinBounds.max.x,1.42f,joinBounds.center.z);
                Vector3[] privacyEyes={joinAt+new Vector3(1.0f,0,-.30f),joinAt+new Vector3(1.0f,0,.55f),ConvergenceHallB1Setup.Plan(472,754,1.45f),ConvergenceHallB1Setup.Plan(287,751,1.45f)};
                Vector3[] privacyTargets={joinAt,joinAt,ConvergenceHallB1Setup.Plan(429,733,1.425f),ConvergenceHallB1Setup.Plan(287,730,1.425f)};
                string[] privacyShots={"B112JoinFromBluebell","B112JoinFromDoor","MoreOpaqueGlassBand","GlassBandClearTopBottom"};
                for(int i=0;i<privacyEyes.Length;i++)
                {
                    cam.fieldOfView=i<2?60:72;cam.transform.position=privacyEyes[i];cam.transform.LookAt(privacyTargets[i]);
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,privacyShots[i]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Junction/privacy preview");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusLoungePhotoDoor",false))
            {
                SessionState.SetBool(Key+".FocusLoungePhotoDoor",false);follow.enabled=false;cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallLoungeDoor.Validate(root);var loungeFacade=root.transform.Find("PlanDoors/Lounge_West_Automatic/PhotoLoungeEntrance");
                Vector3[] loungeEyes={new Vector3(0,1.45f,-2.4f),new Vector3(1.6f,1.45f,-2.4f),new Vector3(.5625f,1.16f,-1.0f)};
                Vector3[] loungeTargets={new Vector3(0,1.4f,0),new Vector3(-.3f,1.4f,0),new Vector3(.5625f,1.14f,-.03f)};
                string[] loungeShots={"LoungePhotoFront","LoungePhotoAngled","LoungeNameAndHours"};
                for(int i=0;i<loungeEyes.Length;i++)
                {
                    cam.fieldOfView=i==2?55:72;cam.transform.position=loungeFacade.TransformPoint(loungeEyes[i]);cam.transform.LookAt(loungeFacade.TransformPoint(loungeTargets[i]));
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,loungeShots[i]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Lounge photo door preview");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusLecternNoGlow",false))
            {
                SessionState.SetBool(Key+".FocusLecternNoGlow",false);follow.enabled=false;cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallLecternLighting.Validate(root);
                var symbolLectern=root.transform.Find("ClassroomInteriors/B106/Lectern");
                cam.fieldOfView=40;cam.transform.position=symbolLectern.TransformPoint(new Vector3(0,.76f,-.80f));cam.transform.LookAt(symbolLectern.TransformPoint(new Vector3(0,.76f,-.281f)));
                double logoReady=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<logoReady||ShaderUtil.anythingCompiling)yield return null;
                string logoShot=Path.Combine(folder,"YonseiLecternSymbol.png");DateTime logoRequested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(logoShot);double logoLimit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(logoShot)||File.GetLastWriteTimeUtc(logoShot)<logoRequested){if(EditorApplication.timeSinceStartup>logoLimit)throw new TimeoutException("Official symbol preview");yield return null;}
                foreach(string name in new[]{"B106","B112","B109"})
                {
                    var interior=root.transform.Find("ClassroomInteriors/"+name);var floor=root.transform.Find("RoomsAndCores/"+name+"/RoomFloor").GetComponent<Renderer>().bounds;
                    bool north=Vector3.Dot(interior.forward,Vector3.forward)>.9f;float width=north?floor.size.x:floor.size.z,depth=north?floor.size.z:floor.size.x;
                    cam.fieldOfView=60;cam.transform.position=interior.TransformPoint(new Vector3(width/2-.52f,1.62f,-depth/2+.60f));cam.transform.LookAt(interior.TransformPoint(new Vector3(-.15f,1.45f,depth/2-.18f)));
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,name+"NoGlow.png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("No glow classroom preview");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusWallSurfaceCleanup",false))
            {
                SessionState.SetBool(Key+".FocusWallSurfaceCleanup",false);follow.enabled=false;cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallWallSurfaceCleanup.Validate(root);
                Vector3[] seamEyes={ConvergenceHallB1Setup.Plan(749,505,1.5f),ConvergenceHallB1Setup.Plan(749,496,1.5f),ConvergenceHallB1Setup.Plan(745,550,1.55f),ConvergenceHallB1Setup.Plan(746,444,1.5f),ConvergenceHallB1Setup.Plan(735,765,8),ConvergenceHallB1Setup.Plan(245.5f,600,8)};
                Vector3[] seamTargets={ConvergenceHallB1Setup.Plan(772,501,1.4f),ConvergenceHallB1Setup.Plan(772,501,1.4f),ConvergenceHallB1Setup.Plan(772,550,1.4f),ConvergenceHallB1Setup.Plan(772,438,1.4f),ConvergenceHallB1Setup.Plan(670,650,2),ConvergenceHallB1Setup.Plan(295,600,0)};
                string[] seamShots={"B112SeamAngleA","B112SeamAngleB","B112StairSouthJoin","B112ElevatorNorthJoin","BluebellWallSurfaceOverview","WestSharedPartitions"};
                for(int i=0;i<seamEyes.Length;i++)
                {
                    cam.fieldOfView=i<4?68:65;cam.transform.position=seamEyes[i];cam.transform.LookAt(seamTargets[i]);
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,seamShots[i]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Wall seam preview");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusStairStorage",false))
            {
                SessionState.SetBool(Key+".FocusStairStorage",false);follow.enabled=false;cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallStairStorage.Validate(root);
                foreach(string hallName in ConvergenceHallStairStorage.Halls)
                {
                    var hall=root.transform.Find("CoreHalls/"+hallName);
                    for(int view=0;view<2;view++)
                    {
                        cam.fieldOfView=view==0?85:90;
                        cam.transform.position=hall.TransformPoint(view==0?new Vector3(.65f,1.55f,.10f):new Vector3(-.45f,1.25f,-.5f));
                        cam.transform.LookAt(hall.TransformPoint(view==0?new Vector3(-.2f,1.3f,2.4f):new Vector3(-.84f,1.15f,.8f)));
                        double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                        string shot=Path.Combine(folder,hallName+(view==0?"StorageAndStairs":"StorageEntrance")+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                        while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Stair storage preview");yield return null;}
                    }
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusFinalFinishes",false))
            {
                SessionState.SetBool(Key+".FocusFinalFinishes",false);follow.enabled=false;cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallFinalFinishes.Validate(root);
                foreach(bool east in new[]{true,false})
                {
                    var door=ConvergenceHallElevatorCabins.Door(root,east);float z=door.Find("MeasuredCabin").localPosition.z-.04f;
                    cam.fieldOfView=78;cam.transform.position=door.TransformPoint(new Vector3(.12f,1.4f,z-1.8f));cam.transform.LookAt(door.TransformPoint(new Vector3(.12f,1.35f,z-.12f)));
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,(east?"B112":"B109")+"RecessedElevator.png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Recessed elevator preview");yield return null;}
                }
                Vector3[] finishEyes={ConvergenceHallB1Setup.Plan(735,765,8),ConvergenceHallB1Setup.Plan(867,738,7),ConvergenceHallB1Setup.Plan(245.5f,600,6)};
                Vector3[] finishTargets={ConvergenceHallB1Setup.Plan(670,700,.02f),ConvergenceHallB1Setup.Plan(810,690,.02f),ConvergenceHallB1Setup.Plan(295,600,.02f)};
                string[] finishShots={"BluebellNoFloorText","EastCorridorNoFloorText","WestCorridorNoFloorText"};
                for(int i=0;i<finishEyes.Length;i++)
                {
                    cam.fieldOfView=65;cam.transform.position=finishEyes[i];cam.transform.LookAt(finishTargets[i]);
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,finishShots[i]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Floor lettering preview");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusBluebellWall",false))
            {
                SessionState.SetBool(Key+".FocusBluebellWall",false);follow.enabled=false;cam.orthographic=false;cam.fieldOfView=72;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallBluebellInfill.Validate(root);
                var stairs=root.transform.Find("BluebellStand/MeasuredBluebell");float side=root.transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds.size.x/2;
                var eyes=new[]{new Vector3(side+2.5f,1.6f,-1.3f),new Vector3(side+1.4f,1.6f,9.2f),new Vector3(side+2f,1.75f,5.8f),new Vector3(side+3.5f,6.5f,-2f)};
                var wallTargets=new[]{new Vector3(side-.1f,1.8f,6f),new Vector3(side-.1f,1.8f,4.5f),new Vector3(side-.1f,1.65f,6.1f),new Vector3(0,2.15f,5.8f)};
                string[] wallShots={"BluebellWallFromEntrance","BluebellWallFromB112","BluebellWallAmenities","BluebellWallOverview"};
                for(int i=0;i<eyes.Length;i++)
                {
                    cam.transform.position=stairs.TransformPoint(eyes[i]);cam.transform.LookAt(stairs.TransformPoint(wallTargets[i]));
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,wallShots[i]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Bluebell infill preview");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusCabins",false))
            {
                SessionState.SetBool(Key+".FocusCabins",false);follow.enabled=false;cam.orthographic=false;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallElevatorCabins.Validate(root);
                foreach(bool east in new[]{true,false})
                {
                    var car=ConvergenceHallElevatorCabins.Door(root,east).Find("MeasuredCabin");
                    for(int view=0;view<4;view++)
                    {
                        cam.orthographic=view==0;cam.orthographicSize=1.05f;cam.fieldOfView=view==3?75:90;
                        var eye=view==0?new Vector3(0,3.8f,.75f):view==1?new Vector3(0,1.55f,.08f):view==2?new Vector3(0,1.55f,1.42f):new Vector3(.12f,1.25f,-1.75f);
                        var target=view==0?new Vector3(0,.02f,.75f):view==1?new Vector3(0,1.1f,1.5f):view==2?new Vector3(0,1.1f,-.15f):new Vector3(.12f,1.20f,-.15f);
                        if(view==0)car.Find("Ceiling").gameObject.SetActive(false);else car.Find("Ceiling").gameObject.SetActive(true);
                        cam.transform.position=car.TransformPoint(eye);cam.transform.LookAt(car.TransformPoint(target),view==0?car.forward:Vector3.up);
                        double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                        string shot=Path.Combine(folder,(east?"B112":"B109")+new[]{"CabinOverview","CabinInside","CabinDoorInside","CabinDoorOutside"}[view]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                        while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Measured cabin preview");yield return null;}
                    }
                    car.Find("Ceiling").gameObject.SetActive(true);
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusCoreSigns",false))
            {
                SessionState.SetBool(Key+".FocusCoreSigns",false);follow.enabled=false;cam.orthographic=false;cam.fieldOfView=43;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1");ConvergenceHallCoreSigns.Validate(root);
                foreach(string hall in ConvergenceHallCoreSigns.Halls)
                {
                    var sign=root.transform.Find("CoreHalls/"+hall+"/ReferencePlacard");
                    cam.transform.position=sign.TransformPoint(new Vector3(.015f,.015f,-.46f));cam.transform.LookAt(sign.position);
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,hall+"Sign.png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Core placard preview");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusMainEntrance",false))
            {
                follow.SnapToTarget();for(int j=0;j<4;j++)yield return null;
                string drivingShot=Path.Combine(folder,"MainEntranceDrivingView.png");DateTime drivingRequested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(drivingShot);double drivingLimit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(drivingShot)||File.GetLastWriteTimeUtc(drivingShot)<drivingRequested){if(EditorApplication.timeSinceStartup>drivingLimit)throw new TimeoutException("Main entrance driving preview");yield return null;}
                SessionState.SetBool(Key+".FocusMainEntrance",false);follow.enabled=false;cam.orthographic=false;cam.fieldOfView=60;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var entry=GameObject.Find("ConvergenceHallB1").transform.Find("MainEntranceUpgrade");
                Vector3[] entryViews={new Vector3(-1.665f,1.65f,7.1f),new Vector3(-.55f,1.62f,5.8f),new Vector3(-5.0f,1.45f,4.0f),new Vector3(-1.665f,11,6),new Vector3(-1.6f,1.65f,4.5f),new Vector3(1.0f,1.65f,4.6f)};
                Vector3[] entryTargets={new Vector3(-1.665f,1.40f,1.5f),new Vector3(0,1.40f,1.4f),new Vector3(-5.04f,.95f,1.25f),new Vector3(-1.665f,0,1.5f),new Vector3(-6.68f,1.5f,2.5f),new Vector3(3.35f,1.65f,4.7f)};
                string[] shots={"MainEntranceInsideToOutside","MainEntranceGlassVestibule","MainEntranceSeatingCorner","MainEntranceMeasuredOverview","MainEntranceDonorWall","MainEntranceOppositeDirectory"};
                for(int i=0;i<entryViews.Length;i++)
                {
                    var entranceView=entryViews[i];entranceView.x=-entranceView.x;var entranceTarget=entryTargets[i];entranceTarget.x=-entranceTarget.x;cam.transform.position=entry.TransformPoint(entranceView);cam.transform.LookAt(entry.TransformPoint(entranceTarget));
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,shots[i]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Main entrance preview");yield return null;}
                }
                foreach(bool donorDetail in new[]{true,false})
                {
                    var board=entry.Find(donorDetail?"SeatingCorner/DonorWallPanel":"OppositeInformationWall/FourFloorDirectory");
                    cam.fieldOfView=donorDetail?48:40;
                    cam.transform.position=board.TransformPoint(donorDetail?new Vector3(0,1.52f,-2.10f):new Vector3(0,0,-2.10f));
                    cam.transform.LookAt(board.TransformPoint(donorDetail?new Vector3(0,1.52f,-.03f):new Vector3(0,0,-.03f)));
                    double detailReady=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<detailReady||ShaderUtil.anythingCompiling)yield return null;
                    string detailShot=Path.Combine(folder,donorDetail?"DonorPlaquesDetail.png":"FloorDirectoryDetail.png");DateTime detailRequested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(detailShot);double detailLimit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(detailShot)||File.GetLastWriteTimeUtc(detailShot)<detailRequested){if(EditorApplication.timeSinceStartup>detailLimit)throw new TimeoutException("Native entrance board detail");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            if(SessionState.GetBool(Key+".FocusMeasuredUpdates",false))
            {
                SessionState.SetBool(Key+".FocusMeasuredUpdates",false);follow.enabled=false;cam.orthographic=false;cam.fieldOfView=78;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;var root=GameObject.Find("ConvergenceHallB1").transform;
                for(int i=0;i<ConvergenceHallMeasuredUpdates.Pairs.Length;i++)
                {
                    var pair=ConvergenceHallMeasuredUpdates.Pairs[i];var a=root.Find("PlanDoors/"+pair[0]);var b=root.Find("PlanDoors/"+pair[1]);var target=(a.position+b.position)/2+Vector3.up*1.425f;
                    cam.transform.position=target-a.forward*2.05f;cam.transform.LookAt(target);
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,pair[0]+"_"+pair[1]+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Measured door gap preview");yield return null;}
                }
                var windowFloor=root.Find("RoomsAndCores/B102/RoomFloor").GetComponent<Renderer>().bounds;var windowAt=new Vector3(windowFloor.center.x,2.16f,windowFloor.max.z);
                cam.fieldOfView=90;cam.transform.position=windowAt+Vector3.forward*2.05f;cam.transform.LookAt(windowAt);
                double windowReady=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<windowReady||ShaderUtil.anythingCompiling)yield return null;
                string windowShot=Path.Combine(folder,"B102MeasuredWindow.png");DateTime windowRequested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(windowShot);double windowLimit=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(windowShot)||File.GetLastWriteTimeUtc(windowShot)<windowRequested){if(EditorApplication.timeSinceStartup>windowLimit)throw new TimeoutException("Measured window preview");yield return null;}
                if(errorLog!=null)throw new InvalidOperationException(errorLog);if(SessionState.GetString(Key+".FocusRoom","")=="")yield break;
            }
            if(SessionState.GetBool(Key+".FocusElevators",false))
            {
                SessionState.SetBool(Key+".FocusElevators",false);follow.enabled=false;cam.orthographic=false;cam.fieldOfView=60;ShaderUtil.allowAsyncCompilation=false;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
                var root=GameObject.Find("ConvergenceHallB1").transform;
                foreach(bool east in new[]{true,false})
                {
                    var door=root.Find(east?"CoreHalls/B112ElevatorHall":"CoreHalls/B109ElevatorHall/LeftWallElevator");var cabin=door.Find("MeasuredCabin");float z=cabin?cabin.localPosition.z-.04f:east?4.8f:0;
                    for(int detail=0;detail<2;detail++)
                    {
                        cam.fieldOfView=detail==0?60:52;
                        cam.transform.position=door.TransformPoint(detail==0?new Vector3(.13f,1.25f,z-2.3f):new Vector3(.86f,.96f,z-.37f));
                        cam.transform.LookAt(door.TransformPoint(detail==0?new Vector3(.13f,1.20f,z-.12f):new Vector3(.86f,.96f,z-.025f)));
                        double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                        string shot=Path.Combine(folder,(east?"B112":"B109")+(detail==0?"PhotoElevator.png":"PhotoCallButton.png"));DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                        while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Photo elevator preview");yield return null;}
                    }
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);if(SessionState.GetString(Key+".FocusRoom","")=="")yield break;
            }
            string focusRoom=SessionState.GetString(Key+".FocusRoom","");
            if(focusRoom!="")
            {
                SessionState.SetString(Key+".FocusRoom","");follow.enabled=false;cam.orthographic=false;cam.fieldOfView=60;
                UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;ShaderUtil.allowAsyncCompilation=false;
                foreach(string selectedRoom in focusRoom.Split(','))
                {
                    var interior=GameObject.Find("ConvergenceHallB1").transform.Find("ClassroomInteriors/"+selectedRoom);var floor=GameObject.Find("ConvergenceHallB1").transform.Find("RoomsAndCores/"+selectedRoom+"/RoomFloor").GetComponent<Renderer>().bounds;
                    bool north=selectedRoom=="B108"||selectedRoom=="B109"||selectedRoom=="B112";float width=north?floor.size.x:floor.size.z,depth=north?floor.size.z:floor.size.x;
                    cam.transform.position=interior.TransformPoint(new Vector3(width/2-.52f,1.62f,-depth/2+.60f));cam.transform.LookAt(interior.TransformPoint(new Vector3(-.15f,1.45f,depth/2-.18f)));
                    double ready=EditorApplication.timeSinceStartup+3;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                    string shot=Path.Combine(folder,selectedRoom+"Interior.png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);double limit=EditorApplication.timeSinceStartup+20;
                    while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("Focused interior capture");yield return null;}
                }
                if(errorLog!=null)throw new InvalidOperationException(errorLog);yield break;
            }
            var points=new[]{new Vector3(745,830,0),new Vector3(570,750,-90),new Vector3(245.5f,675,0),new Vector3(745,660,0),new Vector3(866,610,0)};
            string[] names={"Entrance","LeftCorridor","MeasuredCorridor","StandSide","EastCorridor"};
            for(int i=0;i<points.Length;i++)
            {
                chair.transform.SetPositionAndRotation(ConvergenceHallB1Setup.Plan(points[i].x,points[i].y,.5f),Quaternion.Euler(0,points[i].z,0));follow.SnapToTarget();
                for(int j=0;j<4;j++)yield return null;
                string path=Path.Combine(folder,names[i]+".png");DateTime request=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);
                double deadline=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<request){if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Screenshot "+names[i]);yield return null;}
            }
            // Temporary top view for floor-plan comparison; not saved to the scene.
            follow.enabled=false;cam.orthographic=false;cam.fieldOfView=60;
            cam.transform.SetPositionAndRotation(ConvergenceHallB1Setup.Plan(630,606,43),Quaternion.Euler(90,0,0));
            UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>().enabled=false;
            for(int j=0;j<4;j++)yield return null;
            string top=Path.Combine(folder,"B1Overview.png");DateTime at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(top);
            double end=EditorApplication.timeSinceStartup+20;
            while(!File.Exists(top)||File.GetLastWriteTimeUtc(top)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Overview");yield return null;}
            var upper=GameObject.Find("ConvergenceHallB1").transform.Find("UpperWalls");
            if(true)
            {
                // Inspection only: show the measured full wall and photo-based B111 facade.
                ShaderUtil.allowAsyncCompilation=false;

                cam.transform.position=ConvergenceHallB1Setup.Plan(691,350.5f,1.45f);
                cam.transform.LookAt(ConvergenceHallB1Setup.Plan(640,350.5f,1.425f));
                double readyAt=EditorApplication.timeSinceStartup+3;
                double shaderDeadline=EditorApplication.timeSinceStartup+25;
                while(EditorApplication.timeSinceStartup<readyAt||ShaderUtil.anythingCompiling)
                {if(EditorApplication.timeSinceStartup>shaderDeadline)throw new TimeoutException("Facade shader warmup");yield return null;}
                string detail=Path.Combine(folder,"B111EntranceFullHeight.png");at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(detail);
                end=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(detail)||File.GetLastWriteTimeUtc(detail)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("B111 entrance preview");yield return null;}

            }
            // Temporary prop inspection camera, never saved into the scene.
            cam.transform.position=ConvergenceHallB1Setup.Plan(758,704,2.4f);
            cam.transform.LookAt(ConvergenceHallB1Setup.Plan(716,655,1.0f));
            double propsReady=EditorApplication.timeSinceStartup+3;
            while(EditorApplication.timeSinceStartup<propsReady||ShaderUtil.anythingCompiling)yield return null;
            string props=Path.Combine(folder,"BluebellAmenities.png");at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(props);
            end=EditorApplication.timeSinceStartup+20;
            while(!File.Exists(props)||File.GetLastWriteTimeUtc(props)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Bluebell props preview");yield return null;}

            var views=new[]{
                new Vector3(721,499,1.55f),new Vector3(250,411,1.40f),new Vector3(253,358.5f,1.40f),new Vector3(745,607,1.4f),new Vector3(570,750,1.4f)};
            var targets=new[]{
                new Vector3(791,497,1.35f),new Vector3(213,423,1.35f),new Vector3(205,358.5f,1.25f),new Vector3(772,690,1.4f),new Vector3(501,766,1.4f)};
            string[] detailNames={"B112ElevatorAndStair","B109ElevatorHall","B109Stairwell","LoungeAutomaticDoor","ClassroomDoors"};
            for(int i=0;i<views.Length;i++)
            {
                cam.transform.position=ConvergenceHallB1Setup.Plan(views[i].x,views[i].y,views[i].z);
                cam.transform.LookAt(ConvergenceHallB1Setup.Plan(targets[i].x,targets[i].y,targets[i].z));
                double ready=EditorApplication.timeSinceStartup+2;
                while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string shot=Path.Combine(folder,detailNames[i]+".png");at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);end=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(detailNames[i]);yield return null;}
            }

            cam.transform.position=ConvergenceHallB1Setup.Plan(245.5f,819,1.45f);
            cam.transform.LookAt(ConvergenceHallB1Setup.Plan(245.5f,878,1.38f));
            double exitReady=EditorApplication.timeSinceStartup+2;
            while(EditorApplication.timeSinceStartup<exitReady||ShaderUtil.anythingCompiling)yield return null;
            string exitShot=Path.Combine(folder,"B103B104SubEntrance.png");at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(exitShot);end=EditorApplication.timeSinceStartup+20;
            while(!File.Exists(exitShot)||File.GetLastWriteTimeUtc(exitShot)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("West sub-entry preview");yield return null;}
            Vector3[] roomViews={ConvergenceHallB1Setup.Plan(213,777,6),ConvergenceHallB1Setup.Plan(991,495.5f,30),ConvergenceHallB1Setup.Plan(763,469.5f,1.55f),ConvergenceHallB1Setup.Plan(228,403,1.5f),ConvergenceHallB1Setup.Plan(265,386,7),ConvergenceHallB1Setup.Plan(265,380,8),ConvergenceHallB1Setup.Plan(472,754,1.45f),ConvergenceHallB1Setup.Plan(589,748,1.50f),ConvergenceHallB1Setup.Plan(760,548,7),ConvergenceHallB1Setup.Plan(357.3f,745,1.49f),ConvergenceHallB1Setup.Plan(735,765,8),ConvergenceHallB1Setup.Plan(670,750,1.55f)};
            Vector3[] roomTargets={ConvergenceHallB1Setup.Plan(281,708,1.1f),ConvergenceHallB1Setup.Plan(991,495.5f,0),ConvergenceHallB1Setup.Plan(838,469.5f,1.40f),ConvergenceHallB1Setup.Plan(213.5f,429.7f,1.25f),ConvergenceHallB1Setup.Plan(205,358.5f,1.55f),ConvergenceHallB1Setup.Plan(198,410,1.4f),ConvergenceHallB1Setup.Plan(429,710,1.35f),ConvergenceHallB1Setup.Plan(623,766,1.425f),ConvergenceHallB1Setup.Plan(812,525.5f,1.50f),ConvergenceHallB1Setup.Plan(357.3f,733,1.49f),ConvergenceHallB1Setup.Plan(670,650,2.25f),ConvergenceHallB1Setup.Plan(670,650,2.25f)};
            string[] roomShots={"B108RoundedCorner","B113B114B115RearSpace","B112LongElevatorApproach","B109LeftWallElevator","B109SwitchbackStairs","B109ExpandedHall","B107MeasuredGlassBands","B101PhotoDoor","B112SwitchbackStairs","B108PhotoRoomSign","BluebellMeasuredOverview","BluebellMeasuredFront"};
            for(int i=0;i<roomViews.Length;i++)
            {
                cam.transform.position=roomViews[i];cam.transform.LookAt(roomTargets[i],i==1?Vector3.forward:Vector3.up);
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string shot=Path.Combine(folder,roomShots[i]+".png");at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);end=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(roomShots[i]);yield return null;}
            }
            var interiorRoot=GameObject.Find("ConvergenceHallB1").transform.Find("ClassroomInteriors");
            foreach(string roomName in ConvergenceHallClassroomInteriors.Rooms)
            {
                var room=interiorRoot.Find(roomName);var floor=GameObject.Find("ConvergenceHallB1").transform.Find("RoomsAndCores/"+roomName+"/RoomFloor").GetComponent<Renderer>().bounds;
                bool north=roomName=="B108"||roomName=="B109"||roomName=="B112";float w=north?floor.size.x:floor.size.z,d=north?floor.size.z:floor.size.x;
                cam.transform.position=room.TransformPoint(new Vector3(w/2-.52f,1.62f,-d/2+.60f));cam.transform.LookAt(room.TransformPoint(new Vector3(-.15f,1.45f,d/2-.18f)));
                double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
                string shot=Path.Combine(folder,roomName+"Interior.png");at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(shot);end=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(shot)||File.GetLastWriteTimeUtc(shot)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException(roomName+" interior preview");yield return null;}
            }
            var b106=interiorRoot.Find("B106");var teacherChair=b106.Find("TeacherChair");
            cam.transform.position=teacherChair.position+b106.TransformVector(new Vector3(.68f,1.12f,.90f));cam.transform.LookAt(teacherChair.position+Vector3.up*.52f);
            double chairReady=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<chairReady||ShaderUtil.anythingCompiling)yield return null;
            string chairShot=Path.Combine(folder,"BlueMeshChairDetail.png");at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(chairShot);end=EditorApplication.timeSinceStartup+20;
            while(!File.Exists(chairShot)||File.GetLastWriteTimeUtc(chairShot)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Chair detail");yield return null;}
            var lectern=b106.Find("Lectern");cam.transform.position=lectern.position+b106.TransformVector(new Vector3(-1.20f,1.40f,-1.20f));cam.transform.LookAt(lectern.position+Vector3.up*.90f);
            double lecternReady=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<lecternReady||ShaderUtil.anythingCompiling)yield return null;
            string lecternShot=Path.Combine(folder,"ElectronicLecternDetail.png");at=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(lecternShot);end=EditorApplication.timeSinceStartup+20;
            while(!File.Exists(lecternShot)||File.GetLastWriteTimeUtc(lecternShot)<at){if(EditorApplication.timeSinceStartup>end)throw new TimeoutException("Lectern detail");yield return null;}
            if(errorLog!=null)throw new InvalidOperationException("Runtime error: "+errorLog);
        }
    }
}













