namespace GameData.Resources.Combat;

/// <summary>
/// The recoil an actor shows for two ticks after being hit — <c>markActorHit</c> @0x6157d and
/// <c>tickHitReactionTimers</c> @0x61598.
/// </summary>
/// <remarks>
/// <b>The flag is <see cref="CombatantFlags.Knockback"/>, and the two names describe the same
/// state.</b> <c>defines.h</c> calls the bit <c>CAF_KNOCKBACK</c>; IDA's comments call the state
/// "hit-reaction". An actor knocked back and an actor recoiling from a blow are one thing, and
/// this is what finally answers that member's "here for the bit; nothing reads it yet".
///
/// <para><b>The bit is 0x40, and IDA's own comments on BOTH functions say <c>0x64</c>.</b> The
/// encodings settle it — <c>80 4f 08 <b>40</b></c> for the OR in <c>markActorHit</c> and
/// <c>f6 47 08 <b>40</b></c> for the test in the sweep. The symbol is named
/// <c>unknown_combatStatus_64</c> for DECIMAL 64, and the prose wrote that back with an 0x prefix.
/// Copying the comment would set three bits — 0x40, 0x20 and 0x04 — clobbering
/// <see cref="CombatantFlags.Poisoned"/> and <see cref="CombatantFlags.DefendCommand"/> on every
/// hit. Corrected in the database on 2026-09-01.</para>
///
/// <para><b>AWAITING ITS FEATURE (TASK-103).</b> Nothing draws a recoiling actor yet; this is the
/// rule, not the animation.</para>
/// </remarks>
public static class HitReaction {
    /// <summary>How many ticks the recoil lasts — <c>hitReactionTimer = 2</c>.</summary>
    public const int Ticks = 2;

    /// <summary>
    /// <b>A tick here is a REDRAW OF THE COMBAT VIEW — and that is not a unit of time.</b>
    /// </summary>
    /// <remarks>
    /// The only caller of <c>tickHitReactionTimers</c> is <c>RenderWorldView</c> @0x220f1, so the
    /// recoil is over in two redraws rather than at any turn boundary. A port that ticked it on the
    /// turn boundary would leave every struck actor flinching for a whole round.
    ///
    /// <para><b>But "two frames" is the wrong thing to port, and both obvious readings are wrong.</b>
    /// <c>RenderWorldView</c> is not driven by a timer: its 37 call sites are the animation and
    /// effect loops — <c>Combat_AnimateProjectileToTarget</c>, <c>Spell_RunVfxUntilDone</c>,
    /// <c>playCreatureAnimationToCompletion</c>, <c>combat_arena_turn_loop</c>,
    /// <c>combatTargetingLoop</c>. So the two redraws that follow a hit are the next two STEPS OF
    /// WHATEVER ANIMATION IS PLAYING: the arrow's flight, the melee cine, the spell's vfx. The
    /// flinch is synchronised to the blow that caused it, and its wall-clock length is whatever
    /// that animation's pacing happens to be.</para>
    ///
    /// <para>Which rules out the two ports that suggest themselves. <b>Two Unity frames</b> is
    /// ~33 ms and invisible — and note the animation clock's ~59.17 Hz
    /// (<see cref="Animation.GameTick.TicksPerSecond"/>) makes that look defensible, which is the
    /// trap: this timer does not run off that clock. <b>A fixed duration in seconds</b> is visible
    /// but detaches the flinch from the attack animation, so a slow spell and a quick arrow would
    /// recoil for the same length.</para>
    ///
    /// <para><b>The faithful port is to advance this from the attack animation's own step</b>, so
    /// the recoil ends two steps after the hit whatever that animation costs. Recorded here rather
    /// than decided, because it is a rendering decision and the renderer does not exist yet.</para>
    /// </remarks>
    public static bool TicksPerCombatViewRedraw => true;

    /// <summary>Put an actor into the recoil state, facing <paramref name="direction"/>.</summary>
    /// <remarks>
    /// <c>markActorHit</c> sets all three together: the flag, the timer and the direction. The
    /// direction is the only reason the state carries data at all — it is which way the sprite is
    /// thrown, so it comes from the blow rather than from the actor's facing.
    ///
    /// <para><c>applyDamageToActor</c> inlines this same trio on a damaging hit rather than
    /// calling it, so a port must not treat the explicit callers as the whole list. Those callers
    /// are mostly spells — <c>Spell_PlayHitPaletteFlash</c>, <c>Spell_HealTarget</c>,
    /// <c>Spell_TouchSlayActor</c>, <c>Cast_Drain_Strength</c>, <c>Spell_PlayStormSequence</c> —
    /// plus <c>combat_actor_play_short_cine</c>.</para>
    /// </remarks>
    public static (CombatantFlags Flags, int Timer, int Direction) Begin(
        CombatantFlags flags, int direction) =>
        (flags | CombatantFlags.Knockback, Ticks, direction);

    /// <summary>
    /// One tick of the sweep: count down, and clear the flag when the count reaches zero.
    /// </summary>
    /// <remarks>
    /// <b>The decrement is unconditional once the flag is set, and the clear tests for exactly
    /// zero.</b> That matters for a timer that somehow starts negative: the original would count
    /// down past zero for ever rather than clearing, because the test is <c>or ax,ax / jnz</c> on
    /// the decremented value and not a <c>&lt;= 0</c>. Reproduced rather than corrected — nothing
    /// sets the timer to anything but <see cref="Ticks"/>, so the difference is unreachable, and
    /// guessing at a fix would be inventing behaviour.
    /// </remarks>
    public static (CombatantFlags Flags, int Timer) Tick(CombatantFlags flags, int timer) {
        if ((flags & CombatantFlags.Knockback) == 0) {
            return (flags, timer);
        }
        timer--;
        return timer == 0 ? (flags & ~CombatantFlags.Knockback, timer) : (flags, timer);
    }
}
