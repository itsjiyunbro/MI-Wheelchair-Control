"""Timing regressions; no model files, EEG samples or Unity installation needed."""

import csv
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch, MagicMock

from send_eeg_predictions import replay_schedule, send


class ReplayTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.windows = Path(self.directory.name) / "windows.csv"
        self.onsets = Path(self.directory.name) / "onsets.csv"
        self.predictions = [("Right", "RIGHT", .95)] * 3
        self.write_windows([(0, 0, 2.5), (1, 0, 2.75), (2, 3, 3.0)])
        self.write_onsets([(0, 0, 0, 160), (3, 12.3, 1968, 160)])

    def write_windows(self, rows):
        with self.windows.open("w", newline="") as stream:
            writer = csv.writer(stream)
            writer.writerow(["EEG_Index", "Trial", "End_sec_from_event"])
            writer.writerows(rows)

    def write_onsets(self, rows):
        with self.onsets.open("w", newline="") as stream:
            writer = csv.writer(stream)
            writer.writerow(["Trial", "Onset_sec_from_run_start", "Onset_sample_from_run_start", "Sampling_rate_Hz"])
            writer.writerows(rows)

    def schedule(self):
        return replay_schedule(self.predictions, self.windows, .25, .25, self.onsets)

    def test_absolute_end_times_and_missing_trials_are_preserved(self):
        schedule = self.schedule()
        now = 0
        turns = []
        for prediction, duration in schedule:
            if prediction[1] == "RIGHT":
                turns.append(now)
                self.assertLessEqual(duration, .25)
            now += duration
        for actual, expected in zip(turns, [2.5, 2.75, 15.3]):
            self.assertAlmostEqual(actual, expected)
        self.assertEqual(len(turns), 3)
        self.assertEqual(schedule[3][0][1], "STOP")
        self.assertAlmostEqual(schedule[3][1], 12.3)
        self.assertEqual(schedule[-1], (("Replay complete", "STOP", 0.0), .25))
        self.assertAlmostEqual(now, 15.8)

    def test_removed_window_within_trial_expires_command(self):
        self.write_windows([(0, 0, 2.5), (1, 0, 3.5), (2, 0, 3.75)])
        schedule = self.schedule()
        self.assertEqual(schedule[1][1], .25)
        self.assertEqual(schedule[2], (("No valid window", "STOP", 0.0), .75))

    def test_bad_onsets_rejected_before_connecting(self):
        for rows in [
            [(0, 0, 0, 160)],  # Missing onset for a surviving trial.
            [(0, 0, 0, 160), (0, 4.1, 656, 160)],  # Duplicate trial.
            [(0, 0, 0, 160), (3, 12.3, 1969, 160)],  # Sample/seconds disagreement.
            [(0, 0, 0, 128), (3, 12.3, 1968, 160)],  # Wrong sampling rate.
            [(0, float("nan"), 0, 160), (3, 12.3, 1968, 160)],
        ]:
            with self.subTest(rows=rows):
                self.write_onsets(rows)
                with self.assertRaises(ValueError):
                    self.schedule()

    def test_bad_window_order_or_count_rejected(self):
        for rows in [
            [(1, 0, 2.5), (0, 0, 2.75), (2, 3, 3)],
            [(0, 0, 2.5), (1, 0, 2.5), (2, 3, 3)],
            [(0, 0, 2.5)],
            [(0, 0, 20), (1, 0, 21), (2, 3, 3)],
        ]:
            with self.subTest(rows=rows):
                self.write_windows(rows)
                with self.assertRaises(ValueError):
                    self.schedule()

    def test_onsets_require_windows(self):
        with self.assertRaises(ValueError):
            replay_schedule(self.predictions, None, .25, .25, self.onsets)

    def test_legacy_demo_and_packed_trials_remain_available(self):
        self.assertEqual(replay_schedule(self.predictions, None, 1, .25),
                         [(prediction, 1) for prediction in self.predictions])
        schedule = replay_schedule(self.predictions, self.windows, .25, .25)
        self.assertEqual(len(schedule), 4)
        self.assertEqual(schedule[2], (("Trial gap", "STOP", 0.0), .25))

    def test_sender_uses_deadlines_without_accumulating_send_overhead(self):
        clock = [100.0]
        sent = []

        def transmit(data):
            sent.append((clock[0], json.loads(data)))
            clock[0] += .03  # Socket/logging work must not shift subsequent deadlines.

        def sleep(seconds):
            clock[0] += seconds

        connection = MagicMock()
        connection.__enter__.return_value = connection
        connection.sendall.side_effect = transmit
        with patch("send_eeg_predictions.socket.create_connection", return_value=connection), \
                patch("send_eeg_predictions.time.monotonic", side_effect=lambda: clock[0]), \
                patch("send_eeg_predictions.time.sleep", side_effect=sleep), \
                patch("builtins.print"):
            send([(p, .25) for p in self.predictions], 5055)
        self.assertEqual([round(t - 100, 3) for t, _ in sent], [0, .25, .5])
        self.assertAlmostEqual(clock[0] - 100, .75)
        self.assertEqual([p["sequence"] for _, p in sent], [1, 2, 3])
        self.assertTrue(all(p["command"] == "RIGHT" and p["confidence"] == .95 for _, p in sent))


if __name__ == "__main__":
    unittest.main()
