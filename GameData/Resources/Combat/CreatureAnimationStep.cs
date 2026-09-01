namespace GameData.Resources.Combat;

/// <summary>
/// One step of a creature's bitmap animation — <c>advanceCreatureAnimationFrame</c> @0x5d37a, the
/// walk cycle and the idle gait.
/// </summary>
/// <remarks>
/// <b>This is the half of the animation that changes WHICH FRAME is shown.</b> The other half —
/// which column, i.e. the facing — is <c>DirectionalSprite</c>'s. They are independent, and the
/// task that owns them has said so since before either was written.
///
/// <para><b>AWAITING ITS FEATURE (TASK-103).</b> Nothing steps a combatant's animation yet.</para>
///
/// <para>Traced whole on 2026-09-01. The function's own IDA comment had said the reverse path and
/// the exact <c>isComplete</c> condition were never read (only the first ~75 of 161 instructions),
/// and both turned out to carry rules a port would not have guessed.</para>
/// </remarks>
public static class CreatureAnimationStep {
    /// <summary>
    /// <b>The gait rate is RE-ROLLED AT EVERY FRAME, and only for slot 0.</b>
    /// </summary>
    /// <remarks>
    /// After each advance, slot 0 sets <c>frameDelay = 8 + random(0..7)</c> — a fresh value every
    /// frame, not once per animation. So the idle/walk gait is deliberately irregular, and a port
    /// with a fixed delay produces a metronome the original never has.
    ///
    /// <para>Non-zero slots keep whatever delay they were given, which is what makes an attack or a
    /// death animation play at its authored speed while the creature's own idle breathes.</para>
    /// </remarks>
    public const int GaitDelayMinimum = 8;

    /// <summary>The inclusive top of the re-rolled delay — <c>(rand &amp; 7) + 8</c>.</summary>
    public const int GaitDelayMaximum = 15;

    /// <summary>The delay slot 0 takes for its next frame.</summary>
    public static int NextGaitDelay(int roll) => (roll & 7) + GaitDelayMinimum;

    /// <summary>
    /// Whether this step advances a frame at all — <c>tickCounter % frameDelay == 0</c>.
    /// </summary>
    /// <remarks>
    /// <b>A modulo, not a countdown</b>, and the counter resets to <b>1</b> rather than 0 on an
    /// advance. Every other tick just increments it. Reproduced exactly because the two differ on
    /// the first tick after an advance, which is where a gait visibly stutters.
    /// </remarks>
    public static bool Advances(int tickCounter, int frameDelay) =>
        frameDelay != 0 && tickCounter % frameDelay == 0;

    /// <summary>The counter value after a step that advanced.</summary>
    public const int TickCounterAfterAdvance = 1;

    /// <summary>
    /// <b>Slot 0 PING-PONGS; every other slot runs once and reports complete.</b>
    /// </summary>
    /// <remarks>
    /// Reaching past <c>endFrame</c> going forward does one of two things, and which one is decided
    /// by the slot rather than by any flag on the animation:
    /// <list type="bullet">
    /// <item><b>slot 0</b> — <c>currentFrame = endFrame - 1</c>, <c>endFrame--</c>,
    /// <c>isReversing = 1</c>: it turns round and walks back down.</item>
    /// <item><b>any other slot</b> — <c>currentFrame = endFrame</c>, <c>isComplete = 1</c>: it stops
    /// on the last frame and stays there.</item>
    /// </list>
    ///
    /// <para>The <c>endFrame--</c> is not a leak. Walking back down, dropping below the decremented
    /// <c>endFrame</c> calls <c>combat_actor_anim0_if_not_dead(actor, -1)</c>, which restarts the
    /// animation from slot 0 — so the cycle is forward, bounce, back, restart, and the shrink lasts
    /// exactly one pass. The <c>-1</c> is the direction argument and means "keep the current
    /// facing", which is why a creature does not spin when its idle loops.</para>
    /// </remarks>
    public static bool PingPongs(int animSlotIndex) => animSlotIndex == 0;

    /// <summary>
    /// <b>Facings above 4 are drawn as MIRRORED versions of the others.</b>
    /// </summary>
    /// <remarks>
    /// The step ends by setting the global bitmap flag to a horizontal flip when
    /// <c>facingDirection &gt; 4</c> and clearing it otherwise. So the sheet holds five columns and
    /// the other three are the same art reversed — which is why a creature's left and right poses
    /// are exact mirrors and cannot carry asymmetric detail.
    /// </remarks>
    public static bool DrawnMirrored(int facingDirection) => facingDirection > 4;

    /// <summary>
    /// Where the stepped frame is published: <c>callerBuffer + animSlotIndex</c>.
    /// </summary>
    /// <remarks>
    /// <b>The caller supplies the buffer, and this is how the walk frame reaches the sliding
    /// sprite.</b> <c>animateCombatActorMove</c> passes a small stack buffer here and then hands
    /// its first byte to <c>Combat_AnimateProjectileToTarget</c> as the sprite parameter, so the
    /// creature is drawn mid-stride while it slides between cells. A port that stepped the
    /// animation and the slide independently would show a static pose gliding across the grid.
    /// </remarks>
    public static int PublishOffset(int animSlotIndex) => animSlotIndex;
}
