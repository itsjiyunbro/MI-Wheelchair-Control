# Unity - 컨버전스홀 B1·1F 휠체어 시뮬레이터

`unity`의 B1·1F 구현을 바탕으로 EEG 연동을 추가한 `unity-eeg-inference-demo` 작업 브랜치입니다.

컨버전스홀 지하 1층과 1층을 안내도와 현장 사진 기준으로 구현했습니다. 두 층은 동·서 엘리베이터와 계단실로 이어집니다. 키보드와 Python 테스트 송신기를 지원하며, 학습된 모델의 저장 EEG 예측을 TCP로 받아 회전·정지를 재생할 수 있습니다.

![B112 강의실](images/convergence-b112.png)
![라운지](images/convergence-lounge.png)

## 처음 실행하기

```powershell
git clone --branch unity-eeg-inference-demo --single-branch https://github.com/itsjiyunbro/MI-Wheelchair-Control.git
```

1. Windows에서는 `C:\Projects`처럼 짧은 경로에 복제하세요. 경로가 길면 Unity 패키지 임포트가 실패할 수 있습니다. Unity Hub에서 **Unity 6000.3.24f1**을 준비합니다.
2. `unity/EEGWheelchairSimulator` 폴더를 Unity 프로젝트로 엽니다.
3. 최초 패키지 복원과 에셋 임포트가 완료될 때까지 기다립니다.
4. `Assets/Scenes/ConvergenceHallB1Scene.unity`를 엽니다.
5. Control Source를 **Keyboard**로 설정하고 Play, **START** 순서로 시작합니다. 기본 시작 위치는 지하 1층입니다.
6. 휠체어로 층을 이동하려면 엘리베이터에 탑승해 **B1 / 1F**를 선택합니다. 자유 시점에서 계단을 따라 이동하거나 **PgUp / PgDn**으로 층을 바로 확인할 수 있습니다. [1층과 엘리베이터 안내](EEGWheelchairSimulator/Docs/FirstFloorAndElevators.md)를 참고하세요.

완성된 Scene과 Prefab이 포함돼 있으므로 환경 생성 메뉴를 다시 실행할 필요는 없습니다. `MainScene`, 실내 테스트 코스와 재활 코스도 함께 보존했습니다. 실행 파일은 포함하지 않습니다. 빌드할 때는 Build Profiles의 Scene List에 B1 장면을 등록하세요.

## 조작법

| 모드 | 입력 | 동작 |
|---|---|---|
| 휠체어 | W / ↑ | 전진 |
| 휠체어 | S / ↓ | 후진 |
| 휠체어 | A / ←, D / → | 좌우 회전 |
| 휠체어 | E / 문 버튼 | 가까운 문 열기·닫기 |
| 휠체어 | V / 시점 버튼 | 1인칭·3인칭 전환 |
| 공통 | F / 자유 시점 버튼 | 자유 카메라 진입·복귀 |
| 자유 카메라 | WASD | 카메라 이동 |
| 자유 카메라 | Q / E | 하강 / 상승 |
| 자유 카메라 | 우클릭 드래그 | 시선 회전 |
| 자유 카메라 | Shift / 마우스 휠 | 가속 / 속도 조절 |
| 자유 카메라 | R / 문 버튼 | 가까운 문 열기·닫기 |
| 자유 카메라 | H | 안내 화면 숨김·표시 |
| 자유 카메라 | F / ESC | 휠체어 시점으로 복귀 |
| 공통 | START / STOP / RESET | 시작 / 정지 / 시작 위치 복귀 |

전진 기본 속도는 2m/s, 후진은 1.2m/s입니다. 자유 시점에서는 휠체어가 정지하고 카메라만 이동합니다. 자유 시점 복귀 후 START를 눌러 주행을 재개합니다. 휠체어에는 벽·가구·문 충돌을 적용하고, 자유 카메라는 벽을 통과할 수 있습니다.

## 구현한 공간과 기능

- 주출입구, 복도 바닥·천장, 유리의 불투명 띠, 안내판·표지판과 벽 마감.
- 블루벨리 계단·스탠드·유리 난간·상부 전광판 외형, 두 계단실 아래 창고.
- B112 옆 동쪽 콘크리트 엘리베이터실과 B109 옆 서쪽 흰 벽 엘리베이터실, 점검문·표지판·공식 연세대학교 문양.
- B101-B110, B112-B115 강의실 14개와 사진 기반 책상·의자·천장.
- B112의 책상 21개·학생 의자 42개, 교탁, 화이트보드, 창문·블라인드, AV 장비와 접힌 파티션. 스크린은 테두리 포함 강의실 폭의 약 54%이며 약 12도 사선 천장 수납부에서 수직으로 내려옵니다. 뒤 화이트보드가 양옆으로 보입니다.
- 라운지의 소파·테이블·화분 선반, 단말기·건강 측정 기기 외형과 노출 천장.
- B124 양개문과 표지판, 화장실 반투명 문, 문 48개의 개폐와 자동문 센서.
- 1·3인칭, 시연용 자유 카메라, 휠체어 후진과 STOP/RESET.

