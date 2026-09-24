using UnityEngine;

namespace EEGWheelchairSimulator
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Tooltip("Offset in metres relative to the target's heading; scale is ignored.")]
        private Vector3 offset = new Vector3(0f, 4.5f, -7f);
        [SerializeField, Min(0.01f), Tooltip("Position response rate per second. Higher follows faster.")]
        private float positionSmoothing = 8f;
        [SerializeField, Min(0.01f), Tooltip("Rotation response rate per second. Higher follows faster.")]
        private float rotationSmoothing = 12f;
        [SerializeField, Min(0f), Tooltip("World-space height above the target's centre to look at.")]
        private float lookTargetHeight = 0.5f;

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            Follow(Time.deltaTime);
        }

        // Used at startup and by the optional scene authoring utility.
        public void SnapToTarget()
        {
            if (target == null || target == transform)
                return;
            transform.position = DesiredPosition();
            Vector3 direction = LookPosition() - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void Follow(float deltaTime)
        {
            if (target == null || target == transform || deltaTime <= 0f)
                return;

            // Exponential smoothing keeps the response consistent across frame rates.
            float positionBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, positionSmoothing) * deltaTime);
            float rotationBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationSmoothing) * deltaTime);
            transform.position = Vector3.Lerp(transform.position, DesiredPosition(), positionBlend);

            Vector3 direction = LookPosition() - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
            }
        }

        private Vector3 DesiredPosition()
        {
            // Rotate the offset with heading only. A scaled Cube must not stretch camera distance.
            Quaternion heading = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            return target.position + heading * offset;
        }

        private Vector3 LookPosition()
        {
            return target.position + Vector3.up * lookTargetHeight;
        }
    }
}
