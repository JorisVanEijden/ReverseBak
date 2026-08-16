namespace GameData.Resources.Spells;

/// <summary>
/// The casting screen's spell-info panel — <c>UI_show_spell_info</c> (ovr173 @0x6950f).
///
/// <para>The text comes from <see cref="SpellDescriptions"/>; this is how it is laid out and which
/// two of its seven lines are replaced at runtime.</para>
/// </summary>
public static class SpellInfoPanel {
    // Positions are canonical (1600x1200); the original's own pixels are given alongside because the
    // routine hard-codes them rather than reading them from a layout file.

    /// <summary>Centre the title is measured from — original x 217.</summary>
    public const int TitleCentreX = 1085;

    /// <summary>Baseline of the title — original y 26.</summary>
    public const int TitleY = 156;

    /// <summary>Left edge of every body line — original x 141.</summary>
    public const int BodyX = 705;

    /// <summary>Baseline of the first body line — original y 39 (title y + 13).</summary>
    public const int FirstBodyY = 234;

    /// <summary>
    /// Distance between body lines — original 11.
    /// </summary>
    /// <remarks>
    /// <b>Not the same as the gap under the title.</b> The title is followed by 13 and every line
    /// after that by 11, so using one spacing throughout drifts the panel by two original pixels per
    /// line. Small, and wrong in a way that only shows on the last line.
    /// </remarks>
    public const int BodyLineStep = 66;

    /// <summary>Where the title starts, given its measured width.</summary>
    /// <remarks>The title is the only centred line; the rest are left-aligned at
    /// <see cref="BodyX"/>.</remarks>
    public static int TitleX(int measuredWidth) => TitleCentreX - (measuredWidth / 2);

    /// <summary>
    /// <b>An empty line is skipped without leaving a gap.</b>
    /// </summary>
    /// <remarks>
    /// The routine tests the copied string and, when it is empty, skips <i>both</i> the draw and the
    /// line advance. So a spell with no second effect line closes up rather than showing a blank row
    /// — which is why panels of different lengths all look deliberate.
    /// </remarks>
    public static bool LineAdvances(string line) => !string.IsNullOrEmpty(line);

    /// <summary>
    /// The y of each drawn line, given which of the earlier ones were non-empty.
    /// </summary>
    /// <param name="drawnBefore">How many body lines have already been drawn.</param>
    public static int BodyY(int drawnBefore) => FirstBodyY + (drawnBefore * BodyLineStep);

    // ---------------------------------------------------------------- the two runtime overrides

    /// <summary>
    /// Whether the cost line is replaced with the live figure.
    /// </summary>
    /// <remarks>
    /// The shipped line is a template ("Cost: 5-15 Health/Stamina"); when a cost has been chosen it
    /// is replaced with that number. A cost of zero leaves the template, which is what the panel
    /// shows before the player has picked a power.
    /// </remarks>
    public static bool CostLineIsReplaced(int cost) => cost != 0;

    /// <summary>The magnitude value that means "this spell has no damage figure".</summary>
    /// <remarks>
    /// <b>A thousand, not zero.</b> Zero and 1000 both leave the shipped template in place, so the
    /// sentinel is a second special case rather than a plain "no value" — treating only zero as
    /// absent would print "Damage: 1000" on every spell that has no damage.
    /// </remarks>
    public const int NoDamageMagnitude = 1000;

    /// <summary>Whether the damage line is replaced with the computed magnitude.</summary>
    public static bool DamageLineIsReplaced(int magnitude) =>
        magnitude != 0 && magnitude != NoDamageMagnitude;

    // ---------------------------------------------------------------- the health/stamina footer

    /// <summary>
    /// The nine spells whose panel also shows the caster's health and stamina.
    /// </summary>
    /// <remarks>
    /// A jump table, not a property of the spell data — so it cannot be derived and has to be
    /// carried. These are the spells whose cost the player weighs against what the caster has left.
    /// </remarks>
    public static readonly int[] ShowsCasterHealthStamina = { 0, 2, 8, 11, 17, 18, 26, 34, 35 };

    /// <summary>Whether this spell's panel shows the caster's health and stamina.</summary>
    public static bool ShowsHealthStamina(int spellNumber) =>
        System.Array.IndexOf(ShowsCasterHealthStamina, spellNumber) >= 0;

    /// <summary>Baseline of that footer — original y 94.</summary>
    /// <remarks>
    /// <b>Fixed, not "after the last line".</b> It is drawn at its own position regardless of how
    /// many description lines the spell used, so a short panel leaves a gap above it rather than
    /// pulling it up.
    /// </remarks>
    public const int HealthStaminaY = 564;
}
