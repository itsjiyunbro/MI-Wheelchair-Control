using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallFirstFloorPassageChecks
    {
        static Vector3 Point(GameObject floor,Vector2 p)=>ConvergenceHallFirstFloor.World(floor.transform,p.x,p.y,.5f);
        static void Require(bool ok,string message){if(!ok)throw new Exception("BLUEBELL_101: "+message);}
        static string Path(Transform t){string p=t.name;while(t.parent){t=t.parent;p=t.name+"/"+p;}return p;}
        public static void AuditBefore(GameObject floor)
        {
            var guard=UnityEngine.Object.FindFirstObjectByType<WheelchairCollisionGuard>();
            float height=new SerializedObject(guard).FindProperty("queryHeight").floatValue;
            foreach(var p in new[]{new Vector2(30.7f,28.7f),new Vector2(31.6f,35.04f),new Vector2(29.7f,34.9f)})
            {
                var hits=Physics.OverlapBox(Point(floor,p)+Vector3.up*height,guard.FootprintHalfExtents,Quaternion.identity,LayerMask.GetMask("WheelchairObstacle"),QueryTriggerInteraction.Ignore);
                Debug.Log("BLUEBELL_101_BEFORE "+p+": "+string.Join("; ",hits.Select(c=>Path(c.transform))));
            }
        }
        public static IEnumerator Validate(GameObject floor)
        {
            var chair=UnityEngine.Object.FindFirstObjectByType<WheelchairMovement>();var guard=chair.GetComponent<WheelchairCollisionGuard>();var sim=UnityEngine.Object.FindFirstObjectByType<SimulationController>();
            var portal=floor.transform.Find("RoomsAndCores/Bluebell101Passage").GetComponent<InteractiveDoor>();
            portal.ResetDoor();Require(portal.Progress>.999f,"fire doors must start open");
            var route=new[]{new Vector2(17,26.8f),new Vector2(32,26.8f),new Vector2(32,35.55f),new Vector2(32,38.7f)};
            // Structural regression: the railing aisle, portal, and corridor are collinear.
            var localPortal=floor.transform.InverseTransformPoint(portal.transform.position);
            Require(Mathf.Abs(localPortal.z+32)<.001f,"portal offset from straight railing aisle");
            var lounge=floor.transform.Find("Details/BluebellLoungeSeating");
            Require(lounge&&lounge.Cast<Transform>().Count(t=>t.name.StartsWith("RoundTable"))==3,"three lounge tables missing");
            Require(lounge.Cast<Transform>().Count(t=>t.name.StartsWith("WallChair"))==8,"wall study chairs missing");
            Require(lounge.Cast<Transform>().Count(t=>t.name.StartsWith("RailStool"))==7,"railing seats missing");
            Require(lounge.Cast<Transform>().Count(t=>t.name.StartsWith("CafeChair"))==9,"round table chairs missing");
            foreach(Transform seat in lounge)
            {
                Vector3 expected=seat.name.StartsWith("RailStool")?Vector3.right:seat.name.StartsWith("WallChair")?Vector3.left:seat.name.StartsWith("WindowStool")?Vector3.back:Vector3.zero;
                if(expected!=Vector3.zero)Require(Vector3.Dot(floor.transform.InverseTransformDirection(seat.forward),expected)>.99f,"seat faces away from photographed desk/wall: "+seat.name);
                if(seat.name.StartsWith("CafeChair"))
                {
                    var table=lounge.Find("RoundTable"+seat.name.Substring(9,1));var delta=table.position-seat.position;delta.y=0;
                    Require(Vector3.Dot(seat.forward,delta.normalized)>.99f,"cafe chair faces away from table "+seat.name);
                }
            }
            // Accessible cross-aisle between the railing stools and round tables.
            for(float u=32;u<=38.05f;u+=.10f)
                Require(guard.CanOccupy(Point(floor,new Vector2(u,31.15f)),floor.transform.rotation),"lounge cross aisle blocked at "+u);
            int count=0;float queryHeight=new SerializedObject(guard).FindProperty("queryHeight").floatValue;
            // Check the complete connection, including turn space and floor support, rather
            // than only the independent corridor centre lines on either side of the blockage.
            for(int leg=1;leg<route.Length;leg++)
            {
                var a=Point(floor,route[leg-1]);var b=Point(floor,route[leg]);var rot=Quaternion.LookRotation(b-a);int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/.10f);
                for(int i=0;i<=steps;i++)
                {
                    var p=Vector3.Lerp(a,b,i/(float)steps);Require(guard.CanOccupy(p,rot),"route blocked "+floor.transform.InverseTransformPoint(p));
                    Require(!Physics.CheckBox(p+Vector3.up*queryHeight,guard.FootprintHalfExtents+new Vector3(.06f,0,.06f),rot,LayerMask.GetMask("WheelchairObstacle"),QueryTriggerInteraction.Ignore),"insufficient passage margin "+floor.transform.InverseTransformPoint(p));
                    Require(Physics.RaycastAll(p+Vector3.up*.10f,Vector3.down,.85f,~LayerMask.GetMask("WheelchairObstacle"),QueryTriggerInteraction.Ignore).Any(h=>h.normal.y>.9f&&!h.transform.IsChildOf(chair.transform)&&Mathf.Abs(h.point.y-floor.transform.position.y)<.05f),"missing landing floor "+p);
                    count++;
                }
            }
            foreach(var corner in route.Skip(1).Take(route.Length-2))for(int yaw=0;yaw<360;yaw+=5)Require(guard.CanOccupy(Point(floor,corner),Quaternion.Euler(0,yaw,0)),"turning blocked at "+corner+" yaw="+yaw);
            float speed=new SerializedObject(chair).FindProperty("moveSpeed").floatValue;float turnSpeed=new SerializedObject(chair).FindProperty("turnSpeed").floatValue;
            foreach(int fps in new[]{30,120})foreach(bool reverse in new[]{false,true})
            {
                sim.ResetSimulation();portal.ResetDoor();var path=reverse?route.Reverse().ToArray():route;
                chair.transform.SetPositionAndRotation(Point(floor,path[0]),Quaternion.LookRotation(Point(floor,path[1])-Point(floor,path[0])));sim.SetControlSource(WheelchairControlSource.Keyboard);sim.StartSimulation();
                for(int i=1;i<path.Length;i++)
                {
                    var target=Point(floor,path[i]);float heading=Quaternion.LookRotation(target-chair.transform.position).eulerAngles.y;
                    int limit=0;while(Mathf.Abs(Mathf.DeltaAngle(chair.transform.eulerAngles.y,heading))>.02f)
                    {
                        float delta=Mathf.DeltaAngle(chair.transform.eulerAngles.y,heading);chair.ApplyDirectionalInput(0,Mathf.Clamp(delta/(turnSpeed/fps),-1,1),1f/fps);Require(++limit<fps*7,"steering stalled");
                    }
                    limit=0;while(Vector3.Distance(chair.transform.position,target)>.008f)
                    {
                        float d=Vector3.Distance(chair.transform.position,target);var previous=chair.transform.position;chair.ApplyDirectionalInput(Mathf.Min(1,d/(speed/fps)),0,1f/fps);
                        Require(Vector3.Distance(previous,chair.transform.position)>.00001f,"drive blocked at "+floor.transform.InverseTransformPoint(chair.transform.position));Require(++limit<fps*20,"drive timeout");
                    }
                }
                sim.StopSimulation();Require(Vector3.Distance(chair.transform.position,Point(floor,path[path.Length-1]))<.015f,"wrong destination");yield return null;
            }
            // The same open portal must remain usable after RESET, and Room 101 itself
            // must be reachable from the corridor when its own door is opened.
            sim.ResetSimulation();Require(portal.Progress>.999f,"RESET closed the photo-reference fire doors");
            var entry=floor.transform.Find("RoomsAndCores/Room101/U0/Door_101").GetComponent<InteractiveDoor>();entry.SetProgressInstant(1);
            for(float z=-.8f;z<=.8f;z+=.05f)Require(guard.CanOccupy(entry.transform.TransformPoint(new Vector3(0,.5f,z)),entry.transform.rotation),"Room 101 doorway blocked "+z);
            entry.ResetDoor();UnityEngine.Object.FindFirstObjectByType<ConvergenceFloorNavigation>().GoToFirstFloor();
            Debug.Log("BLUEBELL_101_PASSAGE_OK: "+count+" supported route samples with margin; 360-degree turns; keyboard forward/steer trips both directions at 30/120fps; open fire-door RESET; Room 101 doorway clearance.");
        }
    }
}
