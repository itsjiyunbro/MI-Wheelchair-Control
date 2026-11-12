"""LMDA-Net adapted for this project's motor-imagery windows.

Reference: https://github.com/MiaoZhengQing/LMDA-Code/blob/main/lmda_model.py
Miao et al., NeuroImage (2023), article 120209.
Uses the corrected, registered EEGDepthAttention module from lmda_model.py,
not the per-forward convolution in the older model_paper.py.
"""


def create_lmda(
    channels: int = 9,
    samples: int = 320,
    classes: int = 2,
    depth: int = 9,
    kernel: int = 75,
    channel_depth1: int = 24,
    channel_depth2: int = 9,
    avepool: int = 5,
    dropout: float = 0.65,
    attention_kernel: int = 7,
):
    """Return a PyTorch model producing raw logits from (N, 1, C, T) or (N, C, T).

    Defaults preserve upstream architecture hyperparameters, with C=9, T=320
    and two output classes. Only PyTorch is needed; CPU inference is supported.
    Save/load using state_dict, as with the existing project models.
    """
    try:
        import torch
        from torch import nn
    except ImportError as exc:
        raise RuntimeError("PyTorch is required for LMDA-Net. Use Colab or install torch.") from exc

    dimensions = (channels, samples, classes, depth, kernel, channel_depth1,
                  channel_depth2, avepool, attention_kernel)
    if any(not isinstance(value, int) or isinstance(value, bool) or value < 1 for value in dimensions):
        raise ValueError("LMDA dimensions and kernel sizes must be positive integers")
    width = samples - kernel + 1
    if width < avepool:
        raise ValueError("samples - kernel + 1 must be at least avepool")
    if attention_kernel % 2 != 1:
        raise ValueError("attention_kernel must be odd to preserve feature depth")
    if not 0 <= dropout <= 1:
        raise ValueError("dropout must be between 0 and 1")

    class EEGDepthAttention(nn.Module):
        def __init__(self):
            super().__init__()
            self.adaptive_pool = nn.AdaptiveAvgPool2d((1, width))
            self.conv = nn.Conv2d(1, 1, (attention_kernel, 1),
                                  padding=(attention_kernel // 2, 0))
            self.softmax = nn.Softmax(dim=-2)

        def forward(self, x):
            pooled = self.adaptive_pool(x).transpose(-2, -3)
            weights = self.softmax(self.conv(pooled)).transpose(-2, -3)
            return x * weights * channel_depth1

    class LMDA(nn.Module):
        def __init__(self):
            super().__init__()
            self.channel_weight = nn.Parameter(torch.empty(depth, 1, channels))
            nn.init.xavier_uniform_(self.channel_weight)
            self.time_conv = nn.Sequential(
                nn.Conv2d(depth, channel_depth1, 1, bias=False),
                nn.BatchNorm2d(channel_depth1),
                nn.Conv2d(channel_depth1, channel_depth1, (1, kernel),
                          groups=channel_depth1, bias=False),
                nn.BatchNorm2d(channel_depth1),
                nn.GELU(),
            )
            self.depthAttention = EEGDepthAttention()
            # Retain upstream's attribute spelling for state_dict compatibility.
            self.chanel_conv = nn.Sequential(
                nn.Conv2d(channel_depth1, channel_depth2, 1, bias=False),
                nn.BatchNorm2d(channel_depth2),
                nn.Conv2d(channel_depth2, channel_depth2, (channels, 1),
                          groups=channel_depth2, bias=False),
                nn.BatchNorm2d(channel_depth2),
                nn.GELU(),
            )
            # Equivalent time-only pooling to upstream AvgPool3d((1, 1, avepool))
            # on its four-dimensional feature tensor, with explicit 2D semantics.
            self.norm = nn.Sequential(nn.AvgPool2d((1, avepool)), nn.Dropout(dropout))
            # Analytic size avoids changing BatchNorm statistics during construction.
            self.classifier = nn.Linear(channel_depth2 * (width // avepool), classes)
            for module in self.modules():
                if isinstance(module, (nn.Conv2d, nn.Linear)):
                    nn.init.xavier_uniform_(module.weight)
                    if module.bias is not None:
                        nn.init.zeros_(module.bias)
                elif isinstance(module, nn.BatchNorm2d):
                    nn.init.ones_(module.weight)
                    nn.init.zeros_(module.bias)

        def forward(self, x):
            if x.ndim == 3:
                x = x.unsqueeze(1)
            if x.ndim != 4 or tuple(x.shape[1:]) != (1, channels, samples):
                raise ValueError(f"expected input shape (N, 1, {channels}, {samples}) "
                                 f"or (N, {channels}, {samples}), got {tuple(x.shape)}")
            weighted = torch.einsum("bdct,hdc->bhct", x, self.channel_weight)
            features = self.chanel_conv(self.depthAttention(self.time_conv(weighted)))
            return self.classifier(self.norm(features).flatten(1))

    return LMDA()


def lmda_from_config(config: dict):
    """Use the same architecture options for training and Rest evaluation."""
    options = config.get("lmda", {})
    defaults = {"depth": 9, "kernel": 75, "channel_depth1": 24,
                "channel_depth2": 9, "avepool": 5, "attention_kernel": 7}
    return create_lmda(
        channels=int(config.get("channels", 9)),
        samples=int(config.get("samples", 320)),
        classes=2,
        dropout=float(options.get("dropout", 0.65)),
        **{key: int(options.get(key, default)) for key, default in defaults.items()},
    )
