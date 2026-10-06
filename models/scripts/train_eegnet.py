import argparse
import csv
import sys
from pathlib import Path

import numpy as np

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from models.architectures.eegnet import create_eegnet
from models.data.eeg_dataset import apply_path_overrides, load_config, load_split
from models.utils.metrics import classification_metrics, save_json


def _limit(X, y, limit: int | None):
    if not limit:
        return X, y
    return X[:limit], y[:limit]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Train EEGNet in PyTorch.")
    parser.add_argument("--config", default="models/config/default_config.json")
    parser.add_argument("--data-dir", default=None, help="Override preprocessed data directory.")
    parser.add_argument("--results-dir", default=None, help="Override results directory.")
    parser.add_argument("--checkpoints-dir", default=None, help="Override checkpoint directory.")
    parser.add_argument("--epochs", type=int, default=None)
    parser.add_argument("--limit", type=int, default=None, help="Small local or Colab smoke run.")
    args = parser.parse_args(argv)

    try:
        import torch
        from torch.utils.data import DataLoader, TensorDataset
    except ImportError:
        print("PyTorch is required for EEGNet. Use Colab GPU or install torch.")
        return 2

    config = apply_path_overrides(
        load_config(args.config),
        data_dir=args.data_dir,
        results_dir=args.results_dir,
        checkpoints_dir=args.checkpoints_dir,
    )
    eeg_config = config.get("eegnet", {})
    data_dir = config["data_dir"]
    epochs = args.epochs or int(eeg_config.get("epochs", 10))
    batch_size = int(eeg_config.get("batch_size", 64))
    lr = float(eeg_config.get("learning_rate", 1e-3))

    X_train, y_train = _limit(*load_split(data_dir, "train", mmap=False), args.limit)
    X_val, y_val = _limit(*load_split(data_dir, "val", mmap=False), args.limit)
    X_test, y_test = _limit(*load_split(data_dir, "test", mmap=False), args.limit)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model = create_eegnet(dropout=float(eeg_config.get("dropout", 0.25))).to(device)
    optimizer = torch.optim.Adam(model.parameters(), lr=lr)
    loss_fn = torch.nn.CrossEntropyLoss()

    train_ds = TensorDataset(
        torch.from_numpy(np.asarray(X_train[:, None], dtype=np.float32)),
        torch.from_numpy(np.asarray(y_train, dtype=np.int64)),
    )
    train_loader = DataLoader(train_ds, batch_size=batch_size, shuffle=True)

    best_val = -1.0
    history = []
    results_dir = Path(config["results_dir"])
    results_dir.mkdir(parents=True, exist_ok=True)
    history_path = results_dir / "eegnet_history.csv"
    checkpoint_dir = Path(config["checkpoints_dir"])
    checkpoint_dir.mkdir(parents=True, exist_ok=True)
    checkpoint_path = checkpoint_dir / "eegnet_best.pt"

    for epoch in range(1, epochs + 1):
        model.train()
        total_loss = 0.0
        total_seen = 0
        for xb, yb in train_loader:
            xb = xb.to(device)
            yb = yb.to(device)
            optimizer.zero_grad()
            loss = loss_fn(model(xb), yb)
            loss.backward()
            optimizer.step()
            total_loss += float(loss.item()) * len(yb)
            total_seen += len(yb)

        val_metrics = _evaluate(model, X_val, y_val, device)
        train_loss = total_loss / total_seen if total_seen else 0.0
        history.append(
            {
                "epoch": epoch,
                "train_loss": train_loss,
                "val_accuracy": val_metrics["accuracy"],
                "val_macro_f1": val_metrics["macro_f1"],
            }
        )
        _save_history(history, history_path)
        print(f"epoch={epoch} val_accuracy={val_metrics['accuracy']:.4f} val_macro_f1={val_metrics['macro_f1']:.4f}")
        if val_metrics["macro_f1"] > best_val:
            best_val = val_metrics["macro_f1"]
            torch.save(model.state_dict(), checkpoint_path)

    test_metrics = _evaluate(model, X_test, y_test, device)
    output = results_dir / "eegnet_metrics.json"
    save_json({"best_val_macro_f1": best_val, "test": test_metrics}, output)
    print(f"saved: {output}")
    return 0


def _save_history(history: list[dict[str, float]], output: Path) -> None:
    with output.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(
            f,
            fieldnames=["epoch", "train_loss", "val_accuracy", "val_macro_f1"],
        )
        writer.writeheader()
        writer.writerows(history)


def _evaluate(model, X, y, device):
    import torch

    model.eval()
    with torch.no_grad():
        xb = torch.from_numpy(np.asarray(X[:, None], dtype=np.float32)).to(device)
        logits = model(xb)
        pred = logits.argmax(dim=1).cpu().numpy()
    return classification_metrics(y, pred)


if __name__ == "__main__":
    raise SystemExit(main())
