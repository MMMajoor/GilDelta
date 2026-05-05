using System;

namespace GilDelta.Wallet;

public readonly record struct WalletDiff(
    WalletId Id,
    long Before,
    long After,
    DateTimeOffset At
)
{
    public long Delta => After - Before;
}
