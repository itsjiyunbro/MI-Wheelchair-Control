# 실내 환경 시각 개선 — 에셋 3단계

## 적용 결과

MainScene 및 기존 IndoorTestCourse.prefab에 적용 완료. 이전 생성 메뉴를 다시 실행할 필요가 없습니다.
기존 Floors/Walls/Doors의 위치·크기, 주행 경로, 휠체어 초기 Transform, Camera Follow 설정과 모든 런타임 C# 코드는 유지했습니다.

- 밝은 중성 벽, 차분한 회색 바닥, muted blue 문, 청록 포인트로 팔레트 통일
- 바닥에 얇은 2m 간격 줄눈과 기존 화살표를 잇는 청록 안내선
- START 초록 테두리와 벽 안내판, GOAL 주황 테두리 및 확대 표지판
- LEFT TURN / RIGHT TURN 및 구역 부제
- 하단 벽 패널과 얇은 포인트 띠
- 기존 문 3개에 R-101~103 번호판과 하단 보호판
- 기존 벤치 재질/좌석 이음새 및 기존 화분의 단순 입체 잎 개선
- 새 벤치·가구는 추가하지 않음. 천장, Collider, 런타임 기능 추가 없음.

새 장식은 IndoorTestCourse/VisualPolish 아래에 정리했습니다. 기존 Plant 오브젝트는 유지하고 MeshRenderer만 비활성화했습니다.

## Material 및 조명

Assets/Art/Materials/Environment의 기존 6개 Material GUID 유지. WallAccent, RouteGuide 2개 추가(총 8개). 모두 URP Lit / Metallic 0.

| Material | Base Color RGB (0~1) | Smoothness |
|---|---|---|
| Floor | .56, .61, .63 | .30 |
| Wall | .84, .86, .86 | .12 |
| Door | .19, .28, .33 | .22 |
| Accent | .075, .22, .24 | .18 |
| Start | .12, .43, .29 | .18 |
| Goal | .78, .35, .09 | .18 |
| WallAccent | .43, .49, .49 | .18 |
| RouteGuide | .065, .35, .34 | .20 |

Directional Light 1개 유지: intensity 2 → 1.65, color (1,.99,.98), shadow strength 1 → .65. 방향 유지.
환경광은 Trilight: Sky (.55,.60,.64), Equator (.42,.46,.50), Ground (.29,.32,.35).
새 Light/Probe 없음. Global Volume, URP 설정, 카메라 설정 변경 없음.

## 파일

- 신규: Assets/Editor/EEGWheelchair/IndoorTestCoursePolish.cs (+ meta)
- 신규: Assets/Art/Materials/Environment/WallAccent.mat, RouteGuide.mat (+ meta)
- 수정: 같은 폴더의 기존 Floor/Wall/Door/Accent/Start/Goal.mat
- 수정: Assets/Art/Prefabs/Environment/IndoorTestCourse.prefab
- 수정: Assets/Scenes/MainScene.unity (RenderSettings와 Directional Light만 변경)
- 문서: Docs/IndoorTestCourse-Polish.md

기존 IndoorTestCourseSetup.cs, 모든 런타임 C#, Python, 패키지는 변경하지 않았습니다.

## Editor 메뉴

Tools > EEG Wheelchair > Polish Indoor Test Course

Play Mode 밖에서 실행합니다. 기존 Prefab을 열어 지정된 재질과 장식만 갱신하고 MainScene의 조명을 저장합니다. Scene 전체를 다시 생성하지 않습니다. 이름이 같은 장식은 재사용하여 중복 생성하지 않습니다. 실행을 두 번 반복한 뒤 Scene/Prefab 직렬화 결과가 동일한 것을 검증했습니다.
이 메뉴는 정해진 팔레트·장식·조명 값을 다시 적용하므로, 이후 직접 편집한 해당 값도 재적용됩니다. Prefab의 다른 인스턴스 역시 공유 에셋 변경을 받습니다. 메뉴 실행 전 수정한 Scene 저장 여부를 묻습니다.
자동 생성 후 Editor 스크립트는 런타임에 필요하지 않습니다.

## 자동 검증

Unity 6000.3.24f1 batch mode exit 0.
- 컴파일 / Scene 저장·재로드 / Prefab 정상 로드 / 반복 실행 동일성
- 기존 Prefab 직렬화 블록 삭제 0개. 기존 일반 Transform 중 변경은 표지판 Board 3개의 크기뿐
- 코스 내부 900개 60Hz 이동·회전·카메라 샘플: 양쪽 코너와 GOAL 도달, 기존 벽과 카메라→휠체어 시선 교차 없음
- 실제 Play Mode의 Keyboard, Input System 버튼 클릭, STOP 차단, RESET 및 카메라, HUD 검증
- 실제 Python 송신기/스트림, TCP 연결 해제·재접속, v0.1/v0.2, STOPPED prediction 수신/이동 차단 검증
- START/LEFT/RIGHT/GOAL/전체 코스 렌더 및 HUD 포함 Play Mode 렌더 확인
- 컴파일 오류 및 테스트에서 포착한 런타임 Error/Exception 없음. 잘못된 JSON 테스트의 Warning은 의도된 결과

기존 카메라와 벽 높이를 유지하므로 화면 아래에 가까운 벽 윗면이 보이는 구도는 그대로입니다. 벽 충돌/카메라 충돌 기능은 추가하지 않았습니다.

## 직접 확인

1. Assets/Scenes/MainScene.unity 열기.
2. Hierarchy에서 IndoorTestCourse/VisualPolish와 WheelchairPlaceholder/WheelchairVisual 확인.
3. SimulationController의 Control Source = Keyboard. Play → START.
4. 초록 START에서 W로 첫 직선 진행(약 7m, 2m/s로 약 3.5초).
5. LEFT TURN에서 W를 놓고 A로 약 90° 회전(약 1.5초, HEADING 270°).
6. W로 중간 복도 진행(약 9m, 4.5초).
7. RIGHT TURN에서 W를 놓고 D로 약 90° 회전(HEADING 0°).
8. W로 주황 GOAL 진행(약 8m, 4초). 표지판과 바닥 안내선 확인.
9. STOP으로 정지, RESET으로 원래 START 위치·방향 및 카메라 복원 확인.
10. STOPPED / MOVE STOPPED / STEERING STRAIGHT 및 Console 빨간 오류 없음 확인.

키 입력 시간은 참고값입니다. 코너 중심의 화살표와 HEADING을 보면서 조정하세요. 기존 Python 모드 테스트 송신기도 동일하게 사용할 수 있습니다.
