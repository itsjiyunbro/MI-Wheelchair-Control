using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallReverseMovement
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void ApplyAndValidate()
        {
            EditorSceneManager.OpenScene(ConvergenceHallB1Setup.ScenePath);
            if(UnityEngine.Object.FindFirstObjectByType<DemoFreeCamera>())ConvergenceHallDemoCamera.ConfigureCurrentScene();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            SessionState.SetBool("EEG.ConvergenceQA.FocusReverseMovement",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
        static IEnumerator Wait(float seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)yield return null;}
        public static IEnumerator RunChecks(string folder)
        {
            var movement=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();var hud=UnityEngine.Object.FindFirstObjectByType<WheelchairStatusUI>();var follow=UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            var keyboard=InputSystem.AddDevice<Keyboard>();var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="ReverseQAObstacle";wall.layer=LayerMask.NameToLayer("WheelchairObstacle");wall.transform.position=new Vector3(200,1,198);wall.transform.localScale=new Vector3(5,2,.10f);
            try
            {
                sim.ResetSimulation();sim.SetControlSource(WheelchairControlSource.Keyboard);var start=new Vector3(200,.5f,200);movement.transform.SetPositionAndRotation(start,Quaternion.identity);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));for(int i=0;i<5;i++)yield return null;Check(movement.transform.position==start,"S bypassed STOP");
                sim.StartSimulation();for(int i=0;i<8;i++)yield return null;hud.RefreshDisplay();
                Check(movement.transform.position.z<start.z&&movement.MoveDirection==-1&&hud.transform.Find("StatusPanel/MoveText").GetComponent<Text>().text=="MOVE: BACKWARD","S reverse/HUD failed");
                sim.StopSimulation();var stopped=movement.transform.position;for(int i=0;i<5;i++)yield return null;Check(stopped==movement.transform.position&&!movement.IsMoving,"STOP did not stop held reverse");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;
                foreach(int fps in new[]{30,120})
                {
                    sim.StartSimulation();movement.transform.SetPositionAndRotation(new Vector3(210,.5f,210),Quaternion.identity);
                    for(int i=0;i<fps;i++)movement.ApplyDirectionalInput(-1,0,1f/fps);
                    Check(Mathf.Abs(movement.transform.position.z-208.8f)<.003f,"Reverse speed depends on frame rate");
                    movement.transform.SetPositionAndRotation(start,Quaternion.identity);movement.ApplyDirectionalInput(-1,0,10);
                    Check(movement.transform.position.z>198.35f&&movement.transform.position.z<198.55f,"Reverse crossed rear wall");
                    var nearWall=movement.transform.position;movement.ApplyDirectionalInput(-1,0,1);Check(!movement.IsMoving&&Vector3.Distance(nearWall,movement.transform.position)<.001f,"Blocked reverse reports movement");
                    movement.ApplyInput(true,0,.5f);Check(movement.transform.position.z>nearWall.z+.90f,"Forward cannot leave a wall after reversing");
                    wall.transform.position=new Vector3(200,1,202);movement.transform.SetPositionAndRotation(start,Quaternion.identity);movement.ApplyInput(true,0,10);var frontWall=movement.transform.position;movement.ApplyDirectionalInput(-1,0,1);
                    Check(movement.transform.position.z<frontWall.z-1.1f,"Reverse cannot leave a front wall");wall.transform.position=new Vector3(200,1,198);
                }
                movement.transform.SetPositionAndRotation(new Vector3(210,.5f,210),Quaternion.identity);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.DownArrow,Key.LeftArrow));for(int i=0;i<6;i++)yield return null;
                Check(movement.MoveDirection==-1&&movement.HeadingDegrees>300,"Down arrow plus reverse steering failed");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.S));for(int i=0;i<2;i++)yield return null;var cancelled=movement.transform.position;for(int i=0;i<3;i++)yield return null;Check(cancelled==movement.transform.position&&!movement.IsMoving,"W+S should cancel translation");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;sim.SetControlSource(WheelchairControlSource.Python);var pythonPose=movement.transform.position;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));for(int i=0;i<4;i++)yield return null;Check(pythonPose==movement.transform.position,"S bypassed Python source selection");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;sim.SetControlSource(WheelchairControlSource.Keyboard);
                var door=GameObject.Find("ConvergenceHallB1").transform.Find("PlanDoors/B101_North_West").GetComponent<InteractiveDoor>();door.SetProgressInstant(1);
                var inside=door.InteractionPoint+door.transform.forward*.45f;inside.y=.5f;movement.transform.SetPositionAndRotation(inside,door.transform.rotation);
                var guard=movement.GetComponent<WheelchairCollisionGuard>();
                if(!guard.CanOccupy(inside,door.transform.rotation))
                {
                    var overlaps=Physics.OverlapBox(guard.QueryCentre,guard.FootprintHalfExtents,door.transform.rotation,LayerMask.GetMask("WheelchairObstacle"),QueryTriggerInteraction.Ignore);
                    throw new Exception("Reverse doorway start blocked: "+string.Join(",",System.Array.ConvertAll(overlaps,c=>UnityEditor.AnimationUtility.CalculateTransformPath(c.transform,GameObject.Find("ConvergenceHallB1").transform))));
                }
                sim.StartSimulation();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));var wait=Wait(1.1f);while(wait.MoveNext())yield return wait.Current;
                Check(Vector3.Dot(movement.transform.position-door.InteractionPoint,door.transform.forward)<-.1f,"Reverse cannot cross open 95cm door; signed position="+Vector3.Dot(movement.transform.position-door.InteractionPoint,door.transform.forward));InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;
                sim.ResetSimulation();var resetPose=movement.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));for(int i=0;i<4;i++)yield return null;Check(resetPose==movement.transform.position&&movement.MoveDirection==0,"RESET retained reverse input");
                var free=UnityEngine.Object.FindFirstObjectByType<DemoFreeCamera>();free.EnterFreeView();var cameraPose=free.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));for(int i=0;i<5;i++)yield return null;
                Check(movement.transform.position==resetPose&&Vector3.Distance(cameraPose,free.transform.position)>.01f,"Free camera S moved wheelchair or lost camera backward motion");free.ExitFreeView();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());for(int i=0;i<2;i++)yield return null;
                Debug.Log("REVERSE_MOVEMENT_OK: real S/down arrow, reverse steering, W+S cancellation, BACKWARD HUD, 30/120FPS speed, rear/front-wall clearance, open 95cm reverse passage, STOP/RESET/Python source and free camera isolation passed.");
                movement.transform.SetPositionAndRotation(ConvergenceHallB1Setup.Plan(745,800,.5f),Quaternion.identity);follow.SetViewMode(WheelchairViewMode.ThirdPerson);sim.StartSimulation();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S));for(int i=0;i<4;i++)yield return null;
                string path=Path.Combine(folder,"WheelchairReverse.png");var stamp=DateTime.UtcNow;ScreenCapture.CaptureScreenshot(path);double deadline=EditorApplication.timeSinceStartup+20;
                while(!File.Exists(path)||File.GetLastWriteTimeUtc(path)<stamp){if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException(path);yield return null;}
            }
            finally{InputSystem.RemoveDevice(keyboard);UnityEngine.Object.Destroy(wall);sim.ResetSimulation();sim.SetControlSource(WheelchairControlSource.Keyboard);}
        }
    }
}
