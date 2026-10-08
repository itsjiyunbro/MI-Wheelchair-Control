using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EEGWheelchairSimulator
{
    public sealed class DoorInteractor : MonoBehaviour
    {
        [SerializeField] private Transform wheelchair;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private Button button;
        [SerializeField] private Text buttonLabel,prompt;
        private InteractiveDoor[] doors;
        private DemoFreeCamera freeCamera;
        public InteractiveDoor Selected {get;private set;}
        private void Awake(){doors=FindObjectsByType<InteractiveDoor>(FindObjectsSortMode.None);}
        private void OnEnable(){if(simulation)simulation.SimulationReset+=ResetDoors;}
        private void OnDisable(){if(simulation)simulation.SimulationReset-=ResetDoors;}
        public void Configure(Transform actor,SimulationController controller,Button control,Text label,Text hint)
        {wheelchair=actor;simulation=controller;button=control;buttonLabel=label;prompt=hint;}
        private void Update()
        {
            if(!freeCamera&&Camera.main)freeCamera=Camera.main.GetComponent<DemoFreeCamera>();
            RefreshSelection();
            if(Keyboard.current!=null&&(freeCamera&&freeCamera.IsFreeView?Keyboard.current.rKey.wasPressedThisFrame:Keyboard.current.eKey.wasPressedThisFrame))ToggleNearest();
        }
        public void RefreshSelection()
        {
            if(doors==null)doors=FindObjectsByType<InteractiveDoor>(FindObjectsSortMode.None);
            Selected=null;float best=float.PositiveInfinity;
            var actor=freeCamera&&freeCamera.IsFreeView?freeCamera.transform:wheelchair;
            if(actor)foreach(var door in doors)
            {
                if(!door||!door.isActiveAndEnabled||!door.InReach(actor))continue;
                var delta=door.InteractionPoint-actor.position;delta.y=0;
                float score=delta.magnitude+.30f*(1-Vector3.Dot(actor.forward,delta.normalized));
                if(score<best){Selected=door;best=score;}
            }
            if(button)button.interactable=Selected&&!Selected.Automatic;
            string key=freeCamera&&freeCamera.IsFreeView?"R":"E";
            if(buttonLabel)buttonLabel.text=!Selected?"문 열기 / 닫기 ("+key+")":Selected.Automatic?"자동문":Selected.WantsOpen?"문 닫기 ("+key+")":"문 열기 ("+key+")";
            if(prompt)prompt.text=!Selected?"문 가까이에서 "+key+" 또는 버튼":Selected.Label+(Selected.Blocked?" · 문 앞 공간을 비워주세요":Selected.Automatic?" · 접근하면 열립니다":"");
        }
        public void ToggleNearest(){if(Selected&&!Selected.Automatic)Selected.Toggle();}
        public void ResetDoors(){if(doors==null)doors=FindObjectsByType<InteractiveDoor>(FindObjectsSortMode.None);foreach(var d in doors)if(d)d.ResetDoor();RefreshSelection();}
    }
}
