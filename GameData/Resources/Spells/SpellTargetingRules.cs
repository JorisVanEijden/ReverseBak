namespace GameData.Resources.Spells;

/// <summary>
/// What the player is allowed to aim a spell at — the targeting switch inside
/// <c>combat_arena_disp_spell_action</c> (ovr168 @0x62360), which decides whether the cell under the
/// cursor is a legal target for the selected spell.
///
/// <para><b>The targeting type does two jobs, and they agree.</b> <see cref="SpellCastTail"/> found
/// it selecting the delivery at the end of a cast; here it is the cursor's aiming rule at the start
/// of one. Every group lines up: the types that deliver no damage are the types that aim at ground
/// rather than at anybody, and the type that routes to the heal is the type that demands a named
/// character.</para>
/// </summary>
public static class SpellTargetingRules {
    /// <summary>What a targeting type requires under the cursor.</summary>
    public enum Aim {
        /// <summary>An actor that is still in the fight.</summary>
        LivingActor,

        /// <summary>A named character — a party member rather than a creature.</summary>
        NamedCharacter,

        /// <summary>An empty, unblocked cell with no crystal on it.</summary>
        ClearGround,

        /// <summary>An actor that has already been put out of the fight.</summary>
        DownedActor,

        /// <summary>A cell holding a red or green crystal.</summary>
        Crystal,
    }

    /// <summary>
    /// The aiming rule for a targeting type.
    /// </summary>
    /// <remarks>
    /// Six branches over nine types: 0 and 1/4 want a living actor, 2/3 want a named character, 5/6
    /// want clear ground, 7 wants a downed actor and 8 wants a crystal. The grouping is not in
    /// numeric order and cannot be guessed from the numbers.
    /// </remarks>
    public static Aim AimOf(int targetingType) {
        switch (targetingType) {
            case 2:
            case 3:
                return Aim.NamedCharacter;
            case 5:
            case 6:
                return Aim.ClearGround;
            case 7:
                return Aim.DownedActor;
            case 8:
                return Aim.Crystal;
            default:
                return Aim.LivingActor;
        }
    }

    /// <summary>
    /// <b>A spell that deals no damage is a spell that aims at the ground.</b>
    /// </summary>
    /// <remarks>
    /// The three types <see cref="SpellCastTail.DeliveryFor"/> sends down the charge-only path — 5, 6
    /// and 8 — are exactly the three the cursor refuses to point at an actor. So "this spell delivers
    /// nothing" and "this spell is not aimed at anybody" are the same fact seen from the two ends of
    /// the cast.
    /// </remarks>
    public static bool ChargeOnlyTypesAimAtGround(int targetingType) =>
        SpellCastTail.DeliveryFor(targetingType) != SpellCastTail.Delivery.ChargeOnly
        || AimOf(targetingType) == Aim.ClearGround
        || AimOf(targetingType) == Aim.Crystal;

    /// <summary>
    /// <b>Final Rest is a coup de grâce.</b>
    /// </summary>
    /// <remarks>
    /// Targeting type 7 is the only one that demands an <i>incapacitated</i> target, and Final Rest
    /// is the only spell that carries it. So the spell that kills outright cannot be pointed at
    /// anything still fighting — it finishes what is already down. Nothing in the spell record says
    /// so; the rule lives entirely in the cursor check.
    ///
    /// <para>It also explains why no monster can cast it: the caster AI only ever asks for types 0
    /// and 1 (see <c>MonsterSpellcasting</c>), so type 7 is out of its reach by construction.</para>
    /// </remarks>
    public static bool RequiresADownedTarget(int targetingType) =>
        AimOf(targetingType) == Aim.DownedActor;

    /// <summary>
    /// <b>The buff types demand a named character.</b>
    /// </summary>
    /// <remarks>
    /// Types 2 and 3 test the hovered actor's <i>actor number</i> and reject zero — the value
    /// monsters carry. So the spells that route to the heal delivery or hang a lingering effect can
    /// only be aimed at the party, and the cursor enforces it before the dispatcher ever sees the
    /// cast.
    /// </remarks>
    public static bool PartyOnly(int targetingType) => AimOf(targetingType) == Aim.NamedCharacter;

    /// <summary>
    /// The crystal kinds type 8 accepts.
    /// </summary>
    /// <remarks>
    /// Red or green, and nothing else — a cell with any other trap element is refused, as is a cell
    /// with none. Its counterpart is the clear-ground rule for types 5 and 6, which refuses a cell
    /// that <i>has</i> a crystal: between them the two rules partition the floor.
    /// </remarks>
    public static bool CrystalIsTargetable(bool isRedCrystal, bool isGreenCrystal) =>
        isRedCrystal || isGreenCrystal;

