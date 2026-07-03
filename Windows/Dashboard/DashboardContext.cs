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

    /// <summary>
    /// Manual reclassify hook: (event, newCategory). Null when the dashboard is
    /// built without a persistence layer (e.g. in tests). The Timeline tab only
    /// offers reclassification when this is set.
    /// </summary>
    public Action<GilEvent, GilEventCategory>? Reclassify { get; }

    public DashboardContext(
        IReadOnlyList<Wallet.Wallet> wallets,
        IReadOnlyList<GilEvent> events,
        DateTimeOffset now,
        Theme.Theme theme,
        Action<GilEvent, GilEventCategory>? reclassify = null)
    {
        Wallets    = wallets;
        Events     = events;
        Now        = now;
        Theme      = theme;
        Reclassify = reclassify;

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
