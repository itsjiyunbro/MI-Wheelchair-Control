# T자 교차로 재활센터 맵

> 현재 벽 통과 방지 단계가 추가되었습니다. 아래는 환경 제작 당시 기록이며, 두 맵에는 이제 벽·문 Collider와 공통 이동 제한이 있습니다. 현재 동작은 [WallCollision-Guide.md](WallCollision-Guide.md)를 참고하세요.

기존 MainScene을 보존하고 별도 `RehabJunctionScene`에 만든 두 번째 시연 맵입니다.
환경 제작 단계이며 이동, 카메라, HUD, Python 프로토콜, 모델 추론은 변경하지 않습니다.

## 열기와 주행

1. Unity Project 창에서 `Assets/Scenes/RehabJunctionScene.unity`를 엽니다. MainScene을 함께 Additive로 열지 않습니다. 두 Scene 모두 카메라·HUD·제어기를 포함합니다.
2. Hierarchy의 `RehabJunctionCourse`와 기존 `WheelchairPlaceholder/ WheelchairVisual`을 확인합니다.
3. SimulationController의 **Control Source = Keyboard**를 확인하고 Play를 누릅니다.
4. 최초 상태는 STOPPED입니다. **START 버튼**을 누르고 Game 화면에서 W로 안내선을 따라 직진합니다.
5. 교차로 중앙 `(X=0, Z=10)`에서 W를 놓습니다. 기존 속도 2m/s 기준 약 5초지만 위치를 기준으로 조작하세요.
6. **A**로 약 90도 회전하면 HEADING 약 **270°**입니다. W로 왼쪽 **GOAL A / REHABILITATION**까지 갑니다.
7. **STOP**으로 정지하고 **RESET**으로 최초 START 위치로 복귀합니다. Reset 뒤에는 다시 START가 필요합니다.
8. 같은 교차로에서 **D**로 회전하여 HEADING 약 **90°**에 맞춘 뒤 오른쪽 **GOAL B / EXAMINATION**으로 갑니다.
9. 이동 중 카메라 추적, MOVE / STEERING / HEADING 갱신, 버튼 작동 및 Console 빨간 오류 여부를 확인합니다.
10. 원래 맵을 보려면 Play를 종료하고 `Assets/Scenes/MainScene.unity`를 엽니다.

```text
GOAL A (-11.5,10) ←──── T (0,10) ────→ GOAL B (11.5,10)
 REHABILITATION             ↑               EXAMINATION
                           │ 10m
                           │
                       START (0,0)
```

- 복도 폭 5m, 직선 모듈 길이 5m, 교차로 8×8m, 벽 높이 2.6m.
- 전체 환경은 기존 40×40m Ground 안에 들어갑니다.
- 시작 Transform `(0,0.5,0)`, Y 회전 0°, 기존 휠체어 비주얼·카메라 설정 유지.
- 바닥 상면은 Ground보다 8mm 높습니다. 얇은 안내선과 표식은 그 위에 배치합니다.
- 출발은 초록, 두 도착 영역은 주황이며 A/B 및 방 이름으로 구분합니다.
- 문 3개, 벤치 1개, 안내 데스크 1개, 화분 1개. 소품은 교차로 뒤쪽 벽 가까이에 있습니다.
- 천장, 추가 Light, 환경 Collider, NavMesh, 자동문, 도착 판정, 자동 주행은 없습니다.
- GOAL과 문은 시각 요소입니다. 도착하면 직접 정지하세요. 벽 통과 방지는 기존과 마찬가지로 구현하지 않았습니다.
- 검증 경로는 안내선 중앙에서 멈춘 뒤 제자리 90° 회전입니다. 벽 가까이 붙어서 임의로 회전하는 모든 상황의 카메라 시야를 보장하지 않습니다.

## Python 제어

기존 TCP v0.1/v0.2 수신기와 수동/스트리밍 송신기를 그대로 사용합니다.
새 Scene에서 Control Source를 Python으로 선택하고 Play 후 기존 송신기를 실행합니다.

```powershell
Set-Location 'C:\Users\kjm03\OneDrive\Documents\UnityProjects\EEGWheelchairSimulator\Tools\PythonTestSender'
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1
```

연결되면 CONNECTION: CONNECTED를 확인하고 START를 누릅니다.
`forward`로 전진 → 교차로에서 `stop` → `left` 또는 `right`로 회전 → 목표 HEADING에서 `stop` → `forward` → GOAL에서 `stop` 순서입니다.

LEFT/RIGHT는 제자리 회전 명령을 유지합니다. 약 90°가 되면 STOP 또는 FORWARD 명령을 직접 보내야 합니다.
가짜 스트림은 통신/HUD 검증용이며 맵을 자동으로 주행하는 프로그램이 아닙니다.
STOPPED 동안에도 Prediction 수신·표시는 계속되며 실제 이동은 차단됩니다.
연결이 끊기면 현재 명령과 Prediction 표시가 초기화되고 정지합니다.

