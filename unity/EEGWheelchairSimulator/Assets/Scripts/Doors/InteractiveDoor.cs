using System;
using UnityEngine;

namespace EEGWheelchairSimulator
{
    [DisallowMultipleComponent]
    public sealed class InteractiveDoor : MonoBehaviour
    {
        [Serializable]
        public struct Motion
        {
            public Transform target;
            public Vector3 closedPosition,hinge,slide;
            public Quaternion closedRotation;
            public float angle;
        }
        [SerializeField] private string doorLabel;
        [SerializeField] private Motion[] motions=Array.Empty<Motion>();
        [SerializeField] private BoxCollider[] leafColliders=Array.Empty<BoxCollider>();
        [SerializeField] private Collider[] closedBarriers=Array.Empty<Collider>();
        [SerializeField] private Vector3 interactionPoint;
        [SerializeField] private bool automatic;
        [SerializeField] private float initialProgress;
        [SerializeField] private float duration=.85f;
        [SerializeField] private float sensorHalfWidth=.75f;
        private WheelchairCollisionGuard actor;
        private float progress,targetProgress;
        public string Label=>doorLabel;
        public bool Automatic=>automatic;
        public float Progress=>progress;
        public bool WantsOpen=>targetProgress>.5f;
        public bool Blocked {get;private set;}
        public Vector3 InteractionPoint=>transform.TransformPoint(interactionPoint);
        public Motion[] Motions=>motions;
        public BoxCollider[] LeafColliders=>leafColliders;
        public void Configure(string label,Motion[] parts,BoxCollider[] colliders,Vector3 point,bool auto=false,float initial=0,Collider[] barriers=null,float sensorWidth=.75f)
        {doorLabel=label;motions=parts;leafColliders=colliders;interactionPoint=point;automatic=auto;initialProgress=initial;closedBarriers=barriers??Array.Empty<Collider>();sensorHalfWidth=sensorWidth;}
        private void Awake(){actor=FindFirstObjectByType<WheelchairCollisionGuard>();ResetDoor();}
        private void Update(){Step(Time.unscaledDeltaTime);}
        public void SetOpen(bool open){targetProgress=open?1:0;Blocked=false;}
        public void Toggle()=>SetOpen(!WantsOpen);
        public void ResetDoor(){targetProgress=initialProgress;Apply(initialProgress);Blocked=false;}
        public void SetProgressInstant(float value){targetProgress=Mathf.Clamp01(value);Apply(targetProgress);Blocked=false;}
        private void Apply(float value)
        {
            progress=Mathf.Clamp01(value);
            foreach(var m in motions)
            {
                if(!m.target)continue;
                var rotation=Quaternion.Euler(0,m.angle*progress,0);
                m.target.localPosition=m.hinge+rotation*(m.closedPosition-m.hinge)+m.slide*progress;
                m.target.localRotation=rotation*m.closedRotation;
            }
            foreach(var c in closedBarriers)if(c)c.enabled=progress<.0001f;
            Physics.SyncTransforms();
        }
        public void Step(float delta)
        {
            if(!actor)actor=FindFirstObjectByType<WheelchairCollisionGuard>();
            if(automatic&&actor)
            {
                var point=transform.InverseTransformPoint(actor.transform.position)-interactionPoint;
                if(Mathf.Abs(point.x)<sensorHalfWidth&&Mathf.Abs(point.z)<2.05f)SetOpen(true);
                else if(Mathf.Abs(point.z)>2.8f||Mathf.Abs(point.x)>sensorHalfWidth+.7f)SetOpen(false);
            }
            if(delta<=0||Mathf.Abs(progress-targetProgress)<.0001f)return;
            float next=Mathf.MoveTowards(progress,targetProgress,delta/Mathf.Max(.1f,duration));
            float biggest=0;foreach(var m in motions)biggest=Mathf.Max(biggest,Mathf.Abs(m.angle));
            int count=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(next-progress)*Mathf.Max(biggest,90)/2));
            float start=progress,accepted=progress;Blocked=false;
            for(int i=1;i<=count;i++)
            {
                Apply(Mathf.Lerp(start,next,i/(float)count));
                bool touches=false;if(actor)foreach(var c in leafColliders)if(actor.OverlapsDoor(c)){touches=true;break;}
                if(touches){Apply(accepted);Blocked=true;break;}
                accepted=progress;
            }
        }
        public bool InReach(Transform wheelchair,float maximum=2.15f)
        {
            var delta=InteractionPoint-wheelchair.position;delta.y=0;if(delta.magnitude>maximum)return false;
            var origin=wheelchair.position+Vector3.up*.55f;var aim=InteractionPoint;aim.y=origin.y;
            foreach(var hit in Physics.RaycastAll(origin,(aim-origin).normalized,Vector3.Distance(aim,origin)-.015f,LayerMask.GetMask("WheelchairObstacle"),QueryTriggerInteraction.Ignore))
            {
                bool own=false;foreach(var c in leafColliders)if(hit.collider==c){own=true;break;}
                if(!own)foreach(var c in closedBarriers)if(hit.collider==c){own=true;break;}
                if(!own)return false;
            }
            return true;
        }
    }
}
