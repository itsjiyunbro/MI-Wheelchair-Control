import contextlib
import io
import json
import unittest
from pathlib import Path

import numpy as np

from models.scripts.check_data import main


TMP_ROOT = Path(__file__).resolve().parents[2] / ".test_tmp"


def case_dir(name: str) -> Path:
    path = TMP_ROOT / name
    path.mkdir(parents=True, exist_ok=True)
    return path


class CheckDataScriptTest(unittest.TestCase):
    def test_check_data_prints_split_summaries(self):
        root = case_dir("check_data")
        data_dir = root / "data"
        data_dir.mkdir(exist_ok=True)
        for split in ("train", "val", "test"):
            np.save(data_dir / f"X_{split}.npy", np.zeros((4, 9, 320), dtype=np.float32))
            np.save(data_dir / f"y_{split}.npy", np.array([0, 1, 0, 1], dtype=np.int64))
            np.save(data_dir / f"X_rest_{split}.npy", np.zeros((2, 9, 320), dtype=np.float32))

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

        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            code = main(["--config", str(config_path)])

        self.assertEqual(code, 0)
        self.assertIn("train: X=(4, 9, 320), y={0: 2, 1: 2}", out.getvalue())
        self.assertIn("rest_test: X=(2, 9, 320)", out.getvalue())


if __name__ == "__main__":
    unittest.main()
