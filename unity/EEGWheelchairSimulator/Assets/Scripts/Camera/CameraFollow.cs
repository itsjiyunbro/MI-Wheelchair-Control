using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EEGWheelchairSimulator
{
    public enum WheelchairViewMode { ThirdPerson, FirstPerson }
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
        [SerializeField] private WheelchairViewMode viewMode = WheelchairViewMode.ThirdPerson;
        [SerializeField, Tooltip("Eye position relative to the target's heading, in metres.")]
        private Vector3 firstPersonOffset = new Vector3(0f, .7f, .15f);
        [SerializeField] private bool allowViewToggle = true;
        [SerializeField] private bool avoidObstacles;
        [SerializeField] private LayerMask cameraObstacleMask;
        [SerializeField, Min(.01f)] private float cameraRadius = .14f;
        [SerializeField] private Text viewButtonLabel;
        private Renderer[] hiddenRenderers;
        private bool[] originalRenderingOff;
        public WheelchairViewMode ViewMode => viewMode;

        private void Update()
        {
            if (allowViewToggle && Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
                ToggleViewMode();
        }
        public void ToggleViewMode() => SetViewMode(viewMode == WheelchairViewMode.ThirdPerson ? WheelchairViewMode.FirstPerson : WheelchairViewMode.ThirdPerson);
        public void SetViewMode(WheelchairViewMode mode)
        {
            var free=GetComponent<DemoFreeCamera>();if(free&&free.IsFreeView)free.ExitFreeView();
            viewMode = mode;
            SnapToTarget();
        }
        private void UpdateVisualState()
        {
            if (viewMode == WheelchairViewMode.FirstPerson && target != null && hiddenRenderers == null)
            {
                var visual = target.Find("WheelchairVisual");
                hiddenRenderers = (visual != null ? visual : target).GetComponentsInChildren<Renderer>(true);
                originalRenderingOff = new bool[hiddenRenderers.Length];
                for (int i = 0; i < hiddenRenderers.Length; i++)
                { originalRenderingOff[i] = hiddenRenderers[i].forceRenderingOff; hiddenRenderers[i].forceRenderingOff = true; }
            }
            else if (viewMode == WheelchairViewMode.ThirdPerson) RestoreVisuals();
            if (viewButtonLabel != null) viewButtonLabel.text = viewMode == WheelchairViewMode.ThirdPerson ? "3인칭 → 1인칭 (V)" : "1인칭 → 3인칭 (V)";
        }
        private void RestoreVisuals()
        {
            if (hiddenRenderers != null) for (int i = 0; i < hiddenRenderers.Length; i++)
                if (hiddenRenderers[i] != null) hiddenRenderers[i].forceRenderingOff = originalRenderingOff[i];
            hiddenRenderers = null; originalRenderingOff = null;
        }
        private void OnDisable() => RestoreVisuals();
        private void OnDestroy() => RestoreVisuals();

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
            UpdateVisualState();
            if (target == null || target == transform)
                return;
            transform.position = SafePosition(DesiredPosition());
            Vector3 direction = LookPosition() - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void Follow(float deltaTime)
        {
            if (target == null || target == transform || deltaTime <= 0f)
                return;
            UpdateVisualState();
            if (viewMode == WheelchairViewMode.FirstPerson)
            {
                // Attach the eye to the chair without positional/rotational lag while turning.
                transform.position = DesiredPosition();
                transform.rotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
                return;
            }

            // Exponential smoothing keeps the response consistent across frame rates.
            float positionBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, positionSmoothing) * deltaTime);
            float rotationBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, rotationSmoothing) * deltaTime);
            transform.position = SafePosition(Vector3.Lerp(transform.position, SafePosition(DesiredPosition()), positionBlend));

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
            return target.position + heading * (viewMode == WheelchairViewMode.FirstPerson ? firstPersonOffset : offset);
        }

        private Vector3 LookPosition()
        {
            if (viewMode == WheelchairViewMode.FirstPerson)
                return DesiredPosition() + Quaternion.Euler(0f, target.eulerAngles.y, 0f) * Vector3.forward * 5f;
            return target.position + Vector3.up * lookTargetHeight;
        }
        private Vector3 SafePosition(Vector3 desired)
        {
            if (!avoidObstacles || viewMode == WheelchairViewMode.FirstPerson || cameraObstacleMask.value == 0) return desired;
            Vector3 origin = LookPosition(); Vector3 delta = desired - origin; float distance = delta.magnitude;
            if (distance < .001f) return desired;
            if (Physics.SphereCast(origin, cameraRadius, delta / distance, out var hit, distance, cameraObstacleMask, QueryTriggerInteraction.Ignore))
                return origin + delta / distance * Mathf.Max(0f, hit.distance - .025f);
            return desired;
        }
    }
}
