# 기본 벽 통과 방지 — 두 맵 공통

MainScene과 RehabJunctionScene에 적용했습니다. 이동은 계속 Transform 기반이고 Rigidbody / CharacterController / NavMesh를 추가하지 않았습니다.
기존 키보드/Python 입력, SimulationController, TCP 프로토콜, 카메라, HUD 디자인과 버튼 이벤트는 유지합니다.

## 직접 확인

### Keyboard

1. `Assets/Scenes/MainScene.unity` 또는 `Assets/Scenes/RehabJunctionScene.unity`를 엽니다. 두 Scene을 동시에 Additive로 열지 않습니다.
2. SimulationController의 Control Source를 Keyboard로 설정하고 Play합니다.
3. 초기 STOPPED에서 W/A/D가 이동·회전을 만들지 않는지 확인합니다.
4. START → D로 HEADING 약 90°에 맞춤 → W를 계속 누릅니다.
5. 오른쪽 벽/문 앞에서 정지해야 합니다. W를 계속 눌러도 통과하거나 조금씩 밀려들지 않아야 합니다.
6. SIMULATION은 RUNNING이지만 실제 이동이 없으므로 MOVE: STOPPED, STEERING: STRAIGHT입니다.
7. W를 놓고 A 또는 D로 HEADING 약 270°에 맞춘 뒤 W를 누르면 벽에서 멀어집니다.
8. STOP → 위치 유지, RESET → 최초 위치·방향·카메라 복원 및 STOPPED를 확인합니다.
9. START 후 원래 바닥 안내선을 따라 코스를 주행해 정상 통과되는지 확인합니다.

재활센터 맵에서는 교차로 `(0,10)`에서 우회전 후 GOAL B까지 W를 계속 눌러 닫힌 문 앞에서 정지하는 것도 확인하세요. GOAL은 여전히 시각 표시이며 자동 도착 판정은 없습니다.

### Python

1. Play 전 Control Source = Python → Play → 기존 수동 송신기를 실행합니다.
2. CONNECTED → Unity START → `right`로 벽 쪽을 향하게 합니다. 목표 HEADING에서 `stop`을 보냅니다.
3. `forward`를 보내고 벽 앞에서 자동 정지할 때까지 기다립니다.
4. 정상 표시:

```text
SIMULATION: RUNNING
CONTROL: PYTHON
CONNECTION: CONNECTED
PREDICTION: FORWARD
CONFIDENCE: 87.0%       (기존 수동 송신기의 테스트 값)
MOVE: STOPPED
STEERING: STRAIGHT
```

Prediction은 수신한 데이터, MOVE는 실제 이동 여부이므로 위 조합이 정상입니다.
`left`/`right`로 방향을 바꾼 뒤 `forward`로 벽에서 벗어날 수 있습니다. 회전은 다음 명령 전까지 지속됩니다.
Unity STOP/RESET은 여전히 입력을 제거합니다. START 직후 예전 Python 명령을 다시 실행하지 않습니다.
연결 해제 시 정지 및 재접속도 기존과 같습니다.

```powershell
Set-Location 'C:\Users\kjm03\OneDrive\Documents\UnityProjects\EEGWheelchairSimulator\Tools\PythonTestSender'
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1
```

기존 런처와 Python 환경은 변경하지 않았습니다. 이 PC의 일반 Python 3 미설치 시 기존 Codex 내부 Python fallback을 사용합니다.

## 작동 방식과 조절 값

`Keyboard / Python → WheelchairMovement → WheelchairCollisionGuard → 허용 거리만 이동`

- `WheelchairCollisionGuard`는 이번 프레임에 이동할 거리 전체를 SphereCast로 검사합니다. 높은 이동 속도/긴 프레임에서도 얇은 벽을 건너뛰지 않게 합니다.
- 캐스트 시작점이 이미 벽과 겹치면 CheckSphere가 감지하여 전진을 차단합니다. 수동으로 벽 안에 배치한 상태의 자동 밀어내기는 구현하지 않았으므로 정상 START로 RESET하거나 Play를 종료하고 초기 배치를 수정하세요.
- 새 `WheelchairObstacle` Layer에 있는 벽과 문만 검사합니다. 바닥, 원래 휠체어 BoxCollider, UI, Trigger는 제외합니다.
- autoSyncTransforms 설정을 바꾸지 않고 검사 직전에 Physics.SyncTransforms를 호출합니다.
- 기존 루트 스케일 `(1,1,1.5)`과 무관하게 **월드 단위**로 반경을 사용합니다.
- 원형 여유 공간은 휠체어 전체 비주얼과 발판의 회전 범위를 포함합니다. 방향을 바꾸어도 범위가 같아서 정상 주행으로 접근한 벽 앞에서는 제자리 회전이 가능합니다.

Inspector → WheelchairPlaceholder → Wheelchair Collision Guard:

| 항목 | 초기 값 | 의미 |
|---|---:|---|
| Obstacle Layers | WheelchairObstacle | 벽·닫힌 문만 선택 |
| Clearance Radius | 1.15m | 회전을 포함한 수평 여유 반경 |
| Query Height | 0.2m | 루트 위 검사 높이. 현재 월드 Y=0.7m |
| Skin Width | 0.025m | 검사 경계와 벽 사이 간격 |

선택 시 Scene View에 청록 원이 표시됩니다. Game View에는 표시되지 않습니다.
원형 범위는 휠체어의 좁은 옆면보다 넓어서 벽에 눈에 보이는 여유를 두고 멈춥니다. Skin Width가 비주얼과 벽 사이의 정확한 간격을 뜻하지는 않습니다.
현재 5m 복도에서는 통행과 회전 공간이 충분합니다. 모델 크기를 변경하면 반경도 검토하세요.

