import unittest

import numpy as np

from models.architectures.csp_lda import fit_csp_lda
from models.architectures.svm_baseline import fit_svm_baseline


class ArchitecturesPackageTest(unittest.TestCase):
    def test_classical_models_import_from_architectures_package_and_predict(self):
        rng = np.random.default_rng(42)
        X = rng.normal(0, 0.01, size=(12, 3, 80)).astype(np.float32)
        y = np.array([0] * 6 + [1] * 6, dtype=np.int64)
        X[:6, 0] += rng.normal(0, 1.0, size=(6, 80))
        X[6:, 1] += rng.normal(0, 1.0, size=(6, 80))

        csp_pred = fit_csp_lda(X, y, n_components=2).predict(X)
        svm_pred = fit_svm_baseline(X, y, max_iter=5000).predict(X)

        self.assertEqual(csp_pred.tolist(), y.tolist())
        self.assertEqual(svm_pred.tolist(), y.tolist())


if __name__ == "__main__":
    unittest.main()
