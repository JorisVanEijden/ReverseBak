namespace BetrayalAtKrondor.Tests.Combat;

using GameData.Resources.Combat;
using Xunit;

/// <summary>
/// The encounter turn loop (CACTOR.C / CBENC.C). The cases below pin the two rules that decide real
/// fights — ties going to the last scanned, and the speed floor — plus the end conditions, which are
/// asymmetric between the two sides.
/// </summary>
public class CombatEncounterTests {
    private static Combatant Member(int speed, int slot = 1, int health = 10) => new Combatant {
        PartySlot = slot,
        Speed = speed,
        Health = health,
        Flags = CombatantFlags.Ready,
    };

    private static Combatant Enemy(int speed, int classId = 1, int health = 10) => new Combatant {
        PartySlot = 0,
        ClassId = classId,
        Speed = speed,
        Health = health,
        Flags = CombatantFlags.Ready,
    };

    private static CombatEncounter Encounter(Combatant[] party, Combatant[] enemies) {
        var e = new CombatEncounter();
        e.Party.AddRange(party);
        e.Enemies.AddRange(enemies);
        return e;
    }

    // ---- who acts --------------------------------------------------------------------

    [Fact]
    public void TheFastestCombatantActsFirst() {
        Combatant slow = Member(5);
        Combatant fast = Member(9, slot: 2);
        CombatEncounter e = Encounter(new[] { slow, fast }, new[] { Enemy(3) });

        Assert.Same(fast, e.PickNext());
    }

    [Fact]
    public void ATieGoesToTheLastScannedWhichMeansTheEnemy() {
        // >= not >, and enemies are scanned after the party. A port using > would silently hand
        // every tie to the player.
        Combatant member = Member(5);
        Combatant enemy = Enemy(5);
        CombatEncounter e = Encounter(new[] { member }, new[] { enemy });

        Assert.Same(enemy, e.PickNext());
    }

    [Fact]
    public void AndWithinASideToTheHighestIndex() {
        Combatant first = Member(5);
        Combatant second = Member(5, slot: 2);
        CombatEncounter e = Encounter(new[] { first, second }, new[] { Enemy(1) });

        Assert.Same(second, e.PickNext());
    }

    [Fact]
    public void ASlowedPartyMemberIsNeverStarvedOfTurns() {
        // Speed 0 floors to 1, so it still outranks nobody-at-all but is never skipped outright.
        Combatant slowed = Member(0);
        CombatEncounter e = Encounter(new[] { slowed }, new[] { Enemy(0) });

        Assert.NotNull(e.PickNext());
        Assert.Equal(1, e.ActingSpeed);
    }

    [Fact]
    public void TheAlwaysActsCreatureKeepsItsFloorEvenAtZeroSpeed() {
        Combatant odd = Enemy(0, classId: CombatEncounter.AlwaysActsClassId);
        CombatEncounter e = Encounter(new[] { Member(1) }, new[] { odd });

        // Speed 1 for both; the tie goes to the enemy side.
        Assert.Same(odd, e.PickNext());
    }

    [Fact]
    public void SomebodyWhoCannotActIsSkipped() {
        Combatant stunned = Member(9);
        stunned.Incapacitated = true;
        Combatant ready = Member(2, slot: 2);
        CombatEncounter e = Encounter(new[] { stunned, ready }, new[] { Enemy(1) });

        Assert.Same(ready, e.PickNext());
    }

    [Fact]
    public void SomebodyWhoHasAlreadyActedIsSkipped() {
        Combatant spent = Member(9);
        spent.Flags &= ~CombatantFlags.Ready;
        Combatant ready = Member(2, slot: 2);
        CombatEncounter e = Encounter(new[] { spent, ready }, new[] { Enemy(1) });

        Assert.Same(ready, e.PickNext());
    }

    [Fact]
    public void BeingPickedClearsAParrySoDefendLastsExactlyOneRound() {
        Combatant member = Member(9);
        member.Flags |= CombatantFlags.Parry;
        CombatEncounter e = Encounter(new[] { member }, new[] { Enemy(1) });

        e.PickNext();

        Assert.Equal(CombatantFlags.None, member.Flags & CombatantFlags.Parry);
    }

    // ---- ending ----------------------------------------------------------------------

    [Fact]
    public void TheEncounterEndsWhenThePartyIsWipedOut() {
        Combatant dead = Member(5);
        dead.Flags |= CombatantFlags.Dead;
        CombatEncounter e = Encounter(new[] { dead }, new[] { Enemy(5) });

        Assert.True(e.IsOver());
        Assert.Null(e.PickNext());
    }