추가 원본 화면은 [images/convergence/](images/convergence/)에 있습니다. 설명을 덧붙인 PDF 대신 실제 구현 화면을 포함했습니다.

## 샘플 EEG 추론으로 Unity 조작

팀의 `unity/evaluate/predict.py`를 실행해 저장된 EEG 윈도 3개를 학습된 EEGNet으로 분류합니다. 송신기가 모델의 `label`과 `confidence`를 읽고 `Left`는 `LEFT`, `Hold`는 `STOP`, `Right`는 `RIGHT`로 변환해 기존 Unity TCP 수신기(`127.0.0.1:5055`)에 보냅니다. `Hold`는 모델의 기본 confidence 기준 0.90 미만에서 나오며, Unity에서는 회전이 멈추고 시뮬레이션의 RUNNING 상태가 유지됩니다.

저장소 루트에서 다음 순서로 실행하세요.

1. Python 가상환경에서 `python -m pip install -r unity/evaluate/requirements.txt`를 실행합니다.
2. Unity에서 `Assets/Scenes/ConvergenceHallB1Scene.unity`를 열고 Play를 누릅니다. `SimulationController`의 **Control Source**를 **Python**으로 설정하고 **START**를 누릅니다. Console에서 `WAITING at 127.0.0.1:5055`를 확인합니다.
3. 같은 가상환경에서 아래 명령을 실행합니다.

```powershell
python unity/EEGWheelchairSimulator/Tools/PythonTestSender/send_eeg_predictions.py
```

기본 샘플은 EEGNet에서 제자리 좌회전 → 정지 → 제자리 우회전을 차례로 재생합니다. 송신기는 각 예측을 1초씩 적용한 후 연결을 종료합니다. 재생 간격은 `--interval`로 설정하고, `--` 뒤에 `predict.py` 옵션을 전달할 수 있습니다. 예를 들어 `--interval 1.5 -- --model eegnet --threshold 0.90`을 붙일 수 있습니다. 이 데모는 따로 선별된 EEG 윈도 3개의 저장 데이터 재생입니다. 데이터 형식과 모델 옵션은 [팀 추론 코드 안내](evaluate/README.md)를 참고하세요.

기록 순서가 있는 EEG와 윈도 CSV를 받았다면 두 파일을 함께 지정합니다.

```powershell
python unity/EEGWheelchairSimulator/Tools/PythonTestSender/send_eeg_predictions.py --windows-csv '<windows.csv 경로>' -- --input '<eeg.npy 경로>'
```

`EEG_Index`는 NPY 배열의 0부터 시작하는 윈도 순서와 일치해야 합니다. 같은 `Trial` 안에서는 `End_sec_from_event`의 차이만큼 각 예측을 적용합니다. 구간 마지막 예측은 기본 0.25초 적용하고 구간 사이에는 `STOP`을 기본 0.25초 넣습니다. 실제 구간 사이 간격이 없는 CSV에 적용하는 재생 설정이며, 각각 `--interval`, `--trial-gap`으로 바꿀 수 있습니다. CSV의 정답 라벨은 회전 명령 생성에 쓰이지 않습니다.

Trial 시작 시간표까지 받았다면 **아래 방식을 사용하세요.** 윈도가 제외된 구간의 시간도 보존합니다.

```powershell
python unity/EEGWheelchairSimulator/Tools/PythonTestSender/send_eeg_predictions.py --windows-csv '<S009R08_windows.csv 경로>' --trial-onsets-csv '<S009R08_trial_onsets.csv 경로>' -- --input '<S009R08_eeg.npy 경로>' --model eegnet --threshold 0.90
```

`Trial` 번호로 두 CSV를 연결하고 `Onset_sample_from_run_start / Sampling_rate_Hz + End_sec_from_event` 시점에 예측을 적용합니다. 시간표의 초·샘플 인덱스가 서로 맞는지, 샘플링 주파수가 160 Hz인지 확인합니다. 기록 시작부터 첫 윈도 끝까지는 정지합니다. 예측은 최대 `--interval`(기본 0.25초) 동안 유지하고 다음 예측이 없으면 `STOP`을 보냅니다. 따라서 Trial 2·16뿐 아니라 전처리로 빠진 윈도 사이에서도 이전 회전이 지속되지 않습니다. 이 모드에서는 `--trial-gap` 대신 실제 시각 차이를 사용합니다.

