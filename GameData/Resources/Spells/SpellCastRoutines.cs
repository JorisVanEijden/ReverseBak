namespace GameData.Resources.Spells;

using GameData;
using GameData.Resources.Character;
using GameData.Resources.Inventory;
using GameData.Resources.Object;

/// <summary>
/// The dedicated <c>Cast_*</c> routines that <c>Cast_Spell</c>'s per-spell switch delegates to —
/// where a handful of spells keep the behaviour that makes them worth casting.
///
/// <para>Ported one routine at a time as each is read end to end; see
/// <see cref="SpellPerSpellHandlers"/> for the dispatch layer that reaches them.</para>
/// </summary>
public static class SpellCastRoutines {
    // ---------------------------------------------------------------- Strength Drain
    // Cast_Drain_Strength @0x6841d.

    /// <summary>
    /// <b>Strength Drain is a transfer, not a debuff.</b>
    /// </summary>
    /// <remarks>
    /// The routine takes Strength from the target and then gives Strength to the <i>caster</i>. That
    /// second half is invisible from the dispatcher, from the spell record, and from the spell's
    /// description, and it is the reason the spell is worth its 10-20 cost against a strong enemy
    /// rather than merely being a weakening effect.
    /// </remarks>
    public static bool DrainTransfersToCaster => true;

    /// <summary>
    /// What the caster gains: <b>half of what the target actually lost</b>.
    /// </summary>
    /// <remarks>
    /// Half of the <i>clamped</i> drain, so draining a nearly-spent target gives the caster nearly
    /// nothing. Taking half the requested amount instead would over-reward a cast aimed at a weak
    /// enemy.
    /// </remarks>
    public static int CasterGain(int actualDrain) => actualDrain / 2;

    /// <summary>
    /// <b>The drain is clamped to the Strength the target still has.</b>
    /// </summary>
    /// <remarks>
    /// Read back through <c>GetAttributeFromActor</c> before anything is applied, so the attribute
    /// never goes negative and the caster's share is computed from the real figure rather than the
    /// requested one.
    /// </remarks>
    public static int ActualDrain(int requested, int targetCurrentStrength) =>
        requested < targetCurrentStrength ? requested : targetCurrentStrength;

    /// <summary>
    /// The creature type Strength Drain <b>kills outright</b>.
    /// </summary>
    /// <remarks>
    /// A Wind Elemental whose current Strength is at or below the drain is handed straight to
    /// <c>handleActorDeath</c> — and the routine returns there, so the caster gains nothing from the
    /// kill. Nothing in <c>SPELLS.DAT</c> or the creature data says a wind elemental is made of the
    /// attribute this spell steals; it is a hard-coded creature check inside the routine.
    ///
    /// <para>54 — read from the compare's own bytes (<c>83 7f 02 36</c>) rather than from the
    /// symbol, and it lands inside the band of creature types Grief of 1000 Nights also exempts,
    /// which corroborates it as the elemental/mindless range.</para>
    /// </remarks>
    public const int WindElementalCreatureType = 54;

    /// <summary>Whether this cast kills the target instead of draining it.</summary>
    public static bool DrainKillsOutright(int creatureType, int targetCurrentStrength, int drain) =>
        creatureType == WindElementalCreatureType && targetCurrentStrength <= drain;

    /// <summary>
    /// <b>Resistance stops Strength Drain before anything at all happens.</b>
    /// </summary>
    /// <remarks>
    /// A fifth <c>check_spell_resistance</c> site, on top of the four in the dispatcher — and the
    /// strictest of them: it precedes even the sound, so a resisted drain is silent. See
    /// <see cref="SpellCastTail.ResistanceCheckSites"/>.
    /// </remarks>
    public static bool DrainIsResisted(bool targetResists) => targetResists;

    /// <summary>
    /// <b>Strength Drain announces itself with the healing sound.</b>
    /// </summary>
    /// <remarks>
    /// Recorded because it is the kind of detail a port silently "corrects". The routine plays the
    /// same cue a heal does — appropriate once you know the caster is being topped up, and
    /// misleading if you assume the sound describes what happens to the target.
    /// </remarks>
    public static bool DrainUsesTheHealSound => true;

