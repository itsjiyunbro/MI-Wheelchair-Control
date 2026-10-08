using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallCameraViews
    {
        const BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        [MenuItem("Tools/EEG Wheelchair/Configure B1 First And Third Person Views")]
        public static void Configure()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play Mode first");
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var camera=follow.GetComponent<Camera>();var data=new SerializedObject(follow);
            data.FindProperty("offset").vector3Value=new Vector3(0,1.15f,-2.60f);data.FindProperty("lookTargetHeight").floatValue=.40f;
            data.FindProperty("firstPersonOffset").vector3Value=new Vector3(0,.70f,.15f);data.FindProperty("viewMode").enumValueIndex=0;
            data.FindProperty("avoidObstacles").boolValue=true;data.FindProperty("cameraObstacleMask").intValue=LayerMask.GetMask("WheelchairObstacle");data.FindProperty("cameraRadius").floatValue=.14f;
            data.FindProperty("allowViewToggle").boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();camera.nearClipPlane=.08f;
            var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();var panel=(RectTransform)hud.transform.Find("StatusPanel");panel.sizeDelta=new Vector2(panel.sizeDelta.x,Mathf.Max(panel.sizeDelta.y,419));
            var existing=panel.Find("CameraViewButton");GameObject go=existing?existing.gameObject:UnityEngine.Object.Instantiate(panel.Find("StartButton").gameObject,panel);
            go.name="CameraViewButton";var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(18,-361);rect.sizeDelta=new Vector2(304,36);
            var button=go.GetComponent<Button>();button.interactable=true;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,follow.ToggleViewMode);
            var label=go.GetComponentInChildren<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");label.fontSize=18;
            data.Update();data.FindProperty("viewButtonLabel").objectReferenceValue=label;data.ApplyModifiedPropertiesWithoutUndo();follow.SetViewMode(WheelchairViewMode.ThirdPerson);
            if(UnityEngine.Object.FindFirstObjectByType<DoorInteractor>())ConvergenceHallDoorInteraction.ConfigureScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);ValidateSaved();
        }
        static void ValidateSaved()
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var data=new SerializedObject(follow);
            Check(data.FindProperty("offset").vector3Value==new Vector3(0,1.15f,-2.60f)&&follow.ViewMode==WheelchairViewMode.ThirdPerson,"Saved third-person settings mismatch");
            Check(data.FindProperty("target").objectReferenceValue!=null&&UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"Camera target/count changed");
            var button=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().transform.Find("StatusPanel/CameraViewButton").GetComponent<Button>();
            Check(button.onClick.GetPersistentEventCount()==1&&button.onClick.GetPersistentMethodName(0)=="ToggleViewMode","View button listener missing/duplicated");
            Check(UnityEngine.Object.FindObjectsByType<PythonWheelchairReceiver>(FindObjectsSortMode.None).Length==1&&UnityEngine.Object.FindObjectsByType<SimulationController>(FindObjectsSortMode.None).Length==1,"Input/control graph changed");
            Debug.Log("CAMERA_VIEWS_SAVED_OK: lower third-person offset 1.15m/-2.60m, seated eye +0.70m, one camera/controller/receiver, V and one persistent view button.");
        }
        static void CheckCameraMath()
        {
            var target=new GameObject("CameraViewTestTarget");target.transform.position=new Vector3(200,.5f,200);target.transform.localScale=new Vector3(2,1,3);
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);visual.name="WheelchairVisual";visual.transform.SetParent(target.transform,false);
            var cameraObject=new GameObject("CameraViewTestCamera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;var follow=cameraObject.AddComponent<CameraFollow>();var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                var settings=new SerializedObject(follow);settings.FindProperty("target").objectReferenceValue=target.transform;settings.FindProperty("offset").vector3Value=new Vector3(0,1.15f,-2.6f);settings.FindProperty("lookTargetHeight").floatValue=.4f;settings.ApplyModifiedPropertiesWithoutUndo();
                var tick=(Action<float>)Delegate.CreateDelegate(typeof(Action<float>),follow,typeof(CameraFollow).GetMethod("Follow",Flags));
                foreach(int fps in new[]{30,120})foreach(int turn in new[]{-1,1})
                {
                    follow.SetViewMode(WheelchairViewMode.FirstPerson);
                    for(int i=0;i<fps;i++)
                    {
                        target.transform.rotation=Quaternion.Euler(0,target.transform.eulerAngles.y+turn*60f/fps,0);target.transform.position+=target.transform.forward*(2f/fps);tick(1f/fps);
                        var expected=target.transform.position+target.transform.rotation*new Vector3(0,.7f,.15f);Check(Vector3.Distance(cameraObject.transform.position,expected)<.001f&&Quaternion.Angle(cameraObject.transform.rotation,target.transform.rotation)<.1f,"First-person motion/turn lag");
                    }
                    Check(visual.GetComponent<Renderer>().forceRenderingOff&&visual.GetComponent<Collider>().enabled,"First-person model hiding changed collision");follow.SetViewMode(WheelchairViewMode.ThirdPerson);Check(!visual.GetComponent<Renderer>().forceRenderingOff,"Third-person model restoration failed");
                }
                target.transform.SetPositionAndRotation(new Vector3(200,.5f,200),Quaternion.identity);wall.transform.position=new Vector3(200,1.3f,198.8f);wall.transform.localScale=new Vector3(2,3,.1f);wall.layer=LayerMask.NameToLayer("WheelchairObstacle");Physics.SyncTransforms();
                settings.Update();settings.FindProperty("avoidObstacles").boolValue=true;settings.FindProperty("cameraObstacleMask").intValue=LayerMask.GetMask("WheelchairObstacle");settings.ApplyModifiedPropertiesWithoutUndo();follow.SetViewMode(WheelchairViewMode.ThirdPerson);
                Check(cameraObject.transform.position.z>198.95f&&Vector3.Distance(wall.GetComponent<Collider>().ClosestPoint(cameraObject.transform.position),cameraObject.transform.position)>.14f,"Third-person camera crossed the rear wall");
                Debug.Log("CAMERA_VIEW_MATH_OK: 30/120 FPS turning/motion; target scale ignored; first-person visuals/colliders, restoration and rear-wall camera clearance passed.");
            }
            finally{UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(wall);}
        }
        public static void ApplyAndValidate()
        {
            Configure();int count=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;Configure();Check(count==UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,"Repeated camera setup added duplicate nodes");CheckCameraMath();
            SessionState.SetBool("EEG.ConvergenceQA.FocusCameraViews",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        static IEnumerator Capture(string folder,string name)
        {
            for(int i=0;i<4;i++)yield return null;string path=Path.Combine(folder,name+".png");DateTime requested=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
            while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<requested){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException("View screenshot");yield return null;}
        }
        public static IEnumerator RunChecks(string folder)
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();
            var button=hud.transform.Find("StatusPanel/CameraViewButton").GetComponent<Button>();var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                sim.ResetSimulation();sim.SetControlSource(WheelchairControlSource.Keyboard);follow.SetViewMode(WheelchairViewMode.ThirdPerson);hud.RefreshDisplay();
                var shot=Capture(folder,"LowerThirdPerson");while(shot.MoveNext())yield return shot.Current;
                var pose=chair.transform.position;
                ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                Check(follow.ViewMode==WheelchairViewMode.FirstPerson&&!sim.IsRunning&&chair.transform.position==pose,"UI mode switch changes stopped simulation");
                shot=Capture(folder,"SeatedFirstPerson");while(shot.MoveNext())yield return shot.Current;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.V));for(int i=0;i<4;i++)yield return null;
                Check(follow.ViewMode==WheelchairViewMode.ThirdPerson,"V key did not switch viewpoint");InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;
                follow.SetViewMode(WheelchairViewMode.FirstPerson);sim.StartSimulation();float beforeYaw=chair.HeadingDegrees;pose=chair.transform.position;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D));for(int i=0;i<12;i++)yield return null;
                Check(Vector3.Distance(chair.transform.position,pose)>.01f&&Mathf.Abs(Mathf.DeltaAngle(beforeYaw,chair.HeadingDegrees))>.01f,"First-person keyboard motion/turn failed");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<3;i++)yield return null;
                sim.StopSimulation();pose=chair.transform.position;follow.ToggleViewMode();Check(!sim.IsRunning&&pose==chair.transform.position,"View toggle resumed a stopped chair");
                sim.SetControlSource(WheelchairControlSource.Python);follow.ToggleViewMode();Check(sim.ControlSource==WheelchairControlSource.Python,"View toggle changed Python control selection");
                sim.ResetSimulation();Check(follow.ViewMode==WheelchairViewMode.FirstPerson&&Vector3.Distance(follow.transform.position,chair.transform.position+chair.transform.rotation*new Vector3(0,.7f,.15f))<.001f,"RESET lost selected view/eye pose");
                sim.SetControlSource(WheelchairControlSource.Keyboard);chair.transform.SetPositionAndRotation(ConvergenceHallB1Setup.Plan(245.5f,650,.5f),Quaternion.identity);follow.SetViewMode(WheelchairViewMode.ThirdPerson);hud.RefreshDisplay();
                shot=Capture(folder,"ThirdPersonWestCorridor");while(shot.MoveNext())yield return shot.Current;follow.SetViewMode(WheelchairViewMode.FirstPerson);
                shot=Capture(folder,"FirstPersonWestCorridor");while(shot.MoveNext())yield return shot.Current;
                Debug.Log("CAMERA_VIEWS_PLAY_OK: pointer button, V key, first-person keyboard forward/turn, STOP, RESET, Python source preservation and corridor screenshots passed.");
            }
            finally{InputSystem.RemoveDevice(keyboard);follow.SetViewMode(WheelchairViewMode.ThirdPerson);sim.ResetSimulation();}
        }
    }
}
