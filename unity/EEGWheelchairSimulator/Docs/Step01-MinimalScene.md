# 1단계: 최소 Scene 준비

## 확인한 프로젝트 상태 (2026-09-15)

- Unity 6000.3.24f1 / Universal 3D 템플릿
- URP 17.3.0 / Input System 1.20.0 (manifest, lock 파일 및 설치된 패키지 확인)
- ProjectSettings.asset: activeInputHandler = 1, New Input System 사용
- Assets/InputSystem_Actions.inputactions: 기본 Player/UI 액션 존재
- 원본 Assets/Scenes/SampleScene.unity: Main Camera, Directional Light, Global Volume
- Build Settings에는 현재 SampleScene만 등록되어 있음. 이번 단계는 Editor Play 확인용이므로 유지함.

## 생성되는 Scene

Assets/Scenes/MainScene.unity

| 오브젝트 | 설정 |
| --- | --- |
| Ground | Plane, 위치 (0, 0, 0), Scale (4, 1, 4), 실제 크기 40 × 40, 기본 MeshCollider |
| WheelchairPlaceholder | Cube, 위치 (0, 0.5, 0), Scale (1, 1, 1.5), 기본 BoxCollider |
| Main Camera | 위치 (0, 7, -10), Cube 중심을 바라봄, FOV 60, 고정 카메라 |
| Directional Light | SampleScene의 URP 조명 유지 |
| Global Volume | SampleScene의 프로필 참조 유지 |

Unity 1 unit을 앞으로 1 m 기준으로 사용한다. Cube의 앞 방향은 +Z다.
Ground는 회녹색, Cube는 파란색 URP Lit 머티리얼을 사용한다.
이번 단계에는 Rigidbody, 이동, 키보드 입력, 카메라 추적, UI, Python 통신을 추가하지 않는다.
Play 중 Cube가 정지해 있는 것이 정상이다.

## Unity에서 확인

1. Unity Hub에서 이 프로젝트를 Unity 6000.3.24f1로 연다.
2. 컴파일과 임포트가 끝날 때까지 기다린다.
3. Project 창에서 Assets > Scenes > MainScene을 더블클릭한다.
4. Hierarchy에 위의 오브젝트 5개가 있는지 확인한다.
5. Play를 누르고 Game 탭에서 바닥 위 파란 Cube가 보이는지 확인한다.
6. Console에서 빨간 오류가 없는지 확인하고 Play를 종료한다.

## Editor Script 사용과 제거

- 파일: Assets/Editor/EEGWheelchair/MainSceneSetup.cs
- 실행 메뉴: Tools > EEG Wheelchair > Step 1 - Create MainScene
- 이미 MainScene이 생성되어 있다면 다시 실행할 필요가 없다.
- MainScene 또는 해당 .meta가 있으면 아무것도 덮어쓰지 않는다.
- 최초 실행은 SampleScene을 복사하고 두 도형 및 머티리얼을 생성한다.
- 원본 SampleScene, Input Actions, 패키지, Build Settings는 편집하지 않는다.
- 열린 Scene에 미저장 변경이 있으면 Unity의 저장 확인 창이 표시된다. Cancel로 취소할 수 있다.
- 생성 후에는 Assets/Editor/EEGWheelchair 폴더를 Unity Project 창에서 제거해도 MainScene이 작동한다. Scene/머티리얼은 유지한다.
- 자동 생성은 Unity Editor API를 사용한다. Scene YAML을 직접 작성하지 않는다.

## 다음 단계에서 결정할 사항

키보드 이동 테스트를 시작하기 전에 전진 방식, 키를 누르는 동안의 동작,
좌우 회전 방식을 결정한다. 그때 New Input System 입력과 이동 처리를 분리한다.
LEFT/RIGHT/FORWARD/STOP/IDLE 제어 명령의 세부 의미는 아직 구현하거나 확정하지 않았다.
Python 소켓의 TCP/UDP, 포트, JSON 필드, confidence, IDLE, 연결 끊김 처리는 팀 결정 전이다.

## API 참고

- Unity 6.3 Editor 명령행 실행: https://docs.unity.cn/6000.3/Documentation/Manual/EditorCommandLineArguments.html
