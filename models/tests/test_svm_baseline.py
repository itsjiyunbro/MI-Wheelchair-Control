import json
import shutil
import unittest
from pathlib import Path

import numpy as np

from models.svm_baseline import fit_svm_baseline
from models.scripts.train_svm import main as train_svm_main


TMP_ROOT = Path(__file__).resolve().parents[2] / ".test_tmp"


def fresh_case_dir(name: str) -> Path:
    path = TMP_ROOT / name
    shutil.rmtree(path, ignore_errors=True)
    path.mkdir(parents=True, exist_ok=True)
    return path


class SvmBaselineTest(unittest.TestCase):
    def test_svm_baseline_separates_channel_power_features(self):
        rng = np.random.default_rng(42)
        X = rng.normal(0, 0.01, size=(12, 3, 80)).astype(np.float32)
        y = np.array([0] * 6 + [1] * 6, dtype=np.int64)

        X[:6, 0] += rng.normal(0, 1.0, size=(6, 80))
        X[6:, 1] += rng.normal(0, 1.0, size=(6, 80))

        model = fit_svm_baseline(X, y, c=1.0, max_iter=5000)
        pred = model.predict(X)

        self.assertEqual(pred.tolist(), y.tolist())

    def test_train_svm_writes_metrics_json(self):
        root = fresh_case_dir("svm_script")
        data_dir = root / "data"
        data_dir.mkdir(exist_ok=True)
        results_dir = root / "results"
        checkpoints_dir = root / "checkpoints"

        rng = np.random.default_rng(7)
        for split in ("train", "val", "test"):
            X = rng.normal(0, 0.01, size=(8, 9, 320)).astype(np.float32)
            y = np.array([0, 0, 0, 0, 1, 1, 1, 1], dtype=np.int64)
            X[:4, 0] += rng.normal(0, 1.0, size=(4, 320))
            X[4:, 1] += rng.normal(0, 1.0, size=(4, 320))
            np.save(data_dir / f"X_{split}_unstd.npy", X)
            np.save(data_dir / f"X_{split}.npy", X)
            np.save(data_dir / f"y_{split}.npy", y)

        config_path = root / "config.json"
        config_path.write_text(
            json.dumps(
                {
                    "data_dir": str(data_dir),
                    "results_dir": str(results_dir),
                    "checkpoints_dir": str(checkpoints_dir),
                    "svm": {"C": 1.0, "max_iter": 5000, "unstandardized": True},
                }
            ),
            encoding="utf-8",
        )

        code = train_svm_main(["--config", str(config_path)])

        output = results_dir / "svm_metrics.json"
        metrics = json.loads(output.read_text(encoding="utf-8"))
        self.assertEqual(code, 0)
        self.assertEqual(metrics["test"]["accuracy"], 1.0)


if __name__ == "__main__":
    unittest.main()
