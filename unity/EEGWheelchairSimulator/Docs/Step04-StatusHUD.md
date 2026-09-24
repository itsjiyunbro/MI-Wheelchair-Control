# 4단계: 현재 휠체어 상태 HUD

## 구현 결과

MainScene 왼쪽 위에 다음 상태를 표시하는 HUD를 추가했다.

```text
EEG WHEELCHAIR DEMO
CONTROL: KEYBOARD
MOVE: STOPPED
STEERING: STRAIGHT
HEADING: 0°
```

기존 환경: Unity 6000.3.24f1, URP 17.3.0, Input System 1.20.0, New Input System 전용.
기존 WheelchairMovement에는 ApplyInput만 있었으며 외부 공개 상태는 없었다.
uGUI 2.0.0에 포함된 Text와 내장 LegacyRuntime.ttf를 사용했다.
TextMeshPro는 패키지에 포함되어 있지만 프로젝트에 필수 폰트 리소스가 없어 이번에는 추가 임포트 없이 구현했다.
추가 패키지를 설치하지 않았다. uGUI Text의 사용은 Legacy Input API 사용과 무관하다.

## 파일과 구조

수정:

- Assets/Scripts/Wheelchair/WheelchairMovement.cs: 읽기 전용 IsMoving, TurnInput, HeadingDegrees와 상태 기록 추가.
- Assets/Scenes/MainScene.unity: HUD 추가 및 레퍼런스 저장.

추가:

- Assets/Scripts/UI/WheelchairStatusUI.cs: 이동 컴포넌트의 상태를 표시.
- Assets/Editor/EEGWheelchair/Step04HudSetup.cs: 기존 Scene에 Canvas와 HUD 생성 및 연결.
- Assets/Editor/EEGWheelchair/Step04HudValidation.cs: 배치 실행용 상태·이동·카메라 검사와 선택적 렌더 미리보기.
- Docs/Step04-StatusHUD.md: 이 안내 문서.

```text
StatusCanvas  [Canvas, Canvas Scaler, Wheelchair Status UI]
└─ StatusPanel  [Image]
   ├─ Title
   ├─ ControlText
   ├─ MoveText
   ├─ SteeringText
   └─ HeadingText
```

Camera와 Cube를 포함한 기존 Scene 오브젝트/컴포넌트 설정은 보존했다.
KeyboardWheelchairInput과 CameraFollow 소스는 변경하지 않았다.

## 데이터 흐름

KeyboardWheelchairInput → WheelchairMovement → WheelchairStatusUI.

- 키보드 입력의 Update에서 기존 ApplyInput을 호출한다.
- 이동 컴포넌트가 그 프레임에 적용한 전진/조향 상태를 기록한다. 위치·회전 계산은 기존과 같다.
- HUD는 LateUpdate에서 상태 속성만 읽는다. Keyboard.current나 Input API를 읽지 않는다.
- 이번 프레임에 ApplyInput이 호출되지 않았으면 STOPPED/STRAIGHT를 표시한다.
- 이동 컴포넌트 비활성화, 적용 시간 0, 이동/회전 속도 0도 해당 동작이 적용되지 않은 상태로 표시한다.
- HEADING은 실제 Y축 회전을 정수로 반올림하고 360은 0으로 바꿔 0~359°만 표시한다.
- 각 행은 값이 바뀔 때만 텍스트를 대입한다. HEADING 문자열도 정수 각도가 바뀔 때만 생성한다.
- 연결된 이동 컴포넌트가 사라지면 정지 상태와 HEADING: --를 표시한다.

CONTROL은 입력원을 자동 감지하지 않는 표시용 설정이다.
Hierarchy > StatusCanvas > Wheelchair Status UI > Control Source Label의 기본값이 KEYBOARD다.
향후 Python 입력으로 바꿀 때 이 한 필드의 문구를 변경할 수 있다.
이후 입력 컴포넌트도 Unity 메인 스레드의 Update에서 현재 명령을 ApplyInput에 전달하면 HUD를 재사용할 수 있다.
실제 Python 통신 방식과 명령 유지 정책은 아직 구현하거나 확정하지 않았다.

## UI 배치

- Screen Space - Overlay Canvas: 카메라 회전과 무관하게 화면에 고정.
- Canvas Scaler: Scale With Screen Size, 기준 1600×900, Width/Height Match 0.5.
- 패널: 왼쪽 위 앵커/피벗, 여백 (24, 24), 크기 340×196 (기준 해상도 단위).
- 어두운 반투명 배경, 밝은 글자, 기본 글자 크기 20.
- 표시 전용으로 Raycast Target을 끄고 EventSystem/GraphicRaycaster는 추가하지 않았다.
- 향후 표시 항목은 StatusPanel 아래에 행을 추가하고 패널 높이를 늘리는 방식으로 확장할 수 있다.

