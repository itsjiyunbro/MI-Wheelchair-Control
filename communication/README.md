# Communication

This folder contains the interface between the Python inference pipeline and Unity.

The current plan is to send prediction results such as direction, confidence, and timestamp from Python to Unity through a local socket connection.

Keeping this part separate makes it easier to modify the transport method without changing the model or Unity control logic.
