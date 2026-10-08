using UnityEngine;

namespace EEGWheelchairSimulator
{
    [DisallowMultipleComponent]
    public sealed class WheelchairMovement : MonoBehaviour
    {
        [SerializeField] private SimulationController simulation;
        [SerializeField] private WheelchairCollisionGuard collisionGuard;

        [SerializeField, Min(0f), Tooltip("Forward speed in metres per second.")]
        private float moveSpeed = 2f;
        [SerializeField, Min(0f), Tooltip("Reverse speed in metres per second.")]
        private float reverseSpeed = 1.2f;

        [SerializeField, Min(0f), Tooltip("Yaw speed in degrees per second.")]
        private float turnSpeed = 60f;

        private int lastInputFrame = -1;
        private float appliedMotion;
        private float appliedTurn;

        // Read in LateUpdate, after the active input source has called ApplyInput.
        // A frame with no applied input reports stopped rather than retaining stale state.
        private bool HasCurrentInput => isActiveAndEnabled && simulation != null && simulation.IsRunning
            && lastInputFrame == Time.frameCount;
        public bool IsMoving => MoveDirection != 0;
        public int MoveDirection => HasCurrentInput ? (appliedMotion>0?1:appliedMotion<0?-1:0) : 0;
        public float TurnInput => HasCurrentInput ? appliedTurn : 0f;
        public float HeadingDegrees => Mathf.Repeat(transform.eulerAngles.y, 360f);
        public WheelchairControlSource ControlSource => simulation != null ? simulation.ControlSource : WheelchairControlSource.Keyboard;

        private void OnDisable()
        {
            ClearInputState();
        }

        public void ClearInputState()
        {
            lastInputFrame = -1;
            appliedMotion = 0f;
            appliedTurn = 0f;
        }

        /// <summary>
        /// Apply one frame of input. Forward and turning can be combined.
        /// turnInput: -1 = left, 0 = straight, +1 = right.
        /// The active input source calls this once per frame with Time.deltaTime.
        /// No command is retained, so an inactive input source cannot leave motion running.
        /// </summary>
        public void ApplyInput(bool moveForward, float turnInput, float deltaTime,
            WheelchairControlSource source = WheelchairControlSource.Keyboard)
            => ApplyDirectionalInput(moveForward?1f:0f,turnInput,deltaTime,source);

        // Negative motion reverses along the heading while preserving steering and source gates.
        public void ApplyDirectionalInput(float motionInput,float turnInput,float deltaTime,
            WheelchairControlSource source=WheelchairControlSource.Keyboard)
        {
            // Reject the inactive source without erasing this frame's active-source state.
            if (source != ControlSource) return;
            lastInputFrame = Time.frameCount;
            appliedMotion = 0f;
            appliedTurn = 0f;
            // All input sources share this gate, including a future Python adapter.
            if (!isActiveAndEnabled || simulation == null || !simulation.IsRunning || deltaTime <= 0f)
                return;

            appliedTurn = turnSpeed > 0f ? Mathf.Clamp(turnInput, -1f, 1f) : 0f;

            float deltaYaw=Mathf.Clamp(turnInput,-1f,1f)*Mathf.Max(0f,turnSpeed)*deltaTime;
            if(collisionGuard!=null&&collisionGuard.isActiveAndEnabled)
            {
                float allowed=collisionGuard.LimitYaw(transform.position,transform.eulerAngles.y,deltaYaw);
                if(Mathf.Abs(deltaYaw)>.00001f)appliedTurn*=allowed/deltaYaw;
                deltaYaw=allowed;
            }
            float yaw = transform.eulerAngles.y + deltaYaw;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            float motion=Mathf.Clamp(motionInput,-1f,1f);
            if (Mathf.Abs(motion)<.0001f)
                return;

            Vector3 position = transform.position;
            Vector3 direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized*Mathf.Sign(motion);
            float distance = Mathf.Abs(motion)*Mathf.Max(0f,motion>0?moveSpeed:reverseSpeed)*deltaTime;
            if (collisionGuard != null && collisionGuard.isActiveAndEnabled)
                distance = collisionGuard.LimitForwardDistance(position, direction, distance);
            Vector3 nextPosition = position + direction * distance;
            nextPosition.y = position.y;
            transform.position = nextPosition;
            // HUD reports actual translation, independently of a received Python prediction.
            appliedMotion = (nextPosition-position).sqrMagnitude>0.000000000001f?motion:0;
        }
    }
}
