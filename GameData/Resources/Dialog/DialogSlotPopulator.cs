namespace GameData.Resources.Dialog;

using GameData.Money;
using GameData.Resources.Dialog.Actions;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Builds the 6 text-variable slots for an entry (<c>PopulateDialogSlotText</c> @0x48b66 subset).
/// Defaults each slot to the matching active party member's name (the observed behaviour when
/// no SetTextVariable action populates it — e.g. DDX 94/154's @0 = lead), then applies the
/// entry's <see cref="SetTextVariableAction"/> Source cases that are modelled.
/// </summary>
public static class DialogSlotPopulator {
    public const int SlotCount = 6;

    /// <summary>
    /// The engine global the money text-variables read (<c>global_30014</c>, TEMP.GAM +0x05A6):
    /// the amount currently being quoted to the player. Whatever is about to charge them writes it
    /// first — a shop its price, a temple its fee, the inn its nightly rate, and the inventory
    /// screen's money button the party's own purse (<c>sub_ovr157_4E3</c> @0x54b18, CMBINV.C:354),
    /// which is why "how much do we have?" is spoken through source 19 rather than 20.
    /// </summary>
    public const int QuotedAmountGlobalKey = 30014;

    /// <summary>Source 19: the quoted amount, in the prose wording (0x48c4e).</summary>
    private const int SourceQuotedMoney = 19;

    /// <summary>Source 20: the party purse, in the prose wording (0x48c66).</summary>
    private const int SourcePartyMoney = 20;

    /// <summary>Source 21: the same global as 19, but as a bare integer via <c>ltoa</c> (0x48c7e).
    /// Not money-specific — DDX 1800004 uses it for health points.</summary>
    private const int SourceQuotedNumber = 21;

    /// <param name="partyMoneyInRoyals">The party purse (<c>global_30001_party_gold</c>), in
    /// ROYALS — ten to the sovereign. See <see cref="MoneyFormatter"/>.</param>
    /// <param name="quotedAmount">The engine's cost/amount argument (<c>global_30014</c>): the
    /// price a shop is asking, the fee a temple wants, the inn's nightly rate. In royals when a
    /// money source reads it, a plain number when source 21 does.</param>
    public static string[] BuildSlots(DialogEntry entry, IReadOnlyList<string> partyNames,
        int partyMoneyInRoyals, int quotedAmount) {
        var slots = new string[SlotCount];
        for (int i = 0; i < SlotCount; i++) {
            slots[i] = i < partyNames.Count ? partyNames[i] : string.Empty;
        }
        if (entry?.Actions != null) {
            foreach (DialogActionBase a in entry.Actions) {
                if (a is SetTextVariableAction sv && sv.Slot >= 0 && sv.Slot < SlotCount) {
                    if (sv.Source >= 1 && sv.Source <= 6) {
                        int idx = sv.Source - 1;
                        slots[sv.Slot] = idx < partyNames.Count ? partyNames[idx] : slots[sv.Slot];
                    } else if (sv.Source == SourceQuotedMoney) {
                        // Both money sources use currency_sovereigns_royals — every money amount
                        // SPOKEN in the game is in the prose wording; gold/silver is screen-only.
                        slots[sv.Slot] = MoneyFormatter.Format(quotedAmount,
                            CurrencyStyle.SovereignsAndRoyals);
                    } else if (sv.Source == SourcePartyMoney) {
                        slots[sv.Slot] = MoneyFormatter.Format(partyMoneyInRoyals,
                            CurrencyStyle.SovereignsAndRoyals);
                    } else if (sv.Source == SourceQuotedNumber) {
                        slots[sv.Slot] = quotedAmount.ToString(CultureInfo.InvariantCulture);
                    }
                    // other Source cases: leave the party-name default (documented).
                }
            }
        }
        return slots;
    }
}