추론을 모두 마친 뒤 기록 시작을 0초로 잡아 재생합니다. 누적 지연을 줄이기 위해 단조 시계의 예정 시각에 맞춰 송신합니다. 이는 저장 데이터 재생이며, 실시간 EEG 수집·온라인 추론 지연 측정은 아닙니다. 마지막 윈도 명령이 만료되면 `STOP`을 보내고 0.25초 후 연결을 종료합니다. 원본 녹음 종료 시각은 제공되지 않았으므로 마지막 이후의 녹음 길이는 재현하지 않습니다. 입력 조건과 오분류 내역은 [S009R08 재생 기록](EEGWheelchairSimulator/Docs/EEGReplay-S009R08.md)을 참고하세요.

시간표 검증 테스트는 저장소 루트에서 실행합니다.

```powershell
python -m unittest discover -s unity/EEGWheelchairSimulator/Tools/PythonTestSender -p test_send_eeg_predictions.py -v
```

## Python 테스트 입력

기존 TCP + NDJSON 테스트 입력의 기본 포트는 **5055**, 명령은 **LEFT / RIGHT / FORWARD / STOP**입니다. 키보드 후진은 Python 프로토콜에 새 명령을 추가하지 않습니다. 테스트 송신기의 confidence는 테스트 값이며, 샘플 EEG 송신기의 보류 판단은 위의 모델 추론 단계에서 처리합니다.

```powershell
cd unity/EEGWheelchairSimulator/Tools/PythonTestSender
./run_sender.ps1
# 스트림 테스트
./run_sender.ps1 -Stream -Interval 0.25 -Count 100
```

Unity의 Control Source를 Python으로 선택한 뒤 START로 주행합니다. 송신기는 Python 3.6 이상과 표준 라이브러리만 사용합니다. 메시지 필드와 실행 예시는 [송신기 안내](EEGWheelchairSimulator/Tools/PythonTestSender/README.md), [Stage07 스트림 안내](EEGWheelchairSimulator/Tools/PythonTestSender/README-Stage07.md)를 참고하세요.

## 프로젝트 파일

- 시작 장면: `Assets/Scenes/ConvergenceHallB1Scene.unity`
- 환경 프리팹: `Assets/Art/Prefabs/Environment/ConvergenceHall/ConvergenceHallB1.prefab`
- 런타임 코드: `Assets/Scripts`
- 제작·검증 도구: `Assets/Editor/EEGWheelchair`
- 상세 변경 기록: [ConvergenceHallB1-Guide.md](EEGWheelchairSimulator/Docs/ConvergenceHallB1-Guide.md)

Assets와 `.meta`, Packages, ProjectSettings, Docs, Tools를 포함합니다. Library, Temp, Logs, UserSettings, IDE 생성 파일은 제외합니다. 에셋 임포트 시 Library는 각 PC에서 다시 생성됩니다. 한글 안내 글자는 포함된 Noto Sans CJK KR 글꼴을 사용합니다. [글꼴 출처와 OFL 라이선스](EEGWheelchairSimulator/Docs/ThirdPartyFonts.md)를 함께 포함했습니다.

## 검증과 현재 범위

새로 복제한 프로젝트에서 임포트, 누락 스크립트, 저장된 B1 장면·스크린·라운지·문 48개를 확인했습니다. [최종 검증 기록](EEGWheelchairSimulator/Docs/UploadValidation-2026-10-08.md)을 포함했습니다. 실제 키·버튼과 Python 송신/재연결/잘못된 메시지, STOP/RESET, 자유 카메라, 문 48개의 30/120 FPS 개폐 96회와 출입구 34곳의 충돌을 검증하는 진입점도 포함했습니다.

```powershell
# 프로젝트 폴더를 현재 디렉터리로 두고 실행합니다.
# Unity.exe의 경로와 Python 실행 파일 경로는 본인 PC에 맞춰 입력하세요.
& '<Unity.exe 경로>' -batchmode -projectPath (Get-Location).Path `
  -executeMethod EEGWheelchairSimulator.Editor.ConvergenceHallUploadValidation.Run `
  -pythonExe '<python.exe 경로>' `
  -convergencePreviewFolder '<화면 저장 폴더>' -logFile '<검증 로그 경로>'
```

환경은 사진과 안내도에 따른 추정 비율을 포함하며 정밀 실측 모델은 아닙니다. B1·1F와 두 층의 이동, 저장 EEG 모델 추론·명령 재생을 포함합니다. 다른 층, 파티션 펼침, 전광판·스크린 콘텐츠 재생, 실제 건강 측정, 실시간 EEG 장치 연결은 추가 구현 범위입니다. 모델 연동 성공은 모델의 분류 성능이나 실제 휠체어 운용 성능 검증을 의미하지 않습니다.
