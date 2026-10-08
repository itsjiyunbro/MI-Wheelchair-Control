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
        public bool IsFreeView {get;private set;}
        public float MoveSpeed=>moveSpeed;
        private void Awake(){follow=GetComponent<CameraFollow>();RefreshUI();}
        public void Configure(SimulationController controller,Canvas canvas,Text label,Text hint)
        {simulation=controller;statusCanvas=canvas;buttonLabel=label;instructions=hint;follow=GetComponent<CameraFollow>();RefreshUI();}
        public void ToggleFreeView(){if(IsFreeView)ExitFreeView();else EnterFreeView();}
        public void EnterFreeView()
        {
            if(IsFreeView||!isActiveAndEnabled)return;
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
            ReleaseLook();IsFreeView=false;
            if(statusCanvas)statusCanvas.enabled=canvasWasEnabled;
            if(statusRaycaster)statusRaycaster.enabled=raycasterWasEnabled;
            if(follow){follow.enabled=wasFollowing;if(wasFollowing)follow.SnapToTarget();}
            RefreshUI();
        }
        public void ToggleHUD(){if(IsFreeView&&statusCanvas){statusCanvas.enabled=!statusCanvas.enabled;if(statusRaycaster)statusRaycaster.enabled=statusCanvas.enabled&&raycasterWasEnabled;}}
        private void RefreshUI()
        {
            if(buttonLabel)buttonLabel.text=IsFreeView?"자유 시점 → 휠체어 시점 (F)":"시연용 자유 시점 (F)";
            if(instructions)instructions.text=IsFreeView?"WASD 이동 · Q/E 높이 · 우클릭 회전\nShift 빠르게 · 휠 속도: "+moveSpeed.ToString("0.0")+"m/s\nR 문 조작 · H 화면 숨기기 · F 복귀":"주행 W/S · A/D 회전 · F 자유 시점";
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
            Move(axis,keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed,Time.unscaledDeltaTime);
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
