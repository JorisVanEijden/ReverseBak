namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// Whether the acting character may shoot or cast this turn — the two predicates behind the combat
/// HUD's shared capability cell.
///
/// <para><c>combatenc_show_missile_stat_row</c> and <c>combatenc_actor_can_cast_spells</c>
/// (canassa CBENC.C ~507). <see cref="CombatMenuSlots"/> decides which button occupies the cell;
/// these decide whether it is offered at all.</para>
///
/// <para><b>Both are situational, not character sheet facts.</b> The same character can shoot on one
/// turn and not the next: an enemy stepping adjacent takes the option away. A port that computes
/// these once at the start of a fight gets them wrong from the second turn onward.</para>
/// </summary>
public static class CombatCapability {
    /// <summary>
    /// The terrain kind that denies both shooting and casting.
    /// </summary>
    /// <remarks>
    /// <b>Our <see cref="CombatTerrain"/> does not name this kind, and neither can we yet.</b>
    ///
    /// <para>What is established: a sweep of every <c>combatgrid_tile_terrain_field</c> call shows
    /// terrain 1 is compared <b>only</b> in these two predicates (CBENC.C:511 and :530) — nowhere
    /// else in the game. Its entire observable role is denying ranged attacks and spellcasting to
    /// whoever stands on it. Other kinds are read all over (4, 5, 9 elsewhere), so this is not an
    /// artefact of a narrow search.</para>
    ///
    /// <para>What is NOT established: what the tile actually is. A guess like "water" or "under
    /// cover" would fit the behaviour and has no evidence behind it. <b>Whether it even occurs in
    /// shipped data is also unchecked</b> — the combat grid terrain is not among our extracted
    /// formats (<c>GridData</c> holds only zone border pens), so the branch could be dead. Settling
    /// it means extracting the arena grids.</para>
    /// </remarks>
    public const int DenyingTerrain = 1;

    /// <summary>
    /// A creature that shoots without carrying anything — it needs neither ammunition nor a weapon.
    /// </summary>
    public const int InnatelyMissileCreature = 0x1a;

    /// <summary>Equipment category of a ranged weapon, as <c>cbstat_find_intact_equip_cat</c> counts.</summary>
    public const int RangedWeaponCategory = 2;

    /// <summary>
    /// <b>Neither shooting nor casting is allowed with an enemy adjacent.</b>
    /// </summary>
    /// <remarks>
    /// The two predicates spell the same rule differently — shooting wants <c>1 &lt; dist</c> and
    /// casting <c>dist &gt;= 2</c> — which are the same test on integers. Distance is to the NEAREST
    /// actor, so being surrounded is no different from one neighbour.
    /// </remarks>
    public static bool RangeIsClear(int nearestActorDistance) => nearestActorDistance >= 2;

    /// <summary>
    /// Whether the Shoot button is offered.
    /// </summary>
    /// <param name="terrainKind">Terrain of the tile the actor stands on.</param>
    /// <param name="nearestActorDistance">Distance to the nearest other actor.</param>
    /// <param name="quarrelsOfAnyKind">Ammunition carried, counted across every kind.</param>
    /// <param name="hasIntactRangedWeapon">An undamaged weapon in <see cref="RangedWeaponCategory"/>.</param>
    /// <param name="creatureType">Used only for <see cref="InnatelyMissileCreature"/>.</param>
    /// <param name="crossbowSkill">The actor's Crossbow accuracy — stat 5.</param>
    /// <remarks>
    /// <b>Ammunition AND a weapon, or being the one creature that needs neither.</b> The carried
    /// check is an OR against creature type, so an innately missile creature shoots with an empty
    /// quiver and no bow.
    ///
    /// <para><b>A skill of zero refuses outright</b> rather than shooting and always missing.</para>
    /// </remarks>
    public static bool CanShoot(int terrainKind, int nearestActorDistance, int quarrelsOfAnyKind,
        bool hasIntactRangedWeapon, int creatureType, int crossbowSkill) {
        if (terrainKind == DenyingTerrain) {
            return false;
        }
        bool armed = (quarrelsOfAnyKind != 0 && hasIntactRangedWeapon)
            || creatureType == InnatelyMissileCreature;
        return armed && RangeIsClear(nearestActorDistance) && crossbowSkill != 0;
    }

