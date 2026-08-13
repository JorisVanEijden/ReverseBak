namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog;
using Xunit;

/// <summary>
/// Turning a dialog action's actor operand into a party member. The operand is not a member id, and
/// treating it as one would act on the wrong character without any visible sign.
/// </summary>
public class DialogSlotOperandTests {
    private static DialogSlotTable TableWith(params int[] kinds) {
        var t = new DialogSlotTable();
        for (var i = 0; i < kinds.Length && i < DialogSlotTable.SlotCount; i++) {
            t.Kinds[i] = kinds[i];
        }
        return t;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ZeroAndOneMeanTheWholeParty(int operand) {
        // The bias exists precisely to reserve these two.
        Assert.Equal(DialogSlotTable.PartyWide, TableWith(4, 5).ResolveActorOperand(operand));
    }

    [Fact]
    public void TwoIsTheFirstSpeakerSlotNotTheSecondMember() {
        // operand 2 -> slot 0. Reading it as a member id would pick member 2.
        DialogSlotTable table = TableWith(4, 5, 1);

        Assert.Equal(4, table.ResolveActorOperand(2));
    }

    [Fact]
    public void EachOperandStepsOneSlotAlong() {
        DialogSlotTable table = TableWith(4, 5, 1);

        Assert.Equal(5, table.ResolveActorOperand(3));
        Assert.Equal(1, table.ResolveActorOperand(4));
    }

    [Fact]
    public void TheShippedLearnSpellOperandLandsOnSlotThree() {
        // All four LearnSpell actions in the shipped DDX carry operand 5.
        DialogSlotTable table = TableWith(0, 1, 2, 3);

        Assert.Equal(3, table.ResolveActorOperand(5));
    }

    [Fact]
    public void AnEmptySlotResolvesToNobodyRatherThanMemberZero() {
        // Kinds defaults to NoActor (0xFF). Falling back to "somebody" here would heal or teach a
        // character the dialog never named.
        Assert.Equal(DialogSlotTable.Unresolved, new DialogSlotTable().ResolveActorOperand(2));
    }

    [Fact]
    public void ASlotHoldingACreatureIsNotAPartyMember() {
        var table = new DialogSlotTable();
        table.Kinds[0] = DialogSlotTable.CreatureActor;

        Assert.Equal(DialogSlotTable.Unresolved, table.ResolveActorOperand(2));
    }

    [Fact]
    public void AnOperandPastTheSlotTableIsRefused() {
        DialogSlotTable table = TableWith(4, 5, 1, 2, 3, 0);

        Assert.Equal(DialogSlotTable.Unresolved,
            table.ResolveActorOperand(DialogSlotTable.SlotCount + DialogSlotTable.FirstSpeakerOperand));
    }
}
