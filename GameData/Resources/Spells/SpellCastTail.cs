namespace GameData.Resources.Spells;

/// <summary>
/// Everything a cast does <b>after</b> its magnitude has been computed and its per-spell handler has
/// run — the shared tail of IDA <c>Cast_Spell</c> (ovr173, 0x68774 to 0x68b6b).
///
/// <para>The tail is where a spell finally becomes damage, healing or nothing at all, and it is not
/// a formality: two spells discard the number the calculation just produced, one substitutes a
/// global for it, one hands the whole cast to a different routine, one kills the target instead of
/// damaging it, and three targeting types never deliver anything but a bill. Porting the arithmetic
/// without the tail gives several spells effects the original never gives them.</para>
/// </summary>
public static class SpellCastTail {
    /// <summary>
    /// <b>The spell-record pointer doubles as a "keep going" flag.</b>
    /// </summary>
    /// <remarks>
    /// Four places in the cast path clear the register that holds the pointer to the 22-byte
    /// <c>SPELLS.DAT</c> record, and the tail tests it twice — once before the animation
    /// (<c>or si, si / jnz</c> @0x68962) and once before the delivery switch (@0x68aa7) — returning
    /// immediately when it is zero. Nothing dereferences it in between, so this is not a dangling
    /// pointer: it is a boolean stored in a pointer register.
    ///
    /// <para>This is the answer to the <c>xor si, si</c> that
    /// <see cref="SpellEffectApplication"/> recorded as an open question. It is not a redundant
    /// re-check and not a bug — it is how a spell says <i>"I am finished, run nothing else"</i>.</para>
    /// </remarks>
    public static bool RecordPointerDoublesAsContinueFlag => true;

    /// <summary>
    /// What ending the cast early actually skips.
    /// </summary>
    /// <remarks>
    /// The animation, the post-animation hooks, the resisted-cast sound, and the entire
    /// targeting-type delivery switch — which is where damage is dealt <i>and where the caster is
    /// billed</i>. The one thing that still happens is the surcharge flag being cleared, because
    /// that is done at the shared return.
    /// </remarks>
    public static bool EndingEarlySkipsTheDeliverySwitch => true;

    /// <summary>
    /// Whether a cast that ends early costs the caster anything.
    /// </summary>
    /// <remarks>
    /// <b>It depends on why it ended, and the two reasons are opposites.</b> A <i>suppressed</i>
    /// cast — Skyfire against an unarmoured target, Touch of Lims-Kragma at point blank — never
    /// reaches the payment call and is genuinely free. A cast ended by its own handler is not: Winds
    /// of Eortis and Mad God's Rage are the only two spells that call the payment routine
    /// themselves, and clearing the continue flag is precisely how they stop the delivery switch
    /// billing a <i>second</i> time.
    ///
    /// <para>So the flag does not mean "this cast was cancelled". It means "the caster has already
    /// been dealt with". Reading it as the former makes two spells free and the wrong two.</para>
    /// </remarks>
    public static bool EndingEarlyIsFree(int spellId) => !HandlerEndsTheCast(spellId);

    /// <summary>
    /// Skyfire against a target carrying no metal <b>ends the cast on the spot</b>.
    /// </summary>
    /// <remarks>
    /// The FixedAmount arm of the calculation switch exists only for this test. Combined with
    /// <see cref="SpellEffectMagnitude"/> already yielding zero in the same case, the effect is that
    /// Skyfire on an unarmoured target is not a weak cast — it is not a cast at all: no animation,
    /// no damage, no cost.
    /// </remarks>
    public static bool SkyfireEndsTheCast(int spellId, bool targetUsesMetal) =>
        spellId == SpellIds.Skyfire && !targetUsesMetal;

    /// <summary>
    /// The grid distance at or above which Touch of Lims-Kragma walks the caster to the target.
    /// </summary>
    public const int LimsKragmaApproachRange = 2;

