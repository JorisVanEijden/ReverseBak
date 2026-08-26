namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using System.Collections.Generic;
using System.Linq;
using Xunit;

/// <summary>
/// Which creature art a chunk needs.
/// </summary>
public class CreatureArtResidencyTests {
    private static readonly CreatureType[] Roster = {
        CreatureType.MoredhelWarrior, CreatureType.Rogue,
    };

    [Fact]
    public void TheRosterComesFirstAndAnEncountersOwnCreaturesFollow() {
        // Loading in sequence then warms the zone's own art before anything an encounter added,
        // which is the order the original establishes by testing the roster first.
        IReadOnlyList<CreatureType> needed = CreatureArtResidency.Needed(Roster,
            new[] { (int)CreatureType.Rogue, (int)CreatureType.Troll });

        Assert.Equal(new[] { CreatureType.MoredhelWarrior, CreatureType.Rogue, CreatureType.Troll },
            needed.ToArray());
    }

    [Fact]
    public void ACreatureAlreadyRosteredIsNotCollectedTwice() {
        IReadOnlyList<CreatureType> needed = CreatureArtResidency.Needed(Roster,
            new[] { (int)CreatureType.MoredhelWarrior, (int)CreatureType.MoredhelWarrior });

        Assert.Equal(2, needed.Count);
    }

    [Fact]
    public void CreatureZeroIsNotACreature() {
        // *** An unset slot reads as 0, and NO CreatureType has the value 0. *** Treating it as a
        // type would ask the loader for a creature that does not exist — which fails late, at the
        // point art is wanted, rather than here where the value is obviously not one.
        IReadOnlyList<CreatureType> needed = CreatureArtResidency.Needed(Roster, new[] { 0, 0, 0 });

        Assert.Equal(Roster.Length, needed.Count);
        Assert.Empty(CreatureArtResidency.BeyondTheRoster(Roster, new[] { 0 }));
    }

    [Fact]
    public void AnActorOutsideTheRosterIsORDINARY_NotAnError() {
        // The roster is residency, not permission — the fifth slot exists precisely for this.
        IReadOnlyList<CreatureType> extras =
            CreatureArtResidency.BeyondTheRoster(Roster, new[] { (int)CreatureType.Troll });

        Assert.Equal(new[] { CreatureType.Troll }, extras.ToArray());
        Assert.False(CreatureArtResidency.ExceedsTheOriginalsBudget(Roster,
            new[] { (int)CreatureType.Troll }));
    }

    [Fact]
    public void TWOCreaturesBeyondTheRosterExceedWhatTheOriginalCouldHold() {
        // *** WE RENDER BOTH; THE ORIGINAL COULD NOT. *** Its collection loop is capped at one, so
        // the second monster's art never loaded. That is a 16-bit memory budget, not a game rule —
        // reproducing it drops a sprite for a reason no player could interpret — but a chunk in this
        // state is worth being able to notice.
        int[] placed = { (int)CreatureType.Troll, (int)CreatureType.Spider };

        Assert.Equal(2, CreatureArtResidency.BeyondTheRoster(Roster, placed).Count);
        Assert.True(CreatureArtResidency.ExceedsTheOriginalsBudget(Roster, placed));
        Assert.Equal(4, CreatureArtResidency.Needed(Roster, placed).Count);
    }

    [Fact]
    public void TheBudgetIsTheRostersOwnExtraSlotCount() {
        // Expressed through the roster's constant rather than a literal 1, so the two cannot drift.
        Assert.Equal(1, ZoneMonsterRoster.EngineExtraSlots);
    }

    [Fact]
    public void AZoneWithNothingRosteredStillNeedsWhateverIsPlaced() {
        // "This zone has nothing roaming this chapter" is the COMMON case — 62 of the 108 shipped
        // rows are empty — so an encounter's own creature has to carry itself.
        IReadOnlyList<CreatureType> needed =
            CreatureArtResidency.Needed(new CreatureType[0], new[] { (int)CreatureType.Troll });

        Assert.Equal(new[] { CreatureType.Troll }, needed.ToArray());
    }

    [Fact]
    public void NullsAnswerEmptyRatherThanThrowing() {
        Assert.Empty(CreatureArtResidency.Needed(null, null));
        Assert.Empty(CreatureArtResidency.BeyondTheRoster(null, null));
        Assert.Equal(Roster.Length, CreatureArtResidency.Needed(Roster, null).Count);
    }
}
