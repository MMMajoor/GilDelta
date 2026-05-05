using GilDelta.Wallet;

namespace GilDelta.Events;

public interface IInferenceRule
{
    bool TryClassify(WalletDiff diff, GameContext ctx, out GilEvent? ev);
}
