"""Run the team's saved-EEG inference and send its predictions to Unity."""

import argparse
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


def send(predictions, port, interval):
    with socket.create_connection(("127.0.0.1", port), timeout=3) as connection:
        for sequence, (label, command, confidence) in enumerate(predictions, 1):
            packet = {
                "command": command,
                "confidence": confidence,
                "timestamp": time.time(),
                "sequence": sequence,
            }
            line = json.dumps(packet, allow_nan=False, separators=(",", ":")) + "\n"
            connection.sendall(line.encode("utf-8"))
            print(f"EEG #{sequence}: {label} -> {command} ({confidence:.6f})", flush=True)
            # The receiver applies a command until the next packet or disconnect.
            time.sleep(interval)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--predict-script", type=Path, default=DEFAULT_PREDICT)
    parser.add_argument("--port", type=int, default=5055)
    parser.add_argument("--interval", type=float, default=1.0, help="Seconds to apply each prediction (default: 1)")
    parser.add_argument("predict_args", nargs=argparse.REMAINDER, help="predict.py arguments after --")
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("--port must be between 1 and 65535")
    if not math.isfinite(args.interval) or args.interval <= 0:
        parser.error("--interval must be a finite positive number")
    predict_script = args.predict_script.resolve()
    if not predict_script.is_file():
        parser.error(f"predict.py not found at {predict_script}. Use --predict-script to set its path.")
    predict_args = args.predict_args
    if predict_args and predict_args[0] != "--":
        parser.error("Place predict.py arguments after --.")

    try:
        predictions = infer(predict_script, predict_args[1:])
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"EEG inference failed: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        return 130

    try:
        send(predictions, args.port, args.interval)
    except OSError as error:
        print(f"Unity TCP error: {error}. Check Play mode, Python input and the port.", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        return 130
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
