# Unity

This folder contains the Unity 3D virtual wheelchair environment.

The Unity application will receive prediction results from Python and convert them into wheelchair steering commands.

Initial control mapping:
- Left-hand motor imagery → turn left
- Right-hand motor imagery → turn right
- Hold / low-confidence state → keep current safe state
