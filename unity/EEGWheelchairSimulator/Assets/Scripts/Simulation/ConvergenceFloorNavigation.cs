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
        [SerializeField] Vector3 basementPosition,firstFloorPosition;
        [SerializeField] float basementYaw,firstFloorYaw;
        public Vector3 FirstFloorPosition => firstFloorPosition;
        public void Configure(SimulationController s,WheelchairMovement w,CameraFollow c,Text label,Vector3 b,Vector3 f,float by,float fy)
        {simulation=s;wheelchair=w;follow=c;caption=label;basementPosition=b;firstFloorPosition=f;basementYaw=by;firstFloorYaw=fy;}
        public void GoToBasement()=>Go(false);
        public void GoToFirstFloor()=>Go(true);
        void Go(bool upper)
        {
            if(!simulation||!wheelchair||!follow||simulation.MovementLocked)return;
            var destination=upper?firstFloorPosition:basementPosition;
            var rotation=Quaternion.Euler(0,upper?firstFloorYaw:basementYaw,0);
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
                if(keyboard.pageUpKey.wasPressedThisFrame)GoToFirstFloor();
                if(keyboard.pageDownKey.wasPressedThisFrame)GoToBasement();
            }
            if(caption&&wheelchair)caption.text=(wheelchair.transform.position.y>3?"1F · 1층":"B1 · 지하 1층")+"\n층 바로 이동 · 이동 후 START";
        }
    }
}
