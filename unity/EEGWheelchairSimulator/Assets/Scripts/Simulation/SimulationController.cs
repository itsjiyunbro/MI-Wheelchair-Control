using UnityEngine;

namespace EEGWheelchairSimulator
{
    public enum WheelchairControlSource { Keyboard, Python }

    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class SimulationController : MonoBehaviour
    {
        [SerializeField] private WheelchairMovement movement;
        [SerializeField] private CameraFollow followCamera;
        [SerializeField] private WheelchairControlSource controlSource = WheelchairControlSource.Keyboard;
        private WheelchairControlSource observedSource;

        public WheelchairControlSource ControlSource => controlSource;
        // A small notification lets stateful input adapters discard cached/pending commands.
        public event System.Action InputStateCleared;
        public event System.Action SimulationReset;

        private bool running;
        private bool hasInitialPose;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private Object movementLock;
        public bool MovementLocked => movementLock;
        public bool IsRunning => isActiveAndEnabled && running && !MovementLocked;
        public bool TryLockMovement(Object owner)
        {
            if (!owner || (movementLock && movementLock != owner)) return false;
            movementLock = owner; StopSimulation(); return true;
        }
        public void UnlockMovement(Object owner)
        { if (movementLock == owner) { movementLock = null; StopSimulation(); } }

        private void Awake()
        {
            observedSource = controlSource;
            CaptureInitialPose();
            StopSimulation();
        }

        private void Update()
        {
            // Inspector changes are processed before input adapters' Update methods.
            if (observedSource != controlSource) ClearInputsForSourceChange();
        }

        public void SetControlSource(WheelchairControlSource source)
        {
            if (controlSource == source) return;
            controlSource = source;
            ClearInputsForSourceChange();
        }

        private void ClearInputsForSourceChange()
        {
            observedSource = controlSource;
            if (movement != null) movement.ClearInputState();
            InputStateCleared?.Invoke();
        }

        private void OnDisable()
        {
            StopSimulation();
        }

        private bool CaptureInitialPose()
        {
            if (movement == null)
                return false;
            if (!hasInitialPose)
            {
                initialPosition = movement.transform.position;
                initialRotation = Quaternion.Euler(0f, movement.transform.eulerAngles.y, 0f);
                hasInitialPose = true;
            }
            return true;
        }

        public void StartSimulation()
        {
            if (MovementLocked) return;
            var free=followCamera?followCamera.GetComponent<DemoFreeCamera>():null;if(free)free.ExitFreeView();
            if (!isActiveAndEnabled || !CaptureInitialPose())
                return;
            running = true;
            movement.ClearInputState();
            InputStateCleared?.Invoke();
        }

        public void StopSimulation()
        {
            running = false;
            if (movement != null)
                movement.ClearInputState();
            InputStateCleared?.Invoke();
        }

        public void ResetSimulation()
        {
            var free=followCamera?followCamera.GetComponent<DemoFreeCamera>():null;if(free)free.ExitFreeView();
            StopSimulation();
            if (!CaptureInitialPose())
                return;
            movement.transform.SetPositionAndRotation(initialPosition, initialRotation);
            if (followCamera != null)
                followCamera.SnapToTarget();
            SimulationReset?.Invoke();
        }
    }
}
