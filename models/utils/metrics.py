import json
from pathlib import Path

import numpy as np


def _safe_div(num: float, den: float) -> float:
    return 0.0 if den == 0 else num / den


def classification_metrics(y_true, y_pred, labels=(0, 1)) -> dict:
    y_true = np.asarray(y_true).astype(int)
    y_pred = np.asarray(y_pred).astype(int)
    if y_true.shape != y_pred.shape:
        raise ValueError(f"shape mismatch: {y_true.shape} != {y_pred.shape}")

    matrix = np.zeros((len(labels), len(labels)), dtype=int)
    index = {label: i for i, label in enumerate(labels)}
    for true, pred in zip(y_true, y_pred):
        if true in index and pred in index:
            matrix[index[true], index[pred]] += 1

    per_class = {}
    f1_scores = []
    for label in labels:
        i = index[label]
        tp = float(matrix[i, i])
        fp = float(matrix[:, i].sum() - matrix[i, i])
        fn = float(matrix[i, :].sum() - matrix[i, i])
        precision = _safe_div(tp, tp + fp)
        recall = _safe_div(tp, tp + fn)
        f1 = _safe_div(2 * precision * recall, precision + recall)
        per_class[str(label)] = {
            "precision": precision,
            "recall": recall,
            "f1": f1,
            "support": int(matrix[i, :].sum()),
        }
        f1_scores.append(f1)

    accuracy = _safe_div(float((y_true == y_pred).sum()), float(len(y_true)))
    return {
        "accuracy": accuracy,
        "macro_f1": float(np.mean(f1_scores)) if f1_scores else 0.0,
        "confusion_matrix": matrix.tolist(),
        "per_class": per_class,
    }


def save_json(data: dict, path: str | Path) -> None:
    output = Path(path)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(
        json.dumps(data, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
