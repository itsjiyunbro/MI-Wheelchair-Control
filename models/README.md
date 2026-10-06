# 모델 학습 및 비교 README

이 폴더는 전처리 산출물을 입력으로 받아 왼손/오른손 운동 상상 EEG 분류 모델을 구현하고 비교하기 위한 작업 공간이다. 전처리 담당자가 만든 `BCI_EEG_Preprocessed_Data_103Subjects` 결과를 기반으로 EEGNet, CSP+LDA, ShallowConvNet 등 후보 모델을 동일한 split에서 평가한다.

## 현재 폴더 목적

`models`는 전처리 산출물이 모델 입력으로 이어지는지 확인하기 위한 최소 학습/평가 코드가 들어 있는 작업 폴더이다. 현재 구조는 다음과 같다.

| 경로 | 용도 |
| --- | --- |
| `config/` | 데이터 경로와 공통 실험 설정 |
| `data/` | NumPy 전처리 배열 로더와 경로 설정 유틸리티 |
| `architectures/` | CSP+LDA, SVM baseline, EEGNet, ShallowConvNet 모델 구현 |
| `scripts/` | 데이터 확인, 학습, Rest false command 평가 실행 스크립트 |
| `tests/` | 데이터 로더, 지표, baseline, EEGNet/ShallowConvNet smoke test |
| `results/` | 학습/평가 결과 JSON 저장 위치 |
| `checkpoints/` | PyTorch 모델 checkpoint 저장 위치 |
| `README.md` | 본 문서 |

## 사용할 전처리 산출물

기본 설정은 저장소 루트의 `data/BCI_EEG_Preprocessed_Data_103Subjects`를 전처리 산출물 위치로 사용한다.

```text
MI-Wheelchair-Control/
└── data/
    └── BCI_EEG_Preprocessed_Data_103Subjects/
```

`data/`는 Git에 올리지 않는 로컬 데이터 폴더이다. 다른 위치에 산출물을 둔 경우에는 코드 수정 없이 `--data-dir` 인자나 `MI_WHEELCHAIR_DATA_DIR` 환경변수로 지정한다.

```bash
python models/scripts/check_data.py --data-dir path/to/BCI_EEG_Preprocessed_Data_103Subjects
```

```powershell
$env:MI_WHEELCHAIR_DATA_DIR = "path\to\BCI_EEG_Preprocessed_Data_103Subjects"
python models/scripts/check_data.py
```

핵심 파일:

| 파일 | Shape | 용도 |
| --- | --- | --- |
| `X_train.npy` | (18096, 9, 320) | 표준화된 train 입력 |
| `y_train.npy` | (18096,) | train label, 0 = Left, 1 = Right |
| `X_val.npy` | (4011, 9, 320) | 표준화된 validation 입력 |
| `y_val.npy` | (4011,) | validation label |
| `X_test.npy` | (3931, 9, 320) | 표준화된 test 입력 |
| `y_test.npy` | (3931,) | test label |
| `X_rest_train.npy` | (17990, 9, 320) | train split의 T0 Rest 입력 |
| `X_rest_val.npy` | (3793, 9, 320) | Validation T0 threshold 검토용 |
| `X_rest_test.npy` | (3596, 9, 320) | Test T0 false command 평가용 |
| `X_train_unstd.npy`, `X_val_unstd.npy`, `X_test_unstd.npy` | task 배열과 동일한 shape | CSP+LDA 등 표준화 전 입력이 더 적합한 모델 실험용 |
| `metadata_*.csv` | sample별 기록 | subject/run/trial/window별 오분류 분석용 |
| `split_subjects.csv` | 103명 subject split | 피험자 단위 split 유지 검증용 |

모든 모델은 기존 피험자 단위 split을 그대로 사용해야 한다. sliding window 단위로 train/test를 다시 random split하면 거의 같은 EEG 구간이 서로 다른 split에 들어가 data leakage가 생길 수 있다.

## 모델 입력 형식

전처리 배열의 기본 입력 형식은 `(N, 9, 320)`이다.

- `N`: artifact 제거 후 남은 window 개수
- `9`: FC3, FCz, FC4, C3, Cz, C4, CP3, CPz, CP4
- `320`: 160 Hz 기준 2초 window

PyTorch 모델 예시:

```python
import numpy as np
import torch
from pathlib import Path

data_dir = Path("data/BCI_EEG_Preprocessed_Data_103Subjects")

X_train = np.load(data_dir / "X_train.npy").astype("float32")
y_train = np.load(data_dir / "y_train.npy").astype("int64")

# 모델 구현에 따라 다음 둘 중 하나를 선택한다.
X_train_tensor = torch.from_numpy(X_train)              # (N, 9, 320)
X_train_tensor_4d = torch.from_numpy(X_train[:, None])  # (N, 1, 9, 320)
y_train_tensor = torch.from_numpy(y_train)
```

