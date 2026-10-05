def _torch():
    try:
        import torch
        import torch.nn as nn
    except ImportError as exc:
        raise RuntimeError(
            "PyTorch is required for EEGNet. Run this script in Colab or install torch."
        ) from exc
    return torch, nn


def create_eegnet(
    channels: int = 9,
    samples: int = 320,
    classes: int = 2,
    dropout: float = 0.25,
):
    torch, nn = _torch()

    class EEGNet(nn.Module):
        def __init__(self):
            super().__init__()
            self.features = nn.Sequential(
                nn.Conv2d(1, 8, kernel_size=(1, 64), padding=(0, 32), bias=False),
                nn.BatchNorm2d(8),
                nn.Conv2d(8, 16, kernel_size=(channels, 1), groups=8, bias=False),
                nn.BatchNorm2d(16),
                nn.ELU(),
                nn.AvgPool2d(kernel_size=(1, 4)),
                nn.Dropout(dropout),
                nn.Conv2d(16, 16, kernel_size=(1, 16), padding=(0, 8), groups=16, bias=False),
                nn.Conv2d(16, 16, kernel_size=(1, 1), bias=False),
                nn.BatchNorm2d(16),
                nn.ELU(),
                nn.AvgPool2d(kernel_size=(1, 8)),
                nn.Dropout(dropout),
            )
            with torch.no_grad():
                dummy = torch.zeros(1, 1, channels, samples)
                flat = self.features(dummy).reshape(1, -1).shape[1]
            self.classifier = nn.Linear(flat, classes)

        def forward(self, x):
            x = self.features(x)
            return self.classifier(x.reshape(x.shape[0], -1))

    return EEGNet()
