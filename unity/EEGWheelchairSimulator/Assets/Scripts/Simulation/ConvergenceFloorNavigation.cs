using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EEGWheelchairSimulator
{
    // Explicit inspection shortcuts. These do not simulate an elevator journey.
    public sealed class ConvergenceFloorNavigation : MonoBehaviour
    {
        [SerializeField] SimulationController simulation;
        [SerializeField] WheelchairMovement wheelchair;
        [SerializeField] CameraFollow follow;
        [SerializeField] Text caption;
        [SerializeField] Vector3 basementPosition,firstFloorPosition,secondFloorPosition,thirdFloorPosition;
        [SerializeField] float basementYaw,firstFloorYaw,secondFloorYaw,thirdFloorYaw;
        [SerializeField] bool hasSecondFloor,hasThirdFloor;
        public Vector3 FirstFloorPosition => firstFloorPosition;
        public Vector3 SecondFloorPosition => secondFloorPosition;
        public Vector3 ThirdFloorPosition => thirdFloorPosition;
        public void Configure(SimulationController s,WheelchairMovement w,CameraFollow c,Text label,Vector3 b,Vector3 f,float by,float fy)
        {simulation=s;wheelchair=w;follow=c;caption=label;basementPosition=b;firstFloorPosition=f;basementYaw=by;firstFloorYaw=fy;hasSecondFloor=false;hasThirdFloor=false;}
        public void ConfigureUpToSecondFloor(SimulationController s,WheelchairMovement w,CameraFollow c,Text label,
            Vector3 b,Vector3 f,Vector3 s2,float by,float fy,float sy)
        {
            Configure(s,w,c,label,b,f,by,fy);
            secondFloorPosition=s2;secondFloorYaw=sy;
            hasSecondFloor=true;hasThirdFloor=false;
        }
        public void ConfigureAllFloors(SimulationController s,WheelchairMovement w,CameraFollow c,Text label,
            Vector3 b,Vector3 f,Vector3 s2,Vector3 t3,float by,float fy,float sy,float ty)
        {
            ConfigureUpToSecondFloor(s,w,c,label,b,f,s2,by,fy,sy);
            thirdFloorPosition=t3;thirdFloorYaw=ty;
            hasThirdFloor=true;
        }
        public void GoToBasement()=>GoFloor(0);
        public void GoToFirstFloor()=>GoFloor(1);
        public void GoToSecondFloor(){if(hasSecondFloor)GoFloor(2);}
        public void GoToThirdFloor(){if(hasThirdFloor)GoFloor(3);}
        int CurrentFloorIndex()
        {
            if(!wheelchair)return 0;
            float y=wheelchair.transform.position.y;
            if(hasThirdFloor&&y>12f)return 3;
            if(hasSecondFloor&&y>7.5f)return 2;
            return y>3f?1:0;
        }
        void GoFloor(int floor)
        {
            if(!simulation||!wheelchair||!follow||simulation.MovementLocked)return;
            Vector3 destination=floor==3?thirdFloorPosition:floor==2?secondFloorPosition:floor==1?firstFloorPosition:basementPosition;
            float yaw=floor==3?thirdFloorYaw:floor==2?secondFloorYaw:floor==1?firstFloorYaw:basementYaw;
            var rotation=Quaternion.Euler(0,yaw,0);
            var guard=wheelchair.GetComponent<WheelchairCollisionGuard>();
            if(guard&&!guard.CanOccupy(destination,rotation))return;
            var free=follow.GetComponent<DemoFreeCamera>();
            if(free)free.ExitFreeView();
            simulation.StopSimulation();
            wheelchair.transform.SetPositionAndRotation(destination,rotation);
            Physics.SyncTransforms();follow.SnapToTarget();
        }
        void Update()
        {
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                int current=CurrentFloorIndex();
                int maxFloor=hasThirdFloor?3:hasSecondFloor?2:1;
                if(keyboard.pageUpKey.wasPressedThisFrame)GoFloor(Mathf.Min(maxFloor,current+1));
                if(keyboard.pageDownKey.wasPressedThisFrame)GoFloor(Mathf.Max(0,current-1));
            }
            if(caption&&wheelchair)
            {
                int current=CurrentFloorIndex();
                string label=current==3?"3F · 3층":current==2?"2F · 2층":current==1?"1F · 1층":"B1 · 지하 1층";
                caption.text=label+"\n층 바로 이동 · 이동 후 START";
            }
        }
    }
}
