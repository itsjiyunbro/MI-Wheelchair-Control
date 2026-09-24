# 6단계: Python 테스트 송신기 ↔ Unity

**temporary development protocol / v0.1 — 팀의 최종 통신 규격이 아닙니다.**

실제 EEG 데이터, 모델 추론, confidence 기준, prediction UI는 포함하지 않습니다.

## 임시 규격

| 항목 | 값 |
|---|---|
| Transport | TCP, Unity가 서버 / Python이 클라이언트 |
| Host / Port | `127.0.0.1:5055` (같은 PC만 접속) |
| Encoding / Framing | UTF-8, newline-delimited JSON (NDJSON) |
| Direction | Python → Unity (응답/ACK 없음) |
| Fields | `command` 문자열, `confidence` 숫자, `timestamp` 숫자 |
| 접속 수 | 동시 클라이언트 1개 |

```json
{"command":"LEFT","confidence":0.87,"timestamp":1726362000.125}
```

각 JSON 뒤에 반드시 LF(`\n`)를 붙입니다. 수신기는 CRLF도 허용합니다. 빈 줄은 무시합니다.
Python 콘솔 명령은 소문자로 입력해도 되며 전송 시 대문자로 변환합니다.
confidence는 테스트 상수 `0.87`, timestamp는 `time.time()`으로 얻은 Unix 초입니다.
두 값은 수신·보관·Console 로그에만 쓰고 이동 판단에 쓰지 않습니다.

| 명령 | 지속 동작 | MOVE | STEERING |
|---|---|---|---|
| FORWARD | 전진, 회전 없음 | FORWARD | STRAIGHT |
| LEFT | 제자리 좌회전 | STOPPED | LEFT |
| RIGHT | 제자리 우회전 | STOPPED | RIGHT |
| STOP | 이동·회전 정지 | STOPPED | STRAIGHT |

명령은 다음 유효 명령, Unity STOP/RESET, 입력원 변경 또는 연결 해제까지 지속됩니다.
**Python의 `stop`은 이동 명령만 지웁니다.** `SIMULATION: RUNNING`은 유지됩니다.
Unity의 STOP 버튼은 시뮬레이션을 STOPPED로 바꾸고 입력을 차단합니다.

## 실행 — Windows PowerShell

1. Unity에서 `Assets/Scenes/MainScene.unity`를 엽니다.
2. Hierarchy의 **SimulationController**를 선택합니다.
3. Inspector의 **Control Source → Python**을 선택합니다. 저장하면 다음 실행에도 유지됩니다.
4. Play를 누릅니다. `CONTROL: PYTHON`, `SIMULATION: STOPPED`, `MOVE: STOPPED`, `STEERING: STRAIGHT`가 정상입니다.
5. Unity Console에 `[TCP v0.1 temporary] WAITING at 127.0.0.1:5055`를 확인합니다.
6. 별도 PowerShell 창에서 다음을 실행합니다.

```powershell
Set-Location '<저장소 경로>\unity\EEGWheelchairSimulator\Tools\PythonTestSender'
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1
```

`-ExecutionPolicy Bypass`는 이 실행 프로세스에만 적용하며 Windows의 영구 정책을 바꾸지 않습니다.
7단계에서 런처 탐색 순서를 `py -3` → `python` → 프로젝트 venv → Codex 내장 Python으로 확장했습니다. 스트리밍 실행과 현재 환경은 `README-Stage07.md`를 확인하세요. 외부 패키지는 필요 없습니다.
현재 PC에는 일반 Python 설치가 없으므로 Codex 내장 Python으로 검증했습니다.
런처 없이 직접 실행할 수도 있습니다.

```powershell
& "$env:USERPROFILE\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" .\send_commands.py
```

Python 창과 Unity Console 양쪽에서 `CONNECTED`를 확인합니다. `SENT`는 TCP 전송을 뜻하며, Unity가 이동을 적용했다는 응답은 아닙니다.

## 수동 테스트 순서와 정상 HUD

아래 모든 행에서 `CONTROL: PYTHON`이 유지됩니다.

