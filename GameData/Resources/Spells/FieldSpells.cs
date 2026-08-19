namespace GameData.Resources.Spells;

/// <summary>
/// The spells that can be cast outside a fight — <c>Cast_field_spell</c> (ovr179 @0x6ca30), the
/// overworld counterpart of <c>Cast_Spell</c>.
///
/// <para>Where the combat dispatcher runs a forty-two case switch over every spell, this one
/// recognises <b>nine</b>, by a linear scan of a nine-entry table. Everything else the cast screen
/// might return falls off the end and does nothing at all.</para>
/// </summary>
public static class FieldSpells {
    /// <summary>Dragon's Breath.</summary>
    public const int DragonsBreath = 0;

    /// <summary>Candle Glow.</summary>
    public const int CandleGlow = 2;

    /// <summary>Scent of Sarig.</summary>
    public const int ScentOfSarig = 8;

    /// <summary>Eyes of Ishap.</summary>
    public const int EyesOfIshap = 11;

    /// <summary>The Unseen.</summary>
    public const int TheUnseen = 17;

    /// <summary>Nacre Cicatrix.</summary>
    public const int NacreCicatrix = 18;

    /// <summary>Stardusk.</summary>
    public const int Stardusk = 26;

    /// <summary>Union.</summary>
    public const int Union = 34;

    /// <summary>And the Light Shall Lie.</summary>
    public const int AndTheLightShallLie = 35;

    /// <summary>The nine, in the order the dispatcher's table lists them.</summary>
    public static readonly int[] All = {
        DragonsBreath, CandleGlow, Stardusk, AndTheLightShallLie, Union, ScentOfSarig,
        EyesOfIshap, TheUnseen, NacreCicatrix,
    };

