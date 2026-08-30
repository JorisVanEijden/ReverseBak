namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>Dannon's Delusions: a decoy that looks like the caster.</summary>
public class DecoySummonTests {
    [Fact]
    public void THEDECOYWEARSTHECASTERSTYPE() {
        // Copied from the caster's combat data rather than named by the spell, so the illusion
        // always looks like whoever cast it. A fixed appearance loses the point of it.
        Assert.Equal(17, DecoySummon.CreatureTypeFor(17));
        Assert.Equal(3, DecoySummon.CreatureTypeFor(3));
    }

    [Fact]
    public void ITISHELPLESSBYCONSTRUCTION() {
        // 1 health, 1 stamina, ZERO speed — it dies to anything and never acts. Giving it the
        // caster's speed along with the caster's face would put a second combatant on the field.
        Assert.Equal(1, DecoySummon.Health);
        Assert.Equal(1, DecoySummon.Stamina);
        Assert.Equal(0, DecoySummon.Speed);
        Assert.False(DecoySummon.CanAct);
    }

    [Fact]
    public void ITEXPIRES_WHERETHEPLAINSUMMONDOESNOT() {
        // The decoy takes an effect slot carrying its spell and duration; MonsterSummon sets no slot
        // at all and therefore lasts until killed. Folding the two together would give one an expiry
        // it should not have or strip the other's.
        Assert.True(DecoySummon.ExpiresWithItsSpell);
        Assert.Equal(-1, MonsterSummon.NoEffectSlot);
    }

    [Fact]
    public void ONLYTHEDECOYASKSWHERETOGO() {
        // This is the caller that passes the prompt flag; a spell-cast monster summon lands on the
        // placement globals without asking.
        Assert.True(DecoySummon.PromptsForTile);
        Assert.False(MonsterSummon.PromptsForTile(false));
    }

    [Fact]
    public void TheSpawnWaitsForTheButtonToComeBackUp() {
        // The placement click is consumed by the picker; without the release wait the same press
        // would carry through into whatever the new grid state offers next.
        Assert.True(DecoySummon.WaitsForButtonRelease);
    }

    [Fact]
    public void ItIsStampedWithDannonsDelusions() {
        Assert.Equal(GameData.Resources.Spells.SpellIds.DannonsDelusions, DecoySummon.Spell);
    }
    [Fact]
    public void ADecoyIsREADYAndNotFlaggedASummon_TheReverseOfAMonsterSummon() {
        // The two spawn routines set the flags word to opposite values on both bits. A conjured
        // monster carries AiSummon and is not ready; the decoy is ready and carries no summon bit,
        // so nothing keying off that bit will find it.
        Assert.Equal(CombatantFlags.Ready, DecoySummon.InitialFlags);
        Assert.False(DecoySummon.InitialFlags.HasFlag(CombatantFlags.AiSummon));
        Assert.Equal(CombatantFlags.AiSummon, MonsterSummon.InitialFlags);
        Assert.False(MonsterSummon.InitialFlags.HasFlag(CombatantFlags.Ready));
    }

    [Fact]
    public void ADecoyNeverRoutsEither_ButForASimplerReason() {
        // Same zero, same consequence as MonsterSummon.Morale. The difference is that this routine
        // writes the decoy's stats directly and never calls the MONSTXX.DAT roll, so there is no
        // template value for the zero to have to survive.
        Assert.Equal(0, DecoySummon.Morale);
        Assert.Equal(DecoySummon.Morale, MonsterSummon.Morale);
    }

}
