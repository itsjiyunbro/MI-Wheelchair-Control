# EEG motor imagery model comparison

| Model | Test accuracy (validation-selected checkpoint) |
| --- | ---: |
| ShallowConvNet | 58.53% |
| EEG-TCNet | 57.85% |
| EEGNet | 57.49% |
| LMDA-Net | 55.58% |

The test split contains 3,931 windows from 16 held-out subjects. LMDA-Net and EEG-TCNet ran for 50 epochs on Colab. Their `*_observed.json` files summarize the completed runs.

The original EEGNet and ShallowConvNet metrics use the final epoch for test evaluation. The table uses the validation-selected checkpoint re-evaluation recorded in `accuracy_diagnosis_20261103.json`. The Rest CSVs use the saved best checkpoints. At threshold 0.9, test Rest false-command rates were 5.62% for EEGNet, 2.31% for ShallowConvNet, 3.36% for LMDA-Net, and 5.01% for EEG-TCNet.
