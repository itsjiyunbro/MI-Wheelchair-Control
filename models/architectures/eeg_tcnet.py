def _torch():
    try:
        import torch
        import torch.nn as nn
    except ImportError as exc:
        raise RuntimeError(
            "PyTorch is required for EEGNet. Run this script in Colab or install torch."
        ) from exc
    return torch, nn


def create_eeg_tcnet(
    channels: int = 9,
    samples: int = 320,
    classes: int = 2,
    dropout: float = 0.25,
):
    torch, nn = _torch()

    class CausalConv1d(nn.Conv1d):
        def __init__(self, in_channels, out_channels, kernel_size, stride=1, dilation=1, groups=1, bias=True):
            self.__padding = (kernel_size - 1) * dilation
            super(CausalConv1d, self).__init__(
                in_channels, out_channels, kernel_size=kernel_size, stride=stride,
                padding=self.__padding, dilation=dilation, groups=groups, bias=bias
            )

        def forward(self, x):
            result = super(CausalConv1d, self).forward(x)
            if self.__padding != 0:
                return result[:, :, :-self.__padding]
            return result

    class TCNBlock(nn.Module):
        def __init__(self, in_channels, filters, kernel_size, dilation, dropout_rate):
            super().__init__()
            self.conv1 = CausalConv1d(in_channels, filters, kernel_size, dilation=dilation, bias=False)
            self.bn1 = nn.BatchNorm1d(filters)
            self.elu1 = nn.ELU()
            self.drop1 = nn.Dropout(dropout_rate)
            
            self.conv2 = CausalConv1d(filters, filters, kernel_size, dilation=dilation, bias=False)
            self.bn2 = nn.BatchNorm1d(filters)
            self.elu2 = nn.ELU()
            self.drop2 = nn.Dropout(dropout_rate)

            self.downsample = nn.Conv1d(in_channels, filters, 1) if in_channels != filters else None
            self.elu3 = nn.ELU()

        def forward(self, x):
            out = self.conv1(x)
            out = self.bn1(out)
            out = self.elu1(out)
            out = self.drop1(out)

            out = self.conv2(out)
            out = self.bn2(out)
            out = self.elu2(out)
            out = self.drop2(out)

            res = x if self.downsample is None else self.downsample(x)
            return self.elu3(out + res)

    class EEGTCNet(nn.Module):
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
            
            tcn_filters = 16
            tcn_kernel_size = 4
            self.tcn = nn.Sequential(
                TCNBlock(16, tcn_filters, tcn_kernel_size, dilation=1, dropout_rate=dropout),
                TCNBlock(tcn_filters, tcn_filters, tcn_kernel_size, dilation=2, dropout_rate=dropout),
            )

            with torch.no_grad():
                dummy = torch.zeros(1, 1, channels, samples)
                feat_1d = self.features(dummy).squeeze(2)
                tcn_out = self.tcn(feat_1d)
                flat = tcn_out.reshape(1, -1).shape[1]
            self.classifier = nn.Linear(flat, classes)

        def forward(self, x):
            x = self.features(x)
            x = x.squeeze(2) 
            x = self.tcn(x)
            return self.classifier(x.reshape(x.shape[0], -1))

    return EEGTCNet()