이 PC에서는 `py` 런처가 존재하지만 2026-09-28 검사에서 일반 Python 3 설치는 발견되지 않았습니다.
기존 런처는 Codex 내부 Python을 임시 fallback으로 사용합니다. 별도 설치나 환경 변경은 하지 않았습니다.
팀의 Python 3 환경에서는 표준 라이브러리만으로 `send_commands.py`를 실행할 수 있습니다.

## 추가 파일 / 재사용

- Scene: `Assets/Scenes/RehabJunctionScene.unity`
- 환경 Prefab: `Assets/Art/Prefabs/Environment/RehabJunction/RehabJunctionCourse.prefab`
- 모듈: 같은 폴더의 `Modules/`
  - `CorridorStraight5m.prefab`: 바닥 5×5m, 양옆 벽. 로컬 +Z 방향이 복도 진행 방향입니다.
  - `Junction8m.prefab`: 남·동·서쪽 입구를 가진 8×8m T자 구역.
  - `WallPanel2m.prefab`: 로컬 X 방향 길이 2m, 높이 2.6m. 루트 pivot은 바닥 높이이며 X 스케일로 길이를 조절할 수 있습니다.
  - `DecorativeDoor.prefab`: 패널·문틀·손잡이·작은 창. 앞면은 로컬 -Z, pivot은 바닥 중앙입니다.
- 생성 스크립트: `Assets/Editor/EEGWheelchair/RehabJunctionSetup.cs`
- 선택적 batch 검증/캡처: `Assets/Editor/EEGWheelchair/RehabJunctionValidation.cs`
- 문서: `Docs/RehabJunction-Guide.md`

환경 머티리얼은 기존 `Assets/Art/Materials/Environment`의 Floor / Wall / Door / Accent / Start / Goal / WallAccent / RouteGuide 8개를 읽어서 재사용합니다. 기존 머티리얼 값은 수정하지 않습니다. 공유 머티리얼을 사용하므로 이후 그 값을 직접 변경하면 두 맵에 모두 반영됩니다.
새 환경·모듈은 기존 IndoorTestCourse와 별도 Prefab이므로 새 모듈의 형태 변경은 기존 맵 geometry에 영향을 주지 않습니다.

## 생성 메뉴

**Tools → EEG Wheelchair → Create Rehab Junction Scene**

이미 연결된 결과가 있으므로 실행할 필요가 없습니다. 다시 실행하면 기존 새 Scene을 열고 종료하며 덮어쓰거나 중복 생성하지 않습니다.
새 Scene이 없을 때만 MainScene의 제어 구성을 복사하고, 복사본의 환경 인스턴스를 새 코스로 교체합니다. MainScene 원본과 기존 환경 Prefab은 수정하지 않습니다.
기존 에셋 경로에 유효한 새 모듈/코스 Prefab이 있으면 재사용하여 수동 편집을 보존합니다.
이전 단계 생성 메뉴를 다시 실행하지 마세요.

Editor 스크립트는 재생 중 필요하지 않습니다. 생성 메뉴/자동 검증이 더 필요 없다면 두 스크립트를 제거해도 저장된 Scene과 Prefab은 동작합니다.
이번 단계에서는 Build Settings 및 런타임 맵 선택 UI를 변경하지 않습니다. Unity Editor에서 Scene 파일을 열어 실행하세요.

## 검증 기록

2026-09-28, Unity 6000.3.24f1에서 다음을 확인했습니다.

- Unity C# 컴파일 및 batch Play Mode 검증 종료 코드 0.
- Scene 저장·재로드, 환경/중첩 모듈 Prefab 재로드, 메뉴 재실행 시 중복 및 덮어쓰기 없음.
- MainScene과 복사본의 기존 제어/HUD/카메라 객체 참조 그래프 일치.
- 기존 169개 파일(Assets / Packages / ProjectSettings / Docs / Tools)의 SHA-256이 작업 전후 동일. 기존 파일 수정/삭제 없음.
- 좌·우 경로 합계 1,470회의 60Hz 이동/카메라 갱신 검사: START→교차로→90° 회전→각 GOAL. 환경 geometry가 카메라와 휠체어 중심 사이를 가리지 않음. Reset 초기 pose/STOPPED 확인.
- 새 Scene에서 기존 Step07 Play 검증 재사용: 실제 New Input System 키 입력/버튼 클릭, START/STOP/RESET, HUD, 카메라.
- 실제 Python 수동 송신/가짜 prediction 스트림, v0.1/v0.2, STOPPED 차단과 prediction 갱신, 새 명령만 재시작, 접속 해제/재접속, 잘못된 NDJSON 처리 통과.
- 검증용 잘못된 메시지는 의도한 Warning으로 무시되었습니다. 컴파일 오류 및 검증 중 런타임 Error/Exception은 없었습니다.
- 1600×900 실제 Play Mode Game View 7장: 출발, 교차로 진입, 좌회전, GOAL A, 우회전, GOAL B, 전체 조감도. 기존 HUD를 포함해 시각 확인했습니다. 캡처용 위치/카메라 변경은 저장하지 않았습니다.

이번 검증은 코스 중심선과 정해진 회전 위치를 기준으로 합니다. 자유 주행 충돌 회피 검증이나 모델 성능 평가가 아닙니다.