## Unity에서 확인

1. 컴파일이 끝나면 Assets > Scenes > MainScene을 연다.
2. Hierarchy에서 StatusCanvas가 있는지 확인한다. 레퍼런스는 이미 연결되어 있으므로 수동 연결이 필요 없다.
3. Play를 누르고 Game 화면을 클릭해 키보드 포커스를 준다.
4. 시작 직후 CONTROL: KEYBOARD, MOVE: STOPPED, STEERING: STRAIGHT, HEADING: 0°를 확인한다.
5. 아래 키를 각각 테스트한다. 방향키 ↑/←/→도 같은 결과여야 한다.

| 입력 | MOVE | STEERING |
| --- | --- | --- |
| W | FORWARD | STRAIGHT |
| A | STOPPED | LEFT |
| D | STOPPED | RIGHT |
| W+A | FORWARD | LEFT |
| W+D | FORWARD | RIGHT |
| A+D | STOPPED | STRAIGHT |
| W+A+D | FORWARD | STRAIGHT |
| 모든 키 놓기 | STOPPED | STRAIGHT |

6. A/D로 회전하면서 HEADING이 변하는지 확인한다. +Z는 0°, +X는 90°, -Z는 180°, -X는 270°다.
7. 전진/회전/복합 입력 중에도 카메라가 계속 Cube 뒤쪽에서 정상 추적하는지 확인한다.
8. Game 화면을 16:9와 4:3으로 바꿔 패널이 왼쪽 위에 유지되고 글자가 읽히는지 확인한다.
9. Console에 빨간 오류, Missing Script, NullReferenceException이 없는지 확인한다.
10. Play를 종료한다. 영구적인 UI 설정 변경은 Play 종료 후 Scene을 저장한다.

## Editor 도구 사용/제거

메뉴: Tools > EEG Wheelchair > Step 4 - Create Status HUD.
이미 생성했으므로 다시 실행할 필요가 없다. 재실행하면 기존 HUD 레퍼런스를 확인하고
레이아웃/문구 설정을 유지한다. 같은 이름의 관련 없는 오브젝트가 있으면 덮어쓰지 않고 중단한다.
기존 Scene은 재생성하지 않으며 Step 1/2/3 구성 메뉴도 실행하지 않는다.
미저장 Scene 변경이 있으면 Unity 저장 확인 절차를 따른다.

완료 후 Step04HudSetup.cs와 Step04HudValidation.cs를 함께 제거해도 런타임 HUD는 작동한다.
WheelchairStatusUI.cs, WheelchairMovement.cs와 HUD Scene 오브젝트는 유지한다.

## 검증 결과와 한계

- Unity 6000.3.24f1에서 C# 컴파일 및 배치 종료 코드 0.
- MainScene 저장·재로드 후 이동 컴포넌트와 텍스트/폰트 레퍼런스 검사 통과.
- 전진·좌우·복합 입력에 대응하는 상태, 정지, 방향 반올림/순환, 비활성/이전 프레임/속도 0,
  CONTROL 문구 변경과 텍스트 영역 내 배치 검사 통과.
- 기존 30/120 FPS 전진 거리와 카메라 복합 이동 추적 검사 통과.
- 원래 Scene의 모든 오브젝트/컴포넌트/설정 블록을 비교해 보존 확인. SceneRoots에 HUD 루트만 추가됨.
- 기존 파일 중 MainScene.unity와 WheelchairMovement.cs만 변경됨.
- Unity에서 생성한 1600×900 렌더 미리보기로 배치와 가독성을 확인함.
  미리보기는 내보내기를 위해 일시적으로 Screen Space - Camera로 렌더했으며 저장된 Scene은 Overlay다.
- 실제 Play Mode 키보드 조작과 여러 Game 해상도의 수동 검사는 위 절차로 확인해야 한다.

## 참고

[Unity Canvas Scaler 설명](https://docs.unity3d.com/kr/2021.2/Manual/script-CanvasScaler.html).
실제 사용 API와 내장 폰트 이름은 설치된 uGUI 2.0.0의 Text.cs/CanvasScaler.cs에서도 확인했다.

이번 단계에는 버튼, 시작/정지/초기화, Python/Socket/JSON, 예측·confidence·실제 라벨·latency,
EEG 파형, 실제 휠체어 모델과 실내 환경을 추가하지 않았다.
