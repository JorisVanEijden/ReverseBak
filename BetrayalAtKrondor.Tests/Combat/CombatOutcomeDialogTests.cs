namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The line a fight ends on (COMBAT.C:2587-2672), one case per branch.
/// </summary>
public class CombatOutcomeDialogTests {
    private static Combatant Member(int slot, bool down = false) =>
        new() { PartySlot = slot, Flags = down ? CombatantFlags.Dead : CombatantFlags.None };

    private static Combatant Enemy(bool down, int classId = 18, bool fled = false) =>
        new() {
            ClassId = classId,
            Flags = (down ? CombatantFlags.Dead : CombatantFlags.None)
                | (fled ? CombatantFlags.Fleeing : CombatantFlags.None),
        };

    private static CombatEncounter Fight(Combatant[] party, params Combatant[] enemies) {
        var e = new CombatEncounter();
        e.Party.AddRange(party);
        e.Enemies.AddRange(enemies);
        return e;
    }

    private static readonly Combatant[] Standing = { Member(1), Member(2), Member(3) };

    [Fact]
    public void TwoBodies_TheBattleWasWon_SearchTheBodies() =>
        Assert.Equal(0x20, CombatOutcomeDialog.For(Fight(Standing, Enemy(true), Enemy(true)), 2, false));

    [Fact]
    public void OneBody_SearchTheBody() =>
        Assert.Equal(0x81, CombatOutcomeDialog.For(Fight(Standing, Enemy(true), Enemy(true, fled: true)), 2, false));

    [Fact]
    public void NoBodies_TheEnemyHadFled_OrTheTrapExit() {
        CombatEncounter fight = Fight(Standing, Enemy(true, fled: true));
        Assert.Equal(0x82, CombatOutcomeDialog.For(fight, 2, false));
        Assert.Equal(0x1f, CombatOutcomeDialog.For(fight, 2, true));
    }

    [Fact]
    public void SetPieceEncounter_GetsItsOwnManyBodiesLine() =>
        Assert.Equal(0x142, CombatOutcomeDialog.For(Fight(Standing, Enemy(true), Enemy(true)), 0x97, false));

    [Fact]
    public void VanishingCreatures_HaveTheirOwnLines() {
        Assert.Equal(0x131, CombatOutcomeDialog.For(Fight(Standing, Enemy(true, 0x31), Enemy(true, 0x38)), 2, false));
        Assert.Equal(0x132, CombatOutcomeDialog.For(Fight(Standing, Enemy(true, 0x39)), 2, false));
    }

    [Fact]
    public void ADownedPartyMember_OutranksTheBodyCount_AndIsNamed() {
        Combatant[] party = { Member(1), Member(2, down: true), Member(3) };
        CombatEncounter fight = Fight(party, Enemy(true), Enemy(true));
        Assert.Equal(CombatOutcomeDialog.PartyMemberDown, CombatOutcomeDialog.For(fight, 2, false));
        Assert.Same(party[1], CombatOutcomeDialog.DownedMember(fight));
        Assert.Same(party[0], CombatOutcomeDialog.StandingMember(fight));
    }

    [Fact]
    public void AWipe_AndAFlight_AndMakala() {
        Combatant[] wiped = { Member(1, true), Member(2, true), Member(3, true) };
        Assert.Equal(0x21, CombatOutcomeDialog.For(Fight(wiped, Enemy(false)), 2, false));
        Assert.Equal(0x6b, CombatOutcomeDialog.For(Fight(wiped, Enemy(false)), 2, true));
        Assert.Null(CombatOutcomeDialog.For(Fight(Standing, Enemy(false)), 2, false, fled: true));
        Assert.Equal(0x150, CombatOutcomeDialog.For(Fight(Standing, Enemy(true)), CombatOutcomeDialog.MakalaEncounter, false));
    }
}
