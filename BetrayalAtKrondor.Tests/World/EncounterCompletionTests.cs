namespace BetrayalAtKrondor.Tests.World;

using GameData.Resources.World;
using System.Collections.Generic;
using Xunit;

/// <summary>
/// What runs when a fight is won — <c>evtcond_dispatch_key_to_handler</c>'s table.
/// </summary>
public class EncounterCompletionTests {
    private static System.Func<long, bool> Fought(params long[] beaten) {
        var set = new HashSet<long>(beaten);
        return e => set.Contains(e);
    }

    [Fact]
    public void MostEncountersHaveNoFollowupAtAll() {
        // The table is small and the default is nothing. A lookup that answered something for every
        // encounter would fire a stranger's script on an ordinary bandit.
        foreach (long ordinary in new long[] { 0, 1, 40, 0x4b, 0x88, 0x261, 0x26e, 999 }) {
            Assert.False(EncounterCompletion.HasFollowup(ordinary), $"encounter {ordinary}");
        }
    }

    [Fact]
    public void OneEncounterPlaysALineAndItDoesNotWaitForThePlayer() {
        Assert.Equal(0x1cfdf1u, EncounterCompletion.DialogAfterDefeat(0x4a));
        Assert.Equal(0u, EncounterCompletion.DialogAfterDefeat(0x4b));
        Assert.False(EncounterCompletion.DialogWaitsForThePlayer);
    }

    [Fact]
    public void ElevenEncountersComeBack_AndTheyAreNotTheOnesThatCount() {
        Assert.Equal(11, EncounterCompletion.ReArmingEncounters.Count);
        Assert.True(EncounterCompletion.ReArmsWhenDefeated(0xeb));
        Assert.True(EncounterCompletion.ReArmsWhenDefeated(0x1ae));
        Assert.False(EncounterCompletion.ReArmsWhenDefeated(0x83));

        // The three mechanics are disjoint in the shipped table: no encounter both re-arms and
        // counts toward a group, which is what lets a consumer treat them as separate arms.
        foreach (long e in EncounterCompletion.ReArmingEncounters) {
            Assert.Equal(-1, EncounterCompletion.GroupOf(e));
            Assert.Equal(0u, EncounterCompletion.DialogAfterDefeat(e));
        }
    }

    [Fact]
    public void AGroupFlagIsEarnedONLYWhenEveryMemberIsBeaten() {
        long[] members = { 0x83, 0x84, 0x85, 0x86, 0x87 };

        // Four of five: nothing yet, whichever four.
        for (var skip = 0; skip < members.Length; skip++) {
            var beaten = new List<long>(members);
            beaten.RemoveAt(skip);
            Assert.Equal(0,
                EncounterCompletion.GroupFlagEarnedBy(members[skip], Fought(beaten.ToArray())));
        }

        Assert.Equal(0xdb1c, EncounterCompletion.GroupFlagEarnedBy(0x87, Fought(members)));
    }

    [Fact]
    public void TheJustBeatenEncountersOWNFlagIsPartOfTheGate() {
        // *** ORDERING. *** The caller writes the fought flag first and then dispatches here. Asking
        // before that write makes the last kill of a group look like the second-to-last, and the
        // flag is never earned by anything.
        long[] members = { 0x83, 0x84, 0x85, 0x86, 0x87 };

        Assert.Equal(0, EncounterCompletion.GroupFlagEarnedBy(0x87,
            Fought(0x83, 0x84, 0x85, 0x86)));
        Assert.Equal(0xdb1c, EncounterCompletion.GroupFlagEarnedBy(0x87, Fought(members)));
    }

    [Fact]
    public void TheSecondGroupHasSixMembersAndItsOwnFlag() {
        long[] members = { 0x262, 0x265, 0x267, 0x26a, 0x26b, 0x26d };

        Assert.Equal(0x1d17, EncounterCompletion.GroupFlagEarnedBy(0x26d, Fought(members)));

        // The ids are NOT consecutive — 0x263, 0x264, 0x266, 0x268, 0x269, 0x26c are not members,
        // so a port that read the group as a range would gate on encounters that never count.
        foreach (long between in new long[] { 0x263, 0x264, 0x266, 0x268, 0x269, 0x26c }) {
            Assert.Equal(-1, EncounterCompletion.GroupOf(between));
        }
    }

    [Fact]
    public void ADefeatOutsideBothGroupsCompletesNothing_EvenWithEverythingElseBeaten() {
        Assert.Equal(0, EncounterCompletion.GroupFlagEarnedBy(40,
            Fought(0x83, 0x84, 0x85, 0x86, 0x87, 0x262, 0x265, 0x267, 0x26a, 0x26b, 0x26d)));
    }

    [Fact]
    public void EveryGateKeyIsAMembersOwnFoughtFlag() {
        // The original spells the gates out as literal event keys; each is 5220 + the member id.
        // Pinned because it is what turns two lists of magic numbers into one rule — and because a
        // future edit to the fought-key base has to move these with it.
        const int foughtKeyBase = 5220;
        var expected = new Dictionary<int, long[]> {
            [0xdb1c] = new long[] { 0x14e7, 0x14e8, 0x14e9, 0x14ea, 0x14eb },
            [0x1d17] = new long[] { 0x16c6, 0x16c9, 0x16cb, 0x16ce, 0x16cf, 0x16d1 },
        };

        foreach (EncounterCompletion.Group group in EncounterCompletion.Groups) {
            long[] keys = expected[group.Flag];
            Assert.Equal(keys.Length, group.Members.Count);
            for (var i = 0; i < keys.Length; i++) {
                Assert.Equal(keys[i], foughtKeyBase + group.Members[i]);
            }
        }
    }

    [Fact]
    public void ANullFoughtLookupAnswersNothingRatherThanThrowing() {
        Assert.Equal(0, EncounterCompletion.GroupFlagEarnedBy(0x83, null));
    }
}
