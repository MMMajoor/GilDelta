using System.Numerics;

namespace GilDelta.Theme;

public sealed record Theme(
    Vector4 BgPrimary,
    Vector4 BgSecondary,
    Vector4 TextPrimary,
    Vector4 TextMuted,
    Vector4 TextAccent,
    Vector4 PositiveDelta,
    Vector4 NegativeDelta,
    string FontMono,
    string FontProportional,
    float CornerRadius
);
