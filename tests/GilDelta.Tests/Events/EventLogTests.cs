using System.IO;
using System.Linq;
using GilDelta.Events;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events;

public class EventLogTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public EventLogTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "GilDeltaTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "events.jsonl");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    private static GilEvent E(DateTimeOffset at, long amount, GilEventCategory cat = GilEventCategory.Misc) =>
        new(at, new WalletId(WalletKind.Self, ""), amount, cat, null);

    [Fact]
    public void LoadFromStore_populates_All()
    {
        var store = new EventStore(_path);
        var t0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");
        store.Append(E(t0, 100));
        store.Append(E(t0.AddSeconds(1), -50));

        var log = new EventLog();
        log.LoadFromStore(store);

        Assert.Equal(2, log.All.Count);
        Assert.Equal(100, log.All[0].Amount);
    }

    [Fact]
    public void Add_appends_to_All_in_order()
    {
        var log = new EventLog();
        var t0 = DateTimeOffset.UnixEpoch;
        log.Add(E(t0, 1));
        log.Add(E(t0.AddSeconds(1), 2));

        Assert.Equal(2, log.All.Count);
        Assert.Equal(1, log.All[0].Amount);
        Assert.Equal(2, log.All[1].Amount);
    }

    [Fact]
    public void Since_returns_events_at_or_after_threshold()
    {
        var log = new EventLog();
        var t0 = DateTimeOffset.Parse("2026-05-05T00:00:00+09:00");
        log.Add(E(t0,                    1));
        log.Add(E(t0.AddHours(1),        2));
        log.Add(E(t0.AddHours(2),        3));

        var since = log.Since(t0.AddHours(1)).ToList();

        Assert.Equal(2, since.Count);
        Assert.Equal(2, since[0].Amount);
        Assert.Equal(3, since[1].Amount);
    }

    [Fact]
    public void Replace_swaps_matching_instance()
    {
        var log = new EventLog();
        var t0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");
        var original = E(t0, 100, GilEventCategory.Misc);
        log.Add(original);
        log.Add(E(t0.AddSeconds(1), 200));

        var updated = original with { Category = GilEventCategory.SubmarineReturn };
        var ok = log.Replace(original, updated);

        Assert.True(ok);
        Assert.Equal(GilEventCategory.SubmarineReturn, log.All[0].Category);
        Assert.Equal(200, log.All[1].Amount);   // sibling untouched
    }

    [Fact]
    public void Replace_returns_false_when_not_found()
    {
        var log = new EventLog();
        log.Add(E(DateTimeOffset.UnixEpoch, 1));

        var absent = E(DateTimeOffset.UnixEpoch.AddDays(1), 999);
        var ok = log.Replace(absent, absent with { Category = GilEventCategory.Repair });

        Assert.False(ok);
    }

    [Fact]
    public void Clear_empties_All()
    {
        var log = new EventLog();
        log.Add(E(DateTimeOffset.UnixEpoch, 1));
        log.Clear();

        Assert.Empty(log.All);
    }
}
