import unittest

import numpy as np

from models.eegnet import create_eegnet
from models.scripts.evaluate_rest import false_command_rate


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


if __name__ == "__main__":
    unittest.main()
