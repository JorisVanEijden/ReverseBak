namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// Using an item during a fight — <c>combat_arena_resume_dispatch</c> (COMBAT.C:1762).
/// </summary>
/// <remarks>
/// <b>The command id IS the object id.</b> The routine's <c>switch</c> arms are 0x02, 0x04, 0x09,
/// 0x0b, 0x0c, 0x0d, 0x0f, 0x32, 0x33 and 0x34, and its shared tail calls
/// <c>itemtbl_inv_consume_one_by_kind(inventory, command_id)</c> — the same value. So this is not a
/// menu-action table that happens to line up with items; it is ten object ids dispatched directly.
///
/// <para><b>Two arms deliberately fall out BEFORE the shared tail and so consume nothing</b>: the
/// Lightning Staff underground, and the Idol of Lassur in chapter 8. Reading the tail as
/// unconditional would silently eat an item on both.</para>
///
/// <para><b>The flag <c>g_bStormAmplify</c> is not a storm.</b> canassa's name says weather; the
/// routine sets it on <c>case 0x0d</c>, immediately after the cast menu returns, and 0x0d is object
/// 13 — the <b>Infinity Pool</b>. It is that item's +50% amplification of the spell you just chose.
/// <c>SpellCostModifiers.Effective</c>'s <c>surcharged</c> parameter is this and nothing else.</para>
/// </remarks>
/// <remarks>
/// <b>AWAITING ITS FEATURE (TASK-263).</b> Nothing in the port lets a player use an item during a
/// fight — the combat HUD has no Use command wired — so this table has no caller yet.
///
/// <para>Read by <c>scripts/audit-unconsumed-models.py</c>: it separates a rule ported ahead
/// of its feature from an orphan nobody owns, so the audit stays a signal instead of a list
/// to re-triage every run.</para>
/// </remarks>
public static class CombatItemUse {
    /// <summary>What the picker will accept as a target.</summary>
    public enum Targeting {
        /// <summary>No target is picked at all.</summary>
        None,

        /// <summary>Any encounter actor on the grid — <c>combat_arena_pick_target_actor(0)</c>.</summary>
        AnyEnemy,

        /// <summary>
        /// An encounter actor <b>orthogonally adjacent</b> to the user —
        /// <c>combat_arena_pick_target_actor(1)</c>, which adds <c>combatgrid_actors_ortho_adj</c>.
        /// </summary>
        AdjacentEnemy,
    }

    /// <summary>What using the item does.</summary>
    public enum Effect {
        /// <summary>Cast <see cref="Use.SpellId"/> at <see cref="Use.Cost"/>.</summary>
        CastSpell,

        /// <summary>Kill the target outright — <c>combat_arena_actor_die</c>, no roll.</summary>
        SlayTarget,

        /// <summary>Conjure <see cref="Use.SummonCreature"/>.</summary>
        Summon,

        /// <summary>Send the target off the east edge — <c>combatenc_actor_flee_tile_east</c>.</summary>
        RouteOne,

        /// <summary>Send a whole team off — <c>combatenc_apply_flee_tile_team</c>.</summary>
        RouteTeam,

        /// <summary>Open the cast menu, then amplify what was cast.</summary>
        AmplifiedCast,
    }

