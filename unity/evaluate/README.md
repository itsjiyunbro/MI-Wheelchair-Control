# Unity 연동용 EEG 추론

이 폴더에서 아래 명령을 실행하면 저장된 EEG 샘플을 EEGNet으로 추론합니다. 모델 정의는 저장소 루트의 `models/architectures`에서 불러옵니다.

```powershell
python -m pip install -r requirements.txt
python predict.py
python predict.py --model shallow_convnet
python predict.py --model eeg_tcnet
python predict.py --model lmda
```

표준 출력에는 입력 윈도 순서대로 한 줄에 JSON 객체 하나를 출력합니다.

```json
{"label": "Left", "confidence": 0.95}
{"label": "Hold", "confidence": 0.73}
{"label": "Right", "confidence": 0.97}
```

- `Left`는 좌회전, `Right`는 우회전, `Hold`는 회전 명령 보류입니다.
- `confidence`는 좌·우 소프트맥스 값 중 큰 값입니다. 기본 기준값 0.90 미만이면 `Hold`를 출력합니다. `--threshold`로 기준값을 변경할 수 있습니다.
- `sample/X_test_unstd_sample.npy`에는 표준화 전 실제 EEG 윈도 3개가 들어 있습니다. EEGNet에서 `Left → Hold → Right`를 확인하도록 좌·우 테스트 데이터와 휴식 테스트 데이터에서 각각 골랐습니다. 각 윈도는 연속된 기록에서 잘라낸 시퀀스가 아니며 다른 모델에서는 출력이 달라질 수 있습니다.
- `predict.py`는 `sample/train_channel_mean.npy`와 `sample/train_channel_std.npy`를 불러와 채널별로 `(입력 - 평균) / 표준편차`를 계산한 뒤 모델에 넣습니다. 두 통계 파일은 학습 데이터에서 계산한 값입니다.
- `--input`으로 다른 EEG 파일을 지정할 수 있습니다. 학습 데이터와 같은 필터 및 채널 순서를 적용한 NumPy 배열을 사용합니다. 입력 형태는 `(9, 320)` 또는 `(N, 9, 320)`입니다. 이미 같은 통계로 표준화한 입력에는 `--standardized`를 함께 지정합니다.
- 기본 체크포인트는 저장소 루트의 `models/checkpoints`에서 불러옵니다. `--checkpoint`로 직접 지정할 수 있습니다.
- 현재 스크립트는 JSON을 표준 출력에 기록합니다. 이 단계에서 Unity TCP 수신 코드와 연결하려면 메시지 형식과 연결 방식을 맞춰야 합니다.
