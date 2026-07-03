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

    /// <summary>
    /// Swaps the in-memory instance matching <paramref name="old"/> for
    /// <paramref name="updated"/> so the UI reflects a reclassification without a
    /// disk reload. Returns false if no matching event was found.
    /// </summary>
    public bool Replace(GilEvent old, GilEvent updated)
    {
        var idx = _events.FindIndex(e => e.Equals(old));
        if (idx < 0) return false;
        _events[idx] = updated;
        return true;
    }

    public void Clear() => _events.Clear();

    public IEnumerable<GilEvent> Since(DateTimeOffset threshold)
    {
        foreach (var ev in _events)
            if (ev.Timestamp >= threshold)
                yield return ev;
    }
}
