from dataclasses import dataclass

import numpy as np


def _trial_covariance(trial: np.ndarray, reg: float) -> np.ndarray:
    cov = trial @ trial.T
    trace = float(np.trace(cov))
    if trace > 0:
        cov = cov / trace
    return cov + reg * np.eye(cov.shape[0])


def _mean_covariance(X: np.ndarray, reg: float) -> np.ndarray:
    covs = np.stack([_trial_covariance(trial, reg) for trial in X], axis=0)
    return covs.mean(axis=0)


def _features(X: np.ndarray, filters: np.ndarray) -> np.ndarray:
    projected = np.einsum("kc,nct->nkt", filters, X)
    variances = np.var(projected, axis=2)
    variances = variances / np.maximum(variances.sum(axis=1, keepdims=True), 1e-12)
    return np.log(np.maximum(variances, 1e-12))


@dataclass
class CspLdaModel:
    filters: np.ndarray
    weights: np.ndarray
    bias: float

    def transform(self, X: np.ndarray) -> np.ndarray:
        return _features(np.asarray(X, dtype=np.float64), self.filters)

    def decision_function(self, X: np.ndarray) -> np.ndarray:
        return self.transform(X) @ self.weights + self.bias

    def predict(self, X: np.ndarray) -> np.ndarray:
        return (self.decision_function(X) >= 0).astype(np.int64)


def fit_csp_lda(
    X: np.ndarray,
    y: np.ndarray,
    n_components: int = 4,
    reg: float = 1e-6,
) -> CspLdaModel:
    X = np.asarray(X, dtype=np.float64)
    y = np.asarray(y, dtype=np.int64)
    if X.ndim != 3:
        raise ValueError(f"expected X shape (N, C, T), got {X.shape}")
    if set(y.tolist()) != {0, 1}:
        raise ValueError("CSP+LDA requires both labels 0 and 1")

    class0 = X[y == 0]
    class1 = X[y == 1]
    cov0 = _mean_covariance(class0, reg)
    cov1 = _mean_covariance(class1, reg)
    cov_sum = cov0 + cov1

    eigvals, eigvecs = np.linalg.eigh(cov_sum)
    eigvals = np.maximum(eigvals, reg)
    whitening = np.diag(1.0 / np.sqrt(eigvals)) @ eigvecs.T
    s0 = whitening @ cov0 @ whitening.T
    csp_vals, csp_vecs = np.linalg.eigh(s0)
    filters_all = csp_vecs.T @ whitening

    order = np.argsort(csp_vals)
    n_components = max(2, min(int(n_components), X.shape[1]))
    half = n_components // 2
    selected = np.concatenate([order[:half], order[-(n_components - half) :]])
    filters = filters_all[selected]

    feats = _features(X, filters)
    f0 = feats[y == 0]
    f1 = feats[y == 1]
    m0 = f0.mean(axis=0)
    m1 = f1.mean(axis=0)
    pooled = np.cov(np.vstack([f0 - m0, f1 - m1]).T) + reg * np.eye(feats.shape[1])
    weights = np.linalg.pinv(pooled) @ (m1 - m0)
    bias = -0.5 * float((m0 + m1) @ weights)
    return CspLdaModel(filters=filters, weights=weights, bias=bias)
