# 5단계: Start / Stop / Reset과 시뮬레이션 상태

## 구현 결과

Play 시작 상태는 STOPPED다. START를 클릭한 뒤에만 기존 키보드 이동과 회전이 적용된다.
STOP은 현재 위치·방향을 유지하고 입력 상태를 지운다.
RESET은 실행 시작 시 저장한 위치와 Y축 방향으로 복원하며 STOPPED 상태로 돌아간다.
Reset 직후 기존 CameraFollow.SnapToTarget을 호출해 카메라도 바로 추적 위치로 맞춘다.

기존 Unity 6000.3.24f1 / URP 17.3.0 / Input System 1.20.0 / uGUI 2.0.0을 사용한다.
추가 패키지 설치, Rigidbody 추가, Python 통신, 자동 전진은 없다.

## 코드 구조

```text
KeyboardWheelchairInput.Update
    ↓ ApplyInput
WheelchairMovement ← SimulationController.IsRunning
    ↓ 읽기 전용 상태
WheelchairStatusUI

START / STOP / RESET → SimulationController
RESET → CameraFollow.SnapToTarget
```

- SimulationController: Awake에서 최초 위치·Y축 방향을 저장하고 STOPPED로 시작한다.
  StartSimulation, StopSimulation, ResetSimulation 메서드와 읽기 전용 IsRunning을 제공한다.
- WheelchairMovement: 모든 입력원이 사용하는 ApplyInput에서 실행 여부를 검사한다.
  Controller가 없거나 실행 중이 아니면 이동과 회전을 적용하지 않는다.
  ClearInputState는 Forward/Turn 상태를 즉시 지운다. 기존 이동 계산과 속도는 같다.
- WheelchairStatusUI: 기존 네 항목에 SIMULATION 표시를 추가한다.
  STOPPED에서는 Start 활성/Stop 비활성, RUNNING에서는 반대로 표시한다. Reset은 항상 사용 가능하다.
- KeyboardWheelchairInput과 CameraFollow의 소스는 변경하지 않았다.
- Time.timeScale과 카메라/HUD 컴포넌트를 중지하지 않으므로 Stop 중에도 추적과 표시가 갱신된다.
- Start를 클릭할 때 W/A/D를 이미 누르고 있다면 현재 눌린 입력이 적용된다. 입력 없는 자동 전진은 없다.

향후 Python 입력도 ApplyInput을 통해 전달해야 동일한 실행 상태 차단을 적용받는다.
이번 단계에서는 Python 통신이나 통신 규격을 구현하지 않았다.

## 파일 변경

추가:

- Assets/Scripts/Simulation/SimulationController.cs
- Assets/Editor/EEGWheelchair/Step05SimulationSetup.cs
- Assets/Editor/EEGWheelchair/Step05SimulationValidation.cs
- Docs/Step05-SimulationControls.md

수정:

- Assets/Scripts/Wheelchair/WheelchairMovement.cs
- Assets/Scripts/UI/WheelchairStatusUI.cs
- Assets/Scenes/MainScene.unity

## Scene/UI 구성

- 새 루트 SimulationController에 이동 컴포넌트와 CameraFollow를 연결했다.
- 기존 StatusCanvas/StatusPanel을 보존하고 패널 높이를 286으로 늘렸다.
- SimulationText와 StartButton / StopButton / ResetButton을 추가했다.
- 기존 네 텍스트는 아래로 한 줄씩 이동했으며 왼쪽 위 앵커와 CanvasScaler는 유지했다.
- StatusCanvas에 GraphicRaycaster를 추가했다.
- EventSystem은 기존에 없었으므로 1개 생성하고 InputSystemUIInputModule을 연결했다.
- 기존 Assets/InputSystem_Actions.inputactions의 UI 액션을 참조한다. 원본 액션 파일은 수정하지 않았다.
- StandaloneInputModule은 사용하지 않는다. 버튼은 마우스로 클릭하며 WASD/방향키 UI 탐색은 끈 상태다.
- 버튼에는 Controller 메서드와 HUD 즉시 갱신 호출을 저장해 수동 연결이 필요 없다.

## Unity에서 직접 확인

1. 프로젝트를 열고 컴파일이 끝날 때까지 기다린다.
2. Assets > Scenes > MainScene을 연다.
3. Play를 누르고 Game 화면에서 검사한다.

### 테스트 1: 초기 상태

```text
SIMULATION: STOPPED
MOVE: STOPPED
STEERING: STRAIGHT
```

Start와 Reset은 활성, Stop은 비활성이다. W/A/D 또는 방향키를 눌러도 Cube가 움직이거나 회전하지 않아야 한다.

### 테스트 2: Start

START를 클릭하고 W를 누른다.
SIMULATION: RUNNING, MOVE: FORWARD가 표시되고 Cube가 전진해야 한다.
A/D 제자리 회전과 W+A / W+D 복합 이동도 확인한다. 카메라는 계속 따라와야 한다.

### 테스트 3: 이동 중 Stop

