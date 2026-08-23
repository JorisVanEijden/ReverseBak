namespace GameData.Resources.Spells;

/// <summary>
/// How a cast's magnitude is adjusted and whether it lands —
/// <c>cspell_resolve_cast</c> (canassa CSPELL.C:1258).
///
/// <para>The magnitude ("intensity") arrives already computed by the caller; this is what happens to
/// it on the way to the target, and which casts roll to hit at all.</para>
/// </summary>
public static class SpellCastResolution {
    /// <summary>
    /// <b>A negative magnitude means a HEAL.</b>
    /// </summary>
    /// <remarks>
    /// The routine records the sign, then works with the absolute value — so the same intensity
    /// pipeline serves damage and healing, and the sign is the only thing distinguishing them.
    /// A port that clamped intensity at zero would silently turn every heal into a no-op.
    /// </remarks>
    public static bool IsHeal(int intensity) => intensity < 0;

    /// <summary>
    /// A storm raises every cast's magnitude by half.
    /// </summary>
    /// <remarks>
    /// <c>intensity += intensity >> 1</c>, applied <b>before</b> the sign is taken — so it amplifies
    /// heals as well as damage, by the same fraction. Applying it after the absolute value would
    /// give the same number here but is a different rule the moment anything else reads the sign.
    ///
    /// <para>An arithmetic shift on a negative value rounds toward negative infinity, so a heal of
    /// -5 becomes -8 rather than -7. Reproduced with a shift rather than a division for that reason.</para>
    /// </remarks>
    public static int ApplyStormAmplification(int intensity, bool stormActive) =>
        stormActive ? intensity + (intensity >> 1) : intensity;

    /// <summary>
    /// <b>Some creatures take double from particular spells.</b>
    /// </summary>
    /// <remarks>
    /// The check is a per-(creature, spell) bitmap — <c>cbstat_char_bitmap_3w_test_170c</c> — and the
    /// result doubles the magnitude. It is consulted for <b>any</b> target, so a vulnerable creature
    /// also receives double from a heal aimed at it.
    /// </remarks>
    public static int ApplyVulnerability(int magnitude, bool targetIsVulnerable) =>
        targetIsVulnerable ? magnitude << 1 : magnitude;

    /// <summary>The spell kind that is always cast without a target.</summary>
    /// <remarks>
    /// <c>nSpell_kind == 8</c> discards whatever target it was handed. So passing one is harmless,
    /// but relying on it arriving is not.
    /// </remarks>
    public const int TargetlessSpellKind = 8;

    /// <summary>The only spell kind that rolls to hit.</summary>
    public const int RollsToHitSpellKind = 0;

    /// <summary>
    /// Whether the cast has to beat a skill check, or simply lands.
    /// </summary>
    /// <param name="spellKind">The spell definition's kind.</param>
    /// <param name="isHeal">Whether the magnitude was negative.</param>
    /// <param name="hasTarget">Whether a target survived the kind-8 discard.</param>
    /// <remarks>
    /// <b>Most casts cannot miss.</b> The skill check runs only for an offensive
    /// (<see cref="RollsToHitSpellKind"/>) spell with a target; <b>heals always land, and so does
    /// every non-zero spell kind</b>. A port that rolled for everything would have healers failing to
    /// heal, which no amount of play-testing would attribute to the wrong branch.
    ///
    /// <para>The check itself is <c>combatenc_skill_check_random</c> with the caster's Casting stat
    /// and a quarrel kind of -1 (i.e. no ammunition term).</para>
    /// </remarks>
    public static bool NeedsSkillCheck(int spellKind, bool isHeal, bool hasTarget) =>
        hasTarget && !isHeal && spellKind == RollsToHitSpellKind;

    /// <summary>The full magnitude pipeline, in the original's order.</summary>
    /// <param name="intensity">Signed magnitude as the caller computed it.</param>
    /// <param name="stormActive">Whether the storm amplifier is up.</param>
    /// <param name="targetIsVulnerable">Whether the vulnerability bitmap matched.</param>
    /// <returns>The unsigned magnitude to apply, plus whether it heals.</returns>
    /// <remarks>
    /// <b>Order matters:</b> storm first (on the signed value), then the sign is taken, then
    /// vulnerability doubles the absolute magnitude. Doubling before the storm would compound
    /// differently.
    /// </remarks>
    public static (int Magnitude, bool Heals) Resolve(int intensity, bool stormActive,
        bool targetIsVulnerable) {
        int amplified = ApplyStormAmplification(intensity, stormActive);
        bool heals = IsHeal(amplified);
        int magnitude = heals ? -amplified : amplified;
        return (ApplyVulnerability(magnitude, targetIsVulnerable), heals);
    }
}