    [Fact]
    public void AndWhenTheEnemiesAreGone() {
        Combatant dead = Enemy(5);
        dead.Flags |= CombatantFlags.Dead;
        CombatEncounter e = Encounter(new[] { Member(5) }, new[] { dead });

        Assert.True(e.IsOver());
    }

    [Fact]
    public void ButAnObjectiveKeepsItRunningWithNoEnemiesLeft() {
        // The trap-puzzle case: nothing to fight, but there is still an exit to reach.
        Combatant dead = Enemy(5);
        dead.Flags |= CombatantFlags.Dead;
        CombatEncounter e = Encounter(new[] { Member(5) }, new[] { dead });
        e.HasObjective = true;

        Assert.False(e.IsOver());
        Assert.NotNull(e.PickNext());
    }

    [Fact]
    public void AnAllyWhoIsNotAPartyMemberDoesNotKeepTheFightAlive() {
        // The 1.02 CD rule we target: only actual party members count on that side.
        Combatant summon = new Combatant { PartySlot = 0, Speed = 5, Health = 10 };
        Combatant deadMember = Member(5);
        deadMember.Flags |= CombatantFlags.Dead;
        CombatEncounter e = Encounter(new[] { deadMember, summon }, new[] { Enemy(5) });

        Assert.Equal(0, e.PartyAlive());
        Assert.True(e.IsOver());
    }

    // ---- rounds ----------------------------------------------------------------------

    [Fact]
    public void ARoundEndsWhenEveryoneAbleToActHasActed() {
        Combatant member = Member(5);
        Combatant enemy = Enemy(4);
        CombatEncounter e = Encounter(new[] { member }, new[] { enemy });

        Assert.False(e.RoundComplete());
        e.PickNext();
        e.EndTurn();
        e.PickNext();
        e.EndTurn();

        Assert.True(e.RoundComplete());
    }

    [Fact]
    public void BeginningARoundMakesEveryoneReadyAgainAndLapsesDefendOrders() {
        Combatant member = Member(5);
        member.Flags = CombatantFlags.Defending;
        CombatEncounter e = Encounter(new[] { member }, new[] { Enemy(4) });

        e.BeginRound();

        Assert.True(member.CanAct(strict: true));
        Assert.Equal(CombatantFlags.None, member.Flags & CombatantFlags.Defending);
    }

    [Fact]
    public void ARoundResetStopsAnyonePointingAtACorpse() {
        Combatant enemy = Enemy(4);
        Combatant member = Member(5);
        member.Target = enemy;
        enemy.Flags |= CombatantFlags.Dead;
        CombatEncounter e = Encounter(new[] { member }, new[] { enemy });
        e.HasObjective = true;

        e.BeginRound();

        Assert.Null(member.Target);
    }

    [Fact]
    public void TurnOrderIsRecomputedFromLiveSpeedRatherThanFixedAtTheStart() {
        // The reason there is no initiative queue: slowing someone mid-round changes who is next.
        Combatant quick = Member(9);
        Combatant steady = Member(5, slot: 2);
        CombatEncounter e = Encounter(new[] { quick, steady }, new[] { Enemy(1) });

        Assert.Same(quick, e.PickNext());
        e.EndTurn();

        quick.Speed = 1;      // a debuff lands
        quick.Flags |= CombatantFlags.Ready;

        Assert.Same(steady, e.PickNext());
    }

    // ---- driving the whole thing -----------------------------------------------------

