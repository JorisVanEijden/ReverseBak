namespace BetrayalAtKrondor.Tests.Combat;

using System;
using System.Collections.Generic;
using GameData.Resources.Character;
using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// One swing, resolved — and a whole fight driven to its end on top of it.
/// </summary>
public class MeleeExchangeTests {
    private static Combatant Fighter(int health = 40, int stamina = 40, int partySlot = 0) =>
        new Combatant { PartySlot = partySlot, Health = health, Stamina = stamina, Speed = 5 };

    // Always-hits / always-misses to-hit streams. rnd(100) is the first call.
    private static Func<int, int> AlwaysHits => _ => 0;
    private static Func<int, int> AlwaysMisses => n => n - 1;

    private static readonly MeleeExchange.Attacker Bruiser =
        new MeleeExchange.Attacker(accuracyMelee: 90, strength: 12);

    private static readonly MeleeExchange.Defender Unarmoured =
        new MeleeExchange.Defender(defenseRating: 0, armorRating: 0, applyArmor: false);

    [Fact]
    public void ALandedSwingTakesHealthAndStaminaOffTheDefender() {
        Combatant target = Fighter();
        MeleeExchange.Result r = MeleeExchange.Resolve(Fighter(), target, Bruiser, Unarmoured, AlwaysHits);

        Assert.True(r.Hit);
        Assert.True(r.Damage > 0);
        Assert.True(target.Health + target.Stamina < 80);
    }

    [Fact]
    public void AMissLeavesTheDefenderExactlyAsItWas() {
        Combatant target = Fighter();
        MeleeExchange.Result r = MeleeExchange.Resolve(Fighter(), target, Bruiser, Unarmoured, AlwaysMisses);

        Assert.False(r.Hit);
        Assert.Equal(0, r.Damage);
        Assert.Equal(40, target.Health);
        Assert.Equal(40, target.Stamina);
    }

    [Fact]
    public void SwingingAtACorpseIsAnOrdinaryMiss_NotAThrow() {
        // Targets die between a decision and its execution, so this has to be a normal outcome.
        Combatant dead = Fighter();
        dead.Flags |= CombatantFlags.Dead;

        MeleeExchange.Result r = MeleeExchange.Resolve(Fighter(), dead, Bruiser, Unarmoured, AlwaysHits);
        Assert.False(r.Hit);
    }

    [Fact]
    public void ANullDefenderIsAMissToo() {
        Assert.False(MeleeExchange.Resolve(Fighter(), null, Bruiser, Unarmoured, AlwaysHits).Hit);
        Assert.False(MeleeExchange.Resolve(null, Fighter(), Bruiser, Unarmoured, AlwaysHits).Hit);
    }

    [Fact]
    public void NoRandomSourceIsAProgrammingErrorRatherThanASilentMiss() {
        Assert.Throws<ArgumentNullException>(
            () => MeleeExchange.Resolve(Fighter(), Fighter(), Bruiser, Unarmoured, null));
    }

    [Fact]
    public void ParryPenalisesTheROLL_SoItCanPushAnAttackerBELOWTheFloor() {
        // *** Where "on the roll" actually differs from "off the chance". *** The two are
        // algebraically the same (roll + p < chance  <=>  roll < chance - p), so most inputs cannot
        // tell them apart — my first attempt at this test could not, and a mutation proved it.
        //
        // They part company at the CLAMP. MeleeHitChance floors the chance at MinHitChance (2)
        // BEFORE MeleeHits sees it, and the parry penalty is then added to the roll. So a hopeless
        // attacker who would still land on a roll of 1 cannot land at all against a guard. Folding
        // the penalty into the chance before the clamp would let the floor hold it at 2 and parry
        // would stop mattering against exactly the attackers it should stop hardest.
        var hopeless = new MeleeExchange.Attacker(accuracyMelee: 0, strength: 12);
        Func<int, int> rollOfOne = n => n == 100 ? 1 : 0;

        Combatant open = Fighter();
        Assert.True(MeleeExchange.Resolve(Fighter(), open, hopeless, Unarmoured, rollOfOne).Hit,
            "at the floor a roll of 1 still lands");

        Combatant guarding = Fighter();
        guarding.Flags |= CombatantFlags.Parry;
        Assert.False(MeleeExchange.Resolve(Fighter(), guarding, hopeless, Unarmoured, rollOfOne).Hit,
            "against a guard the same attacker cannot land at all");
    }

    [Fact]
    public void ArmourNeverReducesAHitToNothing() {
        // CombatFormulas floors a fully-absorbed hit at a token 1-2, so heavy armour makes you hard
        // to hurt and never invulnerable.
        Combatant target = Fighter();
        var plated = new MeleeExchange.Defender(defenseRating: 0, armorRating: 100, applyArmor: true);

        MeleeExchange.Result r = MeleeExchange.Resolve(Fighter(), target, Bruiser, plated, AlwaysHits);

        Assert.True(r.Hit);
        Assert.InRange(r.Damage, 1, 2);
    }

    [Fact]
    public void AnImmuneDefenderTakesNothingAtAll() {
        Combatant target = Fighter();
        var immune = new MeleeExchange.Defender(defenseRating: 0, immune: true);

        MeleeExchange.Result r = MeleeExchange.Resolve(Fighter(), target, Bruiser, immune, AlwaysHits);

        Assert.Equal(0, r.Damage);
        Assert.Equal(40, target.Health);
    }

