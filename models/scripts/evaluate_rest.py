import argparse
import sys
from pathlib import Path

import numpy as np

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[2]))


def false_command_rate(probabilities, threshold: float) -> float:
    probabilities = np.asarray(probabilities, dtype=float)
    if probabilities.ndim != 2 or probabilities.shape[1] != 2:
        raise ValueError(f"expected probability shape (N, 2), got {probabilities.shape}")
    confident = probabilities.max(axis=1) >= threshold
    return float(confident.mean()) if len(confident) else 0.0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Evaluate Rest/T0 false command rate.")
    parser.add_argument("--probabilities", required=True, help="Path to N x 2 probability .npy file.")
    parser.add_argument("--threshold", type=float, default=0.9)
    args = parser.parse_args(argv)

    probabilities = np.load(args.probabilities)
    rate = false_command_rate(probabilities, threshold=args.threshold)
    print(f"false_command_rate={rate:.6f} at threshold={args.threshold}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
