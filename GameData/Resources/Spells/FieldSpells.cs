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
}
