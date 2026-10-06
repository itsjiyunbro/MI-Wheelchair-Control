def _torch():
    try:
        import torch
        import torch.nn as nn
    except ImportError as exc:
        raise RuntimeError(
            "PyTorch is required for ShallowConvNet. Run this script in Colab or install torch."
        ) from exc
    return torch, nn


def create_shallow_convnet(
    channels: int = 9,
    samples: int = 320,
    classes: int = 2,
    filters: int = 40,
    temporal_kernel: int = 25,
    pool_size: int = 75,
    pool_stride: int = 15,
    dropout: float = 0.5,
):
    torch, nn = _torch()

    class SquareLayer(nn.Module):
        def forward(self, x):
            return x * x

    class SafeLogLayer(nn.Module):
        def forward(self, x):
            return torch.log(torch.clamp(x, min=1e-6))

    class ShallowConvNet(nn.Module):
        def __init__(self):
            super().__init__()
            self.features = nn.Sequential(
                nn.Conv2d(1, filters, kernel_size=(1, temporal_kernel), bias=False),
                nn.Conv2d(filters, filters, kernel_size=(channels, 1), bias=False),
                nn.BatchNorm2d(filters),
                SquareLayer(),
                nn.AvgPool2d(kernel_size=(1, pool_size), stride=(1, pool_stride)),
                SafeLogLayer(),
                nn.Dropout(dropout),
            )
            with torch.no_grad():
                dummy = torch.zeros(1, 1, channels, samples)
                flat = self.features(dummy).reshape(1, -1).shape[1]
            self.classifier = nn.Linear(flat, classes)

        def forward(self, x):
            x = self.features(x)
            return self.classifier(x.reshape(x.shape[0], -1))

    return ShallowConvNet()
