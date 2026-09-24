# 에셋 작업 2단계 — 실내 휠체어 테스트 코스

Unity Primitive와 6개 환경 머티리얼로 만든 정적 시연 코스입니다. 기존 Ground, 휠체어 루트/비주얼, 이동·입력·카메라·HUD·통신 코드는 보존합니다.

## Unity에서 확인하기

1. `Assets/Scenes/MainScene.unity`를 엽니다.
2. Hierarchy에서 **IndoorTestCourse**를 펼칩니다. Floors / Walls / Doors / Signs / StartArea / GoalArea / Props가 있습니다.
3. `SimulationController`의 **Control Source = Keyboard**를 확인합니다.
4. **Play**를 누릅니다. 초록 START 영역에 기존 휠체어가 있어야 합니다. 초기 상태는 STOPPED입니다.
5. **START 버튼**을 클릭하고 Game 화면을 클릭한 뒤 W로 직진합니다.
6. 바닥 화살표가 왼쪽으로 바뀌는 첫 넓은 구역의 중앙 `(X=0, Z=7)`에서 W를 놓습니다.
7. **A로 약 90° 좌회전**합니다. HUD HEADING 약 `270°`가 기준입니다. W로 약 9m 진행합니다.
8. 두 번째 넓은 구역의 중앙 `(X=-9, Z=7)`에서 W를 놓고 **D로 약 90° 우회전**합니다. HEADING 약 `0°`가 기준입니다.
9. W로 약 8m 전진하면 **주황 GOAL** `(X=-9, Z=15)`에 도착합니다. W를 놓거나 STOP 버튼을 눌러 정지합니다.
10. **RESET**을 누르면 원래 START 위치·방향·카메라로 복원되고 STOPPED가 됩니다.
11. 주행과 회전 중 카메라가 휠체어를 심하게 가리지 않는지, HUD가 계속 표시되는지 확인합니다.
12. Console에 빨간 오류가 없는지 확인합니다.

이동 속도 2m/s, 회전 속도 60°/s 기준으로 첫 직선 약 3.5초, 좌회전 약 1.5초, 두 번째 직선 약 4.5초, 우회전 약 1.5초, 마지막 직선 약 4초입니다. 시간보다 바닥 화살표와 HEADING을 기준으로 조작하세요.

```text
       GOAL (-9,15)
          ↑ 8m
          │
  RIGHT (-9,7) ←──── 9m ──── LEFT (0,7)
                                ↑ 7m
                                │
                           START (0,0)
```

START/GOAL은 시각 표시입니다. 자동 출발·도착 판정이나 자동 정지는 추가하지 않았습니다.

## 크기·배치

- Ground: 기존 원점의 40×40m Plane 보존.
- 기존 휠체어: 위치 `(0,0.5,0)`, 회전 `(0,0,0)`, 스케일 `(1,1,1.5)` 유지.
- 일반 복도: 폭 5m. 두 회전 구역: 각각 8×8m.
- 벽: 높이 2.6m, 두께 0.2m. 하단 0.18m 몰딩.
- 전체 환경은 기존 Ground 안에 들어갑니다. 천장은 없습니다.
- 바닥은 겹치지 않는 Cube 5개로 구성했습니다. 상면은 기존 Ground보다 8mm 높아 Z-fighting을 피합니다. 출발/도착 패드는 얇은 추가 표식입니다.
- 원래 Ground를 수정하거나 끄지 않았습니다. **IndoorTestCourse를 비활성화하면 기존 빈 테스트 공간으로 돌아갑니다.**
- 문 3개는 벽에 붙인 장식입니다. 벤치 1개와 작은 화분 1개는 코너 벽 가까이에 있습니다.
- START는 초록, GOAL은 주황입니다. 코너에는 LEFT/RIGHT 안내판과 바닥 화살표가 있습니다.
- 환경 글자는 기존 uGUI의 World Space Text입니다. 별도 패키지나 기존 StatusCanvas 변경은 없습니다.
- 기존 Directional Light(강도 2), URP/Global Volume을 그대로 사용하며 추가 조명·베이크·Reflection Probe는 없습니다.

## 카메라와 충돌 범위

Camera Follow offset `(0,4.5,-7)`과 기존 smoothing은 변경하지 않았습니다. 코너를 넓혀 카메라가 회전하며 벽 바깥을 지나더라도 휠체어를 볼 수 있는 배치를 사용했습니다.