    /// <summary>Whether this spell does anything when cast outside a fight.</summary>
    public static bool IsFieldSpell(int spellId) {
        foreach (int id in All) {
            if (id == spellId) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>Non-martial does not mean field-castable.</b>
    /// </summary>
    /// <remarks>
    /// Fourteen spells in the catalogue are non-martial and only these nine are dispatched here.
    /// Dannon's Delusions and Nightfingers are non-martial <i>combat</i> spells, and Eagle Wing,
    /// Aether Bridge and Dawn of Truth are non-martial and not handled by this dispatcher at all. So
    /// the martial flag says who may cast a spell in a fight
    /// (<c>MonsterSpellcasting.OnlyCastsMartialSpells</c>), not where it works — the field list is
    /// explicit and there is no field in <c>SPELLS.DAT</c> that derives it.
    /// </remarks>
    public static bool NonMartialImpliesFieldCastable => false;

    /// <summary>
    /// The three field spells that are <b>instantaneous</b>.
    /// </summary>
    /// <remarks>
    /// Six handlers are passed the spell record's duration alongside the invested cost; Eyes of
    /// Ishap, The Unseen and Nacre Cicatrix are passed only the cost. So those three take no
    /// duration argument at all — whatever they do, they do once. A port that hands every field
    /// spell a duration invents a lifetime for three of them.
    /// </remarks>
    public static bool IsInstantaneous(int spellId) =>
        spellId == EyesOfIshap || spellId == TheUnseen || spellId == NacreCicatrix;

    /// <summary>Whether the handler receives the record's duration.</summary>
    public static bool TakesDuration(int spellId) =>
        IsFieldSpell(spellId) && !IsInstantaneous(spellId);

    /// <summary>
    /// <b>The spell table is not resident outside the cast screen.</b>
    /// </summary>
    /// <remarks>
    /// The dispatcher calls the loader on entry and the disposer as soon as the screen closes, so
    /// <c>SPELLS.DAT</c> exists only for the duration of the UI. The duration the handlers receive is
    /// read out of the table <i>before</i> that disposal — by the time a handler runs, the record it
    /// came from is gone.
    ///
    /// <para>That ordering is the whole reason the duration is passed as an argument rather than
    /// looked up. A port holding the catalogue in memory permanently will not notice, but it must not
    /// invert the order and let a handler read the table itself.</para>
    /// </remarks>
    public static bool CatalogueIsLoadedOnlyForTheCastScreen => true;

    /// <summary>
    /// <b>The caster the handlers receive is the one the screen chose, not the one it was seeded
    /// with.</b>
    /// </summary>
    /// <remarks>
    /// The dispatcher opens the cast screen on the first active party slot, but the screen writes
    /// the party member the player actually settled on into a global, and it is that global the
    /// handlers are passed. Seeding and result are two different values, and using the seed would
    /// apply every overworld spell to the wrong character whenever the player switched caster.
    /// </remarks>
    public static bool SeedCasterIsNotTheActingCaster => true;

    /// <summary>The active party slot the cast screen is opened on.</summary>
    public const int SeedPartySlot = 0;

    /// <summary>
    /// An unrecognised spell number is <b>silently ignored</b>.
    /// </summary>
    /// <remarks>
    /// The scan runs off the end of its nine entries and falls through to the return with nothing
    /// done — no message, no refund path, and the cost has already been settled by the screen. The
    /// same shape as a cancelled cast, which returns -1 and also matches nothing.
    /// </remarks>
    public static bool UnknownSpellDoesNothing => true;

    // ---------------------------------------------------------------- the handlers
    // All nine read: 0x6cb44, 0x6cbbe, 0x6cc3f, 0x6ccc0, 0x6cd15, 0x6cd6a, 0x6cdbf, 0x6ce0f,
    // 0x6ce5f. They fall into three groups of three.

    /// <summary>Ticks a minute of spell duration is worth.</summary>
    public const int TicksPerDurationUnit = 30;

    /// <summary>
    /// How long a timed field spell lasts.
    /// </summary>
    /// <param name="duration">The record's duration.</param>
    /// <param name="cost">The power invested.</param>
    /// <param name="powerExtendsIt">Whether this spell's lifetime scales with the power.</param>
    /// <remarks>
    /// <b>The power does not always buy time.</b> Dragon's Breath and Candle Glow compute
    /// <c>duration × cost × 30</c>, so pouring power in makes them last longer. Scent of Sarig
    /// computes <c>duration × 30</c> and ignores the cost entirely — the slider changes what it costs
    /// and nothing else. Assuming one formula for all of them makes a maximum-power Scent of Sarig
    /// last twenty times too long.
    /// </remarks>
    public static int DurationTicks(int duration, int cost, bool powerExtendsIt) =>
        powerExtendsIt ? duration * cost * TicksPerDurationUnit : duration * TicksPerDurationUnit;

    /// <summary>Whether the power invested lengthens this spell.</summary>
    /// <remarks>
    /// True for the three that also drive the lighting; false for the other three timed ones.
    /// </remarks>
    public static bool PowerExtendsDuration(int spellId) => DrivesWorldLighting(spellId);

    /// <summary>
    /// The two field spells that also drive the world lighting.
    /// </summary>
    /// <remarks>
    /// They set a <i>second</i> timer against the light system and refresh the light sources
    /// immediately, so the change is visible at once rather than at the next tick. Dragon's Breath
    /// darkens; Candle Glow and Stardusk lighten.
    ///
    /// <para><b>These are exactly the three whose duration scales with the power</b>, which is why
    /// <see cref="PowerExtendsDuration"/> defers to this. Nothing in the record marks them out — the
    /// two properties happen to name the same three spells, and a port should keep them tied rather
    /// than maintaining two lists that could drift apart.</para>
    /// </remarks>
    public static bool DrivesWorldLighting(int spellId) =>
        spellId == DragonsBreath || spellId == CandleGlow || spellId == Stardusk;

    /// <summary>
    /// <b>Candle Glow does nothing at all above ground.</b>
    /// </summary>
    /// <remarks>
    /// Its handler tests the zone first and returns before the sound, the text, the timers
    /// <i>and the cost</i>. So casting it outdoors is not a wasted cast — it is a complete no-op,
    /// silent and free. A port that charges for it and shows a failure message is being more
    /// informative than the original and wrong about the cost.
    /// </remarks>
    public static bool RequiresUnderground(int spellId) => spellId == CandleGlow;

    /// <summary>
    /// <b>Stardusk is Candle Glow's mirror: it does nothing at all <i>below</i> ground.</b>
    /// </summary>
    /// <remarks>
    /// The same test against the same zone value, with the branch the other way round — and like
    /// Candle Glow it returns before the sound, the text, the timers and the cost. Two light spells,
    /// complementary zones, and each a complete no-op in the other's territory.
    /// </remarks>
    public static bool RequiresAboveGround(int spellId) => spellId == Stardusk;

    /// <summary>Whether this spell is refused outright by the zone it is cast in.</summary>
    public static bool RefusedInZone(int spellId, bool underground) =>
        (RequiresUnderground(spellId) && !underground)
        || (RequiresAboveGround(spellId) && underground);

    /// <summary>
    /// <b>"And the Light Shall Lie" does not touch the lighting.</b>
    /// </summary>
    /// <remarks>
    /// Its name and its flavour text both talk about light, and its handler sets one plain spell
    /// timer with no light timer and no zone gate. The text says as much — the effect is invisible
    /// and "specifically designed for Moraeulf" — but the name invites the wrong grouping, so it is
    /// worth stating that it belongs with Scent of Sarig and Union rather than with the three that
    /// change what the world looks like.
    /// </remarks>
    public static bool NameSuggestsLightingButDoesNot(int spellId) =>
        spellId == AndTheLightShallLie;

    /// <summary>
    /// <b>A timed field spell charges even when it produces no effect.</b>
    /// </summary>
    /// <remarks>
    /// The cost is applied outside the branch that sets the timers, so a computed time of zero means
    /// no timer, no light change — and the caster pays anyway. The two zone-gated spells are the
    /// exceptions, because in the wrong zone they return before reaching either.
    /// </remarks>
    public static bool ChargesEvenWithNoEffect(int spellId) =>
        !RequiresUnderground(spellId) && !RequiresAboveGround(spellId);

    /// <summary>
    /// Eyes of Ishap's success chance: <b>ten percent per point of power</b>.
    /// </summary>
    /// <remarks>
    /// A d100 rolled against <c>cost × 10</c>, and the comparison is inclusive — so a roll equal to
    /// the threshold still succeeds. On success it plays a sound and opens the locator; on failure it
    /// shows a "complete waste of time" message.
    ///
    /// <para><b>The cost is charged before the roll</b>, so a failed cast still costs full price. The
    /// same shape as Black Nimbus in combat, which also rolls after committing.</para>
    /// </remarks>
    public static bool LocatorSucceeds(int rollUnder100, int cost) => rollUnder100 <= cost * 10;

    /// <summary>Whether this spell is a percentage-roll locator rather than a timed effect.</summary>
    /// <remarks>
    /// All three of the no-duration field spells turn out to be locators, and they share one shape:
    /// charge, roll, then either open the map picker or say "complete waste of time".
    /// </remarks>
    public static bool IsLocatorRoll(int spellId) => IsInstantaneous(spellId);

    /// <summary>
    /// The power a locator subtracts before scaling — <b>zero for two of them and four for Nacre
    /// Cicatrix</b>.
    /// </summary>
    /// <remarks>
    /// Eyes of Ishap and The Unseen roll against <c>cost × 10</c>; Nacre Cicatrix rolls against
    /// <c>(cost − 4) × 10</c>. The offset is not a mistake: its cost band starts at 5 rather than 1,
    /// so the subtraction lines the curve back up. <b>All three reach exactly 100% at their maximum
    /// cost</b> — 10 × 10 for the first two, (14 − 4) × 10 for the third — which is what shows the
    /// offset to be deliberate.
    ///
    /// <para>Below the offset the arithmetic goes negative and the original's comparison is
    /// unsigned, so a sub-4 cast would read as always succeeding. That is unreachable: the slider
    /// cannot go below the record's minimum of 5. Recorded because the model must not be handed a
    /// smaller cost from a mod without the reachability argument being re-checked.</para>
    /// </remarks>
    public static int LocatorCostOffset(int spellId) => spellId == NacreCicatrix ? 4 : 0;

    /// <summary>The chance a locator succeeds, as a percentage.</summary>
    public static int LocatorChancePercent(int spellId, int cost) =>
        (cost - LocatorCostOffset(spellId)) * 10;

    /// <summary>Whether the roll lands for a named locator, honouring its offset.</summary>
    public static bool LocatorSucceeds(int spellId, int rollUnder100, int cost) =>
        rollUnder100 <= LocatorChancePercent(spellId, cost);

    /// <summary>
    /// <b>A locator charges before it rolls, so a failure costs full price.</b>
    /// </summary>
    /// <remarks>
    /// The cost is applied as the routine's first act, ahead of the random number. The same shape as
    /// Black Nimbus in combat, and the reason a run of bad luck on a locator is expensive rather
    /// than merely disappointing.
    /// </remarks>
    public static bool LocatorChargesBeforeRolling => true;

    /// <summary>
    /// <b>The field cost function is confirmed against the disassembly.</b>
    /// </summary>
    /// <remarks>
    /// <c>SpellCasting.ApplyCost</c> was modelled from the canassa reconstruction. Reading
    /// <c>ApplySpellCost</c> (ovr179 @0x6d2c7) confirms every part of it: the pool change is
    /// <c>ChangeAttributeValue(actor, HealthStaminaCombo, −cost × 256, 100)</c>, the chapter-8 drain
    /// scans for the Crystal Staff <b>and does require the equipped flag</b>, and it floors the
    /// remaining charge at zero rather than wrapping.
    ///
    /// <para>That last point matters: the equipped-flag requirement is exactly where this function
    /// and the power slider diverge, and both sides are now disassembly-verified rather than
    /// inherited.</para>
    /// </remarks>
    public static bool FieldCostMatchesTheDisassembly => true;
    // ---------------------------------------------------------------- the locator screen
    // CastLocatorSpell @0x6d062.

    /// <summary>What a locator spell actually searches for.</summary>
    public enum LocatorTarget {
        /// <summary>Nothing — not a locator.</summary>
        None,

        /// <summary>Valuables.</summary>
        Valuables,

        /// <summary>Food.</summary>
        Food,

        /// <summary>Magic.</summary>
        Magic,
    }

    /// <summary>
    /// Which search a locator runs.
    /// </summary>
    /// <remarks>
    /// <b>The three locators share one screen and differ only in this.</b> Eyes of Ishap finds
    /// valuables, The Unseen finds food and Nacre Cicatrix finds magic — and nothing in
    /// <c>SPELLS.DAT</c> says so. They are told apart by a three-way comparison on the spell number
    /// inside the shared screen, so the search is the entire difference between them.
    /// </remarks>
    public static LocatorTarget TargetOf(int spellId) {
        switch (spellId) {
            case EyesOfIshap: return LocatorTarget.Valuables;
            case TheUnseen: return LocatorTarget.Food;
            case NacreCicatrix: return LocatorTarget.Magic;
            default: return LocatorTarget.None;
        }
    }

    /// <summary>
    /// <b>The locator screen borrows the world view rather than being a map screen.</b>
    /// </summary>
    /// <remarks>
    /// It saves the render viewport, shrinks it to an inset, raises the camera to the map's maximum
    /// height, draws the map into the clipped region and overlays <c>REQ_CMAP</c> — then puts the
    /// viewport and the camera back on the way out. So a port should treat this as a camera and
    /// clip-rect change over the live world, not as a separate screen with its own art.
    /// </remarks>
    public static bool LocatorReusesTheWorldViewport => true;

    /// <summary>The inset the world view is clipped to while the locator is open.</summary>
    public static (int X, int Y, int Width, int Height) LocatorViewport => (134, 16, 167, 89);

    // ---------------------------------------------------------------- which effect, which text
    // The six timed handlers each own one slot of the running-effects mask and one dialog record.

    /// <summary>
    /// The <see cref="SpellPaletteEvents"/> slot a timed field spell occupies.
    /// </summary>
    /// <remarks>
    /// <b>It is the spell's position in <see cref="All"/>, which is not a coincidence.</b> The
    /// dispatcher's table and the timer keys were written in the same order, so the first six
    /// entries line up with mask bits 0..5 — and that order is <i>not</i> the spell-number order, so
    /// a port that indexes the mask by spell number puts every effect in the wrong slot and shows
    /// the wrong symbol in the travel strip.
    ///
    /// <para>The three locators own no slot: they finish the moment they are cast.</para>
    /// </remarks>
    public static int EventIdOf(int spellId) {
        for (var i = 0; i < All.Length; i++) {
            if (All[i] == spellId) {
                return IsLocatorRoll(spellId) ? -1 : i;
            }
        }

        return -1;
    }

    /// <summary>The first of the six timed spells' dialog records.</summary>
    public const int FirstTimedDialog = 199;

    /// <summary>
    /// The dialog a timed field spell plays.
    /// </summary>
    /// <remarks>
    /// Six consecutive records in the same order as <see cref="All"/> — 199 for Dragon's Breath up
    /// to 204 for Scent of Sarig. Stated as the sequence it is, because the alternative is six
    /// constants that look independent and would hide a transposition.
    /// </remarks>
    public static int DialogFor(int spellId) {
        int slot = EventIdOf(spellId);

        return slot < 0 ? -1 : FirstTimedDialog + slot;
    }

    /// <summary>The sound the three lighting spells play.</summary>
    /// <remarks>IDA calls it <c>sound_mcreate</c>.</remarks>
    public const int CreationSound = 0x3a;

    /// <summary>The sound And the Light Shall Lie and Union play.</summary>
    /// <remarks>IDA calls it <c>sound_mgeneral</c>.</remarks>
    public const int GeneralSound = 0x51;

    /// <summary>The sound Scent of Sarig plays.</summary>
    public const int ScentSound = 0x0c;

    /// <summary>The sound a timed field spell plays, or -1 for one that plays none.</summary>
    /// <remarks>
    /// <b>Three sounds across six spells, and they do not group the way the effects do.</b> The
    /// three lighting spells share one, And the Light Shall Lie and Union share another, and Scent
    /// of Sarig has its own — so the audio grouping matches the duration formula for the first
    /// three and cuts across it for the rest.
    /// </remarks>
    public static int SoundFor(int spellId) {
        if (DrivesWorldLighting(spellId)) {
            return CreationSound;
        }

        switch (spellId) {
            case AndTheLightShallLie:
            case Union: return GeneralSound;
            case ScentOfSarig: return ScentSound;
            default: return -1;
        }
    }
}
