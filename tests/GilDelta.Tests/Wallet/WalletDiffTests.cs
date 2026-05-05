using GilDelta.Wallet;

namespace GilDelta.Tests.Wallet;

public class WalletDiffTests
{
    [Fact]
    public void Delta_is_after_minus_before()
    {
        var id = new WalletId(WalletKind.Self, "");
        var d = new WalletDiff(id, Before: 1_000, After: 1_500, At: DateTimeOffset.UnixEpoch);
        Assert.Equal(500, d.Delta);
    }

    [Fact]
    public void Delta_can_be_negative()
    {
        var id = new WalletId(WalletKind.Self, "");
        var d = new WalletDiff(id, Before: 2_000, After: 500, At: DateTimeOffset.UnixEpoch);
        Assert.Equal(-1500, d.Delta);
    }

    [Fact]
    public void Delta_handles_zero_change()
    {
        var id = new WalletId(WalletKind.Retainer, "Yui");
        var d = new WalletDiff(id, Before: 100, After: 100, At: DateTimeOffset.UnixEpoch);
        Assert.Equal(0, d.Delta);
    }
}
