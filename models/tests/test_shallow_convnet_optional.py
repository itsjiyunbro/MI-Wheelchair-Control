import json
import shutil
import unittest
from pathlib import Path

import numpy as np

from models.shallow_convnet import create_shallow_convnet
from models.scripts.train_shallow_convnet import main as train_shallow_convnet_main


TMP_ROOT = Path(__file__).resolve().parents[2] / ".test_tmp"


def fresh_case_dir(name: str) -> Path:
    path = TMP_ROOT / name
    shutil.rmtree(path, ignore_errors=True)
    path.mkdir(parents=True, exist_ok=True)
    return path


class ShallowConvNetOptionalTest(unittest.TestCase):
    def test_create_shallow_convnet_runs_forward_pass(self):
        try:
            import torch
        except ImportError:
            with self.assertRaisesRegex(RuntimeError, "PyTorch"):
                create_shallow_convnet()
            return

        model = create_shallow_convnet()
        logits = model(torch.zeros(2, 1, 9, 320))

        self.assertEqual(tuple(logits.shape), (2, 2))

    def test_train_shallow_convnet_writes_metrics_history_and_checkpoint(self):
        try:
            import torch  # noqa: F401
        except ImportError:
            self.skipTest("PyTorch is not installed in this Python environment.")

        root = fresh_case_dir("shallow_convnet_history")
        data_dir = root / "data"
        data_dir.mkdir(exist_ok=True)
        results_dir = root / "results"
        checkpoints_dir = root / "checkpoints"

        rng = np.random.default_rng(42)
        for split in ("train", "val", "test"):
            X = rng.normal(0, 0.01, size=(4, 9, 320)).astype(np.float32)
            X[:2, 0] += 0.5
            X[2:, 1] += 0.5
            y = np.array([0, 0, 1, 1], dtype=np.int64)
            np.save(data_dir / f"X_{split}.npy", X)
            np.save(data_dir / f"y_{split}.npy", y)

        config_path = root / "config.json"
        config_path.write_text(
            json.dumps(
                {
                    "data_dir": str(data_dir),
                    "results_dir": str(results_dir),
                    "checkpoints_dir": str(checkpoints_dir),
                    "shallow_convnet": {
                        "batch_size": 2,
                        "epochs": 1,
                        "learning_rate": 0.001,
                        "dropout": 0.5,
                    },
                }
            ),
            encoding="utf-8",
        )

        code = train_shallow_convnet_main(["--config", str(config_path), "--epochs", "1"])

        self.assertEqual(code, 0)
        self.assertTrue((results_dir / "shallow_convnet_metrics.json").exists())
        self.assertTrue((results_dir / "shallow_convnet_history.csv").exists())
        self.assertTrue((checkpoints_dir / "shallow_convnet_best.pt").exists())


if __name__ == "__main__":
    unittest.main()
