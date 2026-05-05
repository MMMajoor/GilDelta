using System;
using System.Collections.Generic;

namespace GilDelta.Events;

/// <summary>
/// In-memory cache of all events for the active character.
/// Loaded once from <see cref="EventStore"/> at startup; updated via Add()
/// when WalletWatcher emits new events. Windows read from this list, never
/// directly from disk.
/// </summary>
public sealed class EventLog
{
    private readonly List<GilEvent> _events = new();

    public IReadOnlyList<GilEvent> All => _events;

    public void LoadFromStore(EventStore store)
    {
        _events.Clear();
        _events.AddRange(store.LoadAll());
    }

    public void Add(GilEvent ev) => _events.Add(ev);

    public void Clear() => _events.Clear();

    public IEnumerable<GilEvent> Since(DateTimeOffset threshold)
    {
        foreach (var ev in _events)
            if (ev.Timestamp >= threshold)
                yield return ev;
    }
}