    /// <summary>
    /// Touch of Lims-Kragma <b>ends its own cast when the caster is already adjacent</b>.
    /// </summary>
    /// <remarks>
    /// The handler measures the Chebyshev distance between the two grid cells and branches
    /// <c>cmp ax, 2 / jge</c>: at 2 or more it calls the melee-approach routine and lets the cast
    /// continue, and below 2 it clears the continue flag. So the spell does nothing to a target the
    /// caster is standing next to.
    ///
    /// <para>That reads backwards for a spell called <i>Touch</i>, and it is recorded rather than
    /// "corrected" — the branch is unambiguous, and inverting it to match the name would be
    /// inventing behaviour. Worth confirming against the running game before anything depends on
    /// it.</para>
    /// </remarks>
    public static bool LimsKragmaEndsTheCast(int chebyshevDistanceToTarget) =>
        chebyshevDistanceToTarget < LimsKragmaApproachRange;

    /// <summary>
    /// Spells whose per-spell handler is the whole spell, and which end the cast when it returns.
    /// </summary>
    /// <remarks>
    /// Their dedicated <c>Cast_*</c> routine has already done everything, so letting the tail run
    /// would animate and bill a second time.
    /// </remarks>
    public static bool HandlerEndsTheCast(int spellId) =>
        spellId == SpellIds.WindsOfEortis || spellId == SpellIds.MadGodsRage;

    /// <summary>
    /// <b>Resistance is checked four times in one cast, and means something different each time.</b>
    /// </summary>
    /// <remarks>
    /// The same <c>check_spell_resistance</c> call, on the same target and spell, gates four
    /// unrelated things:
    /// <list type="number">
    /// <item>in the CostTimesDuration arm, it skips registering the lingering effect;</item>
    /// <item>before the per-spell switch, it skips only the <i>per-spell handler</i> — the generic
    /// effect has already been applied by then, so this one does not cancel the spell;</item>
    /// <item>after the animation, it plays a sound and nothing else — the audible "it resisted";</item>
    /// <item>on the damage path, it suppresses the damage.</item>
    /// </list>
    /// A port that models resistance as one boolean applied once will get at least two of these
    /// wrong, because a resisted cast still animates, still bills the caster, and — for a duration
    /// spell that also deals damage — is stopped at two separate points for two separate reasons.
    /// </remarks>
    public const int ResistanceCheckSites = 4;

    /// <summary>
    /// <b>The weakness doubling is undone before delivery.</b>
    /// </summary>
    /// <remarks>
    /// The prologue doubles the cost against a weak target (<c>shl ax, 1</c>); the tail halves it
    /// back (<c>sar ax, 1</c>) immediately before the delivery switch, and since doubling always
    /// leaves an even number the reversal is exact. So the doubling scales the <i>magnitude</i> only
    /// — by the time the cast pays, heals or damages, the extra is gone.
    ///
    /// <para>Which means "weak to this spell" is not a discount or a penalty on the cast: it is
    /// purely an effect multiplier, applied and then withdrawn inside one function.</para>
    /// </remarks>
    public static int UndoWeakness(int doubledCost) => doubledCost >> 1;

    /// <summary>What a spell does to itself once its animation has finished.</summary>
    public enum PostAnimationHook {
        /// <summary>Nothing — the overwhelming majority.</summary>
        None,

        /// <summary>Discard the computed magnitude.</summary>
        ZeroTheMagnitude,

        /// <summary>Replace the magnitude with a global the cast itself never sets.</summary>
        MagnitudeFromGlobal,

        /// <summary>Hand the effect to <c>Cast_Flamecast</c>.</summary>
        DelegateToFlamecast,

        /// <summary>Remove the target from the grid and kill it.</summary>
        KillOutright,

        /// <summary>Register Grief of 1000 Nights on the target.</summary>
        RegisterGriefOfAThousandNights,
    }