    /// <summary>
    /// <b>The caster's gain is applied at half scale on the permanent path.</b>
    /// </summary>
    /// <remarks>
    /// Both halves of the transfer choose between a timed modifier (a party member) and a permanent
    /// attribute change (a monster), and the <i>loss</i> paths agree: <c>-drain</c> plain versus
    /// <c>drain × -256</c> into an 8.8 field, which are the same number of points. The <i>gain</i>
    /// paths do not: the timed path passes <c>drain/2</c> plain while the permanent path passes
    /// <c>(drain/2) × 128</c>, and 128 is half the fixed-point scale — so a monster caster banks half
    /// the points a party caster does for an identical drain.
    ///
    /// <para>Recorded as arithmetic rather than judged. <see cref="CasterGain"/> models the party
    /// figure, which is the one a player ever sees.</para>
    /// </remarks>
    public static int PermanentCasterGainPoints(int actualDrain) => actualDrain / 2 / 2;

    // ---------------------------------------------------------------- Steelfire
    // Cast_Steelfire @0x68166.

    /// <summary>
    /// <b>Steelfire enchants the target's sword, not the caster's.</b>
    /// </summary>
    /// <remarks>
    /// The dispatcher hands the routine the target actor, so this is a spell you cast <i>on</i> a
    /// party member. Assuming the caster buffs their own weapon puts the enchantment on the wrong
    /// character every time.
    /// </remarks>
    public static bool SteelfireTargetsTheTargetsInventory => true;

    /// <summary>
    /// The index of the item Steelfire enchants: <b>the first equipped sword, and only that one</b>.
    /// </summary>
    /// <returns>The slot index, or -1 when the target carries no equipped sword.</returns>
    /// <remarks>
    /// The routine's own scan is <c>findEquippedItemOfCategory</c> inlined — same walk, same
    /// first-match, same "equipped and of this category" test — so this delegates to
    /// <see cref="InventoryEquip.FindEquippedIndex"/> rather than repeating it. Stopping at the first
    /// match means a character somehow wearing two swords gets the earlier slot enchanted, and a
    /// target with no equipped sword receives nothing at all.
    /// </remarks>
    public static int SteelfireTarget(RuntimeContainer target, ObjectInfoSet objects) =>
        target == null ? -1 : InventoryEquip.FindEquippedIndex(target, ObjectType.Sword, objects);

    /// <summary>
    /// Applying the enchantment: <b>a flag on the item, set and never cleared here</b>.
    /// </summary>
    /// <remarks>
    /// The routine ORs <see cref="ItemFlags.SteelFired"/> into the item and returns. There is no
    /// duration, no charge count and no removal in the cast path — whatever takes it off again lives
    /// elsewhere, so a port that expects the spell to manage its own lifetime will look for code
    /// that does not exist.
    /// </remarks>
    public static ushort ApplySteelfire(ushort itemFlags) =>
        (ushort)(itemFlags | (ushort)ItemFlags.SteelFired);

    /// <summary>
    /// Casting Steelfire on a target with no equipped sword <b>still costs the caster</b>.
    /// </summary>
    /// <remarks>
    /// The routine's failure is silent and the dispatcher never learns about it, so the delivery
    /// switch bills as usual. There is no refund and no message.
    /// </remarks>
    public static bool SteelfireChargesEvenWhenItFindsNothing => true;

    // ---------------------------------------------------------------- Nightfingers
    // Cast_Nightfingers @0x680ac.

    /// <summary>
    /// <b>Nightfingers burns a Glory Hand and opens the target's pack.</b>
    /// </summary>
    /// <remarks>
    /// The routine finds the first Glory Hand in the <i>caster's</i> inventory, destroys it, then
    /// puts the target's inventory on screen for the player to take something out of. So the spell
    /// does not choose what it steals — the player does, and the theft is a UI interaction rather
    /// than a rule.
    ///
    /// <para>This is what the spell record's otherwise-idle <c>ObjectId</c> field is for: it names
    /// a consumable the cast requires. <c>SpellCasting</c> already refuses a cast whose
    /// <c>ObjectId</c> the caster is not carrying, which is why the routine can destroy the item
    /// without checking it found one. Only two spells use the field.</para>
    /// </remarks>
    public const int GloryHandObjectId = 10;

    /// <summary>
    /// <b>Nothing happens unless the player actually takes something.</b>
    /// </summary>
    /// <param name="itemsBefore">The target's item count before the screen opened.</param>
    /// <param name="itemsAfter">Its count after.</param>
    /// <remarks>
    /// The routine compares the counts and returns without animating if they match. The Glory Hand
    /// is destroyed either way, so backing out of the screen costs the caster the item and the cast
    /// for nothing.
    /// </remarks>
    public static bool NightfingersStoleSomething(int itemsBefore, int itemsAfter) =>
        itemsAfter != itemsBefore;

