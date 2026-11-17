"""Run the team's saved-EEG inference and send its predictions to Unity."""

import argparse
import csv
import json
import math
import socket
import subprocess
import sys
import time
from pathlib import Path


DEFAULT_PREDICT = Path(__file__).resolve().parents[3] / "evaluate" / "predict.py"
COMMANDS = {"Left": "LEFT", "Hold": "STOP", "Right": "RIGHT"}


def infer(predict_script, predict_args):
    result = subprocess.run(
        [sys.executable, str(predict_script), *predict_args],
        stdout=subprocess.PIPE,
        encoding="utf-8",
        check=True,
    )
    predictions = []
    for number, line in enumerate(result.stdout.splitlines(), 1):
        try:
            prediction = json.loads(line)
            label = prediction["label"]
            confidence = prediction["confidence"]
            command = COMMANDS[label]
            if type(confidence) not in (int, float) or not 0 <= confidence <= 1:
                raise ValueError("confidence must be a number in [0, 1]")
        except (KeyError, TypeError, ValueError) as error:
            raise ValueError(f"Invalid prediction on stdout line {number}: {error}") from error
        predictions.append((label, command, confidence))
    if not predictions:
        raise ValueError("The model produced no predictions.")
    return predictions


def read_trial_onsets(path):
    """Read recording-relative onsets; never reconstruct missing trials."""
    onsets = {}
    previous_trial, previous_onset = -1, -1.0
    with path.open(encoding="utf-8-sig", newline="") as stream:
        for number, row in enumerate(csv.DictReader(stream), 2):
            try:
                trial = int(row["Trial"])
                seconds = float(row["Onset_sec_from_run_start"])
                sample = int(row["Onset_sample_from_run_start"])
                rate = float(row["Sampling_rate_Hz"])
            except (KeyError, TypeError, ValueError) as error:
                raise ValueError(f"Invalid trial onset on CSV row {number}.") from error
            if (trial <= previous_trial or not math.isfinite(seconds)
                    or seconds < 0 or seconds <= previous_onset or sample < 0
                    or rate != 160.0 or abs(seconds * rate - sample) > 0.001):
                raise ValueError(f"Inconsistent onset, order or 160 Hz sample index on CSV row {number}.")
            onsets[trial] = sample / rate
            previous_trial, previous_onset = trial, seconds
    if not onsets:
        raise ValueError("The trial onset CSV is empty.")
    return onsets


def replay_schedule(predictions, windows_csv, interval, trial_gap, trial_onsets_csv=None):
    if trial_onsets_csv is not None and windows_csv is None:
        raise ValueError("--trial-onsets-csv requires --windows-csv.")
    if windows_csv is None:
        return [(prediction, interval) for prediction in predictions]

    with windows_csv.open(encoding="utf-8-sig", newline="") as stream:
        rows = list(csv.DictReader(stream))
    if len(rows) != len(predictions):
        raise ValueError(f"CSV rows ({len(rows)}) and predictions ({len(predictions)}) must match.")

    timing = []
    for index, row in enumerate(rows):
        try:
            eeg_index = int(row["EEG_Index"])
            trial = int(row["Trial"])
            end = float(row["End_sec_from_event"])
        except (KeyError, TypeError, ValueError) as error:
            raise ValueError(f"Invalid timing metadata on CSV row {index + 2}.") from error
        if eeg_index != index or trial < 0 or not math.isfinite(end) or end < 0:
            raise ValueError(f"Invalid window index, trial or end time on CSV row {index + 2}.")
        timing.append((trial, end))

    for index in range(1, len(timing)):
        trial, end = timing[index]
        previous_trial, previous_end = timing[index - 1]
        if trial < previous_trial or (trial == previous_trial and end <= previous_end):
            raise ValueError(f"Windows are out of recording order at EEG index {index}.")

    if trial_onsets_csv is not None:
        onsets = read_trial_onsets(trial_onsets_csv)
        missing = sorted({trial for trial, _ in timing} - onsets.keys())
        if missing:
            raise ValueError(f"No onset supplied for window trials: {missing}")
        # Inference is available only after collecting the entire EEG window.
        ends = [onsets[trial] + end for trial, end in timing]
        if any(b <= a for a, b in zip(ends, ends[1:])):
            raise ValueError("Absolute window end times must be strictly increasing.")
        schedule = []
        if ends[0] > 0:
            schedule.append((("Recording start", "STOP", 0.0), ends[0]))
        for index, prediction in enumerate(predictions):
            distance = ends[index + 1] - ends[index] if index + 1 < len(ends) else interval
            duration = min(interval, distance)
            schedule.append((prediction, duration))
            if distance - duration > 1e-9:
                schedule.append((("No valid window", "STOP", 0.0), distance - duration))
        # No recording end time was supplied. Stop after the final command expires.
        # Give Unity's main thread a frame to consume STOP before disconnecting.
        schedule.append((("Replay complete", "STOP", 0.0), 0.25))
        return schedule

    schedule = []
    for index, prediction in enumerate(predictions):
        trial, end = timing[index]
        if index + 1 == len(timing):
            schedule.append((prediction, interval))
            break
        next_trial, next_end = timing[index + 1]
        if next_trial == trial:
            schedule.append((prediction, next_end - end))
        else:
            schedule.append((prediction, interval))
            schedule.append((("Trial gap", "STOP", 0.0), trial_gap))
    return schedule