CSP+LDA와 SVM baseline 실험에서는 우선 `X_train_unstd.npy`, `X_val_unstd.npy`, `X_test_unstd.npy`를 사용한다. CSP는 공간 필터 자체가 분산 구조를 사용하고, 현재 SVM baseline은 channel별 log-variance feature를 사용하므로 표준화 전 filtered 신호가 더 해석하기 쉽다.

## 비교할 모델 후보

제안서와 발표자료에서 언급된 후보는 다음과 같다.

| 모델 | 목적 |
| --- | --- |
| CSP+LDA | 고전적 MI EEG baseline. 딥러닝 모델 대비 기준 성능 확인용 |
| SVM | 발표자료에 포함된 추가 baseline 후보 |
| EEGNet | 경량 EEG CNN. 실시간 추론과 Unity 연동을 고려한 주 후보 |
| ShallowConvNet | EEG 시계열 CNN 비교 모델 |
| CNN-LSTM | 발표자료에 포함된 시계열 딥러닝 후보 |

우선순위는 `CSP+LDA -> EEGNet -> ShallowConvNet`으로 두는 것이 좋다. 시간이 허용되면 SVM, CNN-LSTM을 추가 비교한다.

## 평가 지표

모델 비교 시 동일한 데이터 split과 동일한 평가 지표를 사용한다.

| 지표 | 설명 |
| --- | --- |
| Accuracy | Left/Right 전체 분류 정확도 |
| Macro F1-score | class balance 영향을 줄이기 위한 평균 F1 |
| Confusion matrix | Left/Right 오분류 방향 확인 |
| Subject-wise score | 특정 피험자에서 성능이 낮은지 확인 |
| Inference latency | Unity 연동을 고려한 single-window 또는 batch 추론 시간 |
| Parameter count | 실시간 제어에 적합한 모델 크기 비교 |
| Rest false command rate | T0 입력에서 Left/Right 명령이 잘못 발생하는 비율 |

Rest false command 평가는 T1/T2 이진 분류 모델 학습 이후 별도로 수행한다. `X_rest_val.npy`는 confidence threshold를 정하는 데 사용하고, `X_rest_test.npy`는 정해진 threshold에서 실제 false command rate를 보고하는 데 사용한다.

## 현재 실험 결과 요약

현재까지는 동일한 103명 subject split에서 CSP+LDA, SVM, EEGNet, ShallowConvNet을 비교했다.

| 모델 | 학습/실행 위치 | Test accuracy | Test macro F1 | Test Rest false command @0.9 | 비고 |
| --- | --- | ---: | ---: | ---: | --- |
| CSP+LDA | 로컬 | 0.5589 | 0.5589 | 미평가 | 가장 빠른 baseline |
| SVM | 로컬 | 0.5034 | 0.4758 | 미평가 | channel log-variance 기반 linear SVM baseline |
| EEGNet | Colab GPU | 0.5920 | 0.5893 | 0.0562 (202/3596) | Left/Right 분류 성능이 현재 최고 |
| ShallowConvNet | Colab GPU | 0.5792 | 0.5763 | 0.0231 (83/3596) | Rest false command가 EEGNet보다 낮음 |

현재 결과만 보면 Left/Right 분류 성능은 EEGNet이 가장 높고, Rest/T0에서 불필요한 명령을 줄이는 관점은 ShallowConvNet이 더 안정적이다. SVM은 CSP+LDA보다 낮아 현재 주 후보는 아니다. 최종 모델 선택은 validation Rest threshold와 test 성능을 함께 보고 결정한다.

## 권장 작업 순서

1. `config/`에 데이터 경로와 공통 설정을 저장한다.
2. `data/`에 NumPy array를 읽는 공통 data loader를 만든다.
3. 먼저 `X_train.npy`, `y_train.npy`, `X_val.npy`, `y_val.npy`, `X_test.npy`, `y_test.npy`를 불러와 shape과 label 분포를 확인한다.
4. CSP+LDA baseline을 구현해 test accuracy와 macro F1-score를 기록한다.
5. EEGNet을 구현하고 validation 기준으로 hyperparameter를 조정한다.
6. ShallowConvNet 또는 추가 모델을 같은 split에서 학습한다.
7. 모든 모델을 test split으로 최종 비교한다.
8. `metadata_test.csv`를 이용해 피험자별, run별 오분류 분포를 확인한다.
9. `X_rest_val.npy`로 confidence threshold를 정하고, `X_rest_test.npy`로 false command를 평가한다.
10. 최종 선택 모델의 추론 결과를 Unity 연동 메시지 형식으로 보낼 수 있게 정리한다.

## 주의할 점

