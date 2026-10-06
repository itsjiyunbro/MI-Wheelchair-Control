# Unity

This folder contains the Unity 3D virtual wheelchair environment.

The Unity application will receive prediction results from Python and convert them into wheelchair steering commands.

## Current Prototype

A local Unity draft capture has been reviewed. The prototype is an `EEGWheelchairSimulator` MainScene running in Unity 6.3 LTS.

Visible prototype elements:

- Simple virtual wheelchair object on a large ground/road surface
- On-screen `EEG WHEELCHAIR DEMO` status panel
- Simulation state display
- Keyboard control mode display
- Movement state and steering direction display
- Heading display
- Python connection, prediction, and confidence placeholders
- Start, stop, and reset controls

This means the Unity side already has a useful demo shell for visualizing commands. The next implementation step is to commit the Unity project files themselves, then wire the status panel to Python inference output.

Initial control mapping:
- Left-hand motor imagery → turn left
- Right-hand motor imagery → turn right
- Hold / low-confidence state → keep current safe state

## Python Message Fields

The Unity scene should be ready to receive at least the following fields from the Python side:

```json
{
  "label": "Left | Right | Hold",
  "confidence": 0.0,
  "timestamp": 0.0,
  "connection": "connected | disconnected"
}
```

The `label` controls steering, while `confidence` controls whether the scene accepts a Left/Right command or keeps the current safe state.