    /// <summary>
    /// Whether clear ground accepts this cell.
    /// </summary>
    /// <param name="blocked">The cell is impassable.</param>
    /// <param name="hasCrystal">The cell carries a trap crystal.</param>
    public static bool GroundIsTargetable(bool blocked, bool hasCrystal) => !blocked && !hasCrystal;

    /// <summary>
    /// An actor that has been put out of the fight is refused by <b>every</b> actor-aimed type
    /// except type 7.
    /// </summary>
    /// <remarks>
    /// Types 0, 1, 2, 3 and 4 all test the same status bit and reject it when set; type 7 tests it
    /// and rejects when <i>clear</i>. So the same bit reads as "not a valid target" for eight spells
    /// and "the only valid target" for one.
    /// </remarks>
    public static bool AcceptsIncapacitated(int targetingType) =>
        AimOf(targetingType) == Aim.DownedActor;

    /// <summary>
    /// The cursor bounds the check accepts, which are one wider than the nominal grid.
    /// </summary>
    /// <remarks>
    /// The guard is <c>0 &lt;= x &lt;= 8</c> and <c>0 &lt;= y &lt;= 13</c>, against a combat grid
    /// addressed as 8 columns by 13 rows — so it admits one column and one row past the last valid
    /// index. Recorded rather than modelled: whether that is slack in the check or a grid that is
    /// really nine by fourteen has not been established here, and the cell lookups that follow would
    /// decide it.
    /// </remarks>
    public static bool CursorBoundsAreOneWiderThanTheGrid => true;

    // ---------------------------------------------------------------- committing the cast
    // combat_arena_resolve_menu_action @0x626ca, case 4.

    /// <summary>
    /// <b>The ground-aimed types reach the dispatcher with no target at all.</b>
    /// </summary>
    /// <remarks>
    /// Types 5 and 6 are handed to <c>Cast_Spell</c> with a null target actor outright, and type 8
    /// gets there by the empty-cell branch — so all three of the types that aim at floor rather than
    /// at anybody arrive untargeted.
    ///
    /// <para>That is the other half of <see cref="SpellCostModifiers.DiscardsTarget"/>, which
    /// records the dispatcher nulling type 8's target on the way in. For 5 and 6 there was never a
    /// target to null: the UI simply never supplies one.</para>
    /// </remarks>
    public static bool CastsWithoutATarget(int targetingType) =>
        AimOf(targetingType) == Aim.ClearGround || AimOf(targetingType) == Aim.Crystal;

    /// <summary>
    /// <b>Casting ends the turn.</b>
    /// </summary>
    /// <remarks>
    /// Every path that reaches <c>Cast_Spell</c> clears the caster's ready bit immediately
    /// afterwards, the same bit the move and melee actions clear. There is no cast-and-then-move.
    /// </remarks>
    public static bool CastingEndsTheTurn => true;

    /// <summary>
    /// Whether a click commits the cast.
    /// </summary>
    /// <param name="mouseY">Screen Y of the click.</param>
    /// <param name="cursorDistance">The cursor's grid distance, or <see cref="OffGridDistance"/>.</param>
    /// <remarks>
    /// Two independent rejections before any spell rule is consulted: a click at or below
    /// <see cref="FieldBottomY"/> is in the menu bar rather than the field, and a distance of
    /// <see cref="OffGridDistance"/> is the sentinel for a cursor that is not over a cell. Both leave
    /// the action pending rather than cancelling it.
    /// </remarks>
    public static bool ClickCommitsTheCast(int mouseY, int cursorDistance) =>
        mouseY < FieldBottomY && cursorDistance != OffGridDistance;

    /// <summary>Screen Y at which the combat field gives way to the menu bar.</summary>
    public const int FieldBottomY = 0x8C;

    /// <summary>The distance value standing for "the cursor is not over a grid cell".</summary>
    public const int OffGridDistance = 1000;

    /// <summary>
    /// With nothing under the cursor, <b>only a crystal-aimed spell may still be cast at an
    /// actor</b>.
    /// </summary>
    /// <remarks>
    /// The empty-cell branch lets type 8 through to the same call the actor path uses, passing the
    /// null it found. Every other type falls to the ground-cast test instead, so an empty cell and a
    /// type that wants an actor simply does not commit.
    /// </remarks>
    public static bool EmptyCellStillCasts(int targetingType) =>
        AimOf(targetingType) == Aim.Crystal || AimOf(targetingType) == Aim.ClearGround;
}