    /// <summary>One dispatch arm.</summary>
    public readonly struct Use {
        public Use(int objectId, Effect effect, Targeting targeting, int spellId = 0, int cost = 0,
            int summonCreature = 0, int summonCount = 0, int soundId = 0,
            int backfirePercent = 0, bool refusedUnderground = false, int refusedInChapter = 0) {
            ObjectId = objectId;
            Effect = effect;
            Targeting = targeting;
            SpellId = spellId;
            Cost = cost;
            SummonCreature = summonCreature;
            SummonCount = summonCount;
            SoundId = soundId;
            BackfirePercent = backfirePercent;
            RefusedUnderground = refusedUnderground;
            RefusedInChapter = refusedInChapter;
        }

        public int ObjectId { get; }
        public Effect Effect { get; }
        public Targeting Targeting { get; }

        /// <summary>The spell cast, for <see cref="Effect.CastSpell"/>.</summary>
        public int SpellId { get; }

        /// <summary>
        /// The intensity passed to <c>cspell_resolve_cast</c>, <b>negative</b>.
        /// </summary>
        /// <remarks>
        /// These are the literals the routine passes, and they are the reason
        /// <c>SpellCostModifiers</c> treats a negative cost as sign-plus-magnitude rather than as a
        /// negative quantity: every one of them arrives this way.
        /// </remarks>
        public int Cost { get; }

        /// <summary>The creature conjured, for <see cref="Effect.Summon"/>.</summary>
        public int SummonCreature { get; }

        /// <summary>How many are conjured — the Horn does it <b>twice</b>.</summary>
        public int SummonCount { get; }

        /// <summary>A cue played before the effect, or 0.</summary>
        public int SoundId { get; }

        /// <summary>
        /// Chance the effect lands on the <b>user</b> instead of the target.
        /// </summary>
        /// <remarks>
        /// Only Roric's Seal has one: <c>if (RND(100) &lt; 0x1e)</c> casts at the current actor. The
        /// target is still picked first and a cancelled pick still aborts, so the backfire is not a
        /// way to use it on yourself deliberately.
        /// </remarks>
        public int BackfirePercent { get; }

        /// <summary>
        /// <b>Refused in the dungeon</b> (<c>g_game_mode == 2</c>): a cue plays and nothing else
        /// happens — <b>including the consumption</b>.
        /// </summary>
        public bool RefusedUnderground { get; }

        /// <summary>
        /// A chapter in which the item does nothing at all, or 0.
        /// </summary>
        /// <remarks>
        /// <b>V102CD only.</b> The guard is inside <c>#ifdef V102CD</c>, so the floppy build lets the
        /// Idol of Lassur kill in chapter 8 and the CD build does not. We target the CD build, so the
        /// guard applies — see <c>reference_target_exe_is_v102_cd</c>.
        /// </remarks>
        public int RefusedInChapter { get; }
    }

    /// <summary>The Infinity Pool's amplification of the spell chosen through it: <b>+50%</b>.</summary>
    /// <remarks>
    /// Applied by <c>cspell_resolve_cast</c> as <c>intensity += intensity &gt;&gt; 1</c> — see
    /// <see cref="Spells.SpellCostModifiers.Surcharge"/>, which is the same shift and the same
    /// rounding. The flag is cleared by the resolve, so it amplifies exactly one cast.
    /// </remarks>
    public const int InfinityPoolObjectId = 0x0d;

    /// <summary>
    /// <b>There is no "Use" button.</b> The dispatch runs on whatever the in-combat INVENTORY SCREEN
    /// returns.
    /// </summary>
    /// <remarks>
    /// <c>combat_arena_suspend_char_screen</c> (COMBAT.C:1849) is the only caller:
    /// <code>
    /// cmdId = cmbinv_inventory_screen_run(actor->actor_record, partySlot + 1, 0);
    /// ...
    /// combat_arena_resume_dispatch(cmdId, ...);
    /// </code>
    /// So the door is <c>CombatCommands.Command.CharacterScreen</c> — open a fighter's inventory
    /// mid-fight, use something, and the screen hands back the object id it used. A port that adds a
    /// Use command to the combat HUD builds a control the original does not have, and one that
    /// treats <see cref="All"/> as menu actions never finds the caller.
    /// </remarks>
    public static bool EnteredFromTheInventoryScreen => true;

    /// <summary>
    /// <b>Holding SHIFT opens the character INFO screen instead</b>, which uses nothing.
    /// </summary>
    /// <remarks>
    /// The branch is <c>menupage_state_0e7c() == 2 || key_is_down(0x2a) || key_is_down(0x36)</c> —
    /// either shift key. That path calls <c>charscreen_info_loop</c> and never assigns
    /// <c>cmdId</c>, which the dispatch is then called with regardless; in the reconstruction the
    /// value is simply uninitialised. Nothing in the ten arms matches an arbitrary value often, so
    /// it reads as harmless, but it is not a deliberate "no item" sentinel and should not be ported
    /// as one — pass an id that matches nothing, on purpose.
    /// </remarks>
    public const int NoItemUsed = 0;

