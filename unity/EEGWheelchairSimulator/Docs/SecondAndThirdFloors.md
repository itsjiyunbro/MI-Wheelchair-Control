# 컨버전스홀 2층 · 3층 구현 및 검증 안내

## 1. 작업 기준 및 Unity 버전 안내

- **Git 작업 브랜치**: `unity-convergence-hall-2-3` (기준: `origin/unity` 최신 커밋 `e24f133`, `6b56008` 포함 확인 완료, 로컬 작업 전용)
- **프로젝트 버전 파일 (`ProjectSettings/ProjectVersion.txt`)**: 기존 팀 기준 **`6000.3.24f1`** 그대로 유지 (수정하지 않음)
- **2·3층 구현 및 배치 검증 실행 버전**: 본 PC에 설치된 **`Unity 6000.6.8f1`** 에서 컴파일, 씬 생성, Play Mode 자동 검증을 수행함

---

## 2. 좌표 정합 및 배치 가정

- 제공된 2F·3F 피난안내도를 기준으로 **북쪽 → Unity 동쪽(`+X`, `v=0..25`, `NorthElevatorHall`·`NorthStairwell` 라인)**, **남쪽 → Unity 서쪽(`-X`, `v=35..66`, `SouthElevatorHall`·`SouthStairwell` 라인)** 으로 정렬해 기존 B1·1F와 동·서 승강기 및 계단실 코어가 수직으로 일치하도록 배치했다.
- 층간 높이는 기존 B1→1F와 동일하게 **`4.50m`** (`1F y=4.50m`, `2F y=9.00m`, `3F y=13.50m`), 벽 높이는 **`2.85m`** 로 설정했다.
- 도면이 실측 설계도가 아니므로 방 크기, 컨테이너(`201`) 치수, 복도 폭(`2.5~3.8m` 확보), 계단 단수(`26단` 왕복 / 중앙 오픈 계단 `22단` 왕복)는 사진 비율 기반 추정치다.
- 미확인 호실의 상세 명칭은 `Docs/UnconfirmedRoomSigns_2F_3F.md`에 촬영 위치와 함께 분리 기록했다.

---

## 3. 재사용한 B1 · 1F 템플릿

- **호실 표지판**: `ConvergenceHallClassroomSigns.CreateRoomPlaque` + `NotoSansCJKkr-Regular.otf` (흰 상단 호실 번호, 남색 본문, 금속 구분선, 점자판)
- **화장실**: B1 `PlanDoors/NorthWC_*`, `WestWC_*` 반투명 유리문, 새틴 프레임, 긴 손잡이, 회색 타일 바닥
- **승강기 홀**: 동쪽 `CoreHalls/B112ElevatorHall` (노출 콘크리트 마감, 점검문 2개, `MeasuredCabin`), 서쪽 `CoreHalls/B109ElevatorHall` (흰 벽 마감, 좌측 승강기, `EPS`/`TPS` 점검문, `MeasuredCabin`)
- **가구·편의시설**: `TwoSeatFoldingDesk.prefab`, `BlueMeshChair.prefab`, `ElectronicLectern.prefab`, `BluebellAmenities` (녹색/흰색 자판기, 4칸 목재 분리수거함)

---

## 4. 구현된 층간 이동 및 진입 방법

- **씬 진입**: `Assets/Scenes/ConvergenceHallB1Scene.unity` (`ConvergenceHall1FScene.unity`도 동일하게 동기화 저장됨). 시작 및 RESET 위치는 **B1** 유지.
- **승강기 (`PassengerElevator` 4층 확장)**: 동·서 승강기 모두 **B1 · 1F · 2F · 3F** 4개 정차 지점 지원. 객실 내부에 완전히 들어간 뒤 원하는 층 버튼을 선택하면 이동 후 문을 향해 정지 상태로 대기하며, `START` + `W` 전진으로 하차한다.
- **계단 (`StairWalkRoute`)**:
  - `1F ↔ 2F`: 기존 1F `UpperBluebell` 목재 계단 + 동·서 계단실(`1F2F_NorthConnectedStair`, `1F2F_SouthConnectedStair`)
  - `2F ↔ 3F`: 동·서 계단실(`2F3F_NorthConnectedStair`, `2F3F_SouthConnectedStair`) + 중앙 오픈 왕복 계단(`Open2F3FStair`)
  - 자유 시점(`F`)에서 계단 끝부분에 접근해 `G`를 누르면 오르내릴 수 있다.
- **바로가기 (`ConvergenceFloorNavigation`)**: 우상단 HUD 버튼 `B1 · PgDn`, `1F`, `2F`, `3F · PgUp` 또는 키보드 `PgUp` / `PgDn`으로 층간 순차 이동.

---

## 5. 재생성 및 검증 명령

- **2·3층 전체 생성 및 Play 검증**: Unity 메뉴 `Tools > EEG Wheelchair > Build Convergence Hall 2F And 3F` (`ConvergenceHallUpperFloors.BuildAndValidate`)
  - `Tools > EEG Wheelchair > Build Photo Based First Floor` 실행 시에도 `ConvergenceHallUpperFloors.ReconnectIfPresent()`가 호출되어 2·3층 승강기/계단/바로가기 연결이 유지된다.
- **2·3층 배치 Play 검증만 단독 실행**: `EEGWheelchairSimulator.Editor.ConvergenceHallUpperFloorsValidation.CaptureAndCheck`