    /// <summary>
    /// The per-spell hook that runs after the animation, if any.
    /// </summary>
    /// <remarks>
    /// Six spells out of forty-five, found by a <b>linear scan of a six-entry table</b> rather than a
    /// jump table — the original walks the list comparing spell numbers. That the list is short and
    /// hand-written is the point: these are exceptions bolted on after the fact, and none of them is
    /// derivable from anything in <c>SPELLS.DAT</c>.
    /// </remarks>
    public static PostAnimationHook HookFor(int spellId) {
        switch (spellId) {
            case SpellIds.DannonsDelusions:
            case SpellIds.Firestorm:
                return PostAnimationHook.ZeroTheMagnitude;
            case SpellIds.UnfortunateFlux:
                return PostAnimationHook.MagnitudeFromGlobal;
            case SpellIds.Flamecast:
                return PostAnimationHook.DelegateToFlamecast;
            case SpellIds.FinalRest:
                return PostAnimationHook.KillOutright;
            case SpellIds.FettersOfRime:
                return PostAnimationHook.RegisterGriefOfAThousandNights;
            default:
                return PostAnimationHook.None;
        }
    }

    /// <summary>
    /// <b>Two spells compute a cost-scaled magnitude and then throw it away.</b>
    /// </summary>
    /// <remarks>
    /// Dannon's Delusions and Firestorm both carry a CostTimesDamage calculation, so the arithmetic
    /// runs and produces a real number — which the hook zeroes before the delivery switch can turn it
    /// into damage. Their effect is entirely in the animation path. A port that stops at the
    /// calculation gives both of them damage the original never deals.
    /// </remarks>
    public static bool ZeroesItsOwnMagnitude(int spellId) =>
        HookFor(spellId) == PostAnimationHook.ZeroTheMagnitude;

    /// <summary>
    /// The hooks only run <b>if the animation reports back</b>.
    /// </summary>
    /// <remarks>
    /// The animation routine takes an out-parameter, and the whole six-spell lookup is skipped when
    /// it comes back zero. So Final Rest does not kill and Flamecast does not fire unless the
    /// animation says so — the visual is a gate on the mechanic, not a decoration over it.
    /// </remarks>
    public static bool HooksRequireAnimationResult => true;

    /// <summary>How a targeting type finishes a cast.</summary>
    public enum Delivery {
        /// <summary>Charge the caster, then damage the target.</summary>
        DamageTarget,

        /// <summary>Charge the caster and stop — no damage of any kind.</summary>
        ChargeOnly,

        /// <summary>The type-2 routine, which bills differently and can be blocked outright.</summary>
        Type2Routine,
    }

    /// <summary>
    /// Which of the three deliveries this spell's targeting type takes.
    /// </summary>
    /// <remarks>
    /// <b>The targeting type decides whether a spell can deal damage at all</b> — a field that reads
    /// as "who may I aim at" is really the last branch in the cast. Types 5, 6 and 8 reach the end of
    /// the function having paid for the cast and dealt nothing; type 2 goes somewhere else entirely.
    /// </remarks>
    public static Delivery DeliveryFor(int targetingType) {
        switch (targetingType) {
            case 2:
                return Delivery.Type2Routine;
            case 5:
            case 6:
            case 8:
                return Delivery.ChargeOnly;
            default:
                return Delivery.DamageTarget;
        }
    }

    /// <summary>
    /// The gauntlet the damage path runs before anything is applied.
    /// </summary>
    /// <param name="animationReported">The animation routine's out-parameter came back non-zero.</param>
    /// <param name="magnitude">The magnitude as it stands after the post-animation hook.</param>
    /// <param name="targetResists">The fourth resistance check.</param>
    /// <remarks>
    /// Three independent conditions, each an early return. The magnitude test is why the two spells
    /// that zero themselves deal nothing, and the animation test is why a cast whose visual is
    /// suppressed deals nothing either.
    /// </remarks>
    public static bool DealsDamage(bool animationReported, int magnitude, bool targetResists) =>
        animationReported && magnitude != 0 && !targetResists;

