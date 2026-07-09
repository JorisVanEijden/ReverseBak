namespace GameData.Resources.Dialog;

using GameData.Resources.Dialog.Actions;
using System.Collections.Generic;

/// <summary>
/// Builds the 6 text-variable slots for an entry (PopulateDialogSlotText @0x48b66 subset).
/// Defaults each slot to the matching active party member's name (the observed behaviour when
/// no SetTextVariable action populates it — e.g. DDX 94/154's @0 = lead), then applies the
/// entry's SetTextVariableAction Source cases used by the corpse path.
/// </summary>
public static class DialogSlotPopulator {
    public const int SlotCount = 6;

    public static string[] BuildSlots(DialogEntry entry, IReadOnlyList<string> partyNames, int partyGold) {
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
                    } else if (sv.Source == 20) {
                        slots[sv.Slot] = partyGold.ToString();
                    }
                    // other Source cases: leave the party-name default (documented).
                }
            }
        }
        return slots;
    }
}
