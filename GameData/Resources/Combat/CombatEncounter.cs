namespace GameData.Resources.Combat;

using System.Collections.Generic;

/// <summary>
/// The tactical encounter's turn loop: who acts next, when a round ends, and when the fight is over.
///
/// <para>Ported from <c>combat_actor_pick_next</c> (<c>SRC/COMBAT/ACTOR/CACTOR.C</c>) and
/// <c>combatenc_refresh_actor_flags</c> (<c>SRC/COMBAT/ENC/CBENC.C</c>). This is the skeleton the
/// rest of the combat port hangs off — <see cref="CombatFormulas"/>, <see cref="CombatAi"/>,
/// <see cref="CombatGrid"/> and <see cref="CombatMovement"/> all exist to be driven from here.</para>
///
/// <para><b>There is no initiative list.</b> The next actor is re-picked from live speed before every
/// single turn, so a buff, a wound or a condition reorders the fight mid-round. Building a queue at
/// encounter start would look equivalent and behave differently.</para>
/// </summary>
public sealed class CombatEncounter {
    /// <summary>Class id that is immune to damage and always gets a turn even at zero speed.</summary>
    public const int AlwaysActsClassId = 0x36;

    /// <summary>The party's side.</summary>
    public List<Combatant> Party { get; } = new List<Combatant>();

    /// <summary>The opposing side.</summary>
    public List<Combatant> Enemies { get; } = new List<Combatant>();

    /// <summary>
    /// Whether an objective keeps the encounter running with no enemies left — the original's
    /// <c>combatgrid_any_terrain_6()</c>, i.e. a trap puzzle with an exit still to reach.
    /// </summary>
    public bool HasObjective { get; set; }

    /// <summary>Whoever is acting, or null once the encounter is over.</summary>
    public Combatant Current { get; private set; }

    /// <summary>The acting combatant's speed, floored the way the picker floors it.</summary>
    public int ActingSpeed { get; private set; }

    /// <summary>
    /// Living combatants on the party's side. <b>Only actual party members count</b> — that is the
    /// 1.02 CD build's rule and it is what we target; the 1.00 floppy counted any living actor on
    /// that side, which would keep a fight running on a summon alone.
    /// </summary>
    public int PartyAlive() {
        var n = 0;
        foreach (Combatant c in Party) {
            if (!c.IsDead && c.IsPartyMember) {
                n++;
            }
        }
        return n;
    }

    /// <summary>Living combatants on the opposing side.</summary>
    public int EnemiesAlive() {
        var n = 0;
        foreach (Combatant c in Enemies) {
            if (!c.IsDead) {
                n++;
            }
        }
        return n;
    }

    /// <summary>
    /// Whether the encounter has ended: the party is wiped, or the enemies are gone and no objective
    /// remains.
    /// </summary>
    public bool IsOver() => PartyAlive() == 0 || (EnemiesAlive() == 0 && !HasObjective);

    /// <summary>
    /// Starts a new round: everyone becomes ready, defend orders lapse, and anyone whose target died
    /// stops pointing at a corpse (<c>combatenc_refresh_actor_flags</c>).
    /// </summary>
    public void BeginRound() {
        foreach (Combatant c in AllCombatants()) {
            c.Flags |= CombatantFlags.Ready;
            c.Flags &= ~CombatantFlags.Defending;
            if (c.Target != null && c.Target.IsDead) {
                c.Target = null;
            }
        }
    }

    /// <summary>
    /// Picks whoever acts next and makes them <see cref="Current"/>, or ends the encounter.
    /// </summary>
    /// <remarks>
    /// Highest live speed among those that can act, scanning the party side and then the enemies.
    /// Two details decide real fights:
    /// <list type="bullet">
    ///   <item>the comparison is <b>&gt;=</b>, so <b>ties go to the last scanned</b> — the highest
    ///   index within a side, and an enemy over a party member across sides.</item>
    ///   <item>a living party member reading zero speed is <b>floored to 1</b>, so a slowed character
    ///   is never starved of turns. On the enemy side the floor applies to any living zero-speed
    ///   combatant, and unconditionally to class 0x36.</item>
    /// </list>
    /// Parry is cleared on whoever is picked, which is what makes Defend last exactly one round.
    /// </remarks>
    public Combatant PickNext() {
        if (IsOver()) {
            Current = null;
            ActingSpeed = 0;
            return null;
        }

        Combatant best = null;
        var bestSpeed = 0;

        foreach (Combatant c in Party) {
            int speed = EffectiveSpeed(c);
            if (speed >= bestSpeed && c.CanAct(strict: true)) {
                best = c;
                bestSpeed = speed;
            }
        }
        foreach (Combatant c in Enemies) {
            int speed = EffectiveSpeed(c);
            if (speed >= bestSpeed && c.CanAct(strict: true)) {
                best = c;
                bestSpeed = speed;
            }
        }

        Current = best;
        if (best == null) {
            ActingSpeed = 0;
            return null;
        }

        // A parry protects until your own next turn and no longer.
        best.Flags &= ~CombatantFlags.Parry;
        ActingSpeed = best.IsDead ? 0 : EffectiveSpeed(best);
        return best;
    }

    /// <summary>Marks the current combatant as having acted.</summary>
    public void EndTurn() {
        if (Current != null) {
            Current.Flags &= ~CombatantFlags.Ready;
        }
    }

    /// <summary>Whether every combatant that could act has acted, so the round is spent.</summary>
    public bool RoundComplete() {
        foreach (Combatant c in AllCombatants()) {
            if (c.CanAct(strict: true)) {
                return false;
            }
        }
        return true;
    }

    /// <summary>Both sides, party first — the order the picker scans in.</summary>
    public IEnumerable<Combatant> AllCombatants() {
        foreach (Combatant c in Party) {
            yield return c;
        }
        foreach (Combatant c in Enemies) {
            yield return c;
        }
    }

    // The speed floor. A living party member never reads 0; nor does a living enemy; nor does class
    // 0x36 under any circumstances, which is the same creature the damage pipeline treats as immune.
    private static int EffectiveSpeed(Combatant c) {
        if (c.ClassId == AlwaysActsClassId) {
            return c.Speed == 0 ? 1 : c.Speed;
        }
        if (c.Speed == 0 && !c.IsDead) {
            return 1;
        }
        return c.Speed;
    }
}