    /// <summary>
    /// The point of the turn loop: it has to drive the other ported pieces. This runs a fight to its
    /// end through CombatAi for the decision and CombatFormulas for the resolution, which is the
    /// first time those interfaces are used together rather than tested in isolation.
    /// </summary>
    [Fact]
    public void AWholeFightRunsThroughTheAiAndTheDamageFormulasToAConclusion() {
        Combatant hero = Member(6, health: 40);
        hero.Stamina = 10;
        Combatant beast = Enemy(5, health: 30);
        beast.Stamina = 10;
        CombatEncounter e = Encounter(new[] { hero }, new[] { beast });

        int rnd(int n) => n / 2;   // deterministic mid-roll
        var turns = 0;

        while (!e.IsOver() && turns < 500) {
            if (e.RoundComplete()) {
                e.BeginRound();
            }

            Combatant actor = e.PickNext();
            if (actor == null) {
                break;
            }
            turns++;

            Combatant foe = actor.IsPartyMember ? beast : hero;
            if (!foe.IsDead) {
                // Decide (the AI path is what an enemy would use; the hero just swings).
                AiAction action = CombatAi.ChooseAction(
                    actor.ClassId, isFleeing: false, canCastSpells: false, canShoot: false);
                Assert.Equal(AiAction.MeleeOrMove, action);

                // Resolve with the real formulas.
                int chance = CombatFormulas.MeleeHitChance(
                    accuracyMelee: 60, hasWeapon: true, weaponAccuracy: 20, classGroupModifier: 0,
                    weaponConditionPercent: 100, weaponFlags: 0,
                    targetDefenseRating: CombatFormulas.DefenseRating(20, canAct: true, 0));

                if (CombatFormulas.MeleeHits(rnd(100), chance,
                        (foe.Flags & CombatantFlags.Parry) != 0)) {
                    int damage = CombatFormulas.MeleeDamage(
                        strength: 12, hasWeapon: true, weaponBase: 10, weaponConditionPercent: 100,
                        enchantmentBonus: 0, doubled: false);

                    DamageOutcome outcome = CombatFormulas.ApplyDamage(
                        damage, foe.Stamina, foe.Health, immune: false, applyArmor: true,
                        armorRating: 10, absorbPool: null, fromDirectAttack: true, negated: false,
                        weakToDamageType: false, resistsDamageType: false, rnd);

                    foe.Stamina = outcome.Stamina;
                    foe.Health = outcome.Health;
                    if (outcome.Died) {
                        foe.Flags |= CombatantFlags.Dead;
                    }
                }
            }

            e.EndTurn();
        }

        Assert.True(e.IsOver(), "the fight should reach a conclusion");
        Assert.True(turns < 500, "and should not need 500 turns to do it");
        Assert.True(hero.IsDead || beast.IsDead);
    }

    [Fact]
    public void ADeadCombatantStopsBeingPickedSoTheLoopCannotSpin() {
        Combatant hero = Member(6);
        Combatant beast = Enemy(9);
        CombatEncounter e = Encounter(new[] { hero }, new[] { beast });

        beast.Flags |= CombatantFlags.Dead;
        e.HasObjective = true;   // keep the encounter alive so PickNext still runs

        Assert.Same(hero, e.PickNext());
    }

    // ---- dying -----------------------------------------------------------------------

    [Fact]
    public void DyingZeroesTheStatsAndSetsTheFlag() {
        Combatant beast = Enemy(5, health: 30);
        beast.Stamina = 12;
        var e = new CombatEncounter();

        e.Kill(beast);

        Assert.Equal(0, beast.Health);
        Assert.Equal(0, beast.Stamina);
        Assert.True(beast.IsDead);
    }

    [Fact]
    public void MostCreaturesLeaveABodyForTheLootScreen() {
        Combatant beast = Enemy(5, classId: 1);

        Assert.Equal(DeathOutcome.LeavesCorpse, new CombatEncounter().Kill(beast));
    }

    [Theory]
    [InlineData(49)]
    [InlineData(56)]
    [InlineData(57)]
    public void SomeCreaturesVanishInsteadOfLeavingOne(int classId) {
        Combatant odd = Enemy(5, classId: classId);

        Assert.Equal(DeathOutcome.RemovedFromField, new CombatEncounter().Kill(odd));
    }

    [Fact]
    public void AFleeingActorLeavingTheFieldIsRemovedNotKilledIntoACorpse() {
        // The quiet path: play_anim = 0. It always persists as gone and never leaves a body.
        Combatant runner = Enemy(5, classId: 1);

        Assert.Equal(DeathOutcome.RemovedFromField, new CombatEncounter().Kill(runner, playAnimation: false));
    }

    [Fact]
    public void TheGroundUnderTheBodyIsUnchanged() {
        // The original saves the tile kind and timer, clears the tile for the death, then writes them
        // back. Dying on crystal ground must not scrub the crystal ground.
        var grid = new CombatGrid();
        grid.SetTerrain(3, 4, CombatTerrain.Crystal);
        grid.SetOccupied(3, 4, true);
        Combatant beast = Enemy(5);
        beast.X = 3;
        beast.Y = 4;

        new CombatEncounter().Kill(beast, playAnimation: true, grid: grid);

        Assert.Equal(CombatTerrain.Crystal, grid.TerrainAt(3, 4));
        Assert.False(grid.IsOccupied(3, 4));
    }

    [Fact]
    public void ADeadCombatantStopsPointingAtAnyone() {
        Combatant hero = Member(5);
        Combatant beast = Enemy(5);
        beast.Target = hero;

        new CombatEncounter().Kill(beast);

        Assert.Null(beast.Target);
    }

    [Fact]
    public void KillingTheLastEnemyEndsTheEncounter() {
        Combatant beast = Enemy(5);
        CombatEncounter e = Encounter(new[] { Member(5) }, new[] { beast });

        Assert.False(e.IsOver());
        e.Kill(beast);

        Assert.True(e.IsOver());
    }
}
