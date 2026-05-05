using System;
using GilDelta.Wallet;

namespace GilDelta.Events;

public sealed record GilEvent(
    DateTimeOffset Timestamp,
    WalletId Wallet,
    long Amount,
    GilEventCategory Category,
    string? Note
);