W 또는 W+D를 누르는 동안 STOP을 클릭한다.
Cube가 클릭 시점의 위치·방향에 멈추고 STOPPED / STOPPED / STRAIGHT로 표시되어야 한다.
키를 계속 눌러도 추가 이동이나 회전이 없어야 한다. 카메라는 잠시 따라잡은 뒤 안정된다.

### 테스트 4: Stop 상태 입력 차단

STOPPED에서 W/A/D를 다시 눌러도 위치·방향이 변하지 않아야 한다.

### 테스트 5: 이동 후 Reset

START → 이동/회전 → RESET 순서로 조작한다.
Cube가 Play 시작 시의 위치와 방향으로 돌아오고 카메라도 즉시 정상 추적 위치로 복원되어야 한다.
SIMULATION: STOPPED, MOVE: STOPPED, STEERING: STRAIGHT가 표시되어야 한다.
HEADING은 최초 방향으로 복원된다. 최초 방향을 Scene에서 바꾸면 0°가 아닐 수 있다.

### 테스트 6: Reset 후 입력 차단

RESET 후 W를 누른다. 다시 START를 클릭하기 전에는 움직이지 않아야 한다.

### 테스트 7: 오류 확인

Console에 빨간 오류, Missing Script, NullReferenceException 또는 Legacy Input 관련 오류가 없는지 확인한다.

### 선택 확인: 다른 시작 위치

Play를 종료하고 Cube의 시작 위치와 Y축 회전을 바꾼 뒤 Scene을 저장한다.
다시 Play → START → 이동 → RESET하면 새로 지정한 시작 위치·방향으로 돌아와야 한다.
Play 중 바꾼 현재 위치는 Reset 기준 위치로 다시 저장되지 않는다.

## 자동 연결 도구

메뉴: Tools > EEG Wheelchair > Step 5 - Connect Simulation Controls.
이미 연결되어 있으므로 다시 실행할 필요가 없다.
재실행 시 기존 연결을 검사하고 레이아웃과 설정을 보존한다.
MainScene을 새로 만들지 않으며 Step 1~4 생성 도구를 실행하지 않는다.
동일 이름의 충돌하는 UI나 중복 EventSystem 등이 있으면 덮어쓰지 않고 중단한다.

생성 후 Step05SimulationSetup.cs와 Step05SimulationValidation.cs를 함께 제거해도 런타임 기능은 유지된다.
SimulationController.cs, 이동/HUD 런타임 코드와 Scene에 연결된 오브젝트는 유지해야 한다.

## 완료한 검증

- Unity C# 컴파일 성공.
- MainScene 저장·재로드 후 상태 관리자, 이동 차단, HUD, 버튼 이벤트, Raycaster, UI Input Module 연결 검사 통과.
- 실제 Play Mode에서 가상 키보드와 마우스 장치로 입력을 보냈다.
  마우스 이벤트가 InputSystemUIInputModule과 EventSystem을 거쳐 버튼을 클릭하는 경로까지 검사했다.
- 시작 시 입력 차단, Start 후 전진/좌우/복합 입력, Stop 후 키 유지 차단, Reset 및 재시작 전 차단 통과.
- 입력 API를 직접 호출하는 별도 명령도 STOPPED에서 차단되는 것을 확인했다.
- HUD 상태와 버튼 interactable, 카메라 시야 유지 및 Reset 시 즉시 복원 통과.
- 시작 위치 (3, 0.75, -2), Y 회전 37°인 임시 오브젝트로 초기 Transform 동적 저장/복원 통과.
- 최종 Play 검증에서 Runtime Console 오류 없음, 배치 종료 코드 0.
- 최종 렌더 미리보기로 버튼/문구 배치 확인.
- 기존 파일 중 MainScene, WheelchairMovement, WheelchairStatusUI만 변경됨.
  키보드/카메라 소스, 패키지, ProjectSettings와 Input Actions 파일의 해시 보존 확인.

숨겨진 배치 Editor는 Game View 포커스가 없으므로 자동 검사 중에만 포커스와 무관하게
Game View로 입력을 보내도록 설정했다. 이 설정은 프로젝트 파일에 저장하지 않았다.
미리보기 이미지는 기존 도구로 일시적인 Screen Space - Camera 렌더를 내보낸 것이며,
저장된 HUD는 Screen Space - Overlay를 유지한다.

실제 사용자 마우스와 키보드의 조작감은 위 테스트 순서로 확인한다.

## API 확인 근거

설치된 Input System 1.20.0의 InputSystemUIInputModule.cs와 InputActionImporter.cs에서
UI 모듈 및 영구 참조 가능한 InputActionReference를 확인했다.
[Unity UI와 Input System](https://docs.unity3d.com/ja/Packages/com.unity.inputsystem%401.4/manual/UISupport.html)은
UI 모듈의 역할을 설명하는 참고 자료이며, 구현 호환성은 설치된 버전의 코드와 실제 실행으로 검사했다.

5단계까지만 구현했다. Python/Socket/JSON, EEG 관련 표시와 분류 정책은 추가하지 않았다.
