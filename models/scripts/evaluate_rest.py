import argparse
import csv
import sys
from pathlib import Path

import numpy as np

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from models.architectures.eegnet import create_eegnet
from models.architectures.shallow_convnet import create_shallow_convnet
from models.data.eeg_dataset import (
    apply_path_overrides,
    load_config,
    load_rest,
)
from models.utils.metrics import save_json


def false_command_rate(probabilities, threshold: float) -> float:
    probabilities = np.asarray(probabilities, dtype=float)
    if probabilities.ndim != 2 or probabilities.shape[1] != 2:
        raise ValueError(f"expected probability shape (N, 2), got {probabilities.shape}")
    confident = probabilities.max(axis=1) >= threshold
    return float(confident.mean()) if len(confident) else 0.0


def summarize_false_command_rates(probabilities, thresholds):
    probabilities = np.asarray(probabilities, dtype=float)
    if probabilities.ndim != 2 or probabilities.shape[1] != 2:
        raise ValueError(f"expected probability shape (N, 2), got {probabilities.shape}")

    pred = probabilities.argmax(axis=1)
    confidence = probabilities.max(axis=1)
    total = int(len(probabilities))
    summary = []
    for threshold in thresholds:
        threshold = float(threshold)
        confident = confidence >= threshold
        summary.append(
            {
                "threshold": threshold,
                "false_command_count": int(confident.sum()),
                "predicted_0_count": int(((pred == 0) & confident).sum()),
                "predicted_1_count": int(((pred == 1) & confident).sum()),
                "total": total,
                "false_command_rate": float(confident.mean()) if total else 0.0,
            }
        )
    return summary


def model_prefix(model_name: str) -> str:
    if model_name not in {"eegnet", "shallow_convnet"}:
        raise ValueError(f"unsupported model: {model_name}")
    return model_name


def create_model(model_name: str, config: dict):
    prefix = model_prefix(model_name)
    if prefix == "eegnet":
        return create_eegnet(dropout=float(config.get("eegnet", {}).get("dropout", 0.25)))
    model_config = config.get("shallow_convnet", {})
    return create_shallow_convnet(
        dropout=float(model_config.get("dropout", 0.5)),
        filters=int(model_config.get("filters", 40)),
        temporal_kernel=int(model_config.get("temporal_kernel", 25)),
        pool_size=int(model_config.get("pool_size", 75)),
        pool_stride=int(model_config.get("pool_stride", 15)),
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Evaluate Rest/T0 false command rate.")
    parser.add_argument("--model", choices=["eegnet", "shallow_convnet"], default="eegnet")
    parser.add_argument("--config", default="models/config/default_config.json")
    parser.add_argument("--data-dir", default=None, help="Override preprocessed data directory.")
    parser.add_argument("--results-dir", default=None, help="Override results directory.")
    parser.add_argument("--checkpoint", default=None, help="PyTorch model checkpoint path.")
    parser.add_argument("--probabilities", default=None, help="Path to N x 2 probability .npy file.")
    parser.add_argument("--threshold", type=float, default=0.9)
    parser.add_argument("--thresholds", default="0.5,0.6,0.7,0.8,0.9")
    parser.add_argument("--batch-size", type=int, default=512)
    args = parser.parse_args(argv)

    thresholds = _parse_thresholds(args.thresholds)
    if args.probabilities:
        probabilities = np.load(args.probabilities)
        rate = false_command_rate(probabilities, threshold=args.threshold)
        print(f"false_command_rate={rate:.6f} at threshold={args.threshold}")
        for row in summarize_false_command_rates(probabilities, thresholds):
            print(_format_row("probabilities", row))
        return 0

    if not args.checkpoint:
        parser.error("--checkpoint is required unless --probabilities is supplied.")

    try:
        import torch
    except ImportError:
        print("PyTorch is required for Rest/T0 evaluation. Use Colab GPU or install torch.")
        return 2

    config = apply_path_overrides(
        load_config(args.config),
        data_dir=args.data_dir,
        results_dir=args.results_dir,
        checkpoints_dir=None,
    )
    results_dir = Path(config["results_dir"])
    results_dir.mkdir(parents=True, exist_ok=True)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    prefix = model_prefix(args.model)
    model = create_model(prefix, config).to(device)
    state = _load_state_dict(torch, args.checkpoint, device)
    model.load_state_dict(state)
    model.eval()

    all_rows = []
    split_summaries = {}
    probability_paths = {}
    for split in ("train", "val", "test"):
        X_rest = load_rest(config["data_dir"], split, mmap=True)
        probabilities = _predict_probabilities(torch, model, X_rest, device, args.batch_size)
        probability_path = results_dir / f"{prefix}_rest_{split}_probabilities.npy"
        np.save(probability_path, probabilities)
        probability_paths[split] = str(probability_path)

        rows = summarize_false_command_rates(probabilities, thresholds)
        split_summaries[split] = rows
        for row in rows:
            output_row = {"split": split, **row}
            all_rows.append(output_row)
            print(_format_row(split, row))

    csv_path = results_dir / f"{prefix}_rest_false_command.csv"
    json_path = results_dir / f"{prefix}_rest_false_command.json"
    _save_csv(all_rows, csv_path)
    save_json(
        {
            "thresholds": thresholds,
            "splits": split_summaries,
            "probability_paths": probability_paths,
        },
        json_path,
    )
    print(f"saved: {csv_path}")
    print(f"saved: {json_path}")
    return 0


def _parse_thresholds(value: str) -> list[float]:
    thresholds = [float(item.strip()) for item in value.split(",") if item.strip()]
    if not thresholds:
        raise ValueError("at least one threshold is required")
    return thresholds


def _load_state_dict(torch, checkpoint: str, device):
    try:
        return torch.load(checkpoint, map_location=device, weights_only=True)
    except TypeError:
        return torch.load(checkpoint, map_location=device)


def _predict_probabilities(torch, model, X, device, batch_size: int) -> np.ndarray:
    outputs = []
    with torch.no_grad():
        for start in range(0, len(X), batch_size):
            batch = np.asarray(X[start : start + batch_size, None], dtype=np.float32).copy()
            xb = torch.from_numpy(batch).to(device)
            logits = model(xb)
            probabilities = torch.softmax(logits, dim=1).cpu().numpy()
            outputs.append(probabilities.astype(np.float32))
    return np.concatenate(outputs, axis=0) if outputs else np.empty((0, 2), dtype=np.float32)


def _save_csv(rows: list[dict], output: Path) -> None:
    fieldnames = [
        "split",
        "threshold",
        "false_command_count",
        "predicted_0_count",
        "predicted_1_count",
        "total",
        "false_command_rate",
    ]
    with output.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def _format_row(split: str, row: dict) -> str:
    return (
        f"{split} threshold={row['threshold']:.2f} "
        f"false_command_rate={row['false_command_rate']:.6f} "
        f"count={row['false_command_count']}/{row['total']} "
        f"pred0={row['predicted_0_count']} pred1={row['predicted_1_count']}"
    )


if __name__ == "__main__":
    raise SystemExit(main())
