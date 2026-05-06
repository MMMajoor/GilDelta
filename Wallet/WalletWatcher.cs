using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;

namespace GilDelta.Wallet;

public sealed class WalletWatcher : IDisposable
{
    private readonly IFramework _framework;
    private readonly WalletReader _reader;
    private readonly Dictionary<WalletId, long> _last = new();

    public IReadOnlyList<Wallet> LastSnapshot { get; private set; } = Array.Empty<Wallet>();
    public event Action<WalletDiff>? OnDiff;

    public WalletWatcher(IFramework framework, WalletReader reader)
    {
        _framework = framework;
        _reader = reader;
        _framework.Update += Tick;
    }

    private void Tick(IFramework _)
    {
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
