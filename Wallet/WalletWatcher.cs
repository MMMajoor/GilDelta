using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace GilDelta.Wallet;

public sealed class WalletWatcher : IDisposable
{
    private readonly IFramework _framework;
    private readonly WalletReader _reader;
    private readonly Func<bool> _canRead;
    private readonly Dictionary<WalletId, long> _last = new();

    public IReadOnlyList<Wallet> LastSnapshot { get; private set; } = Array.Empty<Wallet>();
    public event Action<WalletDiff>? OnDiff;

    /// <param name="canRead">
    /// Predicate gating whether native game memory is safe to read this tick
    /// (i.e. the player is logged in). Reading FFXIVClientStructs singletons
    /// during logout / game shutdown dereferences freed memory and produces a
    /// native access violation, which a try/catch cannot recover from — so we
    /// must skip the tick entirely rather than catch it.
    /// </param>
    public WalletWatcher(IFramework framework, WalletReader reader, Func<bool> canRead)
    {
        _framework = framework;
        _reader = reader;
        _canRead = canRead;
        _framework.Update += Tick;
    }

    /// <summary>
    /// Drops the cached per-wallet balances. Call on logout so the next login
    /// re-baselines instead of diffing against stale amounts and emitting
    /// phantom events.
    /// </summary>
    public void Reset()
    {
        _last.Clear();
        LastSnapshot = Array.Empty<Wallet>();
    }

    private void Tick(IFramework _)
    {
        if (!_canRead()) return;

        try
        {
            var snapshot = _reader.ReadAll();
            LastSnapshot = snapshot;
            var now = DateTimeOffset.Now;

            foreach (var w in snapshot)
            {
                if (_last.TryGetValue(w.Id, out var prev))
                {
                    if (prev != w.Amount)
                    {
                        OnDiff?.Invoke(new WalletDiff(w.Id, prev, w.Amount, now));
                    }
                }
                _last[w.Id] = w.Amount;
            }
        }
        catch
        {
            // Skip this tick; don't propagate exceptions out of the framework callback.
        }
    }

    public void Dispose()
    {
        _framework.Update -= Tick;
    }
}
