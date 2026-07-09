namespace BetrayalAtKrondor.Tests.Dialog;
using GameData.Resources.Dialog;
using System.Collections.Generic;
using Xunit;

public class TextVariableResolverTests {
    private static readonly IReadOnlyList<string> Party = new[] { "Locklear", "Gorath", "Owyn", "", "", "" };

    [Fact] public void SubstitutesSlotZero() =>
        Assert.Equal("Locklear frowned.", TextVariableResolver.Substitute("@0 frowned.", Party));

    [Fact] public void SubstitutesMultiple() =>
        Assert.Equal("Gorath and Owyn", TextVariableResolver.Substitute("@1 and @2", Party));

    [Fact] public void PossessiveS() =>
        Assert.Equal("Locklears sword", TextVariableResolver.Substitute("@0s sword", Party));

    [Fact] public void NonDigitAtLeftVerbatim() =>
        Assert.Equal("email@host", TextVariableResolver.Substitute("email@host", Party));

    [Fact] public void OutOfRangeLeftVerbatim() =>
        Assert.Equal("@8", TextVariableResolver.Substitute("@8", Party));
}
