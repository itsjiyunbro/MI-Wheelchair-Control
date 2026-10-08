using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using P=EEGWheelchairSimulator.Editor.ConvergenceHallClassroomProps;

namespace EEGWheelchairSimulator.Editor
{
    public static class ConvergenceHallFinalFinishes
    {
        const float WallHeight=2.85f,OpeningHalf=.74f,Face=-.16f;
        static bool FloorText(Text text)=>text.transform.position.y<.25f&&Mathf.Abs(Vector3.Dot(text.transform.forward,Vector3.up))>.95f;
        public static void HideFloorText(GameObject root)
        {
            // Preserve objects referenced by room/stand authoring; suppress their floor presentation.
            foreach(var text in root.GetComponentsInChildren<Text>(true).Where(FloorText))text.gameObject.SetActive(false);
        }
        static Transform Wall(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var t=P.Box(parent,name,position,size,material);t.gameObject.AddComponent<BoxCollider>();t.gameObject.layer=LayerMask.NameToLayer("WheelchairObstacle");return t;
        }
        public static void RecessElevators(GameObject root)
        {
            foreach(bool east in new[]{true,false})
            {
                var door=ConvergenceHallElevatorCabins.Door(root,east);var cabin=door.Find("MeasuredCabin");float doorZ=cabin.localPosition.z-.04f;
                var old=door.Find("RecessedWallSurround");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var surround=P.Group(door,"RecessedWallSurround");surround.localPosition=new Vector3(0,0,doorZ);
                var finish=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/"+(east?"CoreConcrete":"WarmWhite")+".mat");
                var metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/LiftBrushedSteel.mat");
                var navy=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/ConvergenceHall/Navy.mat");
                float left=-1.15f,right=1.15f;
                if(!east)
                {
                    var bounds=root.transform.Find("RoomsAndCores/WestLiftCore/RoomFloor").GetComponent<Renderer>().bounds;
                    var corners=new[]{new Vector3(bounds.min.x,0,bounds.min.z),new Vector3(bounds.min.x,0,bounds.max.z),new Vector3(bounds.max.x,0,bounds.min.z),new Vector3(bounds.max.x,0,bounds.max.z)};
                    left=corners.Min(p=>door.InverseTransformPoint(p).x)+.06f;right=corners.Max(p=>door.InverseTransformPoint(p).x)-.06f;
                }
                float lw=-OpeningHalf-left,rw=right-OpeningHalf;
                Wall(surround,"LeftWall",new Vector3((left-OpeningHalf)/2,WallHeight/2,-.07f),new Vector3(lw,WallHeight,.18f),finish);
                Wall(surround,"RightWall",new Vector3((right+OpeningHalf)/2,WallHeight/2,-.07f),new Vector3(rw,WallHeight,.18f),finish);
                Wall(surround,"OverDoorWall",new Vector3((left+right)/2,2.625f,-.07f),new Vector3(right-left,.45f,.18f),finish);
                for(int i=0;i<2;i++)
                {
                    P.Box(surround,"MetalReveal"+i,new Vector3(i==0?-.7175f:.7175f,1.2f,-.11f),new Vector3(.045f,2.4f,.10f),metal);
                    float x=i==0?(left-OpeningHalf)/2:(right+OpeningHalf)/2;
                    P.Box(surround,"Skirting"+i,new Vector3(x,.055f,Face-.003f),new Vector3(i==0?lw:rw,.11f,.006f),navy);
                }
                P.Box(surround,"MetalTopReveal",new Vector3(0,2.375f,-.11f),new Vector3(1.48f,.05f,.10f),metal);
                // Move the existing call plate onto the new wall face, keeping its design and height.
                door.Find("PhotoElevatorDetails/PhotoCallPanel").localPosition+=Vector3.back*.11f;
                var mount=cabin.Find("CallPanelMount");var at=mount.localPosition;at.z=-.186f;mount.localPosition=at;var size=mount.localScale;size.z=.04f;mount.localScale=size;
            }
        }
        public static void Validate(GameObject root)
        {
            if(root.GetComponentsInChildren<Text>().Any(FloorText))throw new Exception("Visible text remains on the floor");
            foreach(bool east in new[]{true,false})
            {
                var door=ConvergenceHallElevatorCabins.Door(root,east);var cabin=door.Find("MeasuredCabin");var surround=door.Find("RecessedWallSurround");
                if(!surround||door.Cast<Transform>().Count(t=>t.name=="RecessedWallSurround")!=1||surround.GetComponentsInChildren<Collider>().Length!=3)throw new Exception("Missing/duplicate recessed elevator wall");
                float front=surround.localPosition.z+Face;var leaf=door.Find("SlidingLeaf0");float leafFront=leaf.localPosition.z-leaf.localScale.z/2;
                if(leafFront-front<.03f)throw new Exception("Elevator door is not recessed into the wall");
                float callZ=door.InverseTransformPoint(door.Find("PhotoElevatorDetails/PhotoCallPanel").position).z;
                if(callZ>=front)throw new Exception("Call panel hidden by new wall");
                var boundary=root.transform.Find("RoomsAndCores/"+(east?"EastLift":"WestLiftCore")+"/RoomFloor").GetComponent<Renderer>().bounds;
                foreach(var renderer in surround.GetComponentsInChildren<Renderer>())
                {
                    var b=renderer.bounds;if(b.min.x<boundary.min.x+.055f||b.max.x>boundary.max.x-.055f||b.min.z<boundary.min.z+.055f||b.max.z>boundary.max.z-.055f||b.min.y<-.002f||b.max.y>WallHeight+.002f)throw new Exception("Elevator surround crosses core boundary: "+renderer.name);
                }
                if(Mathf.Abs(cabin.Find("CabinFloor").localScale.x-1.7f)>.001f||Mathf.Abs(cabin.Find("CabinFloor").localScale.z-1.5f)>.001f)throw new Exception("Measured cabin size changed");
                foreach(float planX in new[]{245.5f,745f,866f})for(float planY=422;planY<=750;planY+=4)
                {
                    var sample=ConvergenceHallB1Setup.Plan(planX,planY,.7f);
                    foreach(var collider in surround.GetComponentsInChildren<Collider>())if(Vector3.Distance(collider.ClosestPoint(sample),sample)<.58f)throw new Exception("New wall blocks the corridor route");
                }
            }
            ConvergenceHallElevatorCabins.Validate(root);ConvergenceHallBluebellInfill.Validate(root);
            Debug.Log("FINAL_FINISHES_OK: floor labels hidden; both elevators have continuous side/header walls, recessed doors, visible call plates; measured cabins/core limits/corridor guard clearance preserved.");
        }
        [MenuItem("Tools/EEG Wheelchair/Remove Floor Text And Recess Elevator Doors")]
        public static void Apply()
        {ConvergenceHallCoreHalls.Apply();Validate(GameObject.Find("ConvergenceHallB1"));}
        public static void ApplyAndCapture()
        {
            Apply();
            int count=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length;
            Apply();
            if(count!=GameObject.Find("ConvergenceHallB1").GetComponentsInChildren<Transform>(true).Length)throw new Exception("Finish authoring duplicated objects");
            SessionState.SetBool("EEG.ConvergenceQA.FocusFinalFinishes",true);SessionState.SetBool("EEG.ConvergenceQA.CaptureOnly",true);ConvergenceHallB1Play.Begin();
        }
    }
}
