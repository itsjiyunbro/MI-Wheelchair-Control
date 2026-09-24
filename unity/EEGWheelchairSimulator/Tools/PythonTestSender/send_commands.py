"""Temporary development protocol / v0.1. Test sender only; no EEG or AI model.

TCP 127.0.0.1:5055, UTF-8 NDJSON. Unity owns simulation state and movement.
"""
import argparse
import json
import socket
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=5055)
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("port must be between 1 and 65535")
    try:
        connection = socket.create_connection(("127.0.0.1", args.port), timeout=3)
    except OSError as error:
        print(f"Connection failed (socket error {error.errno}).\nOpen MainScene, select Python, and enter Play Mode. Then run this sender again.", flush=True)
        return 1

    print(f"CONNECTED to 127.0.0.1:{args.port} (temporary development protocol / v0.1)", flush=True)
    print("Click START in Unity. Commands: left, right, forward, stop. Quit: quit / exit / q.", flush=True)
    try:
        with connection:
            while True:
                command = input("> ").strip().lower()
                if command in {"quit", "exit", "q"}:
                    break
                if command not in {"left", "right", "forward", "stop"}:
                    print("Use left, right, forward, stop, or quit.", flush=True)
                    continue
                packet = {"command": command.upper(), "confidence": 0.87, "timestamp": time.time()}
                connection.sendall((json.dumps(packet, separators=(",", ":"), allow_nan=False) + "\n").encode("utf-8"))
                print(f"SENT {packet['command']} (no application acknowledgement; check Unity HUD)", flush=True)
    except (EOFError, KeyboardInterrupt):
        print("\nSender closed.", flush=True)
    except OSError as error:
        print(f"Connection lost (socket error {error.errno}).\nCheck Unity Play Mode / Control Source, then run the sender again.", flush=True)
        return 1
    finally:
        connection.close()
    print("Disconnected. Unity clears the active Python command.", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
