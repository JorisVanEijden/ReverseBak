namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>The six AI turn entry points.</summary>
public class AiTurnPacketsTests {
    [Fact]
    public void TheSearchRadiusIsAConstantSix_NotTheGridReach() {
        // *** Every wrapper passes 6. *** The arena is 8x13, so a monster cannot see across it.
        // Passing the grid diagonal instead would have monsters engaging the whole field.
        Assert.Equal(6, AiTurnPackets.TargetSearchRadius);
        Assert.True(AiTurnPackets.TargetSearchRadius < CombatGrid.Height);
    }

    [Fact]
    public void EachPacketSelectsItsOwnFilter() {
        Assert.Equal(TargetRole.Anyone, AiTurnPackets.RoleFor(0));
        Assert.Equal(TargetRole.Spellcaster, AiTurnPackets.RoleFor(1));
        Assert.Equal(TargetRole.Wounded, AiTurnPackets.RoleFor(2));
        Assert.Equal(TargetRole.MissileCapable, AiTurnPackets.RoleFor(3));
        Assert.Equal(TargetRole.Engaged, AiTurnPackets.RoleFor(4));
        Assert.Equal(TargetRole.TargetingTheLeader, AiTurnPackets.RoleFor(5));
        Assert.Equal(6, AiTurnPackets.PacketCount);
    }

    [Fact]
    public void DisengagedIsReachedByNOPacket() {
        // Nothing in the AI's ordinary turn asks for a target whose own quarry just died. Our enum
        // has the filter because the selection routine supports it, but no wrapper requests it.
        Assert.False(AiTurnPackets.IsReachableFromAPacket(TargetRole.Disengaged));
        Assert.Equal(6, (int)TargetRole.Disengaged);
    }

    [Fact]
    public void ThePacketIndicesMatchOurEnumNumbering() {
        // Both were numbered from the same source, so the mapping is the identity for 0..5.
        for (var packet = 0; packet < AiTurnPackets.PacketCount; packet++) {
            Assert.Equal(packet, (int)AiTurnPackets.RoleFor(packet));
        }
    }

    [Fact]
    public void AnUnknownPacketFallsBackToAnyoneRatherThanThrowing() {
        Assert.Equal(TargetRole.Anyone, AiTurnPackets.RoleFor(99));
    }
}
