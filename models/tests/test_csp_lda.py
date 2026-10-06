import unittest

import numpy as np

from models.architectures.csp_lda import fit_csp_lda


class CspLdaTest(unittest.TestCase):
    def test_csp_lda_separates_channel_dominant_signals(self):
        rng = np.random.default_rng(42)
        t = np.linspace(0, 2 * np.pi, 80)
        X = rng.normal(0, 0.02, size=(12, 3, 80)).astype(np.float32)
        y = np.array([0] * 6 + [1] * 6, dtype=np.int64)

        for i in range(6):
            X[i, 0] += np.sin(t) * 2.0
            X[i + 6, 1] += np.cos(t) * 2.0

        model = fit_csp_lda(X, y, n_components=2)
        pred = model.predict(X)

        self.assertEqual(pred.tolist(), y.tolist())


if __name__ == "__main__":
    unittest.main()
