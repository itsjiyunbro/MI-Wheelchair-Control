import argparse
import sys
from collections import Counter
from pathlib import Path

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from models.data.eeg_dataset import (
    apply_path_overrides,
    load_config,
    load_rest,
    load_split,
)


def _dist(y) -> dict[int, int]:
    return dict(sorted(Counter(int(v) for v in y.tolist()).items()))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Check preprocessed EEG arrays.")
    parser.add_argument(
        "--config",
        default="models/config/default_config.json",
        help="Path to config JSON.",
    )
    parser.add_argument("--data-dir", default=None, help="Override preprocessed data directory.")
    parser.add_argument("--results-dir", default=None, help="Override results directory.")
    parser.add_argument("--checkpoints-dir", default=None, help="Override checkpoint directory.")
    args = parser.parse_args(argv)

    config = apply_path_overrides(
        load_config(args.config),
        data_dir=args.data_dir,
        results_dir=args.results_dir,
        checkpoints_dir=args.checkpoints_dir,
    )
    data_dir = config["data_dir"]
    print(f"data_dir: {data_dir}")

    for split in ("train", "val", "test"):
        X, y = load_split(data_dir, split)
        print(f"{split}: X={X.shape}, y={_dist(y)}")

    for split in ("train", "val", "test"):
        X = load_rest(data_dir, split)
        print(f"rest_{split}: X={X.shape}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