| 순서 | 조작 | 정상 결과 |
|---|---|---|
| 1 | Play 직후, START 전에 `forward` 전송 | STOPPED / STOPPED / STRAIGHT, 이동 없음 |
| 2 | Unity START 클릭 | RUNNING / STOPPED / STRAIGHT; 이전 명령 재생 없음 |
| 3 | Python 창에 `left` + Enter | RUNNING / STOPPED / LEFT, Cube 제자리 좌회전 |
| 4 | `right` + Enter | RUNNING / STOPPED / RIGHT, 제자리 우회전 |
| 5 | `forward` + Enter | RUNNING / FORWARD / STRAIGHT, 현재 방향으로 전진 |
| 6 | `stop` + Enter | RUNNING / STOPPED / STRAIGHT, 정지 |
| 7 | `forward` 전송 후 Unity STOP 클릭 | STOPPED / STOPPED / STRAIGHT, 현재 위치·방향 유지 |
| 8 | 정지 상태에서 `left`, `forward` 전송 | 수신 로그는 남지만 움직이지 않음 |
| 9 | Unity START 클릭 | RUNNING / STOPPED / STRAIGHT; 새 명령을 기다림 |
| 10 | 새로 `forward` 전송 | RUNNING / FORWARD / STRAIGHT, 전진 재개 |
| 11 | `quit` + Enter | 연결 해제, RUNNING / STOPPED / STRAIGHT, 즉시 정지; Console WAITING |
| 12 | 송신기를 다시 실행하고 `left` 전송 | 재접속 후 새 명령만 적용, RUNNING / STOPPED / LEFT |
| 13 | Unity RESET 클릭 | 최초 위치·방향·카메라 복원, STOPPED / STOPPED / STRAIGHT |
| 14 | RESET 후 `forward` 전송 | START를 누르기 전까지 이동 없음 |
| 15 | Python 선택 중 Game 창에서 W/A/D | 휠체어 제어에 영향 없음 |
| 16 | Control Source → Keyboard, START 후 W/A/D | CONTROL: KEYBOARD; 기존 전진·회전과 카메라 정상 동작 |
| 17 | Play 종료 후 다시 Python 모드 Play | 포트 재사용 가능, 새 연결 가능, 초기 STOPPED |

표의 상태 순서는 SIMULATION / MOVE / STEERING입니다. 회전할 때 HEADING이 0~359°로 바뀌며 카메라는 계속 따라가야 합니다. Console에 빨간 오류가 없는지 확인합니다. 연결 종료/잘못된 메시지 테스트의 Warning은 허용됩니다.

## 구조와 상태 경계

```text
Python send_commands.py
  → TCP 수신 전용 백그라운드 Thread (TcpNdjsonListener)
  → lock으로 보호하는 bounded queue
  → PythonWheelchairReceiver.Update: JsonUtility 파싱·메타데이터 저장·로그
  → WheelchairMovement.ApplyInput(..., Python)
  → 기존 카메라와 HUD
```

- JSON 파싱도 메인 스레드에서 실행합니다. 백그라운드는 .NET 소켓·바이트 프레이밍·큐만 처리합니다.
- Movement는 `SimulationController.IsRunning`과 실제 Control Source를 검사합니다. 키보드 컴포넌트를 끄는 방식에 의존하지 않습니다.
- SimulationController의 작은 `InputStateCleared` 알림으로 START/STOP/RESET/입력원 전환마다 활성 명령과 대기열을 초기화합니다.
- STOPPED 중 수신한 유효 메시지도 메타데이터/로그는 남지만 이동에는 적용하지 않습니다. START 이후 새 메시지를 보내야 합니다.
- 대기열에는 명령 초기화 세대 번호와 접속 번호를 붙여 이전 세대·접속의 명령을 적용하지 않습니다. 초기화 전에 수신이 시작된 미완성 줄도 버립니다.
- 새 메시지 여부는 Unity의 수신 경계로 판단합니다. timestamp로 정렬하거나 송신 시각과 시계를 동기화하지 않습니다.
- disconnect는 다음 메인 스레드 갱신에서 명령을 지웁니다. Simulation 상태는 유지되고 listener는 재접속을 기다립니다.
- Python 모드에서만 listener가 열립니다. Keyboard 전환, 컴포넌트 비활성화, Play 종료 시 소켓을 닫고 수신 스레드를 정리합니다.
- 프로젝트의 **Run In Background**를 켰습니다. Python 콘솔에 포커스가 있어도 Unity가 갱신되도록 필요한 변경입니다.
- UI/카메라 방식, 이동 속도 2m/s, 회전 속도 60°/s, 기존 Scene 오브젝트는 유지했습니다. 네트워크 상태용 UI는 추가하지 않았습니다.

## 유효성 검사와 한계

