# MI-Wheelchair-Control

A virtual wheelchair control system based on motor imagery EEG.

## Overview

This project classifies motor imagery EEG signals and uses the prediction results to control the direction of a virtual wheelchair in Unity 3D.

The dataset is the [PhysioNet EEG Motor Movement/Imagery Dataset](https://physionet.org/content/eegmmidb/1.0.0/). Left-hand and right-hand motor imagery are used as the main control inputs, while rest data is kept separately for later use in a hold/straight decision.

The final demo is designed to replay recorded EEG data in chronological order and process it as a stream. EEG preprocessing, model inference, command generation, and Unity control are connected as one pipeline.

## System Flow

```text
Recorded EEG (.edf)
        ↓
Streaming input
        ↓
EEG preprocessing
        ↓
Motor imagery classification
        ↓
Left / Right / Hold command
        ↓
Python–Unity communication
        ↓
Virtual wheelchair control
```

## Dataset

- Dataset: PhysioNet EEG Motor Movement/Imagery Dataset
- Sampling rate: 160 Hz
- Motor imagery runs: R04, R08, R12
- T1: Left-hand motor imagery
- T2: Right-hand motor imagery
- T0: Rest

## Current Preprocessing Plan

Selected sensorimotor channels:

```text
FC3  FCz  FC4
C3   Cz   C4
CP3  CPz  CP4
```

Main settings:

- 8–30 Hz causal Butterworth band-pass filter
- 2 s input window (320 samples)
- 0.25 s step (40 samples)
- Subject-wise train / validation / test split
- T1 and T2 for left/right classification
- T0 stored separately for rest-state evaluation
- No cosine tapering
- No baseline correction in the main streaming pipeline

The preprocessing settings may be adjusted after the first experiments.

## Models

The project starts with the following models:

- CSP + LDA
- EEGNet
- ShallowConvNet

Classification performance and inference time will be compared before selecting the model used in the final demo.

## Real-Time Simulation

This project does not acquire EEG from a live EEG device.

Instead, recorded EDF data is read in chronological order to simulate a real-time EEG stream. Only the data available up to the current time is used for preprocessing and inference.

The target inference setup is:

- 2 s rolling EEG buffer
- New prediction every 0.25 s
- Causal preprocessing
- Python inference result sent to Unity 3D

## Repository Structure

```text
MI-Wheelchair-Control/
├── preprocessing/   # EEG loading, filtering, windowing, dataset preparation
├── models/          # CSP+LDA, EEGNet, ShallowConvNet and model experiments
├── realtime/        # EEG stream simulation and online inference
├── communication/   # Python–Unity message transfer
├── unity/           # Unity 3D virtual wheelchair project
└── docs/            # Project notes, preprocessing plans, and references
```

## Project Goal

The goal is to build an end-to-end BCI demonstration in which motor imagery EEG is processed, classified, and translated into directional commands for a virtual wheelchair.

The first implementation focuses on left and right steering. The system will later be evaluated in terms of classification accuracy, F1-score, and end-to-end response time.

## License

This project is released under the MIT License.

## Unity implementation status

The `unity` branch now contains the runnable Unity simulator, primitive wheelchair,
indoor test course, dashboard HUD, Start/Stop/Reset, and TCP test senders.
See [Unity setup, completed work, and temporary protocol](unity/README.md).

Current integration uses **fake test predictions**, not the trained EEG model.
The executable development protocol accepts LEFT / RIGHT / FORWARD / STOP;
Hold/Rest and confidence-based decisions above remain future team plans.
Open `unity/EEGWheelchairSimulator` in Unity Hub, then open `Assets/Scenes/MainScene.unity`.
