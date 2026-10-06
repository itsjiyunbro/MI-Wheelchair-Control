# Unity Prototype Status

This note summarizes the Unity draft capture reviewed from the local project materials.

## Reviewed Prototype

- Scene/application title: `EEGWheelchairSimulator`
- Unity version shown in the capture: Unity 6.3 LTS
- Scene name shown in the capture: `MainScene`
- Prototype mode: running simulation with keyboard control

## Visible Implementation State

The capture shows a simple virtual wheelchair object on a broad ground/road environment. A left-side overlay titled `EEG WHEELCHAIR DEMO` shows the current runtime status:

- simulation state
- control mode
- move state
- steering direction
- heading angle
- Python connection status
- prediction
- confidence
- start, stop, and reset controls

The current UI already matches the fields needed for the planned Python-to-Unity command flow. The connection, prediction, and confidence fields can be used directly once the Python inference side sends live messages.

## Suggested Next Steps

1. Commit the Unity project files under `unity/` when the draft is ready to share.
2. Keep keyboard control as a manual fallback for demos and debugging.
3. Add a small message receiver that updates connection, prediction, confidence, movement, steering, and heading fields.
4. Define the accepted command message schema together with the Python communication layer.
5. Log received commands and applied steering decisions during demo runs.
