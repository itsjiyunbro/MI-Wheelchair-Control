# 7단계: 가짜 prediction 스트림과 HUD

**temporary development protocol / v0.2 — 테스트용 임시 규격이며 최종 팀 규격이 아닙니다.**

실제 EEG/모델은 없습니다. `stream_sender.py`가 정해진 명령·확률을 주기적으로 보내며, Unity는 수신 결과와 실제 이동 상태를 구분하여 표시합니다.

## Windows 실행

1. Unity에서 `Assets/Scenes/MainScene.unity`를 엽니다.
2. Hierarchy의 **SimulationController → Control Source = Python**으로 설정합니다.
3. **Play**를 누릅니다. 초기 HUD는 `SIMULATION: STOPPED`, `CONTROL: PYTHON`, `CONNECTION: WAITING`, `PREDICTION: -`, `CONFIDENCE: -`입니다.
4. 별도 PowerShell 창에서 실행합니다.

```powershell
Set-Location '<저장소 경로>\unity\EEGWheelchairSimulator\Tools\PythonTestSender'
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1 -Stream
```

기본 간격은 **1.0초**, Ctrl+C로 종료합니다. 간격은 모델 성능/추론 시간이 아닌 테스트 설정입니다.

```powershell
# 테스트 간격 0.5초
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1 -Stream -Interval 0.5

# 유한 테스트: 12개 전송하고 마지막 간격 후 연결 종료
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1 -Stream -Interval 0.5 -Count 12

# 기존 v0.1 수동 송신기: left/right/forward/stop, quit
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1
```

`-ExecutionPolicy Bypass`는 해당 PowerShell 프로세스에만 적용됩니다. 영구 실행 정책 변경이나 Python 설치는 하지 않습니다.

## Python 환경

런처는 실행 가능한 Python 3.6 이상을 다음 순서로 확인하며 선택한 경로를 콘솔에 표시합니다.

1. `py -3`
2. PATH의 `python`
3. 프로젝트 루트 `.venv\Scripts\python.exe`, `venv\Scripts\python.exe`, 테스트 폴더 `.venv\Scripts\python.exe`
4. 기존 Codex 내부 Python — **임시 fallback이며 최종 팀 실행 환경이 아님**을 표시

2026-09-18 검사에서는 `py.exe`만 있고 등록된 Python, PATH의 python, 위 venv는 없었습니다. 따라서 이번 검증에는 기존 Codex 내부 Python을 사용했습니다. PC 환경을 설치·변경하지 않았습니다.

팀 Python 환경이 준비되면 런처 없이 직접 실행할 수 있습니다. 표준 라이브러리만 사용합니다.

```powershell
py -3 .\stream_sender.py --interval 1.0
# 또는 python .\stream_sender.py --interval 1.0
# 또는 & '팀 Python 실행파일의 절대경로' .\stream_sender.py --interval 1.0
```

수동 송신기와 스트리밍 송신기를 동시에 연결하지 마세요. 현재 Unity listener는 한 번에 한 클라이언트를 처리합니다.

## 수동 확인 순서

| 단계 | 조작 | 정상 결과 |
|---|---|---|
| 1 | MainScene → Python → Play → 스트리밍 송신기 실행 | CONNECTION: CONNECTED |
| 2 | START를 누르지 않고 몇 초 관찰 | SIMULATION: STOPPED, Prediction/Confidence만 주기적으로 바뀜 |
| 3 | Cube와 MOVE/STEERING 확인 | Cube 정지, MOVE: STOPPED, STEERING: STRAIGHT |
| 4 | START 클릭 | RUNNING; 이후 새 prediction부터 움직임 |
| 5 | FORWARD 수신 | MOVE: FORWARD, STEERING: STRAIGHT |
| 6 | LEFT / RIGHT 수신 | MOVE: STOPPED, STEERING: LEFT / RIGHT, 제자리 회전 |
| 7 | STOP prediction 수신 | RUNNING 유지, MOVE: STOPPED, STEERING: STRAIGHT |
| 8 | Unity STOP 버튼 클릭 | 즉시 정지, SIMULATION: STOPPED; Prediction/Confidence는 계속 갱신 |
| 9 | 다시 START | 이전 prediction을 즉시 재실행하지 않고 다음 수신부터 적용 |
| 10 | Python 창에서 Ctrl+C | CONNECTION: WAITING, PREDICTION: -, CONFIDENCE: -, Cube 정지 |
| 11 | 송신기 재실행 | CONNECTED, 새 prediction 수신. Simulation이 RUNNING이면 새 메시지부터 적용 |
| 12 | RESET | 최초 위치·방향·카메라 복원, STOPPED; 연결 중이면 prediction 수신은 계속 |
| 13 | Control Source = Keyboard → START → W/A/D | 기존 조작 정상. CONNECTION/PREDICTION/CONFIDENCE는 모두 - |
| 14 | Console 확인 | 빨간 오류 없음. 의도적으로 잘못된 메시지를 보내는 검증에서는 Warning 발생 가능 |

실제 모델의 확률이 아닙니다. 화면의 CONFIDENCE는 Python 테스트 값입니다.

정상적으로 다음과 같은 서로 다른 상태를 볼 수 있어야 합니다.

```text
SIMULATION: STOPPED
CONTROL: PYTHON
MOVE: STOPPED
STEERING: STRAIGHT
CONNECTION: CONNECTED
PREDICTION: LEFT
CONFIDENCE: 91.4%
```

## 임시 프로토콜 v0.2

- TCP / loopback `127.0.0.1:5055` / UTF-8 / NDJSON / Python → Unity
- 각 JSON 객체 뒤에 `\n`을 붙입니다. 기존 CRLF·분할/병합 수신 처리도 유지합니다.
- 필드: `command`, `confidence`, `timestamp`, 선택적 `sequence`

