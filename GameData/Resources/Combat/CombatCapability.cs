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
    /// <b>Our <see cref="CombatTerrain"/> does not name this kind.</b> The original tests
    /// <c>combatgrid_tile_terrain_field(...) != 1</c> in both predicates, but 1 is absent from the
    /// terrain enum we reconstructed from <c>Load_grid</c>. Recorded as a number rather than given
    /// an invented name.
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
    /// <param name="healthThresholds">The threshold ladder health is compared against.</param>
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

    /// <summary>Distance standing for "do not apply the adjacency rule".</summary>
    /// <remarks>The original substitutes 100 when it is told not to look for the nearest actor.</remarks>
    public const int DistanceUnchecked = 100;

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
