using UnityEngine;

namespace EEGWheelchairSimulator
{
    [DisallowMultipleComponent]
    public sealed class WheelchairMovement : MonoBehaviour
    {
        [SerializeField] private SimulationController simulation;

        [SerializeField, Min(0f), Tooltip("Forward speed in metres per second.")]
        private float moveSpeed = 2f;

        [SerializeField, Min(0f), Tooltip("Yaw speed in degrees per second.")]
        private float turnSpeed = 60f;

        private int lastInputFrame = -1;
        private bool appliedForward;
        private float appliedTurn;

        // Read in LateUpdate, after the active input source has called ApplyInput.
        // A frame with no applied input reports stopped rather than retaining stale state.
        private bool HasCurrentInput => isActiveAndEnabled && simulation != null && simulation.IsRunning
            && lastInputFrame == Time.frameCount;
        public bool IsMoving => HasCurrentInput && appliedForward;
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
            appliedForward = false;
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
        {
            // Reject the inactive source without erasing this frame's active-source state.
            if (source != ControlSource) return;
            lastInputFrame = Time.frameCount;
            appliedForward = false;
            appliedTurn = 0f;
            // All input sources share this gate, including a future Python adapter.
            if (!isActiveAndEnabled || simulation == null || !simulation.IsRunning || deltaTime <= 0f)
                return;

            appliedForward = moveForward && moveSpeed > 0f;
            appliedTurn = turnSpeed > 0f ? Mathf.Clamp(turnInput, -1f, 1f) : 0f;

            float yaw = transform.eulerAngles.y
                + Mathf.Clamp(turnInput, -1f, 1f) * Mathf.Max(0f, turnSpeed) * deltaTime;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (!moveForward)
                return;

            Vector3 position = transform.position;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 nextPosition = position + forward * (Mathf.Max(0f, moveSpeed) * deltaTime);
            nextPosition.y = position.y;
            transform.position = nextPosition;
        }
    }
}
