using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EEGWheelchairSimulator
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraFollow))]
    public sealed class DemoFreeCamera : MonoBehaviour
    {
        [SerializeField] private SimulationController simulation;
        [SerializeField] private Canvas statusCanvas;
        [SerializeField] private Text buttonLabel,instructions;
        [SerializeField,Min(.1f)] private float moveSpeed=2f;
        [SerializeField,Min(.01f)] private float lookSensitivity=.12f;
        private CameraFollow follow;
        private bool wasFollowing,canvasWasEnabled,raycasterWasEnabled,looking;
        private GraphicRaycaster statusRaycaster;
        private CursorLockMode cursorLock;
        private bool cursorVisible;
        private float yaw,pitch;
        private StairWalkRoute stairRoute;
        private float stairDistance;
        private bool joiningStair;
        public bool IsFreeView {get;private set;}
        public float MoveSpeed=>moveSpeed;
        private void Awake(){follow=GetComponent<CameraFollow>();RefreshUI();}
        public void Configure(SimulationController controller,Canvas canvas,Text label,Text hint)
        {simulation=controller;statusCanvas=canvas;buttonLabel=label;instructions=hint;follow=GetComponent<CameraFollow>();RefreshUI();}
        public void ToggleFreeView(){if(IsFreeView)ExitFreeView();else EnterFreeView();}
        public void EnterFreeView()
        {
            if(IsFreeView||!isActiveAndEnabled||(simulation&&simulation.MovementLocked))return;
            if(!follow)follow=GetComponent<CameraFollow>();
            if(simulation)simulation.StopSimulation();
            wasFollowing=follow.enabled;canvasWasEnabled=statusCanvas&&statusCanvas.enabled;
            statusRaycaster=statusCanvas?statusCanvas.GetComponent<GraphicRaycaster>():null;raycasterWasEnabled=statusRaycaster&&statusRaycaster.enabled;
            yaw=transform.eulerAngles.y;pitch=Mathf.Clamp(Mathf.DeltaAngle(0,transform.eulerAngles.x),-85,85);
            IsFreeView=true;follow.enabled=false;transform.rotation=Quaternion.Euler(pitch,yaw,0);RefreshUI();
        }
        public void ExitFreeView()
        {
            if(!IsFreeView)return;
            ReleaseLook();IsFreeView=false;stairRoute=null;
            if(statusCanvas)statusCanvas.enabled=canvasWasEnabled;
            if(statusRaycaster)statusRaycaster.enabled=raycasterWasEnabled;
            if(follow){follow.enabled=wasFollowing;if(wasFollowing)follow.SnapToTarget();}
            RefreshUI();
        }
        public void ToggleHUD(){if(IsFreeView&&statusCanvas){statusCanvas.enabled=!statusCanvas.enabled;if(statusRaycaster)statusRaycaster.enabled=statusCanvas.enabled&&raycasterWasEnabled;}}
        private void RefreshUI()
        {
            if(buttonLabel)buttonLabel.text=IsFreeView?"자유 시점 → 휠체어 시점 (F)":"시연용 자유 시점 (F)";
            if(instructions)instructions.text=IsFreeView
                ?stairRoute?"W/S 계단 걷기 · 우클릭 시선\nG 자유 이동 · F 휠체어 시점"
                :"WASD 이동 · Q/E 높이 · 우클릭 회전\nShift 빠르게 · 휠 속도: "+moveSpeed.ToString("0.0")+"m/s\nG 계단 걷기 · R 문 · H 화면 숨기기 · F 복귀"
                :"주행 W/S · A/D 회전 · F 자유 시점";
        }
        private void Update()
        {
            var keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.fKey.wasPressedThisFrame)ToggleFreeView();
            if(!IsFreeView)return;
            if(keyboard!=null)
            {
                if(keyboard.escapeKey.wasPressedThisFrame){ExitFreeView();return;}
                if(keyboard.vKey.wasPressedThisFrame){follow.ToggleViewMode();return;}
                if(keyboard.hKey.wasPressedThisFrame)ToggleHUD();
                if(keyboard.gKey.wasPressedThisFrame)ToggleStairWalk();
            }
            var mouse=Mouse.current;
            if(mouse==null){ReleaseLook();return;}
            bool overUI=(!statusCanvas||statusCanvas.enabled)&&EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
            if(mouse.rightButton.isPressed)
            {
                if(!looking&&!overUI){cursorLock=Cursor.lockState;cursorVisible=Cursor.visible;looking=true;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
                else if(looking)Look(mouse.delta.ReadValue());
            }
            else ReleaseLook();
            if(!overUI&&Mathf.Abs(mouse.scroll.ReadValue().y)>.01f)AdjustSpeed(mouse.scroll.ReadValue().y);
        }
        private void LateUpdate()
        {
            if(!IsFreeView)return;
            var keyboard=Keyboard.current;if(keyboard==null)return;
            var axis=new Vector3((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),(keyboard.eKey.isPressed?1:0)-(keyboard.qKey.isPressed?1:0),(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
            if(stairRoute){WalkStairs(axis.z,Time.unscaledDeltaTime);return;}
            Move(axis,keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed,Time.unscaledDeltaTime);
        }
        private void ToggleStairWalk()
        {
            if(stairRoute){stairRoute=null;RefreshUI();return;}
            float best=1.55f;
            foreach(var route in FindObjectsByType<StairWalkRoute>(FindObjectsSortMode.None))
            {
                foreach(float distance in new[]{0f,route.Length})
                {
                    var eye=route.PositionAt(distance)+Vector3.up*StairWalkRoute.EyeHeight;
                    if(Mathf.Abs(eye.y-transform.position.y)>.95f)continue;
                    float gap=Vector3.Distance(eye,transform.position);
                    if(gap>=best)continue;
                    best=gap;
                    stairRoute=route;
                    stairDistance=distance;
                }
            }
            if(!stairRoute)return;
            stairRoute.OpenUpperDoor();
            joiningStair=true;
            RefreshUI();
        }
        private void WalkStairs(float direction,float dt)
        {
            if(joiningStair)
            {
                var start=stairRoute.PositionAt(stairDistance)+Vector3.up*StairWalkRoute.EyeHeight;
                transform.position=Vector3.MoveTowards(transform.position,start,2.2f*dt);
                joiningStair=Vector3.Distance(transform.position,start)>.001f;
                return;
            }
            if(direction==0)return;
            var previous=transform.position;
            float length=stairRoute.Length;
            float next=stairDistance+direction*1.65f*dt;
            stairDistance=Mathf.Clamp(next,0,length);
            transform.position=stairRoute.PositionAt(stairDistance)+Vector3.up*StairWalkRoute.EyeHeight;
            if(!looking)
            {
                var travel=transform.position-previous;
                travel.y=0;
                if(travel.sqrMagnitude>.000001f)
                {
                    yaw=Mathf.MoveTowardsAngle(yaw,Quaternion.LookRotation(travel).eulerAngles.y,200*dt);
                    transform.rotation=Quaternion.Euler(pitch,yaw,0);
                }
            }
            if(next<0||next>length){stairRoute=null;RefreshUI();}
        }
        private void Move(Vector3 axis,bool fast,float deltaTime)
        {
            var direction=transform.right*axis.x+Vector3.up*axis.y+transform.forward*axis.z;
            if(direction.sqrMagnitude>1)direction.Normalize();
            transform.position+=direction*moveSpeed*(fast?3f:1f)*Mathf.Max(0,deltaTime);
        }
        private void Look(Vector2 delta)
        {yaw+=delta.x*lookSensitivity;pitch=Mathf.Clamp(pitch-delta.y*lookSensitivity,-85,85);transform.rotation=Quaternion.Euler(pitch,yaw,0);}
        private void AdjustSpeed(float scroll)
        {moveSpeed=Mathf.Clamp(moveSpeed*Mathf.Pow(1.25f,scroll/120f),.25f,12f);RefreshUI();}
        private void ReleaseLook()
        {if(!looking)return;looking=false;Cursor.lockState=cursorLock;Cursor.visible=cursorVisible;}
        private void OnApplicationFocus(bool focused){if(!focused)ReleaseLook();}
        private void OnDisable()=>ExitFreeView();
        private void OnDestroy()=>ReleaseLook();
    }
}
