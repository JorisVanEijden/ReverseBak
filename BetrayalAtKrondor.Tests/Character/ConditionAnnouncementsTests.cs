namespace BetrayalAtKrondor.Tests.Character;

using GameData;
using GameData.Resources.Character;
using System.Collections.Generic;
using Xunit;

/// <summary>The affliction announcements: which flag a catch writes and what the hourly drain plays.</summary>
public class ConditionAnnouncementsTests {
    [Fact]
    public void CatchingAndShakingOffRaiseTheEvent_ButDrunkNeverDoes() {
        var raised = new List<(ActorCondition, bool)>();
        var conditions = new ActorConditions { EventRaised = (c, caught) => raised.Add((c, caught)) };

        ConditionEngine.Apply(conditions, ActorCondition.Starving, 10);
        ConditionEngine.Apply(conditions, ActorCondition.Starving, 5);
        ConditionEngine.Apply(conditions, ActorCondition.Starving, -100);
        ConditionEngine.Apply(conditions, ActorCondition.Drunk, 10);

        Assert.Equal(new[] { (ActorCondition.Starving, true), (ActorCondition.Starving, false) }, raised);
    }

    [Fact]
    public void TwoStarvingMembersAreOneDialog_NamingBothAndCountingTwo() {
        // Gorath and Locklear both starving is ONE "felt weak" dialog with Var 0 = 2, not two.
        var flags = new HashSet<int> {
            ConditionAnnouncements.FlagFor(0, ActorCondition.Starving),
            ConditionAnnouncements.FlagFor(2, ActorCondition.Starving),
            ConditionAnnouncements.FlagFor(1, ActorCondition.Drunk),
        };

        List<ConditionAnnouncements.Announcement> played =
            ConditionAnnouncements.Drain(new[] { 0, 2, 1 }, flags.Contains, f => flags.Remove(f));

        ConditionAnnouncements.Announcement only = Assert.Single(played);
        Assert.Equal(0x3f, only.DialogId);
        Assert.Equal((0, 2, 2), (only.FirstActor, only.SecondActor, only.Count));
        Assert.Empty(flags); // read flags are cleared, the silent Drunk one included
    }

    [Fact]
    public void TheFlagLayoutIsSevenPerCharacterFrom7320() {
        Assert.Equal(7320, ConditionAnnouncements.FlagFor(0, ActorCondition.Sick));
        Assert.Equal(7320 + 7 * 2 + 6, ConditionAnnouncements.FlagFor(2, ActorCondition.NearDeath));
    }
}
