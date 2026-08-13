namespace GameData.Resources.World;

using GameData.Resources.Character;
using System.Collections.Generic;

/// <summary>
/// Walking into a pit — <c>worldcross_dungeon_descent_anim</c>
/// (<c>SRC/GAME/WORLD/WORLDCRS.C</c>). Terrain kind 15 is walkable on purpose: falling in is how you
/// get to the level below.
/// </summary>
public static class PitDescent {
    /// <summary>The walkable terrain kind that drops you.</summary>
    public const int PitTerrainKind = 0xf;

    /// <summary>
    /// <b>Pits only exist underground.</b> The whole routine is wrapped in a zone-kind test, so a
    /// kind-15 polygon in an outdoor zone does nothing at all. Same enclosed zone kind that gates
    /// doors and Candle Glow — see <c>DoorMechanics</c> and <c>SpellCasting</c>.
    /// </summary>
    public const int RequiredZoneKind = 2;

    /// <summary>Camera frames in the fall.</summary>
    public const int DescentSteps = 9;

    /// <summary>
    /// Frames when scancode 0x19 ('P') is held as you fall — a longer drop, for no stated reason.
    /// The original also then waits for a different key before continuing.
    /// </summary>
    public const int HeldKeyDescentSteps = 0xd;

    /// <summary>World units the camera drops per frame.</summary>
    public const int UnitsPerStep = 0x50;

    /// <summary>Sound effect played for the fall.</summary>
    public const int FallSoundId = 0x2f;

    /// <summary>Dialog shown on landing.</summary>
    public const int LandingDialogId = 0x115;

    /// <summary>
    /// The value the original writes to <c>bCombatExitRequest</c> to make the world loop exit into
    /// the level below. It is not a combat flag despite the name — the same field doubles as the
    /// loop's general "leave, for this reason" signal.
    /// </summary>
    public const int WorldLoopExitRequest = 2;

    /// <summary>
    /// What the fall does to the party.
    ///
    /// <para><b>Every active member is put at full Near-death, not merely damaged.</b> The original
    /// calls <c>stat_combatant_apply_condition(member, 6, 100)</c> — condition 6 is Near-death, not
    /// attribute 6 — so this is far more severe than a hit-point loss, and it lands on the whole
    /// party at once regardless of who was leading.</para>
    ///
    /// <para>Because raising Near-death <i>collapses</i> an actor (see <c>ConditionEngine</c>), the
    /// fall also <b>clears every other affliction</b> as a side effect: a poisoned, starving party
    /// lands near dead but otherwise clean. That is a consequence of the shared condition rule
    /// rather than anything pit-specific, and it is reproduced rather than special-cased.</para>
    /// </summary>
    /// <param name="party">Conditions of the active members; nulls are skipped.</param>
    /// <param name="stats">
    /// Optional matching health/stamina pairs. Supplying them lets the collapse zero and refill the
    /// pool as the original does; omit them and only the condition ranks change.
    /// </param>
    public static void ApplyToParty(IEnumerable<ActorConditions> party,
        IEnumerable<(ActorStat Health, ActorStat Stamina)> stats = null) {
        if (party == null) {
            return;
        }

        IEnumerator<(ActorStat Health, ActorStat Stamina)> pools = stats?.GetEnumerator();
        foreach (ActorConditions conditions in party) {
            bool hasPool = pools != null && pools.MoveNext();
            if (conditions == null) {
                continue;
            }
            if (hasPool) {
                ConditionEngine.Apply(conditions, ActorCondition.NearDeath, ActorConditions.MaxRank,
                    pools.Current.Health, pools.Current.Stamina);
            } else {
                ConditionEngine.Apply(conditions, ActorCondition.NearDeath, ActorConditions.MaxRank);
            }
        }
        pools?.Dispose();
    }

    /// <summary>
    /// How many frames the fall lasts.
    /// </summary>
    /// <param name="descendKeyHeld">Whether scancode 0x19 is down as the fall begins.</param>
    public static int StepsFor(bool descendKeyHeld) =>
        descendKeyHeld ? HeldKeyDescentSteps : DescentSteps;

    /// <summary>The camera's z offset from its starting height at a given frame of the fall.</summary>
    public static int DropAtStep(int step) => -step * UnitsPerStep;

    /// <summary>
    /// Whether stepping onto this terrain drops the party.
    ///
    /// <para>The original checks the <b>crossing</b> kind recorded on the previous loop iteration,
    /// not the kind under the party right now — you fall on the iteration <i>after</i> the crossing
    /// is recorded, which is what gives the step onto the pit a frame to render.</para>
    /// </summary>
    public static bool Triggers(int crossingKind, int zoneKind) =>
        zoneKind == RequiredZoneKind && crossingKind == PitTerrainKind;
}