    /// <summary>
    /// <b>The caster is billed the cost they chose, not the cost the spell used.</b>
    /// </summary>
    /// <remarks>
    /// The dispatcher saves the incoming cost before the surcharge is added and before the sign is
    /// stripped, and it is that saved value the payment routine receives. The surcharge and the
    /// weakness doubling therefore change what the spell <i>does</i> and never what it <i>costs</i>
    /// — the slider the player moved is the bill.
    ///
    /// <para>The single exception is the type-2 delivery, which is handed the running cost instead,
    /// so it alone bills the surcharge.</para>
    /// </remarks>
    public static int AmountBilled(int originalCost, int runningCost, int targetingType) =>
        DeliveryFor(targetingType) == Delivery.Type2Routine ? runningCost : originalCost;

    /// <summary>
    /// <b>A negative cost is cast for free.</b>
    /// </summary>
    /// <remarks>
    /// The payment call sits behind a test of the same flag <see cref="SpellCostModifiers.IsNegated"/>
    /// records, so a cast that arrived with a negative cost skips the charge entirely — on top of
    /// already being exempt from the to-hit roll (see <see cref="SpellHitResolution.CanMiss"/>). The
    /// type-2 delivery does not consult the flag, so it charges the stripped magnitude regardless.
    /// </remarks>
    public static bool CasterPays(bool costWasNegated, int targetingType) =>
        DeliveryFor(targetingType) == Delivery.Type2Routine || !costWasNegated;

    /// <summary>
    /// <b>Casting a spell costs the caster health.</b>
    /// </summary>
    /// <remarks>
    /// The payment routine (<c>sub_ovr173_41C</c> @0x66b1c) charges by calling the same
    /// apply-damage entry point a weapon would — the caster takes the cost as damage to themselves.
    /// It is not a separate "mana" pool with its own rules; the spell price and a sword blow arrive
    /// through the same door, differing only in the flags word (0 for the caster's own cost, 0x200
    /// for spell damage dealt to a target).
    /// </remarks>
    public static bool CostIsPaidInHealth => true;

    /// <summary>
    /// The chapter in which an equipped Crystal Staff is drained alongside the caster.
    /// </summary>
    public const int CrystalStaffDrainChapter = 8;

    /// <summary>
    /// Whether this cast also drains the caster's Crystal Staff.
    /// </summary>
    /// <remarks>
    /// <b>In chapter 8 only</b>, the payment routine additionally looks for an equipped Crystal Staff
    /// (object id 1) in the caster's own inventory and subtracts the same cost from its variable
    /// byte, flooring at zero. The staff is drained <i>as well as</i> the caster, not instead of
    /// them — it is a second charge on the same cast, and it exists for exactly one chapter.
    /// </remarks>
    public static bool DrainsCrystalStaff(int chapter, bool casterHasEquippedCrystalStaff) =>
        chapter == CrystalStaffDrainChapter && casterHasEquippedCrystalStaff;

    /// <summary>
    /// Draining the staff, flooring rather than wrapping.
    /// </summary>
    public static int DrainStaff(int charge, int cost) => charge < cost ? 0 : charge - cost;

    /// <summary>
    /// <b>The type-2 delivery is blocked outright by Thoughts Like Clouds on the caster.</b>
    /// </summary>
    /// <remarks>
    /// Its routine's first act is to look for that effect in the caster's own active-effect slots and
    /// return if it finds one — before the sound, before the charge. So a caster under Thoughts Like
    /// Clouds casts type-2 spells for free and to no effect, which nothing in the spell data hints
    /// at.
    /// </remarks>
    public static bool Type2IsBlocked(bool casterHasThoughtsLikeClouds) => casterHasThoughtsLikeClouds;

    /// <summary>
    /// The surcharge flag is <b>one-shot</b>.
    /// </summary>
    /// <remarks>
    /// The global that adds half the cost is cleared at the dispatcher's shared return, on every path
    /// including the early ones. So whatever sets it buys exactly one boosted cast, and a port that
    /// treats it as a persistent state flag will boost every spell after the first.
    /// </remarks>
    public static bool SurchargeIsConsumedByOneCast => true;
}
