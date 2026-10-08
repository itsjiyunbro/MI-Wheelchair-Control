using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallDoorValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        static Vector3 ActorAt(InteractiveDoor door,float depth)
        {var p=door.InteractionPoint+door.transform.forward*depth;p.y=.5f;return p;}
        static IEnumerator Wait(float seconds)
        {double ready=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<ready)yield return null;}
        static IEnumerator Capture(string folder,string name)
        {
            var ready=Wait(2);while(ready.MoveNext())yield return ready.Current;
            while(ShaderUtil.anythingCompiling)yield return null;
            var path=Path.Combine(folder,name+".png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double limit=EditorApplication.timeSinceStartup+20;
            while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>limit)throw new TimeoutException(name);yield return null;}
        }
        public static IEnumerator RunChecks(string folder)
        {
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var guard=chair.GetComponent<WheelchairCollisionGuard>();
            var manager=UnityEngine.Object.FindFirstObjectByType<DoorInteractor>();var root=GameObject.Find("ConvergenceHallB1");var doors=root.GetComponentsInChildren<InteractiveDoor>();
            var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var cam=follow.GetComponent<Camera>();var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();var button=hud.transform.Find("StatusPanel/DoorButton").GetComponent<Button>();
            Check(doors.Length>=40&&doors.Count(d=>d.Automatic)==3,"Missing door types");
            Check(button.onClick.GetPersistentEventCount()==1&&button.onClick.GetPersistentMethodName(0)=="ToggleNearest","Door button binding invalid");
            var panel=(RectTransform)hud.transform.Find("StatusPanel");var resetRect=(RectTransform)panel.Find("ResetButton");var cameraRect=(RectTransform)panel.Find("CameraViewButton");var doorRect=(RectTransform)button.transform;var hintRect=(RectTransform)panel.Find("DoorPrompt");
            Check(-cameraRect.anchoredPosition.y>=-resetRect.anchoredPosition.y+resetRect.sizeDelta.y+10&&-doorRect.anchoredPosition.y>=-cameraRect.anchoredPosition.y+cameraRect.sizeDelta.y+6&&panel.sizeDelta.y>=-hintRect.anchoredPosition.y+hintRect.sizeDelta.y,"Door/camera controls overlap dashboard or leave panel");
            sim.StopSimulation();foreach(var d in doors)d.enabled=false;manager.enabled=false;follow.enabled=false;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                int poses=0,passages=0;
                foreach(int fps in new[]{30,120})foreach(var door in doors)
                {
                    chair.transform.SetPositionAndRotation(door.Automatic?ActorAt(door,-1.9f):new Vector3(180,.5f,180),door.transform.rotation);
                    door.SetProgressInstant(0);door.SetOpen(true);
                    for(int frame=0;frame<fps;frame++)door.Step(1f/fps);
                    Check(door.Progress>.999f,"Door did not fully open: "+door.name+" at "+fps+"fps");
                    foreach(var m in door.Motions)
                    {
                        var q=Quaternion.Euler(0,m.angle,0);
                        Check(Vector3.Distance(m.target.localPosition,m.hinge+q*(m.closedPosition-m.hinge)+m.slide)<.001f&&Quaternion.Angle(m.target.localRotation,q*m.closedRotation)<.1f,"Wrong hinge/slide pose: "+door.name);
                    }
                    chair.transform.position=new Vector3(180,.5f,180);door.SetOpen(false);for(int frame=0;frame<fps;frame++)door.Step(1f/fps);
                    Check(door.Progress<.001f,"Door did not close: "+door.name);poses++;
                }
                foreach(var door in root.transform.Find("PlanDoors").GetComponentsInChildren<InteractiveDoor>().Concat(new[]{root.transform.Find("DoorsAndSigns/B111Entrance").GetComponent<InteractiveDoor>()}))
                {
                    chair.transform.SetPositionAndRotation(ActorAt(door,0),door.transform.rotation);
                    door.SetProgressInstant(0);Check(!guard.CanOccupy(chair.transform.position,chair.transform.rotation),"Closed doorway allows crossing: "+door.name);
                    door.SetProgressInstant(1);
                    if(!guard.CanOccupy(chair.transform.position,chair.transform.rotation))
                    {
                        var overlaps=Physics.OverlapBox(guard.QueryCentre,guard.FootprintHalfExtents,chair.transform.rotation,LayerMask.GetMask("WheelchairObstacle"),QueryTriggerInteraction.Ignore);
                        throw new Exception("Open doorway blocked: "+door.name+" by "+string.Join(",",overlaps.Select(c=>UnityEditor.AnimationUtility.CalculateTransformPath(c.transform,root.transform))));
                    }
                    passages++;chair.transform.position=new Vector3(180,.5f,180);door.SetProgressInstant(0);
                }
                var example=root.transform.Find("PlanDoors/B101_North_West").GetComponent<InteractiveDoor>();
                chair.transform.SetPositionAndRotation(ActorAt(example,0),example.transform.rotation);example.SetProgressInstant(1);example.SetOpen(false);
                for(int i=0;i<150;i++)example.Step(1/120f);
                Check(example.Blocked&&example.Progress>.01f,"Closing door crushed a chair standing in the opening");
                chair.transform.position=new Vector3(180,.5f,180);for(int i=0;i<150;i++)example.Step(1/120f);Check(example.Progress<.001f,"Waiting door did not finish closing when clear");
                Debug.Log($"DOOR_MOTION_OK: {poses} open/close runs at 30/120fps; {passages} closed/open doorway collision pairs; annotated pivots and slides; close obstruction and resumed closing passed.");
                foreach(var d in doors){d.ResetDoor();d.enabled=true;}manager.enabled=true;
                chair.transform.SetPositionAndRotation(ActorAt(example,-1.75f),example.transform.rotation);for(int i=0;i<4;i++)yield return null;manager.RefreshSelection();
                Check(manager.Selected==example&&!sim.IsRunning&&button.interactable,"Stopped user cannot select the nearby door");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E));for(int i=0;i<4;i++)yield return null;
                Check(example.WantsOpen,"E key failed to open the selected door");InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                var waiting=Wait(1.1f);while(waiting.MoveNext())yield return waiting.Current;Check(example.Progress>.99f,"Actual frame animation failed");
                manager.RefreshSelection();ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
                Check(!example.WantsOpen,"Pointer button failed to close the selected door");waiting=Wait(1.1f);while(waiting.MoveNext())yield return waiting.Current;Check(example.Progress<.01f,"Actual close animation failed");
                example.SetProgressInstant(1);chair.transform.SetPositionAndRotation(ActorAt(example,-.78f),example.transform.rotation);sim.StartSimulation();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
                waiting=Wait(.80f);while(waiting.MoveNext())yield return waiting.Current;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());sim.StopSimulation();
                Check(Vector3.Dot(chair.transform.position-example.InteractionPoint,example.transform.forward)>.10f,"W driving did not cross the 95cm open classroom door");
                sim.SetControlSource(WheelchairControlSource.Python);manager.RefreshSelection();sim.ResetSimulation();
                Check(sim.ControlSource==WheelchairControlSource.Python&&!sim.IsRunning&&example.Progress<.001f,"RESET did not restore door state or changed the Python source");
                sim.SetControlSource(WheelchairControlSource.Keyboard);
                var automatic=root.transform.Find("PlanDoors/Lounge_West_Automatic").GetComponent<InteractiveDoor>();
                chair.transform.SetPositionAndRotation(ActorAt(automatic,-1.9f),automatic.transform.rotation);waiting=Wait(1.2f);while(waiting.MoveNext())yield return waiting.Current;Check(automatic.Progress>.99f,"Lounge approach sensor failed");
                chair.transform.position=new Vector3(180,.5f,180);waiting=Wait(1.2f);while(waiting.MoveNext())yield return waiting.Current;Check(automatic.Progress<.01f,"Lounge departure close failed");
                Debug.Log("DOOR_INPUT_OK: actual E key, pointer button, STOP operation, W through 95cm opening, automatic approach/departure, RESET and Python source preservation passed.");
                foreach(var d in doors)d.enabled=false;manager.enabled=false;sim.ResetSimulation();chair.transform.position=new Vector3(180,.5f,180);ShaderUtil.allowAsyncCompilation=false;cam.orthographic=false;
                hud.GetComponent<Canvas>().enabled=false;
                string[] examples={"PlanDoors/B101_North_West","PlanDoors/B106_South_West","PlanDoors/B108_West_North","PlanDoors/B112_East_North","PlanDoors/Lounge_West_Automatic","CoreHalls/B112ElevatorHall","CoreHalls/B112ElevatorHall/ServicePanels/LargeEntranceSideDoor"};
                string[] shots={"B101OpenDoor","B106OpenDoor","B108OpenDoor","B112OpenDoor","LoungeSlidingOpen","ElevatorSlidingOpen","WhiteServiceDoorOpen"};
                for(int i=0;i<examples.Length;i++)
                {
                    manager.ResetDoors();var door=root.transform.Find(examples[i]).GetComponent<InteractiveDoor>();door.SetProgressInstant(1);cam.fieldOfView=62;
                    cam.fieldOfView=i<4?70:62;
                    cam.transform.position=door.InteractionPoint-door.transform.forward*(i<4?1.65f:2.45f)+Vector3.up*.38f;cam.transform.LookAt(door.InteractionPoint+door.transform.forward*.45f);
                    if(i==6){var hall=root.transform.Find("CoreHalls/B112ElevatorHall");cam.transform.position=hall.TransformPoint(new Vector3(.70f,1.50f,.75f));cam.transform.LookAt(hall.TransformPoint(new Vector3(-1.1f,1.35f,2.2f)));}
                    var shot=Capture(folder,shots[i]);while(shot.MoveNext())yield return shot.Current;
                }
                manager.ResetDoors();example.SetProgressInstant(1);chair.transform.SetPositionAndRotation(ActorAt(example,-1.65f),example.transform.rotation);follow.enabled=true;follow.SetViewMode(WheelchairViewMode.FirstPerson);hud.GetComponent<Canvas>().enabled=true;manager.enabled=true;foreach(var d in doors)d.enabled=true;manager.RefreshSelection();
                var uiShot=Capture(folder,"DoorControlsFirstPerson");while(uiShot.MoveNext())yield return uiShot.Current;
                Debug.Log("INTERACTIVE_DOORS_CAPTURE_OK: classroom hinge directions, lounge, lift, service panel and first-person controls captured.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);foreach(var d in doors)if(d){d.enabled=true;d.ResetDoor();}
                manager.enabled=true;follow.enabled=true;follow.SetViewMode(WheelchairViewMode.ThirdPerson);sim.ResetSimulation();
            }
        }
    }
}
