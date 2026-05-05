using System.Numerics;

namespace GilDelta.Theme;

public static class MidnightCoin
{
    public static readonly Theme Instance = new(
        BgPrimary:        new Vector4(0.094f, 0.094f, 0.106f, 1.0f),  // #18181b
        BgSecondary:      new Vector4(0.153f, 0.153f, 0.165f, 1.0f),  // #27272a
        TextPrimary:      new Vector4(0.992f, 0.945f, 0.804f, 1.0f),  // #fde68a
        TextMuted:        new Vector4(0.443f, 0.443f, 0.478f, 1.0f),  // #71717a
        TextAccent:       new Vector4(0.984f, 0.749f, 0.141f, 1.0f),  // #fbbf24
        PositiveDelta:    new Vector4(0.204f, 0.827f, 0.600f, 1.0f),  // #34d399
        NegativeDelta:    new Vector4(0.973f, 0.443f, 0.443f, 1.0f),  // #f87171
        FontMono:         "JetBrains Mono",
        FontProportional: "Segoe UI",
        CornerRadius:     8.0f
    );
}