    [Fact]
    public void EnoughSwingsPutTheDefenderDown() {
        Combatant target = Fighter(health: 6, stamina: 0);

        var down = false;
        for (var swing = 0; swing < 40 && !down; swing++) {
            down = MeleeExchange.Resolve(Fighter(), target, Bruiser, Unarmoured, AlwaysHits).DefenderDown;
        }

        Assert.True(down, "a fight has to be able to end");
        Assert.True(target.Health <= 0);
    }

    [Fact]
    public void AFightRunsToAFinishThroughTheRealTurnLoop() {
        // *** The point of this class. *** Before it, an encounter could be entered and stepped
        // through for ever without anyone being hurt. Drive CombatEncounter's own picker, swing on
        // every turn, and the encounter reports itself over.
        var encounter = new CombatEncounter();
        var hero = new Combatant { PartySlot = 1, Health = 30, Stamina = 30, Speed = 6 };
        var monster = new Combatant { PartySlot = 0, Health = 8, Stamina = 0, Speed = 3 };
        encounter.Party.Add(hero);
        encounter.Enemies.Add(monster);
        encounter.BeginRound();

        var guard = 0;
        while (!encounter.IsOver() && guard++ < 200) {
            if (encounter.RoundComplete()) {
                encounter.BeginRound();
            }
            Combatant actor = encounter.PickNext();
            if (actor == null) {
                break;
            }

            Combatant foe = actor.IsPartyMember ? monster : hero;
            MeleeExchange.Result r = MeleeExchange.Resolve(actor, foe, Bruiser, Unarmoured, AlwaysHits);
            if (r.DefenderDown) {
                encounter.Kill(foe);
            }
            encounter.EndTurn();
        }

        Assert.True(encounter.IsOver(), "the encounter reached an end");
        Assert.True(guard < 200, "and did so without hitting the guard");
    }

    // --- what a swing trains -------------------------------------------------------------------

    private static ActorStat Skill(byte value = 20) =>
        new ActorStat { Base = value, Max = 99, Experience = 0 };

    [Fact]
    public void ADECLAREDSwingTrainsBothSides_EvenWhenItMisses() {
        // *** The award that is easiest to drop. *** The defender improves Defense for being
        // attacked at all and the attacker improves Melee for swinging, before any roll. Paying
        // these only on a hit halves the attacker's Melee curve and pays a defender nothing for a
        // fight they survived by being missed.
        ActorStat melee = Skill(), strength = Skill(), defense = Skill();
        var awards = new MeleeExchange.Advancement(melee, strength, defense);

        MeleeExchange.Result r = MeleeExchange.Resolve(
            Fighter(), Fighter(), Bruiser, Unarmoured, AlwaysMisses, awards);

        Assert.False(r.Hit);
        Assert.True(melee.Experience > 0, "the attacker was paid for swinging");
        Assert.True(defense.Experience > 0, "the defender was paid for being swung at");
        Assert.Equal(0, strength.Experience);
    }

    [Fact]
    public void ALandedSwingPaysMeleeTWICE_OnceForTryingAndOnceForConnecting() {
        ActorStat missMelee = Skill();
        MeleeExchange.Resolve(Fighter(), Fighter(), Bruiser, Unarmoured, AlwaysMisses,
            new MeleeExchange.Advancement(missMelee, Skill(), Skill()));

        ActorStat hitMelee = Skill(), hitStrength = Skill();
        MeleeExchange.Resolve(Fighter(), Fighter(), Bruiser, Unarmoured, AlwaysHits,
            new MeleeExchange.Advancement(hitMelee, hitStrength, Skill()));

        Assert.Equal(2 * missMelee.Experience, hitMelee.Experience);
        Assert.True(hitStrength.Experience > 0, "and Strength only on a hit");
    }

    [Fact]
    public void ASwingAtACorpseTrainsNothing() {
        // The guard returns before the declaration award, so a swing at something already down is
        // not a free lesson.
        ActorStat melee = Skill(), defense = Skill();
        Combatant dead = Fighter();
        dead.Flags |= CombatantFlags.Dead;

        MeleeExchange.Resolve(Fighter(), dead, Bruiser, Unarmoured, AlwaysHits,
            new MeleeExchange.Advancement(melee, Skill(), defense));

        Assert.Equal(0, melee.Experience);
        Assert.Equal(0, defense.Experience);
    }

    [Fact]
    public void AMonsterWithNoStatsToTrainIsTheOrdinaryCase() {
        // Enemies carry no ActorStat objects; passing none must resolve normally rather than throw.
        Combatant target = Fighter();
        Assert.True(MeleeExchange.Resolve(Fighter(), target, Bruiser, Unarmoured, AlwaysHits).Hit);
    }

    [Fact]
    public void RepetitionIsWhatAdvancesASkill_NotAnyOneSwing() {
        // Awards are single points in SkillUse mode, which banks the sub-unit remainder. One swing
        // moves nothing visible; a run of them does. This is also why there is no kill XP — the
        // curve IS the swinging.
        ActorStat melee = Skill();
        byte before = melee.Base;
        MeleeExchange.Resolve(Fighter(), Fighter(), Bruiser, Unarmoured, AlwaysHits,
            new MeleeExchange.Advancement(melee, Skill(), Skill()));
        Assert.Equal(before, melee.Base);

        for (var i = 0; i < 200; i++) {
            MeleeExchange.Resolve(Fighter(), Fighter(), Bruiser, Unarmoured, AlwaysHits,
                new MeleeExchange.Advancement(melee, Skill(), Skill()));
        }
        Assert.True(melee.Base > before, "200 swings buy whole points");
    }
}
