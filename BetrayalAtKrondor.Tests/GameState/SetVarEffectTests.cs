namespace BetrayalAtKrondor.Tests.GameState;

using GameData.Resources.GameState;
using Xunit;

/// <summary>
/// Which save-state key a write lands on — the decoded-variable form and the raw form, which are now
/// two types rather than one field with two meanings.
/// </summary>
public class SetVarEffectTests {
    [Fact]
    public void ADecodedVariableIsBiasedIntoItsRange() {
        // The decoder subtracted 30000, so applying it has to add it back.
        Assert.Equal(30000, new SetVarEffect { Var = 0 }.GlobalKey);
        Assert.Equal(30004, new SetVarEffect { Var = 4 }.GlobalKey);
        Assert.Equal(30017, new SetVarEffect { Var = 17 }.GlobalKey);
        Assert.Equal(30029, new SetVarEffect { Var = 29 }.GlobalKey);
    }

    [Fact]
    public void ARawWriteKeepsItsKeyAbsolute() {
        // *** The bug the old overload invited. *** A raw key used to ride in SetVarEffect.Var, so
        // `30000 + Var` — the obvious way to apply a variable write — landed on 86277 instead of
        // 56277: a global nothing reads, losing the write with no error. A separate type cannot be
        // applied that way by accident.
        Assert.Equal(56277, new RawGlobalWriteEffect { Key = 56277 }.Key);
        Assert.Equal(8500, new RawGlobalWriteEffect { Key = 8500 }.Key);
    }

    [Fact]
    public void TheValueIsCarriedUntouchedByEitherForm() {
        Assert.Equal(7, new SetVarEffect { Var = 4, Value = 7 }.Value);
        Assert.Equal(7, new RawGlobalWriteEffect { Key = 56277, Value = 7 }.Value);
    }
}
