# 컨버전스홀 2층 구현 및 검증 안내 (`unity-convergence-hall-2`)

- **Git 작업 브랜치**: `unity-convergence-hall-2` (기준: `unity-convergence-hall-1` 커밋 `6b56008`, 로컬 작업 전용)
- **프로젝트 버전 파일 (`ProjectSettings/ProjectVersion.txt`)**: 팀 기준 **`6000.3.24f1`** 유지 (수정하지 않음)
- **빌드 및 Play 검증 실행 버전**: 본 PC에 설치된 **`Unity 6000.6.8f1`**

## 구현 내용
- `Assets/Art/Prefabs/Environment/ConvergenceHall/ConvergenceHall2F.prefab` (`y = 9.00m`, 1F 위 `4.50m`)
- 호실 `201`~`219`, `203-1`, 201 컨테이너 3동(실내 책상·의자 포함), 204/205 라운지, 동·서 화장실(B1 반투명 유리문 템플릿), 동·서 승강기 홀 및 계단실 코어 연결
- `PassengerElevator` 3층 정차 (`B1`, `1F`, `2F`) 및 `ConvergenceFloorNavigation` 바로가기 (`B1 · PgDn`, `1F`, `2F · PgUp`)
- `1F ↔ 2F` 동·서 계단실(`1F2F_NorthConnectedStair`, `1F2F_SouthConnectedStair`)

## 재생성 및 검증 명령
- Unity 메뉴 `Tools > EEG Wheelchair > Build Convergence Hall 2F` (`ConvergenceHallUpperFloors.BuildAndValidate`)
