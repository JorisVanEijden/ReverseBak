namespace BetrayalAtKrondor.Tests.GameState;

using GameData.Resources.GameState;
using Xunit;

/// <summary>
/// Which save-state key a variable write lands on.
/// </summary>
public class SetVarEffectTests {
    [Fact]
    public void ADecodedVariableIsBiasedIntoItsRange() {
        // The confirmed form: the decoder subtracted 30000, so applying it has to add it back.
        Assert.Equal(30000, new SetVarEffect { Var = 0 }.GlobalKey);
        Assert.Equal(30004, new SetVarEffect { Var = 4 }.GlobalKey);
        Assert.Equal(30017, new SetVarEffect { Var = 17 }.GlobalKey);
        Assert.Equal(30029, new SetVarEffect { Var = 29 }.GlobalKey);
    }

    [Fact]
    public void ARAWKeyIsWrittenWhereItSays() {
        // *** The whole reason this is not a bare addition. *** The decoder's fallback keeps the raw
        // key in the same field, so adding 30000 would write to 86277 instead of 56277 — a global
        // nothing reads, silently losing the write.
        Assert.Equal(56277, new SetVarEffect { Var = 56277 }.GlobalKey);
        Assert.Equal(8500, new SetVarEffect { Var = 8500 }.GlobalKey);
    }

    [Fact]
    public void TheTwoFormsCannotBeConfusedInPractice() {
        // A decoded variable is 0..29; a raw key that reached the fallback is outside 1..8499 and
        // outside the variable range, so it is far above 29. The shipped tree has 38 decoded and one
        // raw, with no value ambiguous between the readings.
        Assert.True(new SetVarEffect { Var = 29 }.GlobalKey < SetVarEffect.VarRangeBase + SetVarEffect.VarRangeCount);
        Assert.True(new SetVarEffect { Var = 30 }.GlobalKey < SetVarEffect.VarRangeBase);
    }

    [Fact]
    public void ANegativeVarIsTreatedAsRawRatherThanBiased() {
        // Not expected, but adding the base to it would land inside the variable range and corrupt
        // a real variable; passing it through leaves it obviously wrong instead.
        Assert.Equal(-5, new SetVarEffect { Var = -5 }.GlobalKey);
    }

    [Fact]
    public void TheValueIsCarriedUntouched() {
        Assert.Equal(7, new SetVarEffect { Var = 4, Value = 7 }.Value);
    }
}
