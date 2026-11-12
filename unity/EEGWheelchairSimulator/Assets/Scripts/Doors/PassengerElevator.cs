using System;
using UnityEngine;
using UnityEngine.UI;

namespace EEGWheelchairSimulator
{
    // Two fixed, enclosed cabins represent the stops. Transfer happens behind closed
    // doors under a short fade; this is a passenger journey, not a physics shaft.
    public sealed class PassengerElevator : MonoBehaviour
    {
        [Serializable] public class Stop
        {
            public Transform cabin;
            public InteractiveDoor door;
            public Text indicator;
        }
        public enum Phase { Idle, Closing, Travelling, Opening }
        [SerializeField] Stop[] stops;
        [SerializeField] SimulationController simulation;
        [SerializeField] WheelchairMovement chair;
        [SerializeField] CameraFollow follow;
        [SerializeField] GameObject panel;
        [SerializeField] Text status;
        [SerializeField] Button basementButton,firstButton,openButton,closeButton;
        [SerializeField] Image curtain;
        [SerializeField] string liftName;
        WheelchairCollisionGuard guard;
        int source,destination,near=-1;
        float elapsed;
        bool transferred;
        Vector3 arrival;
        Quaternion arrivalRotation;
        string notice;
        float noticeUntil;
        public Phase State {get;private set;}
        public bool Busy=>State!=Phase.Idle;
        public Stop[] Stops=>stops;
        public void Configure(string name,Stop[] stations,SimulationController sim,WheelchairMovement actor,CameraFollow camera,
            GameObject controls,Text label,Button b,Button f,Button o,Button c,Image fade)
        {liftName=name;stops=stations;simulation=sim;chair=actor;follow=camera;panel=controls;status=label;basementButton=b;firstButton=f;openButton=o;closeButton=c;curtain=fade;}
        void Awake(){guard=chair.GetComponent<WheelchairCollisionGuard>();simulation.SimulationReset+=ResetJourney;}
        void OnDestroy(){if(simulation)simulation.SimulationReset-=ResetJourney;}
        void OnDisable(){if(Busy)Release();if(panel)panel.SetActive(false);}
        public Vector3 BoardingPosition(int floor)=>stops[floor].cabin.TransformPoint(new Vector3(0,.5f,.77f));
        public bool FullyInside(int floor)
        {
            var cabin=stops[floor].cabin;
            var local=cabin.InverseTransformPoint(chair.transform.position);
            var e=guard.FootprintHalfExtents;
            float x=e.x*Mathf.Abs(Vector3.Dot(chair.transform.right,cabin.right))+e.z*Mathf.Abs(Vector3.Dot(chair.transform.forward,cabin.right));
            float z=e.x*Mathf.Abs(Vector3.Dot(chair.transform.right,cabin.forward))+e.z*Mathf.Abs(Vector3.Dot(chair.transform.forward,cabin.forward));
            return Mathf.Abs(local.y-.5f)<.25f&&Mathf.Abs(local.x)+x<.79f&&local.z-z>.06f&&local.z+z<1.43f;
        }
        int Nearby()
        {
            if(!chair)return -1;
            for(int i=0;i<stops.Length;i++)
            {
                var p=stops[i].cabin.InverseTransformPoint(chair.transform.position);
                if(Mathf.Abs(p.y-.5f)<.7f&&Mathf.Abs(p.x)<1.3f&&p.z> -1.9f&&p.z<1.5f)return i;
            }
            return -1;
        }
        void Tell(string message){notice=message;noticeUntil=Time.unscaledTime+3;}
        public void GoToBasement()=>RequestFloor(0);
        public void GoToFirstFloor()=>RequestFloor(1);
        public void OpenDoors(){int i=Nearby();if(!Busy&&!simulation.MovementLocked&&i>=0)stops[i].door.SetOpen(true);}
        public void CloseDoors(){int i=Nearby();if(!Busy&&!simulation.MovementLocked&&i>=0)stops[i].door.SetOpen(false);}
        public bool RequestFloor(int floor)
        {
            if(floor<0||floor>=stops.Length||Busy||simulation.MovementLocked)return false;
            int current=Nearby();
            if(current<0||!FullyInside(current)){Tell("휠체어 전체가 안쪽에 들어온 뒤 층을 선택하세요.");return false;}
            if(current==floor){stops[current].door.SetOpen(true);return true;}
            var free=follow.GetComponent<DemoFreeCamera>();if(free&&free.IsFreeView){Tell("휠체어 시점으로 돌아온 뒤 이용하세요.");return false;}
            // Centre and face the exit behind the transit curtain, before opening.
            // A narrow car must not require the passenger to turn around or reverse.
            arrival=BoardingPosition(floor);
            arrivalRotation=Quaternion.LookRotation(-stops[floor].cabin.forward,Vector3.up);
            if(!guard.CanOccupy(arrival,arrivalRotation)){Tell("도착 층 공간이 막혀 있습니다.");return false;}
            if(!simulation.TryLockMovement(this))return false;
            source=current;destination=floor;transferred=false;elapsed=0;State=Phase.Closing;
            foreach(var s in stops){s.door.enabled=false;s.door.SetOpen(false);}
            return true;
        }
        void Update(){Step(Time.unscaledDeltaTime);RefreshControls();}
        // Public stepping keeps door obstruction and timing checks deterministic.
        public void Step(float dt)
        {
            if(!Busy||dt<=0)return;
            elapsed+=dt;
            if(State==Phase.Closing)
            {
                foreach(var s in stops)s.door.Step(dt);
                bool blocked=false,closed=true;foreach(var s in stops){blocked|=s.door.Blocked;closed&=s.door.Progress<.001f;}
                if(blocked||!FullyInside(source)||elapsed>5){Tell("출입구를 확인하세요. 문을 다시 엽니다.");destination=source;State=Phase.Opening;elapsed=0;stops[source].door.SetOpen(true);return;}
                if(closed){State=Phase.Travelling;elapsed=0;}
            }
            else if(State==Phase.Travelling)
            {
                float alpha=Mathf.Clamp01(elapsed/.35f);SetFade(alpha);
                if(elapsed>=1.6f&&!transferred)
                {
                    if(!guard.CanOccupy(arrival,arrivalRotation)){destination=source;Tell("도착 층이 막혀 출발 층으로 돌아왔습니다.");}
                    else {chair.transform.SetPositionAndRotation(arrival,arrivalRotation);Physics.SyncTransforms();follow.SnapToTarget();}
                    transferred=true;
                }
                if(elapsed>=2.5f){SetFade(0);State=Phase.Opening;elapsed=0;stops[destination].door.SetOpen(true);}
            }
            else if(State==Phase.Opening)
            {
                stops[destination].door.Step(dt);
                if(stops[destination].door.Progress>.999f){Tell("도착했습니다. START를 눌러 하차하세요.");Release();}
                else if(elapsed>5){Tell("문 앞을 확인하고 문 열기를 눌러주세요.");Release();}
            }
        }
        void SetFade(float alpha){if(curtain){curtain.color=new Color(0,0,0,alpha);curtain.gameObject.SetActive(alpha>0);}}
        void Release(){foreach(var s in stops)s.door.enabled=true;SetFade(0);State=Phase.Idle;simulation.UnlockMovement(this);}
        void ResetJourney(){foreach(var s in stops)s.door.ResetDoor();Release();notice=null;noticeUntil=0;}
        void RefreshControls()
        {
            near=Nearby();var free=follow.GetComponent<DemoFreeCamera>();bool show=Busy||(near>=0&&!(free&&free.IsFreeView));panel.SetActive(show);if(!show)return;
            bool inside=near>=0&&FullyInside(near);bool usable=!Busy&&!simulation.MovementLocked;
            basementButton.interactable=usable&&inside;firstButton.interactable=usable&&inside;openButton.interactable=closeButton.interactable=usable;
            string floor=near==1?"1F":"B1";
            string message=Busy?(State==Phase.Closing?"문이 닫힙니다":State==Phase.Travelling?"층 이동 중": "문이 열립니다"):
                Time.unscaledTime<noticeUntil?notice:inside?"이동할 층을 선택하세요":"문 열기 → 탑승 → 층 선택";
            status.text=liftName+" · "+floor+"\n"+message;
            foreach(var s in stops)if(s.indicator)s.indicator.text=Busy?(destination==1?"↑ 1":"↓ B1"):(s==stops[1]?"1":"B1");
        }
    }
}