- 피험자 단위 split을 유지한다. window 단위 random split은 금지한다.
- validation set은 모델 선택과 threshold 조정에 사용하고, test set은 최종 보고용으로 남긴다.
- T0는 Left/Right 학습 class에 넣지 않는다. Rest/Hold/Straight 검토용 별도 데이터로 사용한다.
- 전처리 배열은 이미 Train T1/T2 기준 mean/std로 표준화되어 있다. 모델 코드에서 추가 표준화를 한다면 train 통계만 사용해야 한다.
- `train_channel_mean.npy`, `train_channel_std.npy`는 이후 실시간 입력 또는 새 데이터 전처리 시 동일한 표준화 기준을 재사용하기 위한 파일이다.
- 성능 비교 결과에는 정확도만 적지 말고 macro F1-score, 혼동 행렬, 추론 시간, 모델 크기, Rest false command rate를 함께 기록한다.

## 최소 로딩 체크

모델 구현을 시작하기 전에 다음 명령으로 전처리 배열 shape과 label 분포를 먼저 확인한다.

```bash
python models/scripts/check_data.py --config models/config/default_config.json
```

현재 산출물 기준 예상 출력의 핵심은 다음과 같다.

```text
train: X=(18096, 9, 320), y={0: 9216, 1: 8880}
val: X=(4011, 9, 320), y={0: 1981, 1: 2030}
test: X=(3931, 9, 320), y={0: 2037, 1: 1894}
rest_train: X=(17990, 9, 320)
rest_val: X=(3793, 9, 320)
rest_test: X=(3596, 9, 320)
```

코드로 직접 확인하려면 다음 정도면 충분하다.

```python
import numpy as np
from collections import Counter
from pathlib import Path

data_dir = Path("data/BCI_EEG_Preprocessed_Data_103Subjects")

for split in ["train", "val", "test"]:
    X = np.load(data_dir / f"X_{split}.npy", mmap_mode="r")
    y = np.load(data_dir / f"y_{split}.npy")
    print(split, X.shape, X.dtype, Counter(y.tolist()))

for split in ["train", "val", "test"]:
    X_rest = np.load(data_dir / f"X_rest_{split}.npy", mmap_mode="r")
    print("rest", split, X_rest.shape, X_rest.dtype)
```

예상되는 핵심 shape은 `(N, 9, 320)`이다. 현재 산출물 기준 label 분포는 train `0: 9216, 1: 8880`, validation `0: 1981, 1: 2030`, test `0: 2037, 1: 1894`이다.

## 현재 실행 명령

CSP+LDA baseline:

```bash
python models/scripts/train_csp_lda.py --config models/config/default_config.json
```

SVM baseline:

```bash
python models/scripts/train_svm.py --config models/config/default_config.json
```

EEGNet 학습:

```bash
python models/scripts/train_eegnet.py --config models/config/default_config.json --epochs 50
```

ShallowConvNet 학습:

```bash
python models/scripts/train_shallow_convnet.py --config models/config/default_config.json --epochs 50
```

EEGNet Rest false command 평가:

```bash
python models/scripts/evaluate_rest.py --model eegnet --config models/config/default_config.json --checkpoint models/checkpoints/eegnet_best.pt
```

ShallowConvNet Rest false command 평가:

```bash
python models/scripts/evaluate_rest.py --model shallow_convnet --config models/config/default_config.json --checkpoint models/checkpoints/shallow_convnet_best.pt
```

Colab이나 Google Drive 경로가 기본 `data/` 위치와 다르면 코드 수정 대신 다음 옵션으로 경로만 바꾼다.

```bash
python models/scripts/train_eegnet.py \
  --config models/config/default_config.json \
  --data-dir /content/drive/MyDrive/path/to/BCI_EEG_Preprocessed_Data_103Subjects \
  --results-dir /content/drive/MyDrive/path/to/results \
  --checkpoints-dir /content/drive/MyDrive/path/to/checkpoints \
  --epochs 50
```

ShallowConvNet도 같은 방식으로 실행한다.

```bash
python models/scripts/train_shallow_convnet.py \
  --config models/config/default_config.json \
  --data-dir /content/drive/MyDrive/path/to/BCI_EEG_Preprocessed_Data_103Subjects \
  --results-dir /content/drive/MyDrive/path/to/results \
  --checkpoints-dir /content/drive/MyDrive/path/to/checkpoints \
  --epochs 50
```

## Unity 연동으로 이어지는 방식

모델 학습 단계의 최종 출력은 단순한 test 성능표만이 아니라 Unity 제어 단계에서 사용할 수 있는 추론 인터페이스여야 한다. 각 window에 대해 모델은 다음 정보를 만들 수 있어야 한다.

```json
{
  "label": "Left 또는 Right 또는 Hold",
  "confidence": 0.0,
  "timestamp": 0.0,
  "source": {
    "subject": "S009",
    "run": "04",
    "trial": 1,
    "window_idx": 0
  }
}
```

Left로 분류되면 가상 휠체어 좌회전, Right로 분류되면 우회전, confidence가 threshold보다 낮거나 Rest/Hold로 판단되면 직진 유지 또는 명령 보류로 연결한다. 이 threshold 설계에 Validation T0와 Test T0를 사용한다.