    /// <summary>
    /// <b>Nightfingers' projectile flies the wrong way on purpose.</b>
    /// </summary>
    /// <remarks>
    /// Every other cast animates from the caster to the target; this one passes the target as the
    /// origin and the caster as the destination, because what is travelling is the stolen item.
    /// </remarks>
    public static bool NightfingersProjectileTravelsToTheCaster => true;

    // ---------------------------------------------------------------- Invitation
    // Cast_Invitiation @0x674af.

    /// <summary>
    /// <b>Invitation drags the target toward the caster.</b>
    /// </summary>
    /// <remarks>
    /// It writes the caster's grid cell into the target's movement destination and hands it to the
    /// mover. The spell's name is the whole mechanic: the target is invited over, whether or not it
    /// wants to come.
    /// </remarks>
    public static bool InvitationSetsTheTargetsDestination => true;

    /// <summary>
    /// How far the target is actually moved: <b>the lesser of the real distance and the power</b>.
    /// </summary>
    /// <remarks>
    /// So a weak Invitation pulls a distant target only part of the way, and a strong one cannot
    /// overshoot — the cap is the distance itself. Modelling it as "teleport to the caster" makes
    /// every cast maximal.
    /// </remarks>
    public static int InvitationPull(int chebyshevDistance, int power) =>
        chebyshevDistance < power ? chebyshevDistance : power;

    /// <summary>
    /// A fleeing target <b>re-picks where it is running to</b> after being invited.
    /// </summary>
    /// <remarks>
    /// Recorded as observed: the routine tests a combat-status bit and, if set, calls the
    /// flee-destination chooser, which will overwrite the destination the spell just set. Whether
    /// that defeats the pull or merely redirects the retreat has not been established from the
    /// chooser itself.
    /// </remarks>
    public static bool InvitationRerollsAFleeingTargetsDestination => true;

    // ---------------------------------------------------------------- Evil Seek
    // Cast_Evil_Seek @0x6734d.

    /// <summary>
    /// <b>Evil Seek is chain lightning.</b>
    /// </summary>
    /// <remarks>
    /// It hops from victim to victim, arcing from each one to the next and damaging as it goes,
    /// until it runs out of targets, runs out of power, or has taken as many hops as there are
    /// combat actors. The dispatcher zeroes its magnitude afterwards precisely because the routine
    /// has already dealt all the damage itself.
    /// </remarks>
    public static bool EvilSeekChains => true;

    /// <summary>The power the chain starts with: <b>twice the cost</b>.</summary>
    public static int EvilSeekInitialPower(int spellCost) => spellCost * 2;

    /// <summary>What each hop after the first retains, as a percentage.</summary>
    public const int EvilSeekFalloffPercent = 80;

    /// <summary>
    /// The damage the given hop deals, hop 0 being the original target.
    /// </summary>
    /// <remarks>
    /// <b>The first hop is at full power.</b> The multiplier starts at 100 and only drops to 80
    /// after it has been applied once, so the original target takes <c>cost × 2</c> and each
    /// subsequent victim takes 80% of the one before — integer-truncated, which is what eventually
    /// ends the chain.
    /// </remarks>
    public static int EvilSeekPowerAtHop(int spellCost, int hop) {
        int power = EvilSeekInitialPower(spellCost);
        for (int i = 1; i <= hop; i++) {
            power = power * EvilSeekFalloffPercent / 100;
        }

        return power;
    }

    /// <summary>
    /// <b>Resistance breaks a link's damage but not the chain.</b>
    /// </summary>
    /// <remarks>
    /// The per-hop resistance check skips only that victim's damage; the arc still happens, the
    /// victim is still recorded as visited, and the chain still passes through them to the next
    /// target at the reduced power. A resistant creature standing in the middle shields nobody.
    /// </remarks>
    public static bool EvilSeekResistanceStopsOnlyThatHop => true;

    /// <summary>
    /// The chain also ends when the power <b>truncates to zero</b>.
    /// </summary>
    public static bool EvilSeekEndsAtZeroPower(int power) => power == 0;

