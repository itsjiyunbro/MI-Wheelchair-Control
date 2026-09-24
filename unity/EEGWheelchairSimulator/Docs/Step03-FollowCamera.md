# 3단계: 기본 3인칭 추적 카메라

## 검사 결과와 변경 범위

- Unity 6000.3.24f1 / URP 17.3.0 / Input System 1.20.0, New Input System 전용.
- MainScene에 기존 오브젝트 5개와 2단계 이동/입력 컴포넌트가 있다.
- 기존 Main Camera: 위치 (0, 7, -10), Perspective, FOV 60, Near/Far 0.1/100.
- Cube: 위치 (0, 0.5, 0), Scale (1, 1, 1.5), +Z 방향 전진, 2 m/s, 60도/s.
- 기존 WheelchairMovement와 KeyboardWheelchairInput은 수정하지 않는다.

추가 파일:

- Assets/Scripts/Camera/CameraFollow.cs
- Assets/Editor/EEGWheelchair/Step03CameraSetup.cs
- Docs/Step03-FollowCamera.md

수정 파일: Assets/Scenes/MainScene.unity.
Main Camera에 CameraFollow를 추가하고 Target과 시작 시점을 저장한다.
Camera, AudioListener, URP 카메라 데이터, 렌즈 설정과 다른 오브젝트는 유지한다.
Step 1/2 생성 도구는 실행하지 않는다. 패키지 추가와 Scene 재생성도 하지 않는다.

## Inspector 설정

Hierarchy > Main Camera > Camera Follow에서 조절한다.

| 항목 | 기본값 | 의미 |
| --- | --- | --- |
| Target | WheelchairPlaceholder | 따라갈 Cube의 Transform |
| Offset | (0, 4.5, -7) | 대상 방향 기준 오른쪽/위/앞 거리(m); Z 음수는 뒤쪽 |
| Position Smoothing | 8 | 위치 반응 속도, 클수록 빠르게 추적 |
| Rotation Smoothing | 12 | 회전 반응 속도, 클수록 빠르게 추적 |
| Look Target Height | 0.5 | Cube 중심에서 위로 바라볼 높이(m) |

현재 Cube 중심 Y=0.5이므로 시작 카메라의 월드 위치는 (0, 5, -7), 바라보는 점은 (0, 1, 0)이다.
영구적으로 값을 바꾸려면 Play를 종료한 상태에서 조절하고 Scene을 저장한다.
Play 중 조절한 값은 종료 후 되돌아갈 수 있다.
Smoothing 값은 초 단위 대기 시간이 아니며 값이 작을수록 느리게 따라간다.

## 추적 방식

- KeyboardWheelchairInput.Update에서 처리한 이동 이후 CameraFollow.LateUpdate가 카메라를 갱신한다.
- 목표 위치는 target.position + Y축 회전 * offset으로 계산한다.
- Cube의 크기(특히 Z Scale=1.5)는 Offset에 반영하지 않아 거리 -7m를 유지한다.
- 위치는 Lerp, 회전은 Slerp를 사용하며 보간 비율은 1 - exp(-smoothing * deltaTime)이다.
  프레임마다 고정 비율을 쓰지 않아 프레임률에 따른 반응 차이를 줄인다.
- 바라볼 지점은 Cube 중심 + 월드 Y 방향의 Look Target Height다. 롤 기울기는 넣지 않는다.
- 시작할 때 SnapToTarget으로 바로 뒤쪽에 배치하며 이후 부드럽게 추적한다.
- Target이 없거나 삭제되면 추적을 건너뛰므로 NullReferenceException이 발생하지 않는다.
- 휠체어의 이동·입력과 독립되어 있고 Rigidbody나 추가 패키지는 사용하지 않는다.

## Unity에서 확인

1. Unity에서 임포트와 컴파일이 끝난 후 Assets > Scenes > MainScene을 연다.
2. Hierarchy의 Main Camera에서 Camera Follow가 활성화되어 있고 Target에
   WheelchairPlaceholder가 연결되어 있는지 확인한다. 수동 연결은 필요 없다.
3. Play를 누르고 Game 화면을 클릭해 키보드 포커스를 준다.
4. W 또는 ↑를 누른다. Cube가 전진하면서 카메라도 뒤쪽 위에서 따라와야 한다.
5. W를 놓고 A/← 또는 D/→를 누른다. Cube의 제자리 회전을 카메라가 부드럽게 뒤따라야 한다.
6. W+A와 W+D를 각각 눌러 전진과 회전을 함께 확인한다. Cube가 화면 안에 유지되어야 한다.
7. 모든 키를 놓는다. Cube는 즉시 멈추고 카메라는 잠깐 따라잡은 뒤 흔들림 없이 안정되어야 한다.
8. Console에 빨간 오류, Missing Script 또는 NullReferenceException이 없는지 확인한다.
9. Play를 종료한다. 입력과 이동 속도는 2단계와 동일해야 한다.

기본 속도와 16:9/4:3 화면 비율을 기준으로 검사한다. Inspector에서 Offset을 극단적으로
줄이거나 Smoothing을 매우 낮추면 시야 유지가 달라질 수 있다.
Ground 경계 제한과 카메라 충돌 회피는 없으므로 테스트는 바닥 안에서 짧게 진행한다.

## 자동 연결 도구

메뉴: Tools > EEG Wheelchair > Step 3 - Connect Follow Camera

이미 연결된 상태이므로 다시 실행할 필요가 없다. 재실행 시 컴포넌트를 중복 추가하지 않고
기존 Offset/Smoothing/Target을 유지하며 Target이 비어 있을 때만 Cube를 연결한다.
MainScene, Main Camera, Cube 또는 2단계 컴포넌트가 없으면 새로 생성하지 않고 오류로 중단한다.
미저장 Scene 변경은 Unity의 저장 확인 절차를 따른다.
생성 후 Step03CameraSetup.cs만 제거해도 동작한다. CameraFollow.cs는 유지해야 한다.

## 검증 범위

Unity 배치 실행에서 컴파일과 Scene 저장·재로드 및 연결을 검사한다.
임시 오브젝트로 30/120 FPS, 16:9/4:3 화면 비율에서 전진, 좌우 회전, 복합 이동을 모의 실행하고
Cube 8개 꼭짓점의 화면 내 위치, 카메라의 뒤쪽 배치, 정지 후 안정화, Target 없음 처리를 검사한다.
실제 키보드 조작과 Game 화면의 시각 확인은 위 절차로 직접 수행한다.

## API 참고

- [Unity 6.3 LateUpdate](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.LateUpdate.html)
- [TransformPoint의 Scale 적용](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Transform.TransformPoint.html)

이번 단계에는 UI, 상태 표시, 시작/정지/초기화, Python/Socket/JSON, EEG 파형,
실제 모델, 실내 환경, 시점 전환과 마우스 회전을 추가하지 않는다.
