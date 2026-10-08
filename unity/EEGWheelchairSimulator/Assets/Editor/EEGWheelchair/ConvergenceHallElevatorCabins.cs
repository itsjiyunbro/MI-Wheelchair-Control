using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallElevatorCabins
    {
        public const float Width=1.70f,Depth=1.50f,Height=2.30f,Wall=.08f;
        public static readonly string[] Halls={"B112ElevatorHall","B109ElevatorHall"};
        public static Transform Door(GameObject root,bool east)=>root.transform.Find(east?"CoreHalls/B112ElevatorHall":"CoreHalls/B109ElevatorHall/LeftWallElevator");
        static Transform Part(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collision=false)
        {
            var t=P.Box(parent,name,position,size,material);
            if(collision){var hit=t.gameObject.AddComponent<BoxCollider>();t.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");}
            return t;
        }
        public static void ApplyTo(GameObject root)
        {
            var steel=P.Mat("LiftCabinSteel",new Color(.71f,.73f,.73f),.38f);steel.SetFloat("_Metallic",.30f);EditorUtility.SetDirty(steel);
            var trim=P.Mat("LiftCabinTrim",new Color(.37f,.39f,.40f),.48f);var ceiling=P.Mat("LiftCabinCeiling",new Color(.79f,.80f,.78f),.18f);
            var floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/CorridorTile.mat");
            foreach(bool east in new[]{true,false})
            {
                var hall=root.transform.Find("CoreHalls/"+(east?Halls[0]:Halls[1]));var door=Door(root,east);
                var bounds=root.transform.Find("RoomsAndCores/"+(east?"EastLift":"WestLiftCore")+"/RoomFloor").GetComponent<Renderer>().bounds;
                float doorZ=0;
                if(east)
                {
                    // Allocate the car inside the original core, rather than across its rear corridor.
                    float rear=hall.InverseTransformPoint(new Vector3(bounds.max.x-.06f,0,hall.position.z)).z;
                    doorZ=rear-Wall-Depth-.04f;float delta=doorZ-4.8f;
                    string[] details={"LiftFrame","SlidingLeaf0","SlidingLeaf1","CentreSeam","Header","FloorDisplay","FloorDisplayBacking","Threshold","CallPanel","CallButton","CallButtonBacking"};
                    foreach(Transform t in hall.Cast<Transform>().Where(t=>details.Contains(t.name)||t.name.StartsWith("TactilePad")).ToArray())t.localPosition+=Vector3.forward*delta;
                    door.Find("PhotoElevatorDetails").localPosition=new Vector3(0,0,delta);
                    hall.Find("RearWall").gameObject.SetActive(false);hall.Find("RearWallCollision").gameObject.SetActive(false);
                }
                else
                {
                    // The left-wall car stays centered along the hall and within the south core wall.
                    float back=hall.InverseTransformPoint(new Vector3(hall.position.x,0,bounds.min.z+.06f)).x;
                    var at=door.localPosition;at.x=back+Wall+Depth+.04f;door.localPosition=at;
                    hall.Find("LeftReturn").gameObject.SetActive(false);hall.Find("LeftReturnCollision").gameObject.SetActive(false);
                }
                var old=door.Find("MeasuredCabin");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var car=P.Group(door,"MeasuredCabin");car.localPosition=new Vector3(0,0,doorZ+.04f);
                Part(car,"CabinFloor",new Vector3(0,.018f,Depth/2),new Vector3(Width,.024f,Depth),floor);
                Part(car,"LeftWall",new Vector3(-(Width+Wall)/2,Height/2,Depth/2),new Vector3(Wall,Height,Depth+Wall),steel,true);
                Part(car,"RightWall",new Vector3((Width+Wall)/2,Height/2,Depth/2),new Vector3(Wall,Height,Depth+Wall),steel,true);
                Part(car,"RearWall",new Vector3(0,Height/2,Depth+Wall/2),new Vector3(Width+2*Wall,Height,Wall),steel,true);
                Part(car,"Ceiling",new Vector3(0,Height+.012f,Depth/2),new Vector3(Width,.024f,Depth),ceiling);
                for(int i=0;i<2;i++)
                {
                    float x=(i==0?-1:1)*(.63f+(Width/2-.63f)/2);
                    Part(car,"FrontCheek"+i,new Vector3(x,Height/2,-.04f),new Vector3(Width/2-.63f,Height,.08f),steel,true);
                    Part(car,"DoorJamb"+i,new Vector3(i==0?-.675f:.675f,1.20f,-.085f),new Vector3(.075f,2.40f,.10f),steel);
                    Part(car,"SideSkirting"+i,new Vector3(i==0?-.843f:.843f,.055f,Depth/2),new Vector3(.012f,.050f,Depth),trim);
                }
                Part(car,"RearSkirting",new Vector3(0,.055f,Depth-.006f),new Vector3(Width,.050f,.012f),trim);
                Part(car,"InnerDoorHeader",new Vector3(0,2.1825f,-.10f),new Vector3(1.26f,.235f,.035f),steel);
                Part(car,"CallPanelMount",new Vector3(.86f,.96f,-.081f),new Vector3(.17f,.325f,.025f),steel);
                door.Find("PhotoElevatorDetails/PhotoCallPanel").localPosition+=Vector3.back*.055f;
                Part(car,"ClosedDoorCollision",new Vector3(0,1.03f,-.150f),new Vector3(1.26f,2.06f,.035f),steel,true).GetComponent<Renderer>().enabled=false;
                // The former frame was a filled cube; use separate jambs to expose the inner door faces.
                door.Find("LiftFrame").gameObject.SetActive(false);
            }
        }
        public static void Validate(GameObject root)
        {
            foreach(bool east in new[]{true,false})
            {
                var door=Door(root,east);var car=door.Find("MeasuredCabin");
                if(!car||door.Cast<Transform>().Count(t=>t.name=="MeasuredCabin")!=1)throw new Exception("Cabin missing/duplicated");
                var floor=car.Find("CabinFloor");
                if(Mathf.Abs(floor.localScale.x-Width)>.0001f||Mathf.Abs(floor.localScale.z-Depth)>.0001f)throw new Exception("Cabin measured floor dimensions mismatch");
                if(Mathf.Abs(car.Find("LeftWall").localPosition.x+car.Find("LeftWall").localScale.x/2+Width/2)>.0001f||Mathf.Abs(car.Find("RightWall").localPosition.x-car.Find("RightWall").localScale.x/2-Width/2)>.0001f||Mathf.Abs(car.Find("RearWall").localPosition.z-Wall/2-Depth)>.0001f)throw new Exception("Cabin clear inside surfaces mismatch");
                var boundary=root.transform.Find("RoomsAndCores/"+(east?"EastLift":"WestLiftCore")+"/RoomFloor").GetComponent<Renderer>().bounds;
                foreach(var r in car.GetComponentsInChildren<Renderer>())
                {
                    var b=r.bounds;
                    if(b.min.x<boundary.min.x+.059f||b.max.x>boundary.max.x-.059f||b.min.z<boundary.min.z+.059f||b.max.z>boundary.max.z-.059f)throw new Exception("Cabin crosses original core perimeter: "+r.name);
                }
                if(car.GetComponentsInChildren<Light>().Length>0)throw new Exception("Elevator light effects must stay removed");
                if(door.InverseTransformPoint(door.Find("PhotoElevatorDetails/PhotoCallPanel").position).z>=car.localPosition.z-.08f)throw new Exception("Call panel is recessed behind new front wall");
                var blocker=car.Find("ClosedDoorCollision").GetComponent<BoxCollider>();
                if(!blocker.enabled||!blocker.Raycast(new Ray(car.TransformPoint(new Vector3(0,1,-.5f)),car.forward),out var hit,1))throw new Exception("Closed door guard missing");
            }
            Debug.Log("MEASURED_CABINS_OK: both cars have 1.70m clear door-side width and 1.50m clear depth, fit original core boundaries, closed door guards and no light effects.");
        }
        public static void ApplyAndCapture()
        {
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));
            if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Cabin duplicate authoring");
            SessionState.SetBool("EEG.ConvergenceQA.FocusCabins",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