    /// <summary>
    /// The routine's visited list holds <b>seven</b> entries.
    /// </summary>
    /// <remarks>
    /// Fourteen bytes of stack, written at <c>visited[hop]</c> while the loop is bounded by the
    /// combat actor count rather than by seven. An eighth hop would write over the variable holding
    /// the chain's remaining power, which sits immediately after the buffer.
    ///
    /// <para>Recorded rather than asserted: whether an eighth hop is reachable depends on the
    /// target picker, which has not been read. Our port bounds the chain at seven, which matches
    /// the original for every case where the original is well-defined.</para>
    /// </remarks>
    public const int EvilSeekVisitedCapacity = 7;

    // ---------------------------------------------------------------- Winds of Eortis
    // Cast_Winds_of_Eortis @0x67526, Spell_KnockbackAlongDirection @0x66dd1.

    /// <summary>
    /// <b>Winds of Eortis bills the caster itself — and that is why it ends the cast.</b>
    /// </summary>
    /// <remarks>
    /// It is one of only two spells that call the payment routine directly (Mad God's Rage is the
    /// other), and they are exactly the two whose handler clears the continue flag. The clearing is
    /// not "this cast was cancelled"; it is "do not charge again". See
    /// <see cref="SpellCastTail.EndingEarlyIsFree"/>.
    /// </remarks>
    public static bool WindsOfEortisBillsItself => true;

    /// <summary>
    /// <b>The knockback lands on whoever the projectile actually hit.</b>
    /// </summary>
    /// <remarks>
    /// The routine takes the sweep's return value — the actor it struck — and runs the resistance
    /// check and the knockback on that, not on the actor the player aimed at. Same shape as Strength
    /// Drain's return leg: the sweep, not the targeting, decides who is affected.
    /// </remarks>
    public static bool WindsOfEortisAffectsTheActorStruck => true;

    /// <summary>How many cells the victim is pushed: <b>one per point of cost</b>.</summary>
    public static int KnockbackCells(int spellCost) => spellCost;

    /// <summary>
    /// The horizontal step for a caster-to-victim direction, 0-7.
    /// </summary>
    /// <remarks>
    /// The compass runs 0 = away along -Y, 2 = +X, 4 = +Y, 6 = -X. This axis follows the textbook
    /// pattern exactly.
    /// </remarks>
    public static int KnockbackDx(int direction) {
        if (direction >= 1 && direction <= 3) {
            return 1;
        }

        return direction >= 5 && direction <= 7 ? -1 : 0;
    }

    /// <summary>
    /// The vertical step for a caster-to-victim direction, 0-7.
    /// </summary>
    /// <remarks>
    /// <b>Direction 0 answers 0 where the symmetric pattern wants -1.</b> The original's branches
    /// give -1 for directions 1 and 7 but let 0 fall through to the "no movement" arm, while the
    /// horizontal axis handles its own equivalent cases correctly. The consequence is concrete: a
    /// victim standing directly along direction 0 from the caster gets a step of (0, 0) and is not
    /// pushed at all — the loop still runs, decrementing its allowance and moving nobody.
    ///
    /// <para>Reproduced rather than corrected, because it is decidable from the branches and changes
    /// what the player sees. Flagged for confirmation against the running game.</para>
    /// </remarks>
    public static int KnockbackDy(int direction) {
        if (direction >= 3 && direction <= 5) {
            return 1;
        }

        return direction == 1 || direction == 7 ? -1 : 0;
    }

    /// <summary>Whether this direction produces no movement at all.</summary>
    public static bool KnockbackIsInert(int direction) =>
        KnockbackDx(direction) == 0 && KnockbackDy(direction) == 0;

    /// <summary>
    /// <b>The push stops at the first cell the victim cannot enter.</b>
    /// </summary>
    /// <remarks>
    /// Each step sets a destination one cell along and hands the victim to the mover with an
    /// allowance of exactly one cell — the same global Invitation writes. Afterwards the routine
    /// compares the destination against the position; if they still differ the victim did not move,
    /// and the allowance is zeroed so the loop ends. So a wall two cells away caps a ten-point cast
    /// at one cell rather than shoving the victim into it.
    /// </remarks>
    public static bool KnockbackStopsWhenBlocked => true;

    /// <summary>
    /// <b>Winds of Eortis registers River Song on the victim and then takes it away again.</b>
    /// </summary>
    /// <remarks>
    /// A different spell's effect, applied with zero cost and zero duration before the push starts
    /// and removed by slot once it finishes — so it is a transient marker for "currently being blown
    /// along" rather than an effect the victim keeps. The second spell in the catalogue found
    /// wearing another's identity, after The Fetters of Rime.
    /// </remarks>
    public static bool KnockbackWearsRiverSong => true;

