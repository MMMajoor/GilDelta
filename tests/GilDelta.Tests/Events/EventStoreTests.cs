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
}