```json
{"command":"LEFT","confidence":0.914,"timestamp":1726362000.125,"sequence":12}
```

| 필드 | 의미 |
|---|---|
| command | LEFT / RIGHT / FORWARD / STOP |
| confidence | 0~1의 유한한 테스트 확률. 범위 검사는 형식 검사이며 이동 threshold가 아님 |
| timestamp | 송신 시점 Unix 초. 보관·로그만 하며 latency 계산 없음 |
| sequence | 송신기 실행마다 1부터 증가. 없으면 v0.1로 처리하고 LastSequence=null |

스트리밍 순서는 `FORWARD → LEFT → FORWARD → RIGHT → FORWARD → STOP` 반복입니다. 테스트 confidence는 각각 `0.82, 0.914, 0.76, 0.873, 0.95, 0.68`입니다.

sequence는 진단용으로 보관·로그에만 쓰며 재정렬·다수결·중복 필터에 쓰지 않습니다. JSON 숫자 파싱에서 정확한 정수로 표현 가능한 1~9007199254740991 범위를 허용합니다. 기존 v0.1 메시지는 sequence 없이 그대로 처리합니다.

## Unity 상태 분리

- 기존 `TcpNdjsonListener`가 백그라운드에서 수신하고 보호된 큐에 넣습니다. 이 파일은 변경하지 않았습니다.
- `PythonWheelchairReceiver.Update`가 JSON을 검증하고 마지막 정상 메시지와 연결 상태를 메인 스레드에 반영합니다.
- HUD는 Receiver의 **메인 스레드 스냅샷**만 읽습니다. background thread나 Movement에서 prediction을 추측하지 않습니다.
- `PREDICTION`/`CONFIDENCE`는 마지막 정상 수신 결과, `MOVE`/`STEERING`은 실제 Movement 상태입니다.
- STOP/RESET은 이동 명령과 대기열을 초기화하지만 연결을 끊거나 마지막 정상 prediction 표시를 지우지 않습니다. 이후 STOPPED에서도 표시가 갱신됩니다.
- START는 이전 prediction을 이동 명령으로 복원하지 않습니다. 이후 새 메시지가 필요합니다.
- 연결 해제·재접속·Keyboard 전환·Receiver 비활성화는 이전 prediction/confidence/sequence/timestamp를 초기화합니다.
- 연결됐지만 아직 정상 메시지가 없으면 `CONNECTED`, prediction/confidence는 `-`입니다. Keyboard 모드에서는 세 항목 모두 `-`입니다.
- 잘못된 JSON, command/confidence 누락, 범위 밖 confidence, 알 수 없는 command는 Warning 후 무시합니다. 이전 정상 데이터와 동작 명령을 덮어쓰지 않습니다. 빈 줄은 무시합니다.
- 기존 줄 길이·큐 제한, 연결 해제 시 정지, 재접속 처리도 유지합니다.

## 변경 범위

- 수정: `Assets/Scripts/Networking/PythonWheelchairReceiver.cs`, `Assets/Scripts/UI/WheelchairStatusUI.cs`
- 추가: `Assets/Editor/EEGWheelchair/Step07PredictionSetup.cs`, `Step07PredictionValidation.cs`
- Scene: 기존 StatusPanel에 `ConnectionText`, `PredictionText`, `ConfidenceText` 세 GameObject 추가. 패널 높이 286→373, 버튼 행을 아래로 이동. 기존 참조/버튼 이벤트 유지.
- Python: `stream_sender.py` 추가, `run_sender.ps1`에 Stream/Interval/Count와 일반 Python 탐색 추가. 기존 `send_commands.py` 유지.
- SimulationController, Movement, Keyboard, CameraFollow, TCP worker, 패키지/설정은 변경하지 않습니다.

Scene 연결은 완료되어 Inspector에서 직접 참조를 연결할 필요가 없습니다. 다시 연결할 때만 `Tools > EEG Wheelchair > Step 7 - Connect Prediction HUD`를 사용합니다. 기존 Stage 1~6 생성 메뉴는 실행하지 않습니다.
Step07 Editor 스크립트 두 개는 런타임에서 사용하지 않으므로 필요 없어지면 함께 제거할 수 있습니다.

## 확인 결과

2026-09-18 Unity 6000.3.24f1 자동 검증에서 다음 항목이 통과했습니다.

- `STEP07_SCENE_OK`: Scene 저장·재로드와 세 HUD 참조 유지.
- `STEP07_SOCKET_OK`: 실제 수동 Python 송신기와 v0.1, 네 명령, STOP/RESET 차단, 재접속, NDJSON 분할·병합·오류 처리.
- `STEP07_PREDICTION_OK`: 실제 Python 연속 스트림, sequence/timestamp, STOPPED 표시 갱신과 이동 차단, START 후 새 메시지 적용, 연결 종료 표시 초기화, 0.0%/91.4%/100.0%, invalid JSON/필드 누락/범위 밖 confidence/알 수 없는 명령/빈 줄.
- `STEP07_PLAY_OK`: 기존 키보드 조작, Input System 마우스 버튼 클릭, Start/Stop/Reset, 카메라, HUD.
- `WINDOWS_LAUNCHERS_OK`: 실제 Windows PowerShell 런처의 Stream/Interval/Count 옵션, Codex fallback 명시, 수동 송신기, 증가하는 sequence와 시간 간격, 종료 후 포트 해제.
- 1600×900 HUD 렌더링에서 세 줄의 배치와 읽기 가능 여부를 확인했습니다.

자동 검증은 실제 Unity Play Mode와 테스트용 가상 키보드/마우스를 사용합니다. 사람이 조작하는 시연은 위 수동 절차로 확인하세요.
