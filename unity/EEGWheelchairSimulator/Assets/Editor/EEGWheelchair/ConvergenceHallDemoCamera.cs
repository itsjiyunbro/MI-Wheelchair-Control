using System;
using System.Collections;
using System.IO;
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
    public static class ConvergenceHallDemoCamera
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void ConfigureCurrentScene()
        {
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var free=follow.GetComponent<DemoFreeCamera>();if(!free)free=follow.gameObject.AddComponent<DemoFreeCamera>();
            var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();var panel=(RectTransform)hud.transform.Find("StatusPanel");
            var prompt=(RectTransform)panel.Find("DoorPrompt");float top=-prompt.anchoredPosition.y+prompt.sizeDelta.y+12;
            var existing=panel.Find("FreeCameraButton");var go=existing?existing.gameObject:UnityEngine.Object.Instantiate(panel.Find("StartButton").gameObject,panel);go.name="FreeCameraButton";
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-top);rect.sizeDelta=new Vector2(300,36);
            var button=go.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();button.navigation=new Navigation{mode=Navigation.Mode.None};button.interactable=true;UnityEventTools.AddPersistentListener(button.onClick,free.ToggleFreeView);
            var label=go.GetComponentInChildren<Text>();label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ConvergenceHall/NotoSansCJKkr-Regular.otf");label.fontSize=18;
            var hint=panel.Find("FreeCameraHint");if(!hint){hint=new GameObject("FreeCameraHint",typeof(RectTransform),typeof(Text)).transform;hint.SetParent(panel,false);}
            var hr=(RectTransform)hint;hr.anchorMin=hr.anchorMax=new Vector2(0,1);hr.pivot=new Vector2(0,1);hr.anchoredPosition=new Vector2(20,-top-44);hr.sizeDelta=new Vector2(300,65);
            var text=hint.GetComponent<Text>();text.font=label.font;text.fontSize=13;text.color=new Color(.86f,.90f,.94f);text.raycastTarget=false;
            panel.sizeDelta=new Vector2(panel.sizeDelta.x,Mathf.Max(panel.sizeDelta.y,top+121));
            free.Configure(UnityEngine.Object.FindFirstObjectByType<SimulationController>(),hud.GetComponent<Canvas>(),label,text);EditorUtility.SetDirty(free);
        }
        [MenuItem("Tools/EEG Wheelchair/Enable B1 Demo Free Camera")]
        public static void ApplyAndValidate()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);ConfigureCurrentScene();int count=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length;ConfigureCurrentScene();
            Check(count==UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,"Duplicate free camera UI");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            var free=UnityEngine.Object.FindFirstObjectByType<DemoFreeCamera>();Check(free&&UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"Demo camera missing or extra camera created");
            var button=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().transform.Find("StatusPanel/FreeCameraButton").GetComponent<Button>();Check(button.onClick.GetPersistentEventCount()==1,"Duplicate free camera listener");
            SessionState.SetBool("EEG.ConvergenceQA.FocusDemoCamera",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",false);ConvergenceHallB1Play.Begin();
        }
        public static void ValidateHiddenHUD()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            SessionState.SetBool("EEG.ConvergenceQA.FocusDemoHUD",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        public static IEnumerator CheckHiddenHUD()
        {
            var free=UnityEngine.Object.FindFirstObjectByType<DemoFreeCamera>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>().GetComponent<Canvas>();var raycaster=hud.GetComponent<GraphicRaycaster>();
            free.EnterFreeView();free.ToggleHUD();yield return null;
            Check(!hud.enabled&&!raycaster.enabled,"Hidden dashboard can still receive pointer clicks");
            free.ToggleHUD();Check(hud.enabled&&raycaster.enabled,"Dashboard pointer controls not restored");free.ToggleHUD();sim.ResetSimulation();
            Check(!free.IsFreeView&&hud.enabled&&raycaster.enabled,"RESET left hidden pointer controls disabled");
            Debug.Log("DEMO_HIDDEN_HUD_OK: hidden dashboard cannot intercept camera look or clicks; H and RESET restore display and pointer controls.");
        }
        static void Click(Button button)=>ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        static IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
        static IEnumerator Capture(string folder,string name)
        {
            for(int i=0;i<4;i++)yield return null;var path=Path.Combine(folder,name+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double deadline=EditorApplication.timeSinceStartup+20;
            while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException(path);yield return null;}
        }
        public static IEnumerator RunChecks(string folder)
        {
            var free=UnityEngine.Object.FindFirstObjectByType<DemoFreeCamera>();var follow=free.GetComponent<CameraFollow>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();
            var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();var canvas=hud.GetComponent<Canvas>();var panel=hud.transform.Find("StatusPanel");var button=panel.Find("FreeCameraButton").GetComponent<Button>();var viewButton=panel.Find("CameraViewButton").GetComponent<Button>();
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                sim.ResetSimulation();sim.SetControlSource(WheelchairControlSource.Python);sim.StartSimulation();follow.SetViewMode(WheelchairViewMode.FirstPerson);
                var cameraPose=free.transform.position;var chairPose=chair.transform.position;var chairRotation=chair.transform.rotation;Click(button);
                Check(free.IsFreeView&&!follow.enabled&&!sim.IsRunning&&sim.ControlSource==WheelchairControlSource.Python&&Vector3.Distance(cameraPose,free.transform.position)<.001f,"Free camera entry moved camera/changed source/left chair running");
                foreach(var r in chair.transform.Find("WheelchairVisual").GetComponentsInChildren<Renderer>())Check(!r.forceRenderingOff,"Chair remained hidden in free view");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.D,Key.E));for(int i=0;i<12;i++)yield return null;
                Check(Vector3.Distance(free.transform.position,cameraPose)>.05f&&free.transform.position.y>cameraPose.y&&chair.transform.position==chairPose&&chair.transform.rotation==chairRotation,"WASD/QE failed or chair moved");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;var move=(Action<Vector3,bool,float>)Delegate.CreateDelegate(typeof(Action<Vector3,bool,float>),free,typeof(DemoFreeCamera).GetMethod("Move",flags));
                var moveStart=free.transform.position;free.transform.rotation=Quaternion.identity;
                foreach(int fps in new[]{30,120})
                {
                    free.transform.position=moveStart;for(int i=0;i<fps;i++)move(new Vector3(1,1,1),false,1f/fps);
                    Check(Mathf.Abs(Vector3.Distance(free.transform.position,moveStart)-free.MoveSpeed)<.002f,"Frame rate or diagonal movement speed differs");
                    free.transform.position=moveStart;for(int i=0;i<fps;i++)move(Vector3.forward,true,1f/fps);
                    Check(Mathf.Abs(Vector3.Distance(free.transform.position,moveStart)-free.MoveSpeed*3)<.002f,"Shift speed differs");
                }
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(900,500),buttons=2});for(int i=0;i<2;i++)yield return null;var rotation=free.transform.rotation;
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(900,500),buttons=2,delta=new Vector2(50,-20)});for(int i=0;i<2;i++)yield return null;
                Check(Quaternion.Angle(rotation,free.transform.rotation)>1,"Right mouse look failed");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(900,500),scroll=new Vector2(0,120)});for(int i=0;i<2;i++)yield return null;Check(free.MoveSpeed>2,"Scroll speed adjustment failed");
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(900,500)});InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.H));for(int i=0;i<2;i++)yield return null;Check(!canvas.enabled,"H did not hide HUD");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.H));for(int i=0;i<2;i++)yield return null;Check(canvas.enabled,"H did not restore HUD");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));for(int i=0;i<2;i++)yield return null;
                Check(!free.IsFreeView&&follow.enabled&&!sim.IsRunning&&sim.ControlSource==WheelchairControlSource.Python,"F return changed control/run state");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;Click(button);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.V));for(int i=0;i<2;i++)yield return null;
                Check(!free.IsFreeView&&follow.ViewMode==WheelchairViewMode.ThirdPerson,"V did not leave free view");InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;
                Click(button);Click(viewButton);Check(!free.IsFreeView&&follow.ViewMode==WheelchairViewMode.FirstPerson,"Existing view button did not leave free view");
                Click(button);Click(panel.Find("StartButton").GetComponent<Button>());Check(!free.IsFreeView&&sim.IsRunning,"START did not return to normal simulation");
                sim.StopSimulation();Click(button);free.ToggleHUD();sim.ResetSimulation();Check(!free.IsFreeView&&canvas.enabled&&follow.enabled&&!sim.IsRunning,"RESET did not restore follow camera and HUD");
                Click(button);var door=GameObject.Find("ConvergenceHallB1").transform.Find("PlanDoors/B112_East_North").GetComponent<InteractiveDoor>();door.SetProgressInstant(0);
                free.transform.position=door.InteractionPoint+door.transform.forward*.70f;free.transform.LookAt(door.InteractionPoint);for(int i=0;i<3;i++)yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));for(int i=0;i<3;i++)yield return null;Check(!door.WantsOpen,"E altitude control also opened a door");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.R));for(int i=0;i<4;i++)yield return null;
                Check(door.WantsOpen,"R did not operate door from free camera");InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;
                free.ExitFreeView();sim.ResetSimulation();sim.SetControlSource(WheelchairControlSource.Keyboard);
                var doors=ConvergenceHallDoorValidation.RunChecks(Path.Combine(folder,"DoorChecks"));Directory.CreateDirectory(Path.Combine(folder,"DoorChecks"));while(doors.MoveNext())yield return doors.Current;
                Debug.Log("DEMO_CAMERA_PLAY_OK: pointer/F/V/START/RESET, mouse look/wheel, WASD/QE, 30/120FPS diagonal/Shift speed, HUD hide/restore, frozen wheelchair/Python source, E/R separation and existing door controls passed.");
                sim.ResetSimulation();follow.SetViewMode(WheelchairViewMode.ThirdPerson);Click(button);ShaderUtil.allowAsyncCompilation=false;
                free.transform.SetPositionAndRotation(ConvergenceHallB1Setup.Plan(735,800,1.65f),Quaternion.Euler(0,-25,0));
                var shot=Capture(folder,"DemoFreeCameraControls");while(shot.MoveNext())yield return shot.Current;
                var room=GameObject.Find("ConvergenceHallB1").transform.Find("ClassroomInteriors/B112");var floor=GameObject.Find("ConvergenceHallB1").transform.Find("RoomsAndCores/B112/RoomFloor").GetComponent<Renderer>().bounds;
                free.transform.position=room.TransformPoint(new Vector3(0,1.6f,-floor.size.z/2+1));free.transform.LookAt(room.TransformPoint(new Vector3(0,1.6f,floor.size.z/2)));free.ToggleHUD();
                shot=Capture(folder,"DemoB112CleanView");while(shot.MoveNext())yield return shot.Current;
                free.transform.position=room.TransformPoint(new Vector3(-1,2.2f,floor.size.z/2-4));free.transform.LookAt(room.TransformPoint(new Vector3(0,1.7f,floor.size.z/2)));
                shot=Capture(folder,"DemoB112PresentationView");while(shot.MoveNext())yield return shot.Current;
            }
            finally{free.ExitFreeView();InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);sim.ResetSimulation();sim.SetControlSource(WheelchairControlSource.Keyboard);}
        }
    }
}
