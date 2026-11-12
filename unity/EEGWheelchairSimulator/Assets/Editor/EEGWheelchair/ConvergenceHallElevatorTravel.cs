using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallElevatorTravel
    {
        static Text Text(Transform p,string name,string value,Font font,Vector2 size,Vector2 offset)
        {
            var t=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();t.transform.SetParent(p,false);
            t.font=font;t.text=value;t.fontSize=15;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
            var r=(RectTransform)t.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=offset;r.sizeDelta=size;return t;
        }
        static Button Button(Transform panel,Transform template,string name,string label,Vector2 pos,UnityEngine.Events.UnityAction action)
        {
            var r=(RectTransform)UnityEngine.Object.Instantiate(template.gameObject,panel).transform;r.name=name;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=pos;r.sizeDelta=new Vector2(149,34);
            var b=r.GetComponent<Button>();b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);
            var t=r.GetComponentInChildren<Text>();t.fontSize=14;t.text=label;return b;
        }
        public static void Build(GameObject b1,GameObject first,Font font)
        {
            var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var actor=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();
            var old=sim.transform.Find("PassengerElevators");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            old=hud.transform.Find("ElevatorControls");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(string n in new[]{"EastTransitFade","WestTransitFade"}){old=hud.transform.Find(n);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var root=P.Group(sim.transform,"PassengerElevators");var ui=new GameObject("ElevatorControls",typeof(RectTransform)).GetComponent<RectTransform>();ui.SetParent(hud.transform,false);ui.anchorMin=Vector2.zero;ui.anchorMax=Vector2.one;ui.offsetMin=ui.offsetMax=Vector2.zero;
            var template=hud.transform.Find("StatusPanel/StartButton");
            foreach(bool east in new[]{true,false})
            {
                string name=east?"동쪽 엘리베이터":"서쪽 엘리베이터";
                var g=P.Group(root,east?"EastLift":"WestLift");var lift=g.gameObject.AddComponent<PassengerElevator>();
                var lower=ConvergenceHallElevatorCabins.Door(b1,east);var upper=first.transform.Find(east?"RoomsAndCores/NorthElevatorHall":"RoomsAndCores/SouthElevatorHall/LeftWallElevator");
                var stops=new[]{new PassengerElevator.Stop{cabin=lower.Find("MeasuredCabin"),door=lower.GetComponent<InteractiveDoor>()},new PassengerElevator.Stop{cabin=upper.Find("MeasuredCabin"),door=upper.GetComponent<InteractiveDoor>(),indicator=upper.Find("PhotoElevatorDetails/PhotoFloorIndicator/FloorNumber").GetComponent<Text>()}};
                var panel=new GameObject(east?"EastControls":"WestControls",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();panel.SetParent(ui,false);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,1);panel.anchoredPosition=new Vector2(0,-18);panel.sizeDelta=new Vector2(330,158);panel.GetComponent<Image>().color=new Color(.035f,.065f,.11f,.97f);
                var label=Text(panel,"Status",name,font,new Vector2(314,64),new Vector2(0,-5));
                var b=Button(panel,template,"B1","B1 · 지하 1층",new Vector2(12,-72),lift.GoToBasement);var f=Button(panel,template,"1F","1F · 1층",new Vector2(169,-72),lift.GoToFirstFloor);
                var o=Button(panel,template,"Open","문 열기 / 호출",new Vector2(12,-114),lift.OpenDoors);var c=Button(panel,template,"Close","문 닫기",new Vector2(169,-114),lift.CloseDoors);
                var fade=new GameObject(east?"EastTransitFade":"WestTransitFade",typeof(RectTransform),typeof(Canvas),typeof(Image));fade.transform.SetParent(ui,false);var rect=(RectTransform)fade.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                // The full-screen fade must be parented directly to the screen-sized HUD.
                rect.SetParent(hud.transform,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                var fc=fade.GetComponent<Canvas>();fc.overrideSorting=true;fc.sortingOrder=200;var image=fade.GetComponent<Image>();image.color=Color.clear;image.raycastTarget=false;
                var ft=Text(rect,"TravelMessage","엘리베이터 이동 중",font,new Vector2(400,60),Vector2.zero);var fr=(RectTransform)ft.transform;fr.anchorMin=fr.anchorMax=fr.pivot=new Vector2(.5f,.5f);
                fade.SetActive(false);panel.gameObject.SetActive(false);
                lift.Configure(name,stops,sim,actor,follow,panel.gameObject,label,b,f,o,c,image);
                foreach(var stop in stops)
                {
                    var existing=stop.cabin.Find("PassengerControlPanel");if(existing)UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    var p=P.Group(stop.cabin,"PassengerControlPanel");p.localPosition=new Vector3(.75f,1.13f,.65f);p.localRotation=Quaternion.Euler(0,90,0);
                    var metal=upper.Find("CallPanel").GetComponent<Renderer>().sharedMaterial;var dark=upper.Find("FloorDisplayBacking").GetComponent<Renderer>().sharedMaterial;
                    P.Box(p,"Plate",Vector3.zero,new Vector3(.2f,.4f,.02f),metal);
                    foreach(int i in new[]{0,1}){var t=P.Box(p,"Button"+i,new Vector3(0,i*.12f-.06f,-.016f),new Vector3(.065f,.065f,.014f),dark);}
                }
            }
        }
        static void Require(bool ok,string message){if(!ok)throw new Exception("ELEVATOR: "+message);}
        public static IEnumerator CaptureJourney(string dir,Action<Camera,string> capture)
        {
            var lift=UnityEngine.Object.FindObjectsByType<PassengerElevator>(FindObjectsSortMode.None).First(e=>e.name=="EastLift");var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();
            sim.ResetSimulation();var s=lift.Stops[0];chair.transform.SetPositionAndRotation(lift.BoardingPosition(0),s.cabin.rotation*Quaternion.Euler(0,180,0));s.door.SetProgressInstant(1);follow.SetViewMode(WheelchairViewMode.FirstPerson);follow.SnapToTarget();
            float ready=Time.realtimeSinceStartup+.3f;while(Time.realtimeSinceStartup<ready)yield return null;
            var panel=hud.transform.Find("ElevatorControls/EastControls");Require(panel.gameObject.activeInHierarchy,"boarding controls invisible");
            capture(Camera.main,Path.Combine(dir,"09_ElevatorBoarding.png"));
            panel.Find("1F").GetComponent<Button>().onClick.Invoke();Require(lift.Busy,"floor button is not connected");
            float end=Time.realtimeSinceStartup+20;bool transit=false;
            while(lift.Busy)
            {
                Require(Time.realtimeSinceStartup<end,"real-time journey timed out");
                if(!transit&&lift.State==PassengerElevator.Phase.Travelling){transit=true;}
                yield return null;
            }
            Require(transit&&lift.FullyInside(1)&&!sim.IsRunning,"real-time trip failed");
            ready=Time.realtimeSinceStartup+.3f;while(Time.realtimeSinceStartup<ready)yield return null;Require(panel.Find("B1").GetComponent<Button>().interactable&&!sim.MovementLocked,"arrival controls locked");capture(Camera.main,Path.Combine(dir,"10_ElevatorArrival1F.png"));
            panel.Find("B1").GetComponent<Button>().onClick.Invoke();Require(lift.Busy,"return button is not connected");end=Time.realtimeSinceStartup+20;while(lift.Busy){Require(Time.realtimeSinceStartup<end,"return timed out");yield return null;}
            Require(lift.FullyInside(0),"real-time return failed");sim.ResetSimulation();follow.SetViewMode(WheelchairViewMode.ThirdPerson);UnityEngine.Object.FindFirstObjectByType<ConvergenceFloorNavigation>().GoToFirstFloor();
            Debug.Log("ELEVATOR_UI_OK: visible boarding panel, actual 1F/B1 button callbacks, frame-driven round trip and arrival.");
        }
        public static void Validate()
        {
            var lifts=UnityEngine.Object.FindObjectsByType<PassengerElevator>(FindObjectsSortMode.None);Require(lifts.Length==2,"two lifts missing");
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var guard=chair.GetComponent<WheelchairCollisionGuard>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();var free=follow.GetComponent<DemoFreeCamera>();var nav=UnityEngine.Object.FindFirstObjectByType<ConvergenceFloorNavigation>();
            foreach(var lift in lifts)
            {
                foreach(int fps in new[]{30,120})foreach(int start in new[]{0,1})
                {
                    sim.ResetSimulation();var s=lift.Stops[start];s.door.SetProgressInstant(1);chair.transform.SetPositionAndRotation(lift.BoardingPosition(start),s.cabin.rotation);Physics.SyncTransforms();
                    Require(guard.CanOccupy(chair.transform.position,chair.transform.rotation),lift.name+" cabin occupied "+start);
                    // The open threshold and a straight boarding route must also be traversable.
                    for(float z=-.65f;z<.78f;z+=.05f)Require(guard.CanOccupy(s.cabin.TransformPoint(new Vector3(0,.5f,z)),s.cabin.rotation),lift.name+" blocked boarding "+start+" z="+z);
                    chair.transform.position=s.cabin.TransformPoint(new Vector3(0,.5f,.12f));Require(!lift.RequestFloor(1-start),"threshold boarding accepted");
                    s.door.SetOpen(false);for(int k=0;k<fps*2;k++)s.door.Step(1f/fps);Require(s.door.Blocked&&s.door.Progress>.01f,"closing door did not stop for wheelchair");s.door.SetProgressInstant(1);
                    chair.transform.position=lift.BoardingPosition(start);Physics.SyncTransforms();Require(lift.FullyInside(start),"inside detection");
                    Require(lift.RequestFloor(1-start),lift.name+" trip rejected "+start);var before=chair.transform.position;
                    sim.StartSimulation();Require(!sim.IsRunning,"START bypassed interlock");free.EnterFreeView();Require(!free.IsFreeView,"free view bypassed interlock");nav.GoToFirstFloor();Require(Vector3.Distance(before,chair.transform.position)<.001f,"shortcut bypassed interlock");
                    sim.SetControlSource(WheelchairControlSource.Python);chair.ApplyDirectionalInput(1,0,1,WheelchairControlSource.Python);Require(Vector3.Distance(before,chair.transform.position)<.001f,"Python moved during trip");
                    for(int k=0;k<fps*6;k++)lift.Step(1f/fps);
                    Require(!lift.Busy&&!sim.MovementLocked&&!sim.IsRunning,"trip did not release stopped");Require(lift.FullyInside(1-start),"wrong arrival cabin");Require(lift.Stops[1-start].door.Progress>.99f&&s.door.Progress<.001f,"door sequencing");
                    Require(Vector3.Dot(chair.transform.forward,-lift.Stops[1-start].cabin.forward)>.999f,"arrival not facing exit");
                    // W alone must clear the doorway after arrival at either floor.
                    sim.SetControlSource(WheelchairControlSource.Keyboard);sim.StartSimulation();for(int k=0;k<fps;k++)chair.ApplyDirectionalInput(1,0,1f/fps,WheelchairControlSource.Keyboard);sim.StopSimulation();Require(!lift.FullyInside(1-start),"unable to drive forward out of destination");
                    Require(lift.Stops[1-start].cabin.InverseTransformPoint(chair.transform.position).z<-.5f,"exit path blocked");
                }
                foreach(float seconds in new[]{.1f,1.3f,3f})
                {
                    sim.ResetSimulation();chair.transform.SetPositionAndRotation(lift.BoardingPosition(0),lift.Stops[0].cabin.rotation);lift.Stops[0].door.SetProgressInstant(1);Require(lift.RequestFloor(1),"reset test request");for(int k=0;k<(int)(seconds*60);k++)lift.Step(1f/60);sim.ResetSimulation();Require(!lift.Busy&&!sim.MovementLocked&&!sim.IsRunning,"RESET left travel lock");foreach(var s in lift.Stops)Require(s.door.enabled&&s.door.Progress<.001f,"RESET left elevator door");
                }
            }
            nav.GoToFirstFloor();Debug.Log("ELEVATOR_TRAVEL_OK: both lifts, B1/1F round trips at 30/120fps, exit-facing arrival and W-only disembarkation, boarding clearance, doorway obstruction, START/Python/free-view/shortcut interlocks, RESET during closing/travel/opening.");
        }
    }
}
