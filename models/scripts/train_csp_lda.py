import argparse
import sys
import time
from pathlib import Path

if __package__ in (None, ""):
    sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from models.architectures.csp_lda import fit_csp_lda
from models.data.eeg_dataset import apply_path_overrides, load_config, load_split
from models.utils.metrics import classification_metrics, save_json


def _slice(X, y, limit: int | None):
    if not limit:
        return X, y
    return X[:limit], y[:limit]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Train CSP+LDA baseline.")
    parser.add_argument("--config", default="models/config/default_config.json")
    parser.add_argument("--data-dir", default=None, help="Override preprocessed data directory.")
    parser.add_argument("--results-dir", default=None, help="Override results directory.")
    parser.add_argument("--checkpoints-dir", default=None, help="Override checkpoint directory.")
    parser.add_argument("--limit", type=int, default=None, help="Use first N windows per split.")
    parser.add_argument("--output", default=None, help="Optional metrics JSON path.")
    args = parser.parse_args(argv)

    config = apply_path_overrides(
        load_config(args.config),
        data_dir=args.data_dir,
        results_dir=args.results_dir,
        checkpoints_dir=args.checkpoints_dir,
    )
    data_dir = config["data_dir"]
    csp_config = config.get("csp", {})
    X_train, y_train = load_split(data_dir, "train", unstandardized=True)
    X_val, y_val = load_split(data_dir, "val", unstandardized=True)
    X_test, y_test = load_split(data_dir, "test", unstandardized=True)

    X_train, y_train = _slice(X_train, y_train, args.limit)
    X_val, y_val = _slice(X_val, y_val, args.limit)
    X_test, y_test = _slice(X_test, y_test, args.limit)

    model = fit_csp_lda(
        X_train,
        y_train,
        n_components=int(csp_config.get("n_components", 4)),
        reg=float(csp_config.get("reg", 1e-6)),
    )

    results = {}
    for split, X, y in (
        ("train", X_train, y_train),
        ("val", X_val, y_val),
        ("test", X_test, y_test),
    ):
        start = time.perf_counter()
        pred = model.predict(X)
        elapsed = time.perf_counter() - start
        metrics = classification_metrics(y, pred)
        metrics["latency_sec_per_window"] = elapsed / max(len(y), 1)
        results[split] = metrics
        print(f"{split}: accuracy={metrics['accuracy']:.4f}, macro_f1={metrics['macro_f1']:.4f}")

    output = args.output or str(Path(config["results_dir"]) / "csp_lda_metrics.json")
    save_json(results, output)
    print(f"saved: {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
