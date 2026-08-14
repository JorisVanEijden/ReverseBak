namespace GameData.Resources.Spells;

using GameData;
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
    /// </remarks>
    /// <remarks>
    /// 54 — read from the compare's own bytes (<c>83 7f 02 36</c>) rather than from the symbol, and
    /// it lands inside the band of creature types Grief of 1000 Nights also exempts, which is a
    /// useful corroboration that this is the elemental/mindless range.
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
}
