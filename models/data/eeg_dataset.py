import json
import os
from pathlib import Path
from typing import Any

import numpy as np


PATH_ENV_VARS = {
    "data_dir": "MI_WHEELCHAIR_DATA_DIR",
    "results_dir": "MI_WHEELCHAIR_RESULTS_DIR",
    "checkpoints_dir": "MI_WHEELCHAIR_CHECKPOINTS_DIR",
}


def _expand_path(value: str) -> Path:
    return Path(os.path.expandvars(value)).expanduser()


def _resolve(base: Path, value: str) -> str:
    path = _expand_path(value)
    if path.is_absolute():
        return str(path)
    return str((base / path).resolve())


def _resolve_override(value: str) -> str:
    path = _expand_path(value)
    if path.is_absolute():
        return str(path)
    return str(path.resolve())


def load_config(config_path: str | Path) -> dict[str, Any]:
    path = Path(config_path)
    config = json.loads(path.read_text(encoding="utf-8"))
    base = path.resolve().parent
    for key in ("data_dir", "results_dir", "checkpoints_dir"):
        if key in config:
            config[key] = _resolve(base, config[key])
    return config


def apply_path_overrides(
    config: dict[str, Any],
    *,
    data_dir: str | None = None,
    results_dir: str | None = None,
    checkpoints_dir: str | None = None,
) -> dict[str, Any]:
    updated = dict(config)
    for key, value in (
        ("data_dir", data_dir),
        ("results_dir", results_dir),
        ("checkpoints_dir", checkpoints_dir),
    ):
        if value is None:
            value = os.environ.get(PATH_ENV_VARS[key])
        if value:
            updated[key] = _resolve_override(value)
    return updated


def validate_X(X: np.ndarray, channels: int = 9, samples: int = 320) -> None:
    if X.ndim != 3:
        raise ValueError(f"expected X with 3 dimensions, got shape {X.shape}")
    if X.shape[1:] != (channels, samples):
        raise ValueError(f"expected X shape (N, {channels}, {samples}), got {X.shape}")


def validate_X_y(
    X: np.ndarray,
    y: np.ndarray,
    channels: int = 9,
    samples: int = 320,
) -> None:
    validate_X(X, channels=channels, samples=samples)
    if y.ndim != 1:
        raise ValueError(f"expected y with 1 dimension, got shape {y.shape}")
    if len(X) != len(y):
        raise ValueError(f"X/y length mismatch: {len(X)} != {len(y)}")
    labels = set(np.asarray(y).astype(int).tolist())
    if not labels.issubset({0, 1}):
        raise ValueError(f"expected labels 0/1 only, got {sorted(labels)}")


def _load_array(path: Path, mmap: bool) -> np.ndarray:
    if not path.exists():
        raise FileNotFoundError(f"missing required array: {path}")
    return np.load(path, mmap_mode="r" if mmap else None)


def load_split(
    data_dir: str | Path,
    split: str,
    *,
    unstandardized: bool = False,
    mmap: bool = True,
) -> tuple[np.ndarray, np.ndarray]:
    suffix = "_unstd" if unstandardized else ""
    root = Path(data_dir)
    X = _load_array(root / f"X_{split}{suffix}.npy", mmap=mmap)
    y = _load_array(root / f"y_{split}.npy", mmap=mmap)
    validate_X_y(X, y)
    return X, y


def load_rest(
    data_dir: str | Path,
    split: str,
    *,
    unstandardized: bool = False,
    mmap: bool = True,
) -> np.ndarray:
    suffix = "_unstd" if unstandardized else ""
    X = _load_array(Path(data_dir) / f"X_rest_{split}{suffix}.npy", mmap=mmap)
    validate_X(X)
    return X
