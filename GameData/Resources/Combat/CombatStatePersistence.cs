namespace GameData.Resources.Combat;

using GameData.Resources.Data;

/// <summary>
/// Turning a live <see cref="Combatant"/> into the 22-byte <c>CombatantState</c> record a save
/// holds — <c>combatenc_persist_actors_to_temp</c> (canassa CBENC.C:115).
/// </summary>
/// <remarks>
/// <b>PATCH, DO NOT BUILD.</b> The record has nineteen fields and a fight knows about four of them.
/// The rest — the attack types, the movement AI kind, the spell ability, the animation timers — come
/// from the creature's own data and are meaningless to invent. So this takes the record that is
/// already there and returns a copy with the live fields replaced, exactly as
/// <c>SaveGameWriter</c> patches a body rather than composing one.
///
/// <para><b>Only the ENEMY side is persisted.</b> The original walks <c>g_combat_actors_B</c> —
/// count B, the encounter's actors — and writes each at its ROSTER slot. The party's combat state is
/// not in this block at all, which is consistent with the party being written back through its
/// character records instead.</para>
/// </remarks>
public static class CombatStatePersistence {
    /// <summary>
    /// <b>The stored target is a raw pointer, and writing zero is safer than reproducing it.</b>
    /// </summary>
    /// <remarks>
    /// <c>CombatantState.target</c> holds a heap address from the session that saved it. That
    /// address is meaningless in any later run — the original's own save/load carries a dangling
    /// value across the boundary, so a port is not making anything worse by not reproducing it.
    /// Zero is additionally the value the engine's own code tests for
    /// (<c>if (target != NULL &amp;&amp; target-&gt;inner-&gt;flags &amp; CAF_DEAD) target = NULL</c>),
    /// so it reads as "no target" rather than as a wild pointer.
    /// </remarks>
    public const ushort NoTargetPointer = 0;

    /// <summary>Combat status the save migration forces on every roster actor.</summary>
    /// <remarks>
    /// <c>combatenc_saves_migr_all_slots</c> writes <c>innerBuf[8] = 1</c>, and byte 8 of the record
    /// is <see cref="SaveGameCombatData.CombatStatus"/>. So an actor arriving through a migrated save
    /// is normalised to this whatever it was doing when the fight was saved.
    /// </remarks>
    public const byte MigratedCombatStatus = 1;

    /// <summary>
    /// The record for <paramref name="combatant"/>, keeping every field a fight does not own.
    /// </summary>
    /// <param name="existing">The record currently in the save for this actor slot.</param>
    /// <param name="combatant">The live combatant.</param>
    public static SaveGameCombatData WithLiveState(SaveGameCombatData existing, Combatant combatant) {
        if (existing == null || combatant == null) {
            return existing;
        }

        Combatant target = combatant.Target;
        return new SaveGameCombatData(
            // Deliberately not the live target's address — see NoTargetPointer.
            NoTargetPointer,
            existing.CreatureType,
            (byte)combatant.X,
            (byte)combatant.Y,
            target != null ? (byte)target.X : existing.TargetXOnGrid,
            target != null ? (byte)target.Y : existing.TargetYOnGrid,
            existing.CombatStatus,
            existing.AnimEffectType,
            existing.ActiveSpellEffectSlot,
            existing.UnusedPadding,
            existing.AnimDurationTimer,
            existing.MonsterSpellAbility,
            existing.MeleeAttackType,
            existing.RangedAttackType,
            existing.MovementAiType,
            existing.PreferredArrowType,
            existing.LastSpellSymbolFile,
            existing.FloatingDamageValue,
            existing.FloatingDamageTimer);
    }
}
