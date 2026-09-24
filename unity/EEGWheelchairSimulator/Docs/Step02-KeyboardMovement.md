# 2단계: 임시 휠체어 키보드 이동 및 회전

## 현재 환경과 변경 범위

Unity 6000.3.24f1 / URP 17.3.0 / Input System 1.20.0.
New Input System만 활성화되어 있으며 기존 MainScene의 오브젝트 5개를 유지한다.
WheelchairPlaceholder는 위치 (0, 0.5, 0), 크기 (1, 1, 1.5)의 파란 Cube다.
기존 Transform, MeshFilter, MeshRenderer, BoxCollider에 아래 컴포넌트 2개만 추가한다.

- Assets/Scripts/Wheelchair/WheelchairMovement.cs: XZ 이동과 Y축 회전 담당.
- Assets/Scripts/Wheelchair/KeyboardWheelchairInput.cs: New Input System 키보드 입력 담당.
- Assets/Editor/EEGWheelchair/Step02KeyboardSetup.cs: 기존 Cube에 컴포넌트를 연결하는 Editor 전용 도구.
- Assets/Scenes/MainScene.unity: 위 두 컴포넌트 연결을 저장.
- Docs/Step02-KeyboardMovement.md: 이 안내 문서.

추가 패키지와 Input Actions Asset은 없다. SampleScene, 1단계 생성 도구, 머티리얼,
고정 카메라, 조명, Global Volume, Input Actions, Build Settings를 유지한다.

## 조작

| 키 | 동작 |
| --- | --- |
| W 또는 ↑ | 누르는 동안 현재 바라보는 방향으로 전진 |
| A 또는 ← | 누르는 동안 좌회전, 제자리 회전 가능 |
| D 또는 → | 누르는 동안 우회전, 제자리 회전 가능 |
| W + A / D | 전진하면서 좌/우 회전 |
| 전진 키 모두 놓기 | 전진 정지. 회전 키가 눌려 있으면 제자리 회전은 계속됨 |
| 좌우 키 동시에 누르기 | 회전 상쇄. 전진 키가 눌려 있으면 직진 |
| 모든 키 놓기 | 이동 및 회전 정지 |

W와 ↑를 동시에 눌렀다면 둘 다 놓아야 전진이 멈춘다. 후진은 구현하지 않았다.
방향키와 WASD는 같은 동작이며 중복 입력으로 속도가 증가하지 않는다.

## Unity에서 실행 확인

1. Unity 6000.3.24f1로 프로젝트를 열고 컴파일과 임포트가 끝날 때까지 기다린다.
2. Project 창에서 Assets > Scenes > MainScene을 더블클릭한다.
3. Hierarchy에서 WheelchairPlaceholder를 선택한다. Inspector에 Wheelchair Movement와
   Keyboard Wheelchair Input이 각각 1개씩 있고 활성화되어 있는지 확인한다.
4. Play 버튼을 누르고 Game 탭의 화면을 클릭하여 키보드 입력 포커스를 준다.
5. W 또는 ↑를 약 1초 누르면 Cube가 앞쪽으로 약 2 m 이동한다. 키를 놓으면 바로 멈춘다.
6. A/←, D/→를 각각 약 1초 누르면 위치를 유지하면서 약 60도 좌/우 회전한다.
7. 회전 후 W를 누르면 Cube가 새로 바라보는 방향으로 전진한다.
8. W+A 또는 W+D를 눌러 전진과 회전을 함께 확인한다. A+D는 회전하지 않아야 한다.
9. Inspector의 Transform에서 Position Y가 0.5로 유지되는지 확인한다.
10. Console에 빨간 오류, Missing Script, NullReferenceException 또는 Legacy Input 관련 오류가 없는지 확인한다.
11. Play를 종료하면 Scene에 저장된 시작 위치로 돌아온다.

카메라는 고정되어 있으므로 오래 전진하면 Cube가 화면 밖으로 나갈 수 있다.
짧게 입력해 확인하거나 Play를 종료하고 다시 시작한다.

## Inspector 속도 조절

WheelchairPlaceholder > Wheelchair Movement:

- Move Speed: 기본 2 (m/s)
- Turn Speed: 기본 60 (도/s)

두 필드는 SerializeField와 Min(0)으로 노출한다. 계속 사용할 값은 Play를 종료한 상태에서
변경하고 Scene을 저장한다. Play 중 변경한 값은 일반적으로 종료 후 되돌아간다.

## 구조와 이동 방식

KeyboardWheelchairInput.Update가 Keyboard.current의 isPressed를 읽고
WheelchairMovement.ApplyInput(전진 여부, 회전 입력, Time.deltaTime)을 매 프레임 호출한다.
회전 입력은 왼쪽 -1, 정지 0, 오른쪽 +1이다. 명령을 저장하지 않고 호출된 프레임만 이동하므로
키 해제나 입력 컴포넌트 비활성화 후 이전 명령으로 계속 움직이지 않는다.
Keyboard.current가 null이면 입력 처리를 건너뛴다.

전진과 회전의 동시 입력을 간단히 표현하기 위해 지금은 enum을 추가하지 않았다.
나중에 Python 입력 컴포넌트가 같은 이동 메서드를 호출할 수 있다. 그때 키보드 컴포넌트를
비활성화하여 입력 소스가 하나만 이동을 지시하도록 한다. 소켓 콜백에서 Transform을 직접
조작하지 않고 Unity 메인 스레드에서 처리하도록 연동 단계에 구현한다.

현재는 평평한 바닥의 이동 테스트이므로 Rigidbody 없이 Transform을 사용한다.
Time.deltaTime으로 초당 속도를 계산하고, Y 위치를 유지하며 XZ 평면에서 이동한다.
기존 Collider는 유지하지만 물리 충돌에 의한 벽 막힘, 경사면, 낙하, Ground 경계 제한은 없다.

## 자동 연결 도구

메뉴: Tools > EEG Wheelchair > Step 2 - Connect Keyboard Movement

이번 작업에서 이미 연결하므로 사용자가 수동으로 컴포넌트를 붙일 필요가 없다.
재실행해도 컴포넌트를 중복 추가하거나 기존 속도 값을 초기화하지 않는다.
MainScene이나 Cube가 없거나 Cube가 여러 개라면 오류로 중단하고 새로 만들지 않는다.
Rigidbody가 있으면 Transform 이동과의 충돌 가능성을 검토하도록 중단한다.
미저장 Scene 변경이 있으면 Unity의 저장 확인 절차를 따른다.
완료 후 이 Step 2 Editor Script만 제거해도 실행 가능하다. 런타임 스크립트 두 개는 유지해야 한다.

## 검증 범위

자동 구성 시 Scene 저장 및 재로드, 컴포넌트 중복/활성 상태를 검사한다.
배치 실행에서는 임시 오브젝트로 30/120 FPS의 1초 전진 거리와 좌/우 회전각,
입력 해제 시 정지, 회전 후 전진 방향, Y 유지, 이동 컴포넌트 비활성화를 확인한다.
이 검사는 실제 키보드 입력이나 Play/Game 화면의 시각 검사를 대체하지 않는다.

## API 근거

- Keyboard API는 설치된 Input System 1.20.0의 Runtime/Devices/Keyboard.cs와
  Runtime/Controls/ButtonControl.cs에서 확인했다.
- [Unity 6.3 Time.deltaTime](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-deltaTime.html)

이번 단계에는 카메라 추적, UI, 버튼, Python 소켓, JSON, confidence, 실제 휠체어 모델,
실내 환경을 구현하지 않는다. Python 통신 규격과 모델 명령의 의미는 아직 확정하지 않는다.