def send(schedule, port):
    with socket.create_connection(("127.0.0.1", port), timeout=3) as connection:
        start = time.monotonic()
        offset = 0.0
        for sequence, ((label, command, confidence), duration) in enumerate(schedule, 1):
            time.sleep(max(0.0, start + offset - time.monotonic()))
            packet = {
                "command": command,
                "confidence": confidence,
                "timestamp": time.time(),
                "sequence": sequence,
            }
            line = json.dumps(packet, allow_nan=False, separators=(",", ":")) + "\n"
            connection.sendall(line.encode("utf-8"))
            print(f"EEG #{sequence} t={offset:.3f}s: {label} -> {command} ({confidence:.6f})", flush=True)
            # The receiver applies a command until the next packet or disconnect.
            offset += duration
        time.sleep(max(0.0, start + offset - time.monotonic()))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--predict-script", type=Path, default=DEFAULT_PREDICT)
    parser.add_argument("--port", type=int, default=5055)
    parser.add_argument("--interval", type=float, help="Prediction duration when there is no next window in the same trial (default: 1 s, or 0.25 s with CSV)")
    parser.add_argument("--windows-csv", type=Path, help="Match inference rows to EEG_Index, Trial and End_sec_from_event for replay")
    parser.add_argument("--trial-onsets-csv", type=Path, help="Replay at recording-relative window end times; requires --windows-csv. --interval caps command duration (default 0.25 s); gaps use STOP.")
    parser.add_argument("--trial-gap", type=float, default=0.25, help="STOP duration between trials with CSV timing (default: 0.25 s)")
    parser.add_argument("predict_args", nargs=argparse.REMAINDER, help="predict.py arguments after --")
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("--port must be between 1 and 65535")
    interval = args.interval if args.interval is not None else (0.25 if args.windows_csv else 1.0)
    if not math.isfinite(interval) or interval <= 0:
        parser.error("--interval must be a finite positive number")
    if not math.isfinite(args.trial_gap) or args.trial_gap <= 0:
        parser.error("--trial-gap must be a finite positive number")
    if args.trial_onsets_csv and not args.windows_csv:
        parser.error("--trial-onsets-csv requires --windows-csv")
    predict_script = args.predict_script.resolve()
    if not predict_script.is_file():
        parser.error(f"predict.py not found at {predict_script}. Use --predict-script to set its path.")
    predict_args = args.predict_args
    if predict_args and predict_args[0] != "--":
        parser.error("Place predict.py arguments after --.")

    try:
        predictions = infer(predict_script, predict_args[1:])
        schedule = replay_schedule(predictions, args.windows_csv, interval, args.trial_gap, args.trial_onsets_csv)
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"EEG inference failed: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        return 130

    try:
        send(schedule, args.port)
    except OSError as error:
        print(f"Unity TCP error: {error}. Check Play mode, Python input and the port.", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        return 130
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