움직일 수 있는 거리만 이동하고, 실제 위치가 변한 프레임에만 `IsMoving`이 true입니다. 막힌 다음 프레임부터 HUD가 STOPPED를 표시합니다. HUD 스크립트에서 prediction으로 이동 상태를 추론하지 않습니다.
충돌 때문에 Simulation 상태를 STOPPED로 바꾸지는 않습니다. 유효한 전진 명령을 유지한 채 사용자가 방향을 바꾸거나 장애물을 제거하면 다시 이동할 수 있습니다.

## 추가·수정 파일

- 신규 런타임: `Assets/Scripts/Wheelchair/WheelchairCollisionGuard.cs`
- 수정 런타임: `Assets/Scripts/Wheelchair/WheelchairMovement.cs` — 허용 이동 거리와 실제 이동 상태만 연결.
- 신규 Editor: `Assets/Editor/EEGWheelchair/WallCollisionSetup.cs`, `WallCollisionValidation.cs`
- 기존 검증 수정: `IndoorTestCourseSetup.cs`, `RehabJunctionValidation.cs` — 이전 에셋 단계의 'Collider가 0개' 조건을 현재 단계에 맞게 조정. 생성 로직은 그대로입니다.
- Scene: `Assets/Scenes/MainScene.unity`, `Assets/Scenes/RehabJunctionScene.unity` — 루트에 guard 추가, Movement 참조 연결.
- Prefab: `Assets/Art/Prefabs/Environment/IndoorTestCourse.prefab`의 Walls/Wall_* 및 Doors/*/Panel.
- Prefab: `Assets/Art/Prefabs/Environment/RehabJunction/Modules/WallPanel2m.prefab`의 WallBody, `DecorativeDoor.prefab`의 Panel. 중첩 Prefab을 사용하는 재활센터 코스에 자동 반영됩니다.
- 설정: `ProjectSettings/TagManager.asset` — 비어 있던 사용자 Layer에 WheelchairObstacle 추가. 기존 Layer/Physics 설정 유지.

벽·문에는 각 Cube Mesh에 맞는 비 Trigger BoxCollider만 추가했습니다. Renderer, Transform, Material, 기존 루트 BoxCollider는 변경하지 않았습니다.

## 설정 메뉴

**Tools → EEG Wheelchair → Enable Wall Collision In Both Maps**

두 Scene에 이미 연결했으므로 실행할 필요 없습니다. 재실행해도 guard/Collider를 중복 생성하지 않으며, 기존 guard의 반경·높이·간격 조절값은 유지합니다.
기존 맵을 새로 만들거나 이전 단계 생성 스크립트를 실행하지 않습니다. 작업 전에 열려 있던 Scene 구성을 복원합니다.
Prefab의 벽/문 형태를 편집했다면 해당 BoxCollider도 함께 확인하세요. 새 맵/임의 이름의 새 오브젝트까지 자동 탐색하는 생성기는 아닙니다.

## 범위와 제한

현재의 평평한 바닥과 높은 정적 벽/닫힌 문을 위한 기본 이동 제한입니다.
벤치·화분·안내 데스크는 이번 충돌 대상에 포함하지 않습니다.
경사로, 계단, 자동 회피, 벽 미끄러짐, 후진, 이동 장애물의 밀어내기, 카메라 충돌 회피는 추가하지 않았습니다.
벽 방향으로 대각선 전진하다 막히면 W를 놓고 방향을 조절한 뒤 다시 전진하세요.

## 검증

2026-09-28, Unity 6000.3.24f1에서 검증했습니다.

- Unity 컴파일과 두 Scene의 최종 Play Mode 검사 종료 코드 0.
- 두 번 설정 후 Scene/Prefab/Layer 파일이 동일하여 중복 생성 및 설정 누적 없음.
- Scene 저장/재로드 후 벽·문 Collider와 Movement→Guard 참조 정상.
- 두 맵의 전체 휠체어 비주얼 Mesh bounds가 반경 1.15m 안에 포함되는지 확인.
- 30/60/120 FPS에서 벽 앞 정지 위치 일치, 장시간 전진 입력에도 위치 유지.
- 제자리 360° 회전 후 벽 반대쪽으로 전진하여 탈출 가능.
- 대각선/큰 deltaTime 이동과 두께 1cm 시험 벽에서 통과 방지 확인.
- Trigger/다른 Layer 제외, cast 시작점 겹침 시 전진 차단 확인.
- 두 맵의 기존 코스 중심선 주행과 카메라 시야 검사 통과.
- 두 맵에서 기존 Step07 실제 키보드·마우스 버튼·Python 수동/스트림 송신·재접속 검증 통과.
- 두 맵에서 실제 TCP FORWARD 수신 유지 중 벽에 막히면 Prediction=FORWARD / MOVE=STOPPED, 이후 STOP/START/RESET 정상.
- 재활센터 문 앞 정지 상태의 실제 Game View 캡처 및 시각 확인 완료. 초기 batch 캡처 실패는 검증 도구의 Game View 준비 설정으로 수정했으며 게임 코드는 변경하지 않았습니다.
- 직렬화 파일 비교: 기존 오브젝트 제거 없음. 기존 Transform/Renderer/Camera/UI/Prefab 참조 및 다른 런타임 컴포넌트 값 동일. Scene은 루트 컴포넌트 추가와 Movement의 Guard 참조만 변경했습니다.
- 최종 검사에서 컴파일 오류/런타임 Error·Exception 없음. 의도적으로 보낸 잘못된 TCP 메시지는 기존 Warning 처리로 무시됩니다.

## 사용한 Unity API

- [SphereCast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.SphereCast.html)
- [CheckSphere](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.CheckSphere.html)
- [SyncTransforms](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.SyncTransforms.html)
