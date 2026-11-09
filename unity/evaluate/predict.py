import argparse
import json
import sys
from importlib import import_module
from pathlib import Path

import numpy as np
import torch

EVALUATE = Path(__file__).resolve().parent
UNITY = EVALUATE.parent
CHECKPOINTS = UNITY.parent / "models" / "checkpoints"
sys.path.insert(0, str(UNITY))
MODEL_NAMES = ("eegnet", "shallow_convnet", "eeg_tcnet", "lmda")
LABELS = ("Left", "Right")
INPUT_SHAPE = (9, 320)


def load_model(name, checkpoint=None):
    module = import_module(f"architectures.{name}")
    model = getattr(module, f"create_{name}")()
    path = checkpoint if checkpoint is not None else CHECKPOINTS / f"{name}_best.pt"
    model.load_state_dict(torch.load(path, map_location="cpu", weights_only=True))
    return model.eval()


@torch.inference_mode()
def predict(model, windows, threshold=0.90):
    for batch in np.array_split(windows, (len(windows) + 63) // 64):
        inputs = torch.tensor(batch, dtype=torch.float32)
        probabilities = model(inputs.unsqueeze(1)).softmax(dim=1)
        scores, classes = probabilities.max(dim=1)
        for score, index in zip(scores.tolist(), classes.tolist()):
            label = LABELS[index] if score >= threshold else "Hold"
            yield {"label": label, "confidence": score}


def main():
    parser = argparse.ArgumentParser(description="Predict Unity commands from standardized EEG.")
    parser.add_argument("--model", choices=MODEL_NAMES, default="eegnet")
    parser.add_argument("--checkpoint", type=Path)
    parser.add_argument("--input", type=Path, default=EVALUATE / "sample" / "X_test_sample.npy")
    parser.add_argument("--threshold", type=float, default=0.90)
    args = parser.parse_args()

    if not 0.5 <= args.threshold <= 1.0:
        parser.error("--threshold must be between 0.5 and 1.0")

    windows = np.load(args.input, allow_pickle=False)
    if windows.shape == INPUT_SHAPE:
        windows = windows[np.newaxis]
    if windows.ndim != 3 or windows.shape[1:] != INPUT_SHAPE or len(windows) == 0:
        parser.error("--input must have shape (9, 320) or (N, 9, 320) with N > 0")
    if windows.dtype.kind not in "fiu" or not np.isfinite(windows).all():
        parser.error("--input must contain finite real EEG values")

    model = load_model(args.model, args.checkpoint)
    for result in predict(model, windows, args.threshold):
        print(json.dumps(result), flush=True)


if __name__ == "__main__":
    main()
