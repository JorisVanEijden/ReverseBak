namespace GameData.Resources.World;

using GameData.Resources.Character;

/// <summary>
/// Putting a defeated encounter's monsters back on their feet —
/// <c>combatenc_rearm_roster_actors</c> (IDA <c>0x64977</c>).
/// </summary>
/// <remarks>
/// <b>It is a FULL HEAL, not a re-read of the shipped data.</b> The original does not consult the
/// chapter's enemy-party template: for every live slot in the encounter's seven-entry roster it
/// reads the records already in TEMP.GAM, copies the actor's <c>Health.Maximum</c> over its
/// <c>Health.Current</c>, forces the combat record's status to
/// <see cref="Combat.CombatStatePersistence.MigratedCombatStatus"/>, and writes both back. So the
/// reload can undo damage and downed-ness; it cannot restore an actor the game has *removed*.
///
/// <para>Confirmed by reading the whole function rather than the name: the writes are
/// <c>actor.health.current = actor.health.max</c> and <c>combatData.combatStatus = 1</c>, at
/// <c>0x90E7 + slot*0x5F</c> and <c>0x312E5 + slot*0x16</c> — the same two bases
/// <c>SaveEncounterNpcsToTempGam</c> uses, which are <c>SaveGameOffsets.RosterActors</c> and
/// <c>CombatDataOffset</c>.</para>
///
/// <para><b>Only eleven encounters do this</b> — see
/// <see cref="EncounterCompletion.ReArmingEncounters"/>. It is paired with clearing the encounter's
/// fought flag and the hotspot's done / scouted / scout-tried flags (<see cref="EncounterReset"/>);
/// healing without clearing them leaves monsters nobody can meet, and clearing without healing
/// leaves an encounter that arms again and fields the wounded and the dead.</para>
/// </remarks>
public static class EncounterRearm {
    /// <summary>Roster slots per encounter.</summary>
    public const int RosterSlots = EncounterDefeat.RosterSlots;

    /// <summary>The combat status a re-armed actor is set to.</summary>
    /// <remarks>
    /// The same 1 the save migration forces, and for the same reason: whatever the actor was doing
    /// when its fight ended is not something the next fight should inherit.
    /// </remarks>
    public const byte RearmedCombatStatus = Combat.CombatStatePersistence.MigratedCombatStatus;

    /// <summary>
    /// Heals one actor's stats in place — <c>Health.Current = Health.Maximum</c>.
    /// </summary>
    /// <param name="stats">The actor's live attributes, from <c>GameSession.RosterStatsOf</c>.</param>
    /// <returns>Whether anything was changed, so a caller can skip staging an untouched actor.</returns>
    /// <remarks>
    /// <b>Health only.</b> The original copies one byte over one byte; stamina, conditions and the
    /// rest of the 95-byte record are left exactly as the fight left them. A port that "restores the
    /// actor" wholesale hands back a creature the game never intended to reset.
    /// </remarks>
    public static bool HealToFull(ActorStat[] stats) {
        if (stats == null) {
            return false;
        }
        var i = (int)ActorAttribute.Health;
        if (i < 0 || i >= stats.Length || stats[i] == null || stats[i].Base == stats[i].Max) {
            return false;
        }
        stats[i].Base = stats[i].Max;
        stats[i].Effective = stats[i].Max;
        return true;
    }

    /// <summary>The combat record a re-armed actor takes: its own, with the status reset.</summary>
    /// <remarks>
    /// Grid position and target are deliberately carried over rather than cleared — the original
    /// rewrites only the status byte, and the placement pass resolves stale tiles when the fight
    /// next opens (<c>CombatPlacement.FindTile</c>).
    /// </remarks>
    public static Data.SaveGameCombatData WithStatusReset(Data.SaveGameCombatData existing) =>
        existing == null ? null : new Data.SaveGameCombatData(
            existing.TargetActorPointer, existing.CreatureType,
            existing.XOnGrid, existing.YOnGrid, existing.TargetXOnGrid, existing.TargetYOnGrid,
            RearmedCombatStatus, existing.AnimEffectType, existing.ActiveSpellEffectSlot,
            existing.UnusedPadding, existing.AnimDurationTimer, existing.MonsterSpellAbility,
            existing.MeleeAttackType, existing.RangedAttackType, existing.MovementAiType,
            existing.PreferredArrowType, existing.LastSpellSymbolFile,
            existing.FloatingDamageValue, existing.FloatingDamageTimer);
}
