namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// The per-chapter creature roster a zone offers. The chapter index being 1-based and empty slots
/// being skipped rather than terminating the row are the two things a port gets wrong quietly.
/// </summary>
public class ZoneMonsterRosterTests {
    private static ZoneShape Zone(params CreatureType[][] rows) {
        var zone = new ZoneShape("Z01SHP.DAT");
        foreach (CreatureType[] row in rows) {
            zone.Chapters.Add(new ChapterMonsters {
                Slot1 = row[0], Slot2 = row[1], Slot3 = row[2], Slot4 = row[3],
            });
        }
        return zone;
    }

    private static CreatureType[] Row(CreatureType a, CreatureType b, CreatureType c, CreatureType d) =>
        new[] { a, b, c, d };

    private static CreatureType[] Empty =>
        Row(CreatureType.None, CreatureType.None, CreatureType.None, CreatureType.None);

    private static ZoneShape Shipped() => Zone(
        Row(CreatureType.MoredhelWarrior, CreatureType.MoredhelSpellcaster, CreatureType.Rogue, CreatureType.Shade),
        Row(CreatureType.Troll, CreatureType.Rogue, CreatureType.None, CreatureType.None),
        Empty);

    [Fact]
    public void TheChapterIndexIsOneBased() {
        // The original seeks (chapter - 1) * 8 bytes in. Off by one here shows the wrong chapter's
        // monsters, which looks like content rather than a bug.
        Assert.Equal(CreatureType.MoredhelWarrior, ZoneMonsterRoster.For(Shipped(), 1).Slot1);
        Assert.Equal(CreatureType.Troll, ZoneMonsterRoster.For(Shipped(), 2).Slot1);
    }

    [Fact]
    public void AChapterWithNoRowAnswersNothingRatherThanThrowing() {
        Assert.Null(ZoneMonsterRoster.For(Shipped(), 0));
        Assert.Null(ZoneMonsterRoster.For(Shipped(), 99));
        Assert.Null(ZoneMonsterRoster.For(null, 1));
        Assert.Empty(ZoneMonsterRoster.TypesIn(null, 1));
    }

    [Fact]
    public void TheTypesComeBackInTheFilesSlotOrder() {
        Assert.Equal(
            new[] {
                CreatureType.MoredhelWarrior, CreatureType.MoredhelSpellcaster,
                CreatureType.Rogue, CreatureType.Shade,
            },
            ZoneMonsterRoster.TypesIn(Shipped(), 1));
    }

    [Fact]
    public void EmptySlotsAreDropped() {
        Assert.Equal(new[] { CreatureType.Troll, CreatureType.Rogue },
            ZoneMonsterRoster.TypesIn(Shipped(), 2));
    }

    [Fact]
    public void AGapInTheMiddleIsSkippedNotStoppedAt() {
        // The original tests all four slots independently rather than breaking on the first empty.
        // The shipped data keeps its empties trailing, but the format promises nothing.
        ZoneShape gapped = Zone(Row(CreatureType.Troll, CreatureType.None, CreatureType.Rogue, CreatureType.None));

        Assert.Equal(new[] { CreatureType.Troll, CreatureType.Rogue },
            ZoneMonsterRoster.TypesIn(gapped, 1));
    }

    [Fact]
    public void AnEmptyChapterIsOrdinaryNotBroken() {
        // Most shipped rows are empty — the party either cannot reach the zone that chapter or meets
        // nothing there.
        Assert.False(ZoneMonsterRoster.HasAny(Shipped(), 3));
        Assert.Empty(ZoneMonsterRoster.TypesIn(Shipped(), 3));
        Assert.True(ZoneMonsterRoster.HasAny(Shipped(), 1));
    }

    [Fact]
    public void OffersIsTheMembershipTestAnEncounterDoes() {
        Assert.True(ZoneMonsterRoster.Offers(Shipped(), 1, CreatureType.Shade));
        Assert.False(ZoneMonsterRoster.Offers(Shipped(), 1, CreatureType.Troll));

        // Chapter-sensitive: the same zone offers a different set later.
        Assert.True(ZoneMonsterRoster.Offers(Shipped(), 2, CreatureType.Troll));
    }

    [Fact]
    public void AbsenceIsNotAMember() {
        // Otherwise every empty slot would report the roster as offering "None".
        Assert.False(ZoneMonsterRoster.Offers(Shipped(), 2, CreatureType.None));
    }

    [Fact]
    public void TheShapeOfTheFileIsFourSlotsAcrossNineChapters() {
        Assert.Equal(4, ZoneMonsterRoster.SlotsPerChapter);
        Assert.Equal(9, ZoneMonsterRoster.ChapterCount);
    }
}
