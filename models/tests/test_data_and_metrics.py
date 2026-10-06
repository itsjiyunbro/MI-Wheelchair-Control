import json
import os
import unittest
from pathlib import Path
from unittest.mock import patch

import numpy as np

from models.data.eeg_dataset import (
    apply_path_overrides,
    load_config,
    load_rest,
    load_split,
    validate_X_y,
)
from models.utils.metrics import classification_metrics, save_json


TMP_ROOT = Path(__file__).resolve().parents[2] / ".test_tmp"


def case_dir(name: str) -> Path:
    path = TMP_ROOT / name
    path.mkdir(parents=True, exist_ok=True)
    return path


class DataAndMetricsTest(unittest.TestCase):
    def test_load_config_resolves_relative_paths(self):
        root = case_dir("config")
        config_path = root / "config.json"
        config_path.write_text(
            json.dumps(
                {
                    "data_dir": "data",
                    "results_dir": "results",
                    "checkpoints_dir": "checkpoints",
                }
            ),
            encoding="utf-8",
        )

        config = load_config(config_path)

        self.assertEqual(Path(config["data_dir"]), root / "data")
        self.assertEqual(Path(config["results_dir"]), root / "results")
        self.assertEqual(Path(config["checkpoints_dir"]), root / "checkpoints")

    def test_apply_path_overrides_replaces_only_supplied_paths(self):
        config = {
            "data_dir": "original_data",
            "results_dir": "original_results",
            "checkpoints_dir": "original_checkpoints",
        }

        updated = apply_path_overrides(
            config,
            data_dir="drive_data",
            results_dir=None,
            checkpoints_dir="drive_checkpoints",
        )

        self.assertEqual(Path(updated["data_dir"]), Path("drive_data").resolve())
        self.assertEqual(updated["results_dir"], "original_results")
        self.assertEqual(Path(updated["checkpoints_dir"]), Path("drive_checkpoints").resolve())
        self.assertEqual(config["data_dir"], "original_data")

    def test_apply_path_overrides_uses_environment_when_cli_is_omitted(self):
        config = {
            "data_dir": "original_data",
            "results_dir": "original_results",
            "checkpoints_dir": "original_checkpoints",
        }

        with patch.dict(
            os.environ,
            {
                "MI_WHEELCHAIR_DATA_DIR": "env_data",
                "MI_WHEELCHAIR_RESULTS_DIR": "env_results",
            },
            clear=False,
        ):
            updated = apply_path_overrides(config)

        self.assertEqual(Path(updated["data_dir"]), Path("env_data").resolve())
        self.assertEqual(Path(updated["results_dir"]), Path("env_results").resolve())
        self.assertEqual(updated["checkpoints_dir"], "original_checkpoints")

    def test_cli_path_override_wins_over_environment(self):
        config = {"data_dir": "original_data"}

        with patch.dict(os.environ, {"MI_WHEELCHAIR_DATA_DIR": "env_data"}, clear=False):
            updated = apply_path_overrides(config, data_dir="cli_data")

        self.assertEqual(Path(updated["data_dir"]), Path("cli_data").resolve())

    def test_load_split_and_rest_validate_expected_arrays(self):
        data_dir = case_dir("arrays")
        np.save(data_dir / "X_train.npy", np.zeros((3, 9, 320), dtype=np.float32))
        np.save(data_dir / "y_train.npy", np.array([0, 1, 0], dtype=np.int64))
        np.save(data_dir / "X_rest_train.npy", np.zeros((2, 9, 320), dtype=np.float32))

        X, y = load_split(data_dir, "train", mmap=False)
        X_rest = load_rest(data_dir, "train", mmap=False)

        self.assertEqual(X.shape, (3, 9, 320))
        self.assertEqual(y.tolist(), [0, 1, 0])
        self.assertEqual(X_rest.shape, (2, 9, 320))

    def test_validate_rejects_wrong_shape_and_labels(self):
        with self.assertRaises(ValueError):
            validate_X_y(np.zeros((1, 8, 320)), np.array([0]))

        with self.assertRaises(ValueError):
            validate_X_y(np.zeros((1, 9, 320)), np.array([2]))

    def test_classification_metrics_are_hand_checked(self):
        metrics = classification_metrics(
            np.array([0, 0, 1, 1]),
            np.array([0, 1, 1, 1]),
        )

        self.assertEqual(metrics["accuracy"], 0.75)
        self.assertEqual(metrics["confusion_matrix"], [[1, 1], [0, 2]])
        self.assertAlmostEqual(metrics["macro_f1"], (2 / 3 + 4 / 5) / 2)

    def test_save_json_creates_parent_directory(self):
        output = case_dir("json") / "nested" / "metrics.json"
        save_json({"accuracy": 1.0}, output)
        self.assertEqual(json.loads(output.read_text(encoding="utf-8"))["accuracy"], 1.0)


if __name__ == "__main__":
    unittest.main()
