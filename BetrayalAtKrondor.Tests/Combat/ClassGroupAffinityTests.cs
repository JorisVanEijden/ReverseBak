namespace BetrayalAtKrondor.Tests.Combat;

using GameData;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The class-vs-item affinity lookup — <c>getCreatureCombatGroup</c> feeding <c>racialMods?</c>.
/// </summary>
/// <remarks>
/// <b>The modifier reached <c>CombatFormulas</c> as a hard-coded 0 for every swing in the game</b>,
/// because the row selector (<c>g_aClassCombatGroup</c> @0x3B65E) was never extracted — only the
/// modifier table beside it was. These pin the two-table lookup and, more importantly, the two ways
/// the shipped data leaves the range the table was built for.
/// </remarks>
public class ClassGroupAffinityTests {
    // The shipped 1.02 CD tables, so the assertions below are about real data.
    private static CombatAffinityTables Shipped() {
        var groups = new int[59];
        groups[15] = 2;      // Gorath
        groups[18] = 2;
        groups[21] = 2;
        return new CombatAffinityTables("KRONDOR.EXE") {
            ClassGroupModifier = new[] {
                new[] { 0, -1, -1, -2 },
                new[] { -1, 0, -1, -2 },
                new[] { -1, -1, 0, -2 },
            },
            ClassCombatGroup = groups,
        };
    }

    [Fact]
    public void AClassTakesNoPenaltyForItsOwnGroupAndOneForAnother() {
        CombatAffinityTables t = Shipped();

        Assert.Equal(0, t.ModifierFor(creatureClass: 0, (int)Race.None));
        Assert.Equal(-1, t.ModifierFor(creatureClass: 0, (int)Race.Elf));
    }

    [Fact]
    public void GORATHIsTheOneCombatantTheSystemVISIBLYDoesSomethingFor() {
        // *** The finding that makes this table worth wiring at all. *** The class table is zero
        // everywhere except classes 15, 18 and 21, and 15 is Gorath. Group 2's zero column is the
        // ELF one, so the moredhel alone swings elven weapons without the mismatch penalty.
        CombatAffinityTables t = Shipped();

        Assert.Equal(0, t.ModifierFor(creatureClass: 15, (int)Race.Elf));
        Assert.Equal(-1, t.ModifierFor(creatureClass: 17, (int)Race.Elf));
        Assert.Equal(-1, t.ModifierFor(creatureClass: 15, (int)Race.None));
    }

    [Fact]
    public void GroupOneIsDEADDataInTheShippedBuild() {
        // No creature class selects it, so the middle row is never read. Asserted so nobody infers
        // what the three groups "mean" from the modifier table alone.
        CombatAffinityTables t = Shipped();

        for (var creatureClass = 0; creatureClass < t.ClassCombatGroup.Length; creatureClass++) {
            Assert.NotEqual(1, t.ClassCombatGroup[creatureClass]);
        }
    }

    [Fact]
    public void HUMANRaceItemsAlwaysTakeTheLastColumn_whichIsAPenaltyForEVERYONE() {
        // *** Race is a VALUE being used as a COLUMN INDEX and the ranges do not match. *** Race 3
        // is the fourth column, which is -2 in all three rows — so seventeen shipped items,
        // including the Broadsword and both non-Tsurani crossbows, are worse for every wielder in
        // the game with no one to be better for.
        CombatAffinityTables t = Shipped();

        Assert.Equal(3, (int)Race.Human);
        for (var group = 0; group < CombatAffinityTables.ClassGroups; group++) {
            Assert.Equal(-2, t.ClassGroupModifier[group][(int)Race.Human]);
        }
        Assert.Equal(-2, t.ModifierFor(creatureClass: 0, (int)Race.Human));
        Assert.Equal(-2, t.ModifierFor(creatureClass: 15, (int)Race.Human));
    }

    [Fact]
    public void DWARFRaceItemsReadPASTTheEndOfTheirRow_andThatIsReproducedNotClamped() {
        // *** The out-of-bounds read the shipped data actually performs. *** Race 4 is off the end
        // of a four-wide row and the original does not bound-check: racialMods[group * 4 + 4] lands
        // on the NEXT row's first column, or for group 2 past the modifier table entirely and into
        // the first word of the class table. Two shipped items do this, one being the Sword of
        // Kinnur.
        CombatAffinityTables t = Shipped();

        Assert.Equal(4, (int)Race.Dwarf);
        Assert.Equal(CombatAffinityTables.ItemGroups, (int)Race.Dwarf);

        // group 0 -> index 4 -> row 1 column 0
        Assert.Equal(t.ClassGroupModifier[1][0], t.ModifierFor(creatureClass: 0, (int)Race.Dwarf));
        // group 2 -> index 12 -> off the modifier table, onto ClassCombatGroup[0]
        Assert.Equal(t.ClassCombatGroup[0], t.ModifierFor(creatureClass: 15, (int)Race.Dwarf));
    }

    [Fact]
    public void ClampingDwarfWouldBeABALANCECHANGE_notATidyUp() {
        // Recorded as the reason the read is reproduced. Clamping to the last column turns -1 into
        // -2 for an ordinary creature and 0 into -2 for the moredhel, on two real weapons.
        CombatAffinityTables t = Shipped();
        int lastColumn = CombatAffinityTables.ItemGroups - 1;

        Assert.NotEqual(t.ClassGroupModifier[0][lastColumn],
            t.ModifierFor(creatureClass: 0, (int)Race.Dwarf));
        Assert.NotEqual(t.ClassGroupModifier[2][lastColumn],
            t.ModifierFor(creatureClass: 15, (int)Race.Dwarf));
    }

    [Fact]
    public void AClassPastTheTableReadsAsGroupZERO_whichIsWhatTheOriginalDoes() {
        // The class table is 59 entries and creature classes run 0..63, so five of them index past
        // it — into the weakness table, whose first entries are zero for every class that exists.
        CombatAffinityTables t = Shipped();

        Assert.Equal(t.ModifierFor(creatureClass: 0, (int)Race.Elf),
            t.ModifierFor(creatureClass: 63, (int)Race.Elf));
    }

    [Fact]
    public void AnEmptyTableIsInertRatherThanThrowing() {
        // A fight built before the EXE tables load must not crash; zero is also the value the whole
        // game ran on before this was wired.
        var bare = new CombatAffinityTables("none");
        Assert.Equal(0, bare.ModifierFor(0, (int)Race.Elf));
        Assert.Equal(0, bare.ModifierFor(15, (int)Race.Dwarf));
    }
}