    /// <summary>
    /// <b>The fight's combatants are a SNAPSHOT, and the screen edits the real characters.</b>
    /// </summary>
    /// <remarks>
    /// Before opening the screen the routine copies every party combatant out into
    /// <c>g_gameState.characters</c> (nulling <c>inner</c>, the combat-only state), and afterwards
    /// copies them back while <b>restoring each saved <c>inner</c> pointer</b>. So an item consumed
    /// or a stat changed in the screen survives into the rest of the fight, and the combat state does
    /// not. Skipping either half loses the consumption or resets the turn's flags.
    /// </remarks>
    public static bool PartyStateRoundTripsThroughTheScreen => true;

    /// <summary>The ten arms, in the routine's own order.</summary>
    public static readonly IReadOnlyList<Use> All = new[] {
        // Idol of Lassur: kills whatever it is pointed at, with no roll and no save.
        new Use(0x0c, Effect.SlayTarget, Targeting.AnyEnemy, refusedInChapter: 8),
        // Lightning Staff: works everywhere EXCEPT underground, where it only makes a noise.
        new Use(0x02, Effect.CastSpell, Targeting.AnyEnemy, spellId: 5, cost: -9,
            soundId: 0x13, refusedUnderground: true),
        new Use(0x04, Effect.CastSpell, Targeting.AnyEnemy, spellId: 4, cost: -0x1e),
        // Roric's Seal: three times in ten it goes off in your own hand.
        new Use(0x0f, Effect.CastSpell, Targeting.AnyEnemy, spellId: 0x16, cost: -0xf,
            backfirePercent: 0x1e),
        new Use(0x34, Effect.RouteTeam, Targeting.None, soundId: 0x4b),
        new Use(0x0b, Effect.Summon, Targeting.None, summonCreature: 0x2e, summonCount: 2,
            soundId: 0x4c),
        new Use(0x09, Effect.Summon, Targeting.None, summonCreature: 0x38, summonCount: 1),
        new Use(0x33, Effect.CastSpell, Targeting.AdjacentEnemy, spellId: 0xd, cost: -0x10),
        new Use(0x32, Effect.RouteOne, Targeting.AdjacentEnemy),
        new Use(InfinityPoolObjectId, Effect.AmplifiedCast, Targeting.None),
    };

    /// <summary>The arm for an object id, or null when the item does nothing in a fight.</summary>
    public static Use? For(int objectId) {
        foreach (Use use in All) {
            if (use.ObjectId == objectId) {
                return use;
            }
        }
        return null;
    }

    /// <summary>
    /// Whether using it here does anything — and therefore whether it is consumed.
    /// </summary>
    /// <param name="underground">The dungeon mode, <c>g_game_mode == 2</c>.</param>
    /// <param name="chapter">The current chapter.</param>
    /// <remarks>
    /// <b>Refusal and consumption are the same question, because both refusals return before the
    /// shared tail.</b> An implementation that consumed first and then checked would eat the Idol in
    /// chapter 8 and the Lightning Staff in every dungeon.
    /// </remarks>
    public static bool Works(Use use, bool underground, int chapter) =>
        !(use.RefusedUnderground && underground)
        && !(use.RefusedInChapter != 0 && use.RefusedInChapter == chapter);

    /// <summary>
    /// Whether a cancelled target pick aborts the use.
    /// </summary>
    /// <remarks>
    /// <b>Yes, and nothing is consumed.</b> Every targeted arm is <c>if (target == 0) return;</c>
    /// before the tail, so backing out of the pick costs nothing — which is what makes the picker
    /// safe to open on a misclick.
    /// </remarks>
    public static bool CancellingTheTargetPickCostsNothing => true;
}