    /// <summary>
    /// Whether the Cast button is offered.
    /// </summary>
    /// <param name="terrainKind">Terrain of the tile the actor stands on.</param>
    /// <param name="nearestActorDistance">
    /// Distance to the nearest other actor. <b>Pass <see cref="DistanceUnchecked"/> to skip the
    /// adjacency rule</b> — the original's <c>find_nearest</c> argument does exactly that, and it is
    /// not a bool despite reading like one.
    /// </param>
    /// <param name="castingSkill">The actor's Casting accuracy — stat 7.</param>
    /// <param name="health">Current health — stat 0.</param>
    /// <param name="healthThresholds">
    /// The threshold ladder health is compared against. Pass <see cref="ShippedHealthThresholds"/>
    /// unless you have reason not to; with that table the gate is exactly "health &gt; 0".
    /// </param>
    /// <param name="chapterEightWithoutRequiredItem">
    /// Chapter 8 only, and only for a character outside slot 0: casting is refused when they carry
    /// none of the required item kind.
    /// </param>
    /// <remarks>
    /// <b>Casting is gated on HEALTH, not on knowing any spells.</b> The original sums nine calls to
    /// <c>combatenc_actor_stat_above_table</c>, and every one of them compares <b>stat 0</b> — health
    /// — against a different entry of a threshold table. The count is discarded; only "not zero"
    /// matters, so the whole loop reduces to health being above the LOWEST threshold. The helper's
    /// name suggests it walks nine different stats. It does not.
    /// </remarks>
    public static bool CanCast(int terrainKind, int nearestActorDistance, int castingSkill,
        int health, IReadOnlyList<int> healthThresholds, bool chapterEightWithoutRequiredItem) {
        if (terrainKind == DenyingTerrain || chapterEightWithoutRequiredItem) {
            return false;
        }
        return castingSkill != 0
            && ClearsAnyThreshold(health, healthThresholds)
            && RangeIsClear(nearestActorDistance);
    }

    /// <summary>
    /// The shipped threshold ladder — <c>g_anStatCheckThreshold</c> (canassa CBENC.C:47).
    /// </summary>
    /// <remarks>
    /// <b>Six of the nine entries are zero, so the whole health gate reduces to "health &gt; 0".</b>
    /// The three 10s never decide anything: clearing ANY entry is enough, and every living character
    /// clears the zeros. So the rule this expresses is simply that <b>a character on zero health
    /// cannot cast</b> — not a minimum-health requirement, which is what the nine-entry table and the
    /// summing loop both suggest at a glance.
    ///
    /// <para>Kept as the real table rather than collapsed to <c>health &gt; 0</c> so the data stays
    /// visible and a different build's table would still be expressed correctly.</para>
    /// </remarks>
    public static readonly int[] ShippedHealthThresholds = { 10, 10, 10, 0, 0, 0, 0, 0, 0 };

    /// <summary>Distance standing for "do not apply the adjacency rule".</summary>
    /// <remarks>
    /// The original substitutes 100 when it is told not to look for the nearest actor — and
    /// <c>combatenc_find_nearest_actor</c> also STARTS its search at 100, so "nobody found" and
    /// "did not look" produce the same answer by construction. See <see cref="NearestOpponent"/>.
    /// </remarks>
    public const int DistanceUnchecked = 100;

    /// <summary>
    /// Distance to the nearest living OPPONENT — what both capability predicates measure.
    /// </summary>
    /// <param name="fromX">The acting combatant's column.</param>
    /// <param name="fromY">Its row.</param>
    /// <param name="opponents">The other side. Dead entries are skipped, not merely ignored.</param>
    /// <remarks>
    /// <b>Opponents, not everyone.</b> <c>combatenc_find_nearest_actor</c> swaps the target state
    /// first when called for a non-encounter actor, so a party member measures against the enemy
    /// list and an enemy against the party's. An ally standing beside you does not take away your
    /// shot — only an opponent does. Searching "all combatants" would silently disarm a bunched-up
    /// party.
    ///
    /// <para><b>Chebyshev</b>, like every other distance in the arena — diagonals cost the same as
    /// orthogonals (<see cref="CombatGrid.ChebyshevDistance"/>).</para>
    ///
    /// <para>Returns <see cref="DistanceUnchecked"/> when there is nobody to find, which is the
    /// original's own starting value rather than a sentinel invented here — so an empty field reads
    /// as "clear" and both predicates behave as they do on a field with distant enemies.</para>
    /// </remarks>
    public static int NearestOpponent(int fromX, int fromY, IEnumerable<Combatant> opponents) {
        int best = DistanceUnchecked;
        if (opponents == null) {
            return best;
        }
        foreach (Combatant candidate in opponents) {
            if (candidate == null || candidate.IsDead) {
                continue;
            }
            int distance = CombatGrid.ChebyshevDistance(fromX, fromY, candidate.X, candidate.Y);
            if (distance < best) {
                best = distance;
            }
        }
        return best;
    }

    /// <summary>Whether health is above at least one entry of the ladder.</summary>
    public static bool ClearsAnyThreshold(int health, IReadOnlyList<int> healthThresholds) {
        if (healthThresholds == null) {
            return false;
        }
        for (var i = 0; i < healthThresholds.Count; i++) {
            if (health > healthThresholds[i]) {
                return true;
            }
        }
        return false;
    }
}
