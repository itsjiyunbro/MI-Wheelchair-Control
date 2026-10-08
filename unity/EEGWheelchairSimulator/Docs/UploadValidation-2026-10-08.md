# GitHub 작업 브랜치 검증 기록

기준일: 2026-10-08. Unity 6000.3.24f1 / Windows.

## 저장된 프로젝트

- 원본의 Assets, Packages, ProjectSettings, Docs, Tools 파일 2,844개를 비교했습니다.
- 배포용 한글 글꼴과 해당 Editor 생성 경로 교체를 제외한 원본 차이는 없습니다.
- 에셋의 `.meta` 누락과 중복 GUID가 없고, 패키지 JSON과 Python 송신기 문법 검사를 통과했습니다.
- 시작 장면에 누락된 스크립트가 없고 문 48개, 최신 크기의 B112 사선 스크린, 라운지와 자유 카메라를 확인했습니다.
- 공개 배포 사본은 Noto Sans CJK KR과 OFL을 포함합니다. 기존 font GUID를 유지했으며 B124 한글·영문 표지판을 실제 화면에서 확인했습니다.

## 실행 검증

짧은 프로젝트 경로에서 새 임포트 및 글꼴 교체 후 통합 실행 검증을 완료했습니다. 최종 Unity 프로세스 종료 코드는 0입니다.

- 실제 키보드 입력과 포인터 버튼, START/STOP/RESET 및 HUD.
- Python 테스트 송신과 prediction 스트림, 잘못된 JSON, 연결 해제와 재연결.
- 자유 카메라의 F/V/START/RESET, 마우스 시선·휠, WASD/QE, 30/120 FPS 대각선 이동과 Shift 속도, 안내 숨김/표시.
- 자유 카메라에서 휠체어 정지와 Python 제어 소스 유지, 문 키 E/R 구분.
- 문 48개의 30/120 FPS 열림·닫힘 96회와 출입구 34곳의 닫힘/열림 충돌.
- 닫히는 문에서 휠체어 감지, 문 통과, 자동문 접근/이탈 및 RESET 복원.

최종 로그의 성공 표식:

```text
UPLOAD_FONT_OK
UPLOAD_IMPORT_OK
DOOR_MOTION_OK
DOOR_INPUT_OK
DEMO_CAMERA_PLAY_OK
CONVERGENCE_PLAY_OK
```

## 임포트와 검증 환경 참고

Windows에서 경로가 긴 복사본은 URP 패키지 내부 파일 임포트가 실패하여 짧은 경로에서 재검증했습니다. 팀원도 `C:\Projects` 같은 짧은 경로에서 프로젝트를 여는 것을 권장합니다.

Unity 6000.3의 배치 시작 검색 인덱스에서 내부 예외가 발생했습니다. `ConvergenceHallUploadValidation`을 명시적으로 실행하는 배치 모드에 한해 검색 인덱스 시작 콜백만 제외했습니다. 일반 Editor와 런타임 코드, 장면·글꼴·입력·렌더링 검사는 변경하지 않습니다. 최종 통합 검증 로그에는 컴파일 오류나 런타임 예외가 없습니다.

배포 실행 파일의 빌드와 성능 측정은 이번 업로드 범위에 포함하지 않습니다.
