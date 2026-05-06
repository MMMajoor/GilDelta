using System.Collections.Generic;
using GilDelta.Events;
using GilDelta.Theme;
using GilDelta.Wallet;
using GilDelta.Windows.Widget;

namespace GilDelta.Tests.Windows.Widget;

public class WidgetContextTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-05-05T18:00:00+09:00");

    [Fact]
    public void Total_sums_all_wallets()
    {
        var ctx = new WidgetContext(
            wallets: new[]
            {
                new GilDelta.Wallet.Wallet(new WalletId(WalletKind.Self, ""), 5_000_000),
                new GilDelta.Wallet.Wallet(new WalletId(WalletKind.Retainer, "A"), 2_000_000),
                new GilDelta.Wallet.Wallet(new WalletId(WalletKind.FreeCompanyChest, "FC"), 18_000_000),
            },
            recentEvents: Array.Empty<GilEvent>(),
            now: Now,
            theme: MidnightCoin.Instance);

        Assert.Equal(25_000_000, ctx.Total);
    }

    [Fact]
    public void TodayDelta_sums_today_events_only()
    {
        var ctx = new WidgetContext(
            wallets: Array.Empty<GilDelta.Wallet.Wallet>(),
            recentEvents: new[]
            {
                new GilEvent(Now,                  new WalletId(WalletKind.Self, ""), +500, GilEventCategory.Misc, null),
                new GilEvent(Now.AddHours(-1),     new WalletId(WalletKind.Self, ""), +200, GilEventCategory.Misc, null),
                new GilEvent(Now.AddDays(-2),      new WalletId(WalletKind.Self, ""), +999, GilEventCategory.Misc, null),
            },
            now: Now,
            theme: MidnightCoin.Instance);

        Assert.Equal(700, ctx.TodayDelta);
    }

    [Fact]
    public void Breakdown_groups_by_wallet_kind()
    {
        var ctx = new WidgetContext(
            wallets: new[]
            {
                new GilDelta.Wallet.Wallet(new WalletId(WalletKind.Self, ""), 5_000_000),
                new GilDelta.Wallet.Wallet(new WalletId(WalletKind.Retainer, "A"), 2_000_000),
                new GilDelta.Wallet.Wallet(new WalletId(WalletKind.Retainer, "B"), 4_000_000),
                new GilDelta.Wallet.Wallet(new WalletId(WalletKind.FreeCompanyChest, "FC"), 18_000_000),
            },
            recentEvents: Array.Empty<GilEvent>(),
            now: Now,
            theme: MidnightCoin.Instance);

        Assert.Equal(5_000_000, ctx.SelfTotal);
        Assert.Equal(6_000_000, ctx.RetainerTotal);
        Assert.Equal(18_000_000, ctx.FreeCompanyTotal);
    }
}