    // ---------------------------------------------------------------- The heal (targeting type 2)
    // Spell_HealTarget @0x682f3.

    /// <summary>
    /// The heal's ceiling: <b>80% of the target's combined health and stamina maximum</b>.
    /// </summary>
    /// <remarks>
    /// Passed straight through as <c>StatEngine.ModifyHealthPool</c>'s <c>healTargetPercent</c>, which
    /// already models the consequence: a positive delta only applies while the pool is <i>below</i>
    /// the target, and is clamped to it. So <b>a spell heal cannot take anyone past four fifths of
    /// full</b>, and casting on someone already there does nothing at all — while still costing.
    ///
    /// <para>The same four fifths that <see cref="CharacterHeal.PartialHealPercent"/> lands on,
    /// reached by a different route: the rest-and-dialog heal fills the pool outright and then gives
    /// a fifth back, while the spell heal simply caps. Two arithmetics, one ceiling — so 80% of
    /// maximum is the engine's idea of "as good as magic gets", and only an exact-100 heal goes
    /// past it.</para>
    /// </remarks>
    public const int HealTargetPercent = 80;

    /// <summary>
    /// The afflictions that <b>block a heal outright</b>.
    /// </summary>
    /// <remarks>
    /// Six of the seven, tested one after another; any non-zero rank skips the heal entirely — not
    /// reduces it. <see cref="ActorCondition.Healing"/> is the one deliberately left out, which is
    /// the only sensible exclusion: being under a healing effect should not stop another.
    ///
    /// <para>So a poisoned or starving character cannot be healed by magic at all, which is a
    /// substantial tactical rule that no part of the spell data expresses.</para>
    /// </remarks>
    public static readonly ActorCondition[] AfflictionsThatBlockHealing = {
        ActorCondition.Sick,
        ActorCondition.Plagued,
        ActorCondition.Poisoned,
        ActorCondition.Drunk,
        ActorCondition.Starving,
        ActorCondition.NearDeath,
    };

    /// <summary>
    /// Whether the heal lands, given the target's afflictions.
    /// </summary>
    /// <param name="targetActorNumber">0 for a monster; 1-6 for a member of the party.</param>
    /// <param name="conditions">The target's affliction ranks, or null for an actor without a row.</param>
    /// <remarks>
    /// <b>A monster is always healable</b> — the routine tests the actor number first and jumps
    /// straight to the heal when it is zero, because non-party actors have no affliction row at all.
    /// </remarks>
    public static bool HealApplies(int targetActorNumber, ActorConditions conditions) {
        if (targetActorNumber == 0 || conditions == null) {
            return true;
        }

        foreach (ActorCondition condition in AfflictionsThatBlockHealing) {
            if (conditions[condition] != 0) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// <b>Thoughts Like Clouds on the caster blocks the heal before the caster is charged.</b>
    /// </summary>
    /// <remarks>
    /// The test is the routine's first act, ahead of the sound, the charge and everything else — so
    /// this is the one case in which a targeting-type-2 delivery does not bill. Contrast
    /// <see cref="SpellCastTail.CasterPays"/>, which holds for every type-2 cast that gets past this
    /// gate, including a negative-cost one.
    /// </remarks>
    public static bool HealIsBlockedForFree(bool casterHasThoughtsLikeClouds) =>
        casterHasThoughtsLikeClouds;

    /// <summary>
    /// The floating number the heal shows: <b>the total gain, negated</b>.
    /// </summary>
    /// <remarks>
    /// Computed from health and stamina snapshots taken before the change and summed, then negated —
    /// so healing displays as a negative figure and damage as a positive one. Showing the gain as a
    /// positive number puts the sign the wrong way round against every other floating number in
    /// combat.
    ///
    /// <para>It is also computed on the blocked path, where the delta is zero, so a heal that an
    /// affliction refused still flashes a 0 over the target rather than nothing.</para>
    /// </remarks>
    public static int HealFloatingNumber(int healthBefore, int healthAfter,
        int staminaBefore, int staminaAfter) =>
        -((healthAfter - healthBefore) + (staminaAfter - staminaBefore));

    /// <summary>How many frames that number stays on screen.</summary>
    public const int HealFloatingNumberFrames = 8;
}
