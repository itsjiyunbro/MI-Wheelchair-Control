"""Fake prediction stream: temporary development protocol / v0.2, NOT an EEG model.

Standard library only. TCP 127.0.0.1, UTF-8 NDJSON. Interval is for testing only.
"""
import argparse
import json
import math
import socket
import time

PATTERN = (("FORWARD", 0.82), ("LEFT", 0.914), ("FORWARD", 0.76),
           ("RIGHT", 0.873), ("FORWARD", 0.95), ("STOP", 0.68))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=5055)
    parser.add_argument("--interval", type=float, default=1.0, help="Test interval in seconds (default: 1.0)")
    parser.add_argument("--count", type=int, default=0, help="0 = repeat until Ctrl+C; otherwise send this many messages")
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("port must be between 1 and 65535")
    if not math.isfinite(args.interval) or args.interval <= 0:
        parser.error("interval must be a finite positive number")
    if args.count < 0:
        parser.error("count must be 0 or greater")
    try:
        with socket.create_connection(("127.0.0.1", args.port), timeout=3) as connection:
            print(f"CONNECTED to 127.0.0.1:{args.port} | FAKE stream / temporary v0.2", flush=True)
            print("Predictions update while Unity is STOPPED. START permits movement. Ctrl+C exits.", flush=True)
            sequence = 1
            while args.count == 0 or sequence <= args.count:
                command, confidence = PATTERN[(sequence - 1) % len(PATTERN)]
                packet = {"command": command, "confidence": confidence,
                          "timestamp": time.time(), "sequence": sequence}
                connection.sendall((json.dumps(packet, separators=(",", ":"), allow_nan=False) + "\n").encode("utf-8"))
                print(f"FAKE prediction #{sequence}: {command}, {confidence:.1%}", flush=True)
                sequence += 1
                # Keep the connection alive for the final interval too, so Unity can consume it.
                time.sleep(args.interval)
    except KeyboardInterrupt:
        print("\nStream stopped; disconnected.", flush=True)
    except OSError as error:
        print(f"Connection unavailable/lost (socket error {error.errno}).\nCheck Unity Play Mode, Python Control Source and port, then run again.", flush=True)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
