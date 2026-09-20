# Preprocessing

This folder contains the EEG preprocessing pipeline.

Planned steps include:
- loading PhysioNet EDF files
- selecting motor imagery runs and sensorimotor channels
- applying an 8–30 Hz causal Butterworth band-pass filter
- creating 2 s windows with a 0.25 s step
- separating train, validation, and test data by subject
- exporting model-ready NumPy arrays

T0 (rest) data is processed with the same signal pipeline but kept separate from T1/T2 classification data.
