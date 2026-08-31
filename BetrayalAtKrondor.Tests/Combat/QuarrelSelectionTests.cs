namespace BetrayalAtKrondor.Tests.Combat;

using System.Collections.Generic;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// Which quarrel a shot uses, and whether it is spent — <c>combataiturn_sel_consum_qrl</c>.
/// </summary>
/// <remarks>
/// <b>TASK-252 landed the shot and recorded this as a known gap:</b> "the quarrel is not CONSUMED on
/// firing". Unlimited ammunition is the visible half; the selection rules underneath it are the part
/// that is easy to get subtly wrong.
/// </remarks>
public class QuarrelSelectionTests {
    private static System.Func<int, int> Carrying(params (int Kind, int Count)[] held) {
        var map = new Dictionary<int, int>();
        foreach ((int kind, int count) in held) {
            map[kind] = count;
        }
        return kind => map.TryGetValue(kind, out int n) ? n : 0;
    }

    [Fact]
    public void AnUnspecifiedKindTakesTheHIGHESTCarried_notTheLowest() {
        // *** The scan runs 7 DOWN TO 0. *** The kinds run cheapest-first, so scanning upward would
        // have a shooter spend its best ammunition last instead of first.
        Assert.Equal(6, QuarrelInventory.SelectKind(
            creatureType: 1, QuarrelInventory.AllKinds, Carrying((0, 5), (3, 2), (6, 1))));
        Assert.Equal(0, QuarrelInventory.SelectKind(
            creatureType: 1, QuarrelInventory.AllKinds, Carrying((0, 5))));
    }

    [Fact]
    public void ARequestedKindIsNotReScanned_soAnEmptyChoiceRefusesRatherThanSubstituting() {
        // Only kind == -1 searches. Asking for a kind you have none of answers -1; falling back to
        // another would fire ammunition the player did not choose.
        Assert.Equal(QuarrelInventory.NoKind, QuarrelInventory.SelectKind(
            creatureType: 1, requestedKind: 2, Carrying((0, 9), (5, 9))));
        Assert.Equal(2, QuarrelInventory.SelectKind(
            creatureType: 1, requestedKind: 2, Carrying((2, 1))));
    }

    [Fact]
    public void CARRYINGNOTHINGMeansNoShot() {
        Assert.Equal(QuarrelInventory.NoKind, QuarrelInventory.SelectKind(
            creatureType: 1, QuarrelInventory.AllKinds, Carrying()));
    }

    [Fact]
    public void CREATURE0x1AShootsWithoutCarryingAnything_andSpendsNothing() {
        // *** The early return, before the count is read and before anything is consumed. *** Kind 9
        // is OUTSIDE the 0..7 range, so its ammunition is innate. A port that runs this creature
        // through the ordinary path finds an empty pack and refuses every shot it takes.
        int kind = QuarrelInventory.SelectKind(
            QuarrelInventory.InnateAmmoCreature, QuarrelInventory.AllKinds, Carrying());

        Assert.Equal(QuarrelInventory.InnateAmmoKind, kind);
        Assert.Equal(9, kind);
        Assert.False(QuarrelInventory.Spends(kind), "innate ammunition costs nothing");
        Assert.True(kind >= QuarrelInventory.ObjectIdByKind.Length);
    }

    [Fact]
    public void EveryOrdinaryKindSpendsOne() {
        for (var kind = 0; kind < QuarrelInventory.ObjectIdByKind.Length; kind++) {
            Assert.True(QuarrelInventory.Spends(kind));
        }
        Assert.False(QuarrelInventory.Spends(QuarrelInventory.NoKind));
    }

    [Fact]
    public void THEKINDTOOBJECTMapIsNotSequential_andItIsRightAtBOTHENDS() {
        // 0x24, 0x25, 0x26, then 0x2a, then 0x27, 0x28, 0x29, then 0x2b. Kind 3 is displaced, which
        // shifts 4, 5 and 6 down one — so a `0x24 + kind` reading spends the wrong item for FOUR of
        // the eight.
        //
        // *** And it is correct for kinds 0-2 AND for kind 7, which is what makes it dangerous. ***
        // The two ends agree by coincidence, so a naive implementation tested on the first kind or
        // the last one looks right.
        Assert.Equal(new[] { 0x24, 0x25, 0x26, 0x2a, 0x27, 0x28, 0x29, 0x2b },
            QuarrelInventory.ObjectIdByKind);

        var wrong = 0;
        for (var kind = 0; kind < QuarrelInventory.ObjectIdByKind.Length; kind++) {
            if (QuarrelInventory.ObjectIdByKind[kind] != 0x24 + kind) {
                wrong++;
            }
        }
        Assert.Equal(4, wrong);
        Assert.Equal(0x24 + 0, QuarrelInventory.ObjectIdByKind[0]);
        Assert.Equal(0x24 + 7, QuarrelInventory.ObjectIdByKind[7]);
        Assert.NotEqual(0x24 + 3, QuarrelInventory.ObjectIdByKind[3]);
    }
}
