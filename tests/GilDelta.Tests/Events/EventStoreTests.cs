using System.Collections.Generic;
using System.IO;
using System.Linq;
using GilDelta.Events;
using GilDelta.Wallet;

namespace GilDelta.Tests.Events;

public class EventStoreTests : IDisposable
{
    private readonly string _dir;

    public EventStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "GilDeltaTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    private string Path_ => Path.Combine(_dir, "events.jsonl");

    private static GilEvent SampleEvent(DateTimeOffset at, long amount = 100, string note = "n") =>
        new(at, new WalletId(WalletKind.Self, ""), amount, GilEventCategory.Misc, note);

    [Fact]
    public void Append_writes_one_jsonl_line()
    {
        var store = new EventStore(Path_);
        var ev = SampleEvent(DateTimeOffset.Parse("2026-05-05T14:00:00+09:00"));

        store.Append(ev);

        var lines = File.ReadAllLines(Path_);
        Assert.Single(lines);
        Assert.Contains("\"category\":\"Misc\"", lines[0]);
        Assert.Contains("\"amount\":100", lines[0]);
        Assert.Contains("\"v\":1", lines[0]);
    }

    [Fact]
    public void LoadAll_round_trips_appended_events()
    {
        var store = new EventStore(Path_);
        var t0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");
        store.Append(SampleEvent(t0, 100, "first"));
        store.Append(SampleEvent(t0.AddSeconds(1), -50, "second"));

        var loaded = store.LoadAll().ToList();

        Assert.Equal(2, loaded.Count);
        Assert.Equal(100, loaded[0].Amount);
        Assert.Equal(-50, loaded[1].Amount);
        Assert.Equal("first", loaded[0].Note);
    }

    [Fact]
    public void LoadAll_returns_empty_when_file_missing()
    {
        var store = new EventStore(Path_);

        Assert.Empty(store.LoadAll());
    }

    [Fact]
    public void LoadAll_skips_corrupt_lines()
    {
        File.WriteAllLines(Path_, new[]
        {
            """{"v":1,"ts":"2026-05-05T14:00:00+09:00","wallet":{"kind":"Self","id":""},"amount":100,"category":"Misc","note":"good"}""",
            """{"v":1, this is not json""",
            "",
            """{"v":1,"ts":"2026-05-05T15:00:00+09:00","wallet":{"kind":"Self","id":""},"amount":200,"category":"Misc","note":"good2"}""",
        });

        var store = new EventStore(Path_);

        var loaded = store.LoadAll().ToList();

        Assert.Equal(2, loaded.Count);
        Assert.Equal(100, loaded[0].Amount);
        Assert.Equal(200, loaded[1].Amount);
    }

    [Fact]
    public void LoadAll_skips_unknown_schema_version()
    {
        File.WriteAllLines(Path_, new[]
        {
            """{"v":1,"ts":"2026-05-05T14:00:00+09:00","wallet":{"kind":"Self","id":""},"amount":100,"category":"Misc","note":"v1"}""",
            """{"v":99,"ts":"2026-05-05T14:00:00+09:00","wallet":{"kind":"Self","id":""},"amount":200,"category":"Misc","note":"v99"}""",
        });

        var store = new EventStore(Path_);

        var loaded = store.LoadAll().ToList();

        Assert.Single(loaded);
        Assert.Equal(100, loaded[0].Amount);
    }

    [Fact]
    public void Reclassify_replaces_categories_via_inferrer_and_keeps_bak()
    {
        var store = new EventStore(Path_);
        var t0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");

        // Two existing events, both Misc.
        store.Append(SampleEvent(t0,            100, "old1"));
        store.Append(SampleEvent(t0.AddSeconds(1), 200, "old2"));

        // Reclassify in range; rule turns everything into NpcShopBuy.
        var inferrer = new Inferrer(new IInferenceRule[] { new RewriteToShopBuy() });
        store.Reclassify(t0, t0.AddSeconds(2), inferrer);

        var loaded = store.LoadAll().ToList();
        Assert.Equal(2, loaded.Count);
        Assert.All(loaded, ev => Assert.Equal(GilEventCategory.NpcShopBuy, ev.Category));

        // .bak should exist with original Misc events.
        var bakPath = Path_ + ".bak";
        Assert.True(File.Exists(bakPath));
        Assert.Contains("\"category\":\"Misc\"", File.ReadAllText(bakPath));
    }

    [Fact]
    public void Reclassify_leaves_out_of_range_events_untouched()
    {
        var store = new EventStore(Path_);
        var t0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");
        var laterT = t0.AddDays(5);

        store.Append(SampleEvent(t0,     100, "in-range"));
        store.Append(SampleEvent(laterT, 200, "out-of-range"));

        var inferrer = new Inferrer(new IInferenceRule[] { new RewriteToShopBuy() });
        store.Reclassify(t0, t0.AddHours(1), inferrer);

        var loaded = store.LoadAll().ToList();
        var inRange  = loaded.Single(e => e.Note == "old-note rewritten" || e.Amount == 100);
        var outRange = loaded.Single(e => e.Amount == 200);

        Assert.Equal(GilEventCategory.NpcShopBuy, inRange.Category);
        Assert.Equal(GilEventCategory.Misc, outRange.Category);
    }

    [Fact]
    public void SetCategory_rewrites_target_and_keeps_others_and_bak()
    {
        var store = new EventStore(Path_);
        var t0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");
        var target = SampleEvent(t0,             100, "shop");
        var other  = SampleEvent(t0.AddSeconds(1), 200, "keep");
        store.Append(target);
        store.Append(other);

        var updated = store.SetCategory(target, GilEventCategory.SubmarineReturn);

        Assert.NotNull(updated);
        Assert.Equal(GilEventCategory.SubmarineReturn, updated!.Category);
        Assert.Contains("manual reclassify; was=Misc", updated.Note);

        var loaded = store.LoadAll().ToList();
        Assert.Equal(2, loaded.Count);
        Assert.Equal(GilEventCategory.SubmarineReturn, loaded[0].Category);
        Assert.Equal(GilEventCategory.Misc, loaded[1].Category);   // untouched
        Assert.Equal("keep", loaded[1].Note);

        // Original preserved in .bak.
        var bak = File.ReadAllText(Path_ + ".bak");
        Assert.Contains("\"note\":\"shop\"", bak);
    }

    [Fact]
    public void SetCategory_returns_null_when_target_not_found()
    {
        var store = new EventStore(Path_);
        var t0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");
        store.Append(SampleEvent(t0, 100, "present"));

        var absent = SampleEvent(t0.AddDays(1), 999, "absent");
        var result = store.SetCategory(absent, GilEventCategory.Repair);

        Assert.Null(result);
        Assert.False(File.Exists(Path_ + ".bak"));   // no rewrite happened
    }

    [Fact]
    public void SetCategory_returns_null_when_category_unchanged()
    {
        var store = new EventStore(Path_);
        var t0 = DateTimeOffset.Parse("2026-05-05T14:00:00+09:00");
        var ev = SampleEvent(t0, 100, "n");   // already Misc
        store.Append(ev);

        var result = store.SetCategory(ev, GilEventCategory.Misc);

        Assert.Null(result);
    }

    /// <summary>Test helper: a fake rule that always rewrites to NpcShopBuy.</summary>
    private sealed class RewriteToShopBuy : IInferenceRule
    {
        public bool TryClassify(WalletDiff diff, GameContext ctx, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out GilEvent? ev)
        {
            ev = new GilEvent(diff.At, diff.Id, diff.Delta,
                GilEventCategory.NpcShopBuy, "old-note rewritten");
            return true;
        }
    }
}