- 알 수 없는 명령, 잘못된 JSON, 필수 필드 누락, 유한하지 않은 숫자는 Warning 후 무시합니다. 이전 유효 명령은 유지됩니다.
- 필드는 평평한 `command/confidence/timestamp`만 사용합니다. confidence 범위나 임계값으로 이동을 차단하지 않습니다.
- 최대 한 줄 4096바이트, 큐 128줄, 한 프레임 최대 64줄 처리입니다. 과도한 줄/큐 초과 또는 잘못된 UTF-8은 해당 연결을 닫아 정지시키고 재접속을 기다립니다.
- newline 없이 끊긴 마지막 줄은 적용하지 않습니다. TCP 패킷이 분할되거나 합쳐져도 newline 기준으로 조립합니다.
- 연결이 살아 있지만 아무 명령이 오지 않는 경우의 시간 제한은 구현하지 않았습니다. 마지막 명령이 지속되는 이번 단계의 규칙을 따릅니다.

## 문제 해결

- **Connection failed**: MainScene Play 여부, Control Source=Python, receiver 활성화, Console WAITING을 확인한 뒤 송신기를 다시 실행합니다.
- **포트 사용 중 / Listener unavailable**: 다른 Unity 실행이나 5055를 사용하는 프로그램을 확인합니다. 해결 후 Play를 다시 시작하거나 Keyboard→Python으로 전환합니다.
- 포트를 변경할 경우 Cube의 Python Wheelchair Receiver Port와 송신기의 `--port`를 동일하게 맞춥니다. 예: `python send_commands.py --port 5056`. 실행 중인 listener에는 다음 시작 때 반영됩니다.
- **수신 로그는 있지만 안 움직임**: SIMULATION=RUNNING인지 확인합니다. START 후 새 명령을 보내야 합니다.
- Python 입력 대기 중 Unity를 종료하면 소켓 연결 해제를 다음 전송 때 알게 될 수 있습니다. 이 경우 안내 메시지 후 종료하며 다시 실행하면 됩니다.

## 변경 파일과 자동 연결

- 추가: `Assets/Scripts/Networking/TcpNdjsonListener.cs`, `PythonWheelchairReceiver.cs`
- 최소 수정: `SimulationController.cs`, `WheelchairMovement.cs`, `KeyboardWheelchairInput.cs`, `WheelchairStatusUI.cs`
- 추가: `Assets/Editor/EEGWheelchair/Step06SocketSetup.cs`, `Step06SocketValidation.cs`
- 추가: `Tools/PythonTestSender/send_commands.py`, `run_sender.ps1`, 이 문서
- 기존 `MainScene.unity`의 Cube에 receiver를 추가하고 Movement/Simulation 참조를 저장했습니다. 기존 Scene을 다시 만들지 않았습니다.
- 필요 시 메뉴 `Tools > EEG Wheelchair > Step 6 - Connect Python Test Receiver`로 참조를 연결할 수 있습니다. **이미 연결되어 있으므로 다시 실행할 필요가 없습니다.** Stage 1~5 생성 메뉴를 실행하지 마세요.
- Step06 Editor 스크립트 두 개는 런타임 의존성이 없으므로 이후 불필요하면 함께 제거할 수 있습니다. 검증 스크립트는 명시적 batch 검증에서만 동작합니다.

## 자동 검증

2026-09-17, Unity 6000.3.24f1 batch Play Mode 검증이 종료 코드 0으로 통과했습니다.

- `STEP06_SCENE_OK`: 기존 MainScene 저장·재로드, receiver 참조 유지.
- `STEP06_PLAY_OK`: 실제 New Input System 키보드 이벤트 및 마우스 클릭, 기존 버튼·카메라·HUD, 초기 정지·STOP·RESET.
- `STEP06_SOCKET_OK`: 실제 Python 프로세스가 보내는 네 명령, metadata 저장, Python/Keyboard 선택, STOP 중 입력 차단, 재시작 시 이전 명령 미적용, disconnect 정지·재접속.
- 추가 TCP 검증: 분할 줄, 여러 줄 동시 수신, 잘못된 JSON/명령/누락 필드, STOP/START 경계를 넘는 미완성 줄, 4096바이트 초과 줄, 같은 포트 listener 재시작.
- Scene 변경 비교: Cube receiver 추가, Controller의 입력원 필드 추가, HUD의 고정 문자열 필드 제거만 확인했습니다. 카메라 파일과 패키지는 변경하지 않았습니다.

자동 검증은 실제 Unity Play Mode와 테스트용 가상 키보드/마우스를 사용합니다. 사람이 조작하는 최종 시연은 위 수동 테스트 절차로 확인하세요.
