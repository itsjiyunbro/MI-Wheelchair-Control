using UnityEngine;

namespace EEGWheelchairSimulator
{
    /// <summary>
    /// Query-only guard for the demo's tall, static walls/closed doors.
    /// A heading-independent clearance circle encloses the whole visual, including
    /// the footrest, so turning in place never sweeps a corner into a wall.
    /// This is not a Rigidbody controller, ground probe or automatic steering system.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelchairCollisionGuard : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField, Min(0.01f), Tooltip("World metres; encloses the visual for every yaw. Ignores root scale.")]
        private float clearanceRadius = 1.15f;
        [SerializeField, Tooltip("World height above the controller pivot. Floor is excluded by layer.")]
        private float queryHeight = 0.2f;
        [SerializeField, Min(0.001f), Tooltip("Small world-space gap left before a wall or closed door.")]
        private float skinWidth = 0.025f;
        [SerializeField] private bool useOrientedFootprint;
        [SerializeField] private Vector3 footprintHalfExtents = new Vector3(.34f,.58f,.46f);
        public Vector3 FootprintHalfExtents => useOrientedFootprint ? footprintHalfExtents : Vector3.one * clearanceRadius;
        public Vector3 QueryCentre => transform.position + Vector3.up * queryHeight;

        public bool CanOccupy(Vector3 position,Quaternion orientation)
        {
            if(!useOrientedFootprint||obstacleLayers.value==0)return true;
            Physics.SyncTransforms();
            return !Physics.CheckBox(position+Vector3.up*queryHeight,footprintHalfExtents,orientation,obstacleLayers,QueryTriggerInteraction.Ignore);
        }
        public float LimitYaw(Vector3 position,float start,float requested)
        {
            if(!useOrientedFootprint||Mathf.Abs(requested)<.00001f)return requested;
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(requested)/2));float accepted=0;
            for(int i=1;i<=steps;i++)
            {
                float angle=requested*i/steps;
                if(!CanOccupy(position,Quaternion.Euler(0,start+angle,0)))break;
                accepted=angle;
            }
            return accepted;
        }
        public bool OverlapsDoor(BoxCollider collider,float padding=.005f)
        {
            if(!collider||!collider.enabled||!collider.gameObject.activeInHierarchy)return false;
            var centre=collider.transform.TransformPoint(collider.center);
            var extent=Vector3.Scale(collider.size,collider.transform.lossyScale)*.5f;
            var actor=FootprintHalfExtents;
            if(Mathf.Abs(centre.y-QueryCentre.y)>extent.y+actor.y)return false;
            var ax=transform.right;var az=transform.forward;var bx=collider.transform.right;var bz=collider.transform.forward;
            var delta=centre-QueryCentre;delta.y=0;
            foreach(var axis in new[]{ax,az,bx,bz})
            {
                float a=actor.x*Mathf.Abs(Vector3.Dot(axis,ax))+actor.z*Mathf.Abs(Vector3.Dot(axis,az));
                float b=extent.x*Mathf.Abs(Vector3.Dot(axis,bx))+extent.z*Mathf.Abs(Vector3.Dot(axis,bz));
                if(Mathf.Abs(Vector3.Dot(delta,axis))>=a+b+padding)return false;
            }
            return true;
        }

        public float LimitForwardDistance(Vector3 position, Vector3 direction, float requestedDistance)
        {
            if (requestedDistance <= 0f) return 0f;
            if (obstacleLayers.value == 0) return requestedDistance;
            float radius = Mathf.Max(0.01f, clearanceRadius);
            float skin = Mathf.Max(0.001f, skinWidth);
            Vector3 centre = position + Vector3.up * queryHeight;
            // Project settings have autoSyncTransforms disabled. Include freshly enabled,
            // edited or repositioned static colliders without changing global physics settings.
            Physics.SyncTransforms();
            if(useOrientedFootprint)
            {
                var orientation=transform.rotation;
                if(Physics.CheckBox(centre,footprintHalfExtents,orientation,obstacleLayers,QueryTriggerInteraction.Ignore))return 0;
                if(Physics.BoxCast(centre,footprintHalfExtents,direction,out var boxHit,orientation,requestedDistance+skin,obstacleLayers,QueryTriggerInteraction.Ignore))return Mathf.Clamp(boxHit.distance-skin,0,requestedDistance);
                return requestedDistance;
            }
            // SphereCast alone does not report colliders already overlapping its origin.
            // Invalid manually placed/teleported starts fail closed; RESET restores a clear start.
            if (Physics.CheckSphere(centre, radius, obstacleLayers, QueryTriggerInteraction.Ignore))
                return 0f;
            if (Physics.SphereCast(centre, radius, direction, out RaycastHit hit,
                requestedDistance + skin, obstacleLayers, QueryTriggerInteraction.Ignore))
                return Mathf.Clamp(hit.distance - skin, 0f, requestedDistance);
            return requestedDistance;
        }

        private void OnDrawGizmosSelected()
        {
            // Only an Editor selection aid; no visible runtime ring or extra UI.
            Color previous = Gizmos.color;
            Gizmos.color = new Color(0.2f, 0.85f, 0.85f, 0.8f);
            Vector3 centre = transform.position + Vector3.up * queryHeight;
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48f, b = (i + 1) * Mathf.PI * 2f / 48f;
                Gizmos.DrawLine(centre + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * clearanceRadius,
                    centre + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * clearanceRadius);
            }
            Gizmos.color = previous;
        }
    }
}
