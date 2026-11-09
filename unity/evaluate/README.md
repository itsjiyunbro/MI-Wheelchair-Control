# Unity 연동용 EEG 추론

이 폴더에서 아래 명령을 실행하면 저장된 EEG 샘플을 EEGNet으로 추론합니다. 모델 정의는 바로 위 Unity 폴더의 `architectures`에 있습니다.

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
- 기본 입력인 `sample/X_test_sample.npy`는 표준화된 실제 좌·우 및 휴식 EEG 윈도 3개입니다. EEGNet에서 세 명령을 확인하도록 선별했습니다. 다른 모델에서는 출력이 달라질 수 있습니다. 각 윈도는 별도로 골랐으므로 연속 재생용 데이터와 성능 평가용 데이터는 별도로 준비합니다.
- `--input`에는 학습 당시와 동일한 전처리와 채널 순서를 적용한 NumPy 파일을 지정합니다. 입력 형태는 `(9, 320)` 또는 `(N, 9, 320)`입니다. 샘플은 표준화가 끝난 상태이므로 바로 실행할 수 있습니다.
- 기본 체크포인트는 저장소 루트의 `models/checkpoints`에서 불러옵니다. `--checkpoint`로 직접 지정할 수 있습니다.
- 현재 스크립트는 JSON을 표준 출력에 기록합니다. 이 단계에서 Unity TCP 수신 코드와 연결하려면 메시지 형식과 연결 방식을 맞춰야 합니다.
