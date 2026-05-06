using System;
using System.Collections.Generic;
using System.Linq;
using GilDelta.Events;
using GilDelta.Theme;
using GilDelta.Wallet;

namespace GilDelta.Windows.Widget;

public sealed class WidgetContext
{
    public IReadOnlyList<Wallet.Wallet> Wallets { get; }
    public IReadOnlyList<GilEvent> RecentEvents { get; }
    public DateTimeOffset Now { get; }
    public Theme.Theme Theme { get; }

    public long Total { get; }
    public long SelfTotal { get; }
    public long RetainerTotal { get; }
    public long FreeCompanyTotal { get; }

    public long TodayDelta { get; }
    public long WeekDelta  { get; }
    public long MonthDelta { get; }

    public WidgetContext(
        IReadOnlyList<Wallet.Wallet> wallets,
        IReadOnlyList<GilEvent> recentEvents,
        DateTimeOffset now,
        Theme.Theme theme)
    {
        Wallets = wallets;
        RecentEvents = recentEvents;
        Now = now;
        Theme = theme;

        Total = 0;
        SelfTotal = RetainerTotal = FreeCompanyTotal = 0;
        foreach (var w in wallets)
        {
            Total += w.Amount;
            switch (w.Id.Kind)
            {
                case WalletKind.Self:             SelfTotal        += w.Amount; break;
                case WalletKind.Retainer:         RetainerTotal    += w.Amount; break;
                case WalletKind.FreeCompanyChest: FreeCompanyTotal += w.Amount; break;
            }
        }

        var dayStart   = new DateTimeOffset(now.Date, now.Offset);
        var weekStart  = dayStart.AddDays(-((int)now.DayOfWeek));
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);

        TodayDelta = recentEvents.Where(e => e.Timestamp >= dayStart  ).Sum(e => e.Amount);
        WeekDelta  = recentEvents.Where(e => e.Timestamp >= weekStart ).Sum(e => e.Amount);
        MonthDelta = recentEvents.Where(e => e.Timestamp >= monthStart).Sum(e => e.Amount);
    }
}
