# 휠체어 비주얼 에셋 1단계

Unity 기본 Cube/Cylinder로 만든 정적인 테스트 모델입니다. 기존 제어 루트를 보존하고 시각 모델만 자식으로 추가했습니다.

## 확인 방법

1. `Assets/Scenes/MainScene.unity`를 엽니다.
2. Hierarchy에서 **WheelchairPlaceholder → WheelchairVisual**을 펼칩니다.
3. 파란 좌석·등받이, 어두운 프레임·팔걸이, 큰 뒤쪽 바퀴 2개, 작은 앞바퀴 2개, 회색 발판이 보여야 합니다.
4. 루트 `WheelchairPlaceholder`의 기존 MeshRenderer는 꺼져 있습니다. 원래 파란 Cube와 휠체어가 겹쳐 보이지 않아야 합니다.
5. Control Source=Keyboard로 Play → START → W/A/D 또는 방향키를 누릅니다. 휠체어 전체가 함께 이동·회전하고 카메라가 따라가야 합니다.
6. STOP은 현재 위치 정지, RESET은 최초 위치·방향·카메라 복원입니다.
7. Python 모드는 기존 절차 그대로 확인합니다. Control Source=Python → Play → 아래 송신기 실행 → START. Prediction HUD와 동작이 계속 반영돼야 합니다.

```powershell
Set-Location '<저장소 경로>\unity\EEGWheelchairSimulator\Tools\PythonTestSender'
powershell -NoProfile -ExecutionPolicy Bypass -File .\run_sender.ps1 -Stream
```

Python 테스트 종료는 Ctrl+C입니다. 연결 해제 시 정지, HUD 초기화도 기존과 같습니다. Console에 빨간 오류가 없는지 확인합니다.

## 생성 에셋

- `Assets/Art/Prefabs/WheelchairVisual.prefab`
- `Assets/Art/Materials/Wheelchair/CushionBlue.mat`
- `Assets/Art/Materials/Wheelchair/Frame.mat`
- `Assets/Art/Materials/Wheelchair/Tire.mat`
- `Assets/Art/Materials/Wheelchair/Metal.mat`
- `Assets/Editor/EEGWheelchair/WheelchairVisualSetup.cs`

외부 모델·텍스처·패키지는 없습니다. URP Lit 머티리얼 4개를 전체 모델에서 공유합니다. 기본 Primitive mesh를 사용하므로 별도 mesh 파일이나 ModelsGenerated 폴더는 필요하지 않습니다.

## 루트와 배치

```text
WheelchairPlaceholder  ← 기존 이름, Transform, Collider, 동작 컴포넌트 유지
└─ WheelchairVisual    ← Prefab 인스턴스, 시각 모델만 포함
   ├─ Seat / Backrest
   ├─ LeftArmrest / RightArmrest
   ├─ LeftWheel / RightWheel
   ├─ FrontWheel_L / FrontWheel_R
   ├─ Footrest
   ├─ Frame
   └─ Rim / Hub / Spokes 등의 단순 장식
```

- 기존 루트 위치 `(0, 0.5, 0)`, 스케일 `(1, 1, 1.5)`를 변경하지 않았습니다.
- Prefab은 바닥 높이 0을 원점으로 하며 +Z가 전방입니다. 좌석 앞쪽, 작은 앞바퀴와 발판이 +Z에 있습니다.
- 현재 Scene의 자식 인스턴스는 localPosition `(0, -0.5, 0)`, localScale `(1, 1, 0.6666667)`입니다. 부모의 Z 스케일을 상쇄해 바퀴가 타원으로 늘어나지 않습니다.
- 대략 폭 1.25m, 높이 1.39m, 앞뒤 길이 1.69m입니다. 큰 바퀴 지름 0.96m, 작은 바퀴 지름 0.30m입니다.
- 생성 시 기존 Ground 높이에 맞춥니다. 카메라의 추적 대상/offset/smoothing은 변경하지 않았습니다.
- 원래 Cube의 MeshFilter·BoxCollider는 보존하고 **MeshRenderer만 비활성화**했습니다. 시각 모델은 새 Collider/Rigidbody/MonoBehaviour를 포함하지 않습니다. 이번 작업은 물리 형상 변경이 아닙니다.
- 바퀴는 정적인 비주얼입니다. 회전 애니메이션이나 탑승 캐릭터는 없습니다.

## 자동 생성 메뉴와 재실행

메뉴: **Tools → EEG Wheelchair → Create Wheelchair Visual**

현재 MainScene에는 이미 연결되어 있으므로 다시 실행할 필요가 없습니다.

- 처음 실행하면 Prefab/머티리얼을 만들고 기존 루트에 인스턴스를 연결하며 Scene을 저장합니다.
- 같은 Prefab 인스턴스가 이미 있으면 아무것도 다시 만들지 않습니다. 수동 수정한 위치·머티리얼·Prefab 편집도 덮어쓰지 않습니다.
- 같은 이름의 다른 오브젝트가 있거나 중복 자식이 있으면 원인을 알려주고 중단합니다. 기존 오브젝트를 지우지 않습니다.
- 인스턴스가 없지만 Prefab이 있으면 기존 Prefab을 재사용합니다.
- 머티리얼이 이미 있으면 기존 에셋을 재사용합니다.
- Stage 1~7 생성 스크립트는 호출하지 않습니다.
- 모델을 편집하려면 Prefab을 열어 자식 Primitive나 공용 머티리얼을 조정하면 됩니다. 생성 메뉴는 강제 재생성 기능이 아닙니다.
- 이 Editor 스크립트는 런타임 의존성이 없으므로, 이후 자동 생성 메뉴가 필요 없으면 제거해도 Prefab과 Scene은 동작합니다.

## 보존 범위 및 검증

기존 Runtime C# 파일, 패키지·프로젝트 설정, Python 파일은 변경하지 않았습니다. 기존 MainScene에서는 루트 MeshRenderer와 비주얼 자식 연결만 변경합니다.

검증 항목: Unity 컴파일, Prefab 및 Scene 저장·재로드, 네 바퀴 Ground 접촉, 4개 공용 머티리얼, 자식의 Collider/동작 스크립트 없음, 메뉴 재실행 시 무변경, 기존 키보드·Python·HUD·카메라·Start/Stop/Reset Play Mode 테스트.

2026-09-21 Unity 6000.3.24f1에서 위 검증을 실행하고 **종료 코드 0**으로 통과했습니다. `VISUAL_ASSET_OK`, `STEP07_SOCKET_OK`, `STEP07_PREDICTION_OK`, `STEP07_PLAY_OK`를 확인했습니다. 앞쪽 사선 보기와 기존 Game 카메라 렌더링도 확인했습니다.

기존 파일 해시와 Scene 객체별 비교 결과, 기존 파일 중 MainScene만 변경됐습니다. 원래 Scene 객체의 변경은 Cube MeshRenderer 비활성화와 루트 Transform의 자식 목록 추가뿐입니다. 기존 루트 위치·방향·스케일, 모든 제어/UI 참조, BoxCollider는 보존됐습니다.
