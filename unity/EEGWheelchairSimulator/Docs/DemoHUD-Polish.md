# HUD 시각 개선 — UI 1단계

## 적용 범위

MainScene의 기존 StatusCanvas/StatusPanel을 재배치하고 스타일링했습니다. 기존 Text 8개, Title, Button 3개와 콜백을 유지했습니다. 모든 런타임 C#, 환경/휠체어 Prefab·Material, 카메라, TCP, Python, 패키지는 변경하지 않았습니다.

uGUI Text/Image/Button 및 Unity 내장 LegacyRuntime.ttf를 그대로 사용합니다. 값 문자열은 기존 코드에서 생성하는 SIMULATION: STOPPED, PREDICTION: LEFT 등의 형식을 유지합니다. 새로운 제어 상태 판단, confidence 판단, 그래프, 가짜 데이터는 없습니다. 상태별 동적 색상/Progress Bar도 추가하지 않았습니다.

## 디자인

- 짙은 남색 반투명 패널, 청록 상단 라인, 얇은 테두리
- SYSTEM: Simulation 상태와 Connection 배지
- MODEL: 청록 굵은 Prediction 행, Confidence 숫자
- WHEELCHAIR: 실제 Control / Move / Steering / Heading
- 제목 24, Prediction 22, Simulation 20, 실제 동작 19, Confidence 18, 섹션 제목 14px
- START: 차분한 녹청색, STOP: 차분한 주황갈색, RESET: 회청색
- START/STOP 144×40px, RESET 300×32px. 비활성 버튼은 어두운 반투명 배경
- 기존 interactable 처리, 버튼 클릭 이벤트, EventSystem/InputSystemUIInputModule 유지

Panel 색상 RGBA=(.035,.065,.085,.94), Card=(.085,.13,.16,.95), 본문=(.91,.95,.96), 보조=(.60,.71,.76), 포인트=(.38,.82,.78).

## 최종 Hierarchy

기존 참조와 기존 검증 도구의 경로를 모두 보존하기 위해 바인딩된 Text/Button은 StatusPanel의 직속 자식으로 유지합니다. 시각적 그룹은 배경 카드로 구분합니다.

```
StatusCanvas (Canvas, CanvasScaler, GraphicRaycaster, WheelchairStatusUI)
└─ StatusPanel
   ├─ HudChrome                 # 추가된 비상호작용 장식
   │  ├─ TopAccent / LeftBorder / RightBorder / BottomBorder
   │  ├─ Kicker / Subtitle
   │  ├─ SystemCard (SectionTitle, Rule)
   │  ├─ ConnectionBadge
   │  ├─ ModelCard (SectionTitle, Rule)
   │  └─ WheelchairCard (SectionTitle, Rule)
   ├─ Title
   ├─ SimulationText / ConnectionText
   ├─ PredictionText / ConfidenceText
   ├─ ControlText / MoveText / SteeringText / HeadingText
   └─ StartButton / StopButton / ResetButton (각 Label 유지)
```

HudChrome은 첫 번째 sibling으로 배치하여 기존 Text/Button 뒤에 표시합니다. 새 Image/Text는 raycastTarget=false이므로 클릭을 가로채지 않습니다.

## 해상도

Screen Space Overlay 유지. CanvasScaler는 Scale With Screen Size, Reference Resolution=1920×1080, Match Width Or Height=.5.
패널은 왼쪽 위 (24,24), 340×622px. 16:9에서는 화면 폭의 약 17.7%입니다.

| 해상도 | 패널 실제 크기(대략) |
|---|---|
| 1920×1080 | 340×622 |
| 1600×900 | 283×518 |
| 1280×720 | 227×415 |

1024×768에서도 패널 전체가 화면 안에 들어오는 것을 검사했습니다. 발표용 기본 권장은 16:9입니다.

## 생성/수정 파일

- 추가: Assets/Editor/EEGWheelchair/DemoHudPolish.cs (+ meta)
- 수정: Assets/Scenes/MainScene.unity — UI 오브젝트의 시각 설정 및 장식 추가
- 추가: Docs/DemoHUD-Polish.md

메뉴: Tools > EEG Wheelchair > Polish Demo HUD

이미 적용됐으므로 메뉴를 다시 실행할 필요는 없습니다. 재실행하면 같은 이름의 장식을 재사용하고 정해진 스타일을 다시 적용합니다. 현재 Scene을 삭제하거나 기존 Text/Button을 재생성하지 않습니다. 수동으로 바꾼 같은 UI 스타일 값은 재적용될 수 있습니다. Editor 스크립트는 게임 실행에 필요하지 않습니다.

## 검증

- 컴파일, Scene 저장/재로드, 반복 적용 후 Scene 직렬화 동일성
- 기존 Scene 블록 삭제 0개
- WheelchairStatusUI의 직렬화 데이터/참조 불변
- Start/Stop/Reset의 onClick 직렬화 내용 불변
- 1920×1080, 1600×900, 1280×720 및 1024×768 패널 범위/텍스트 잘림 검사
- START, 좌/우 코너, GOAL의 카메라 위치에서 HUD 포함 렌더 확인
- 실제 Play Mode Input System 키 입력과 포인터 클릭
- STOP 차단/RESET 위치·방향·카메라 복원, HUD/버튼 상태
- 실제 Python 송신기 및 스트림, 연결 해제/재접속, Prediction/Confidence, STOPPED 수신/이동 차단

최종 After/코너/GOAL PNG는 실제 Play Mode에서 Unity ScreenCapture로 캡처했습니다. 기존 Screen Space Overlay를 그대로 사용합니다. 변경 전 PNG와 Python 회귀 테스트 PNG는 오프스크린 렌더이므로 글자 선명도 비교보다 배치·데이터 확인용입니다. 캡처용 위치 변경은 저장하지 않고 Game View 해상도도 복원합니다.

## Unity에서 직접 확인

1. Assets/Scenes/MainScene.unity 열기. SimulationController의 Control Source=Keyboard.
2. Play: SIMULATION STOPPED, MOVE STOPPED, STEERING STRAIGHT, PREDICTION/CONFIDENCE '-' 확인.
3. START 클릭: RUNNING 및 START 비활성/STOP 활성 확인.
4. Game View에 포커스를 두고 W/A/D, W+A, W+D 입력. MOVE/STEERING/HEADING 및 카메라 확인.
5. SimulationController Inspector에서 Control Source=Python으로 변경.
6. PowerShell에서 기존 송신기 실행:

```powershell
Set-Location '<저장소 경로>\unity\EEGWheelchairSimulator'
powershell -ExecutionPolicy Bypass -File .\Tools\PythonTestSender\run_sender.ps1 -Stream
```

7. CONTROL PYTHON, CONNECTION CONNECTED, Prediction/Confidence 변화 확인.
8. STOP 클릭: MOVE STOPPED / STEERING STRAIGHT. Prediction/Confidence 수신은 계속 갱신되는 것이 정상.
9. RESET 클릭: 시작 위치·방향·카메라 복원, Simulation STOPPED 유지.
10. Python 콘솔 Ctrl+C: CONNECTION WAITING, Prediction/Confidence '-' 확인. 다시 실행하면 재접속.
11. Game View 해상도를 1920×1080 → 1600×900 → 1280×720으로 변경. 패널/버튼/문자 잘림 및 휠체어·코너·GOAL 시야 확인.
12. Console에 빨간 오류가 없는지 확인.

송신기는 기존의 py → python → project venv → Codex 내부 Python fallback 탐색을 그대로 사용합니다. 이번 작업에서 Python 환경이나 송신 코드를 변경하지 않았습니다.
