namespace GameData.Money;

using System.Globalization;
using GameData.Resources.Text;

/// <summary>
/// Renders a party-money amount as the original does. Faithful port of
/// <c>FormatMoneyToString</c> @0x42d9a (canassa <c>gstate_format_money</c>, GSTATE.C:442) plus the
/// inventory readout's inline abbreviation (<c>UI_DrawInventory</c> @0x56dd0, INVENTOR.C:530-543).
/// Spec: <c>docs/specs/party-money-display.md</c>.
///
/// <para><b>Amounts are counts of ROYALS, never sovereigns.</b> Ten royals make one sovereign —
/// the game says so itself in DDX 1800034. The party purse (<c>global_30001_party_gold</c>,
/// TEMP.GAM +0x0002) and every price the engine quotes are stored in royals; sovereigns exist only
/// as a presentation split. Anything that divides by ten before reaching this class is a bug.</para>
///
/// <para>The pluralisation quirks below are the original's observable output and are reproduced
/// deliberately — see the spec's "Non-goals" list before "fixing" any of them.</para>
/// </summary>
public static class MoneyFormatter {
    private const string KeyGoldAndSilver = "base:uistring:money.gold_and_silver";
    private const string KeySilverOnly = "base:uistring:money.silver_only";
    private const string KeyGoldOnly = "base:uistring:money.gold_only";
    private const string KeySovereignsAndRoyal = "base:uistring:money.sovereigns_and_royal";
    private const string KeySovereignAndRoyal = "base:uistring:money.sovereign_and_royal";
    private const string KeyRoyalOnly = "base:uistring:money.royal_only";
    private const string KeyAbbreviated = "base:uistring:money.abbreviated";

    // "%d sovereigns" — the teleport fare, and the one wording with no singular form and no royals
    // part at all. Both are the original's, not an omission here: see FormatMoneyToString's callers
    // versus drawTeleportMenu's inline sprintf.
    private const string KeyTeleportCost = "base:uistring:money.teleport_cost";

    /// <summary>Ten royals to the sovereign (DDX 1800034; the <c>idiv 10</c> at 0x42dab).</summary>
    public const int RoyalsPerSovereign = 10;

    /// <summary>Above this many sovereigns the abbreviated readout drops both the royals and the
    /// unit letters (<c>cmp 9999 / jle</c> at 0x56dfe).</summary>
    public const int AbbreviatedSovereignLimit = 9999;

    /// <summary>The sovereign part of an amount. Truncating division, so a negative amount yields
    /// a negative part — the original uses signed <c>idiv</c> and clamps nowhere in the display
    /// path.</summary>
    public static int Sovereigns(int amountInRoyals) => amountInRoyals / RoyalsPerSovereign;

    /// <summary>The leftover royal part of an amount. Carries the amount's sign, as C's
    /// <c>%</c> does.</summary>
    public static int Royals(int amountInRoyals) => amountInRoyals % RoyalsPerSovereign;

    /// <summary>Format an amount of royals in the given wording.</summary>
    public static string Format(int amountInRoyals, CurrencyStyle style) {
        int sovereigns = Sovereigns(amountInRoyals);
        int royals = Royals(amountInRoyals);
        return style switch {
            CurrencyStyle.GoldAndSilver => FormatGoldAndSilver(sovereigns, royals),
            CurrencyStyle.Abbreviated => FormatAbbreviated(sovereigns, royals),
            CurrencyStyle.TeleportQuote => CFormat.Apply(UiStrings.Get(KeyTeleportCost), sovereigns),
            _ => FormatSovereignsAndRoyals(sovereigns, royals),
        };
    }

    // currency_gold_silver (0x42de0-0x42e19). Note the asymmetry with the prose wording: a
    // zero-royals amount says only the gold part, and a zero-gold amount says only the silver.
    private static string FormatGoldAndSilver(int sovereigns, int royals) {
        if (royals == 0) {
            return CFormat.Apply(UiStrings.Get(KeyGoldOnly), sovereigns);
        }
        return sovereigns == 0
            ? CFormat.Apply(UiStrings.Get(KeySilverOnly), royals)
            : CFormat.Apply(UiStrings.Get(KeyGoldAndSilver), sovereigns, royals);
    }

    // currency_sovereigns_royals (0x42e1b-0x42e99).
    private static string FormatSovereignsAndRoyals(int sovereigns, int royals) {
        if (royals == 0) {
            // "%ld sovereign%c" with '\0' for <= 1: the NUL terminates the string, so one (or
            // zero, or a negative count of) sovereigns reads singular. "0 sovereign" is what an
            // empty purse says in prose. Not expressible as a catalog entry (no NUL-conversion
            // in CFormat), so this branch deliberately keeps its C# literal — do not "finish"
            // this cutover by moving it to the catalog, or the singular breaks.
            return Num(sovereigns) + " sovereign" + (sovereigns > 1 ? "s" : string.Empty);
        }
        string text = sovereigns > 1
            ? CFormat.Apply(UiStrings.Get(KeySovereignsAndRoyal), sovereigns, royals)
            : sovereigns != 0
                ? CFormat.Apply(UiStrings.Get(KeySovereignAndRoyal), sovereigns, royals)
                : CFormat.Apply(UiStrings.Get(KeyRoyalOnly), royals);
        // The "s" is appended AFTER the sentence is built, and only above one — so a negative
        // royal count stays singular ("-5 royal"), exactly as the original prints it.
        return royals > 1 ? text + "s" : text;
    }

    // The inventory readout's own inline form (0x56dfe-0x56e33), not a formatter mode. Past the
    // limit it prints the sovereigns alone: no royals, and no unit letters at all.
    private static string FormatAbbreviated(int sovereigns, int royals) =>
        sovereigns > AbbreviatedSovereignLimit
            ? Num(sovereigns)
            : CFormat.Apply(UiStrings.Get(KeyAbbreviated), sovereigns, royals);

    // The original's sprintf("%ld") — a plain, culture-free decimal with no group separators.
    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
}
