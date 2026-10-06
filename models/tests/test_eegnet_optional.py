import json
import shutil
import unittest
from pathlib import Path

import numpy as np

from models.architectures.eegnet import create_eegnet
from models.scripts.evaluate_rest import (
    false_command_rate,
    model_prefix,
    summarize_false_command_rates,
)
from models.scripts.train_eegnet import main as train_eegnet_main


TMP_ROOT = Path(__file__).resolve().parents[2] / ".test_tmp"


def fresh_case_dir(name: str) -> Path:
    path = TMP_ROOT / name
    shutil.rmtree(path, ignore_errors=True)
    path.mkdir(parents=True, exist_ok=True)
    return path


class EegNetOptionalTest(unittest.TestCase):
    def test_create_eegnet_reports_missing_torch_or_runs_forward_pass(self):
        try:
            import torch
        except ImportError:
            with self.assertRaisesRegex(RuntimeError, "PyTorch"):
                create_eegnet()
            return

        model = create_eegnet()
        logits = model(torch.zeros(2, 1, 9, 320))

        self.assertEqual(tuple(logits.shape), (2, 2))

    def test_create_eegnet_missing_torch_message_helper(self):
        try:
            import torch  # noqa: F401
        except ImportError:
            pass
        else:
            self.skipTest("PyTorch is installed in this Python environment.")
        with self.assertRaisesRegex(RuntimeError, "PyTorch"):
            create_eegnet()

    def test_false_command_rate_uses_confidence_threshold(self):
        probabilities = np.array(
            [
                [0.55, 0.45],
                [0.91, 0.09],
                [0.20, 0.80],
            ]
        )

        rate = false_command_rate(probabilities, threshold=0.9)

        self.assertEqual(rate, 1 / 3)

    def test_summarize_false_command_rates_reports_counts_per_threshold(self):
        probabilities = np.array(
            [
                [0.51, 0.49],
                [0.80, 0.20],
                [0.10, 0.90],
            ]
        )

        summary = summarize_false_command_rates(probabilities, thresholds=[0.5, 0.85])

        self.assertEqual(
            summary,
            [
                {
                    "threshold": 0.5,
                    "false_command_count": 3,
                    "predicted_0_count": 2,
                    "predicted_1_count": 1,
                    "total": 3,
                    "false_command_rate": 1.0,
                },
                {
                    "threshold": 0.85,
                    "false_command_count": 1,
                    "predicted_0_count": 0,
                    "predicted_1_count": 1,
                    "total": 3,
                    "false_command_rate": 1 / 3,
                },
            ],
        )

    def test_model_prefix_accepts_supported_rest_evaluation_models(self):
        self.assertEqual(model_prefix("eegnet"), "eegnet")
        self.assertEqual(model_prefix("shallow_convnet"), "shallow_convnet")
        with self.assertRaises(ValueError):
            model_prefix("unknown")

    def test_train_eegnet_writes_epoch_history_csv(self):
        try:
            import torch  # noqa: F401
        except ImportError:
            self.skipTest("PyTorch is not installed in this Python environment.")

        root = fresh_case_dir("eegnet_history")
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
                    "eegnet": {
                        "batch_size": 2,
                        "epochs": 1,
                        "learning_rate": 0.001,
                        "dropout": 0.25,
                    },
                }
            ),
            encoding="utf-8",
        )

        code = train_eegnet_main(["--config", str(config_path), "--epochs", "1"])

        history_path = results_dir / "eegnet_history.csv"
        self.assertEqual(code, 0)
        self.assertTrue(history_path.exists())
        lines = history_path.read_text(encoding="utf-8").splitlines()
        self.assertEqual(lines[0], "epoch,train_loss,val_accuracy,val_macro_f1")
        self.assertEqual(len(lines), 2)


if __name__ == "__main__":
    unittest.main()
