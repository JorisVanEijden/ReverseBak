namespace GameData.Money;

/// <summary>
/// How a money amount is worded. The original has one formatter, <c>FormatMoneyToString</c>
/// @0x42d9a, whose <c>currency</c> argument selects between two shipped wordings — plus a third,
/// terser form that the inventory screen writes inline rather than going through the formatter.
///
/// <para>See <c>docs/specs/party-money-display.md</c> §2-§3 for the derivation. The formatter's
/// mode 0 (<c>"%ld.%ld gold"</c>) is deliberately absent: no shipped code path selects it.</para>
/// </summary>
public enum CurrencyStyle {
    /// <summary>The terse screen wording — the inn rest screen's readout and shop item prices
    /// (<c>currency_gold_silver</c>, the formatter's mode 1): "12 gold 5 silver".</summary>
    GoldAndSilver = 1,

    /// <summary>The in-fiction prose wording — every money amount spoken in dialog text
    /// (<c>currency_sovereigns_royals</c>, mode 2): "12 sovereigns and 5 royals".</summary>
    SovereignsAndRoyals = 2,

    /// <summary>The inventory screen's abbreviated readout, written inline by
    /// <c>UI_DrawInventory</c> @0x56dd0 rather than by the formatter: "123s 4r", collapsing to a
    /// bare sovereign count past 9999.</summary>
    Abbreviated = 3,

    /// <summary>The teleport screen's fare quote, written inline by <c>drawTeleportMenu</c>
    /// @0x4ecff: "12 sovereigns", always plural and with the royals dropped.</summary>
    TeleportQuote = 4,
}
