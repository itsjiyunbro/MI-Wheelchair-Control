# Real-Time Simulation

This folder contains the streaming simulation and online inference pipeline.

Recorded EDF data will be replayed in chronological order to simulate a real-time EEG stream.

Target flow:

```text
EEG stream
→ causal preprocessing
→ 2 s rolling buffer
→ inference every 0.25 s
→ Left / Right / Hold command
```

No future EEG samples should be used during online inference.