검증 경로는 **바닥 경로의 중앙을 따라 이동하고, 넓은 코너 중앙에서 멈춰 90° 회전**하는 방식입니다. 임의 위치에서 벽에 붙어 회전하는 모든 경우를 보장하는 카메라 충돌 회피 기능은 아닙니다.

이번 환경에는 새 Collider/Rigidbody를 추가하지 않았습니다. 벽 통과 방지는 아직 없으며, Ground와 기존 휠체어 Collider는 그대로입니다. 이동 코드나 물리 제어는 변경하지 않았습니다.

## Python 모드

기존 Python 제어와 Prediction HUD를 그대로 사용할 수 있습니다. Control Source=Python → Play → 기존 송신기 실행 → START 순서입니다.

```powershell
Set-Location '<저장소 경로>\unity\EEGWheelchairSimulator\Tools\PythonTestSender'
# 수동 명령: 코너에서 LEFT/RIGHT를 보내는 시연에 사용
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1

# 기존 가짜 prediction 스트림 확인용
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1 -Stream
```

가짜 스트림은 고정 패턴이며 이 코스를 자동으로 따라가는 프로그램이 아닙니다. 경로 탐색이나 실제 모델 연결은 추가하지 않았습니다. 수동 송신기로 정해진 위치에서 명령을 보내거나 키보드로 코스를 확인하세요.

## 생성 에셋

- Prefab: `Assets/Art/Prefabs/Environment/IndoorTestCourse.prefab`
- Materials: `Assets/Art/Materials/Environment/`
  - `Floor.mat`, `Wall.mat`, `Door.mat`, `Accent.mat`, `Start.mat`, `Goal.mat`
- Editor Script: `Assets/Editor/EEGWheelchair/IndoorTestCourseSetup.cs`

모든 환경 오브젝트는 IndoorTestCourse 아래에 있습니다. 환경 머티리얼은 휠체어 머티리얼과 분리되어 있습니다. 글자는 Unity 내장 폰트/기본 UI 머티리얼을 사용합니다.

메뉴: **Tools → EEG Wheelchair → Create Indoor Test Course**

현재 Scene에는 이미 연결했습니다. 다시 실행할 필요가 없습니다.

- 같은 이름의 환경 Root가 있으면 안내하고 종료합니다. 중복 생성하거나 기존 편집을 덮어쓰지 않습니다.
- Root가 없고 Prefab이 있으면 기존 Prefab을 사용합니다.
- 처음 생성할 때만 Prefab/머티리얼을 만들고 현재 Scene에 추가합니다.
- 기존 Stage 1~7 또는 휠체어 비주얼 생성 메뉴를 실행하지 않습니다.
- Prefab Mode에서 환경을 편집할 수 있습니다. 생성 메뉴는 강제 재생성 버튼이 아닙니다.
- Editor Script는 런타임 의존성이 없으므로 추후 자동 생성 메뉴가 필요 없으면 제거해도 환경이 표시됩니다.

## 검증

Unity 컴파일, Scene/Prefab 저장·재로드, 재실행 시 중복 없음, Ground 범위 내 배치, 기존 기능 회귀 테스트와 렌더 미리보기를 확인합니다. 카메라 시야 검증은 기존 Movement와 Camera Follow 함수를 60Hz 간격으로 900회 진행하며 직선 3개와 회전 2개를 재현하고, 벽과 카메라→휠체어 중심 시선의 교차 여부를 검사합니다.

2026-09-21 Unity 6000.3.24f1에서 검증 종료 코드 0으로 통과했습니다.

- `INDOOR_ROUTE_OK`: 900회 경로·회전 검사에서 벽이 카메라→휠체어 중심 시선을 가리지 않음.
- `INDOOR_SCENE_OK`: Scene/Prefab 재로드, 중복 없음, 전체 환경 Ground 범위 내 배치.
- `STEP07_SOCKET_OK`, `STEP07_PREDICTION_OK`, `STEP07_PLAY_OK`: 실제 Python 프로세스, 키보드/마우스 입력, 통신·Prediction HUD·카메라·Start/Stop/Reset 유지.
- 출발·좌회전·우회전·도착·전체 배치와 기존 HUD가 포함된 Game 렌더를 확인했습니다.
- 파일 해시/Scene 객체 비교에서 기존 C#·Python·휠체어 에셋·설정은 모두 동일했습니다. 기존 Scene 객체는 수정하지 않고 환경 Prefab과 Scene 루트 목록만 추가했습니다.

이 검증은 자동 주행 기능을 추가한 것이 아닙니다. 경로 검증 코드는 Editor 검증용이며 일반 Play에서는 실행되지 않습니다.
