namespace GameData.Resources.Combat;

/// <summary>
/// The fallen getting back up — <c>SRC/COMBAT/AI/CBTAIACT.C</c>'s revive cycle and the per-tick
/// sweep that drives it.
///
/// <para><b>A fallen Nighthawk rises as a Black Slayer.</b> The revive does not merely restore an
/// actor: for one of the two eligible creatures it <i>changes its species</i>, releasing the old
/// animation set and loading the other one, so the thing that gets up is not the thing that went
/// down.</para>
/// </summary>
public static class SlayerRevival {
    /// <summary>What a risen creature always ends up as.</summary>
    public const int RisenType = 22;

    /// <summary>The creature that changes species on rising.</summary>
    public const int TransformingType = 23;

    /// <summary>
    /// The actor flag that marks a candidate: <b>dead</b>.
    /// </summary>
    /// <remarks>
    /// The same flag gates a creature out of acting after it moves (see
    /// <see cref="MonsterTurnRoutines.ActsAfterMoving"/>), which is consistent — the actors that do
    /// not act are the ones waiting to get up.
    /// </remarks>
    public const int DeadFlag = 0x02;

    /// <summary>
    /// The flag that bars an otherwise-eligible corpse from rising: it <b>fled</b>.
    /// </summary>
    /// <remarks>
    /// A creature that ran off the field is barred twice over — it keeps this flag, and the exit
    /// path kills it without playing the death animation, which is the only place the countdown is
    /// ever set. Either alone would be enough.
    /// </remarks>
    public const int FledFlag = 0x10;

    /// <summary>Shortest countdown rolled when one of these creatures dies.</summary>
    public const int MinimumCountdown = 4;

    /// <summary>Longest countdown rolled when one of these creatures dies.</summary>
    public const int MaximumCountdown = 10;

    /// <summary>Grid coordinate meaning the actor is not on the field.</summary>
    public const int OffGrid = -1;

    /// <summary>Terrain effect left on the tile a creature rises from.</summary>
    public const int RisenTileEffect = 9;

    /// <summary>How long that effect is set to run for.</summary>
    public const int RisenTileEffectDuration = 400;

    /// <summary>
    /// Whether the sweep runs at all this tick.
    /// </summary>
    /// <param name="slayersPresent">Combatants of <see cref="RisenType"/> in the encounter's list.</param>
    /// <remarks>
    /// <b>Nothing rises unless one of these is already in the fight</b>, so an encounter with only
    /// the transforming creature in it never sees a single revival — the first riser needs one to
    /// already be there.
    ///
    /// <para><b>And killing them does not stop it.</b> The count is taken by creature type with no
    /// test that the actor is alive, and the encounter's combatant list never shrinks — the only
    /// write to its length sets it to zero at setup, and the one removal routine compacts a
    /// different list. So a dead one keeps the sweep running, and since it is itself an eligible
    /// corpse with a countdown of its own, it gets back up too. The mechanic is self-sustaining:
    /// killing the risers is not the way to end it.</para>
    /// </remarks>
    public static bool SweepRuns(int slayersPresent) => slayersPresent != 0;

    /// <summary>Whether a creature is one of the two that can rise.</summary>
    public static bool IsEligibleSpecies(int creatureType) =>
        creatureType == RisenType || creatureType == TransformingType;

    /// <summary>
    /// Whether this actor is a candidate at all — before the countdown is consulted.
    /// </summary>
    public static bool IsCandidate(int creatureType, int flags) =>
        (flags & DeadFlag) != 0
        && (flags & FledFlag) == 0
        && IsEligibleSpecies(creatureType);

    /// <summary>
    /// What the sweep does with a candidate this tick.
    /// </summary>
    /// <param name="countdown">
    /// Ticks left before it rises, rolled at death as <see cref="MinimumCountdown"/> to
    /// <see cref="MaximumCountdown"/>. The original reuses the actor's damage-float frame counter
    /// for this, so the field means two different things depending on whether the actor is dead.
    /// </param>
    /// <param name="gridX">The actor's grid column, or <see cref="OffGrid"/>.</param>
    /// <returns>True to rise now; false to count down one tick.</returns>
    /// <remarks>
    /// <b>An actor that is off the grid counts down forever and never rises.</b> The position test
    /// sits alongside the countdown rather than before it, so being off the field does not cancel
    /// the wait — it just never ends.
    /// </remarks>
    public static bool RisesThisTick(int countdown, int gridX) =>
        countdown == 0 && gridX != OffGrid;

    /// <summary>
    /// Whether the revive can actually happen where the actor is lying.
    /// </summary>
    /// <remarks>
    /// The whole cycle is wrapped in this test, so a body under something that has since blocked its
    /// tile stays down — and, because the countdown already reached zero, it will be retried every
    /// tick until the tile clears.
    /// </remarks>
    public static bool CanRiseOnTile(bool tileBlocked) => !tileBlocked;

    /// <summary>
    /// The species the actor is once it is up.
    /// </summary>
    /// <remarks>
    /// One-way: the transforming creature becomes the other, and the other stays as it is. Nothing
    /// turns back.
    /// </remarks>
    public static int TypeAfterRising(int creatureType) =>
        creatureType == TransformingType ? RisenType : creatureType;

    /// <summary>
    /// Whether rising restores the creature's first two stats to full.
    /// </summary>
    /// <remarks>
    /// It does — so what gets up is at full strength, not a weakened survivor. Only those two of its
    /// stats are touched.
    /// </remarks>
    public static bool RisesAtFullStrength => true;
}
