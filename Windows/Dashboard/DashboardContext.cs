using System;
using System.Collections.Generic;
using GilDelta.Events;

namespace GilDelta.Windows.Dashboard;

public sealed class DashboardContext
{
    public IReadOnlyList<Wallet.Wallet> Wallets { get; }
    public IReadOnlyList<GilEvent> Events { get; }
    public DateTimeOffset Now { get; }
    public Theme.Theme Theme { get; }

    public long Total { get; }

    public DashboardContext(
        IReadOnlyList<Wallet.Wallet> wallets,
        IReadOnlyList<GilEvent> events,
        DateTimeOffset now,
        Theme.Theme theme)
    {
        Wallets = wallets;
        Events  = events;
        Now     = now;
        Theme   = theme;

        long total = 0;
        foreach (var w in wallets) total += w.Amount;
        Total = total;
    }

    /// <summary>Net gil change on the local day containing <paramref name="day"/>.</summary>
    public long DailyNet(DateTimeOffset day)
    {
        var start = new DateTimeOffset(day.Date, day.Offset);
        var end   = start.AddDays(1);
        long sum  = 0;
        foreach (var ev in Events)
            if (ev.Timestamp >= start && ev.Timestamp < end)
                sum += ev.Amount;
        return sum;
    }

    /// <summary>Net gil change in [from, to], grouped by category.</summary>
    public IReadOnlyDictionary<GilEventCategory, long> NetByCategory(DateTimeOffset from, DateTimeOffset to)
    {
        var dict = new Dictionary<GilEventCategory, long>();
        foreach (var ev in Events)
        {
            if (ev.Timestamp < from || ev.Timestamp > to) continue;
            dict.TryGetValue(ev.Category, out var prev);
            dict[ev.Category] = prev + ev.Amount;
        }
        return dict;
    }
}
