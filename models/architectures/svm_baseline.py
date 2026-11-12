from dataclasses import dataclass

import numpy as np


def log_variance_features(X: np.ndarray, eps: float = 1e-8) -> np.ndarray:
    X = np.asarray(X, dtype=np.float64)
    if X.ndim != 3:
        raise ValueError(f"expected X shape (N, C, T), got {X.shape}")
    return np.log(np.var(X, axis=2) + eps)


@dataclass
class SvmBaselineModel:
    mean: np.ndarray
    std: np.ndarray
    weights: np.ndarray
    bias: float

    def transform(self, X: np.ndarray) -> np.ndarray:
        features = log_variance_features(X)
        return (features - self.mean) / self.std

    def decision_function(self, X: np.ndarray) -> np.ndarray:
        return self.transform(X) @ self.weights + self.bias

    def predict(self, X: np.ndarray) -> np.ndarray:
        return (self.decision_function(X) >= 0).astype(np.int64)


def fit_svm_baseline(
    X: np.ndarray,
    y: np.ndarray,
    *,
    c: float = 1.0,
    max_iter: int = 2000,
    learning_rate: float = 0.05,
    tol: float = 1e-8,
) -> SvmBaselineModel:
    features = log_variance_features(X)
    y = np.asarray(y, dtype=np.int64)
    if y.ndim != 1 or len(y) != len(features):
        raise ValueError(f"X/y length mismatch: {len(features)} != {len(y)}")
    if set(y.tolist()) != {0, 1}:
        raise ValueError("SVM baseline requires both labels 0 and 1")

    mean = features.mean(axis=0)
    std = np.maximum(features.std(axis=0), 1e-8)
    Z = (features - mean) / std
    target = np.where(y == 1, 1.0, -1.0)

    weights = np.zeros(Z.shape[1], dtype=np.float64)
    bias = 0.0
    n = float(len(Z))
    c = float(c)
    learning_rate = float(learning_rate)

    for step in range(1, int(max_iter) + 1):
        margins = target * (Z @ weights + bias)
        active = margins < 1.0
        grad_w = weights.copy()
        grad_b = 0.0
        if np.any(active):
            grad_w -= c * (target[active, None] * Z[active]).sum(axis=0) / n
            grad_b -= c * target[active].sum() / n

        step_size = learning_rate / np.sqrt(step)
        next_weights = weights - step_size * grad_w
        next_bias = bias - step_size * grad_b
        delta = np.linalg.norm(next_weights - weights) + abs(next_bias - bias)
        weights = next_weights
        bias = float(next_bias)
        if delta < tol:
            break

    return SvmBaselineModel(mean=mean, std=std, weights=weights, bias=bias)
