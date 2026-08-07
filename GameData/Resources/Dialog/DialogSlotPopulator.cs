namespace GameData.Resources.Dialog;

using GameData.Money;
using GameData.Resources.Dialog.Actions;
using GameData.Resources.Text;
using System.Globalization;

/// <summary>
/// Fills a <see cref="DialogSlotTable"/> — the port of <c>dialog_cmbt_name_assign_kind</c>
/// (<c>PopulateDialogSlotText</c> @0x48b66, DIALOG.C:444) and of the seeding pass
/// <c>dialog_combatant_name_table_init</c> (DIALOG.C:777) that every dialog play begins with.
///
/// <para>The engine's model, which this reproduces: a slot is written by <b>kind</b>, never by slot
/// index. Four slots get standing defaults at the start of a play; an entry's own ops then overwrite
/// whichever slots they name. A slot nothing wrote stays empty, and the renderer emits nothing for
/// it — it never shows the token.</para>
///
/// <para><b>Modelled kinds:</b> 1-6 (a specific party member), 7 (the chapter speaker), 13-16 and 31
/// (the constrained random picker), 19/20 (money, in the prose wording) and 21 (a bare number).
/// Any other kind leaves the slot's current text alone rather than inventing one, so an unmodelled
/// kind now shows the seeded default instead of a raw token — better, but still not right; see
/// the kind table in <c>PopulateDialogSlotText</c> for what is missing.</para>
/// </summary>
public static class DialogSlotPopulator {
    public const int SlotCount = DialogSlotTable.SlotCount;

    /// <summary>
    /// The engine global the money text-variables read (<c>global_30014</c>, TEMP.GAM +0x05A6):
    /// the amount currently being quoted to the player. Whatever is about to charge them writes it
    /// first — a shop its price, a temple its fee, the inn its nightly rate, and the inventory
    /// screen's money button the party's own purse (<c>sub_ovr157_4E3</c> @0x54b18, CMBINV.C:354),
    /// which is why "how much do we have?" is spoken through source 19 rather than 20.
    /// </summary>
    public const int QuotedAmountGlobalKey = 30014;

    /// <summary>Global 30005 — the chapter's designated speaker, slot 4's standing default.</summary>
    public const int ChapterSpeakerGlobalKey = 30005;

    /// <summary>Global 30004 — the actor a bare <c>@</c> names.</summary>
    public const int CurrentActorGlobalKey = 30004;

    // Kinds, named where the engine only numbers them.
    private const int KindChapterSpeaker = 7;          // global 30005, no randomness
    private const int KindPrimaryActor = 10, KindPrimaryActorAlt = 30;
    private const int KindSecondaryActor = 11;
    private const int KindTertiaryActor = 12;
    private const int KindRandomFirst = 13, KindRandomLast = 16;
    private const int KindCreatureName = 17;
    private const int KindObjectName = 18;
    private const int KindRandomNotSpeaker = 31;
    private const int KindShopOrTavernKeeper = 28;
    private const int SourceQuotedMoney = 19;
    private const int SourcePartyMoney = 20;
    private const int SourceQuotedNumber = 21;
    private const int SourceGlobal30015 = 22;
    private const int SourceGlobal30018 = 23;
    private const int KindAttributeNameAndValue = 27;
    private const int KindAttributeName = 29;

    private const int RandomDrawLimit = 500;

    /// <summary>
    /// The table a dialog play starts from: cleared, then seeded with the four standing defaults in
    /// the engine's own order — slot 4, slot 5, slot 3, slot 0 (DIALOG.C:789-792).
    ///
    /// <para>The order is not cosmetic. The random picker refuses an actor already held by a
    /// <i>lower</i> slot, so seeding 4 before 5 makes slot 5 defer to slot 4, while slot 3 — seeded
    /// third but sitting below both — defers to neither.</para>
    /// </summary>
    public static DialogSlotTable CreateForPlay(DialogSlotContext context) {
        var table = new DialogSlotTable();
        table.Clear();
        Assign(table, 4, KindChapterSpeaker, 0, context);
        Assign(table, 5, 15, 0, context);
        Assign(table, 3, 14, 0, context);
        Assign(table, 0, KindRandomNotSpeaker, 0, context);
        return table;
    }

    /// <summary>
    /// Apply one entry's <see cref="SetTextVariableAction"/>s on top of the table, as
    /// <c>dialog_play_record</c> does for each record it displays (DIALOG.C:861).
    ///
    /// <para><b>Known gap:</b> the engine runs this for every record it walks THROUGH, not only the
    /// leaf it ends up showing, so an op on an intermediate branch entry is currently lost. Nothing
    /// in the dialogs reached so far depends on it.</para>
    /// </summary>
    public static void ApplyEntryActions(DialogSlotTable table, DialogEntry entry,
        DialogSlotContext context) {
        if (table == null || entry?.Actions == null) {
            return;
        }
        foreach (DialogActionBase action in entry.Actions) {
            if (action is SetTextVariableAction sv) {
                Assign(table, sv.Slot, sv.Source, sv.Aux, context);
            }
        }
    }

    /// <summary>Seed a table for this play and apply <paramref name="entry"/>'s own ops — the whole
    /// flow for the one-entry-per-play shape every caller has today. Returns the slot text for
    /// <see cref="TextVariableResolver"/>.</summary>
    public static string[] BuildSlots(DialogEntry entry, DialogSlotContext context) {
        DialogSlotTable table = CreateForPlay(context);
        ApplyEntryActions(table, entry, context);
        return table.Names;
    }

    /// <summary>
    /// Write one slot from one kind — the engine's switch. Note it clears the slot's <i>kind</i>
    /// first and only some branches write a <i>name</i>: an unhandled kind stops the slot claiming
    /// to hold an actor without wiping the text that is there.
    /// </summary>
    public static void Assign(DialogSlotTable table, int slot, int kind, int aux,
        DialogSlotContext context) {
        if (table == null || context == null || slot < 0 || slot >= SlotCount) {
            return;
        }
        table.Kinds[slot] = DialogSlotTable.NoActor;

        if (kind >= 1 && kind <= 6) {
            // A specific party member, one-based.
            int actor = kind - 1;
            table.Kinds[slot] = actor;
            table.Names[slot] = context.NameOf(actor);
            return;
        }
        if (kind == KindChapterSpeaker) {
            table.Kinds[slot] = context.ChapterSpeakerId;
            table.Names[slot] = context.NameOf(context.ChapterSpeakerId);
            return;
        }
        if ((kind >= KindRandomFirst && kind <= KindRandomLast) || kind == KindRandomNotSpeaker) {
            AssignRandomActor(table, slot, kind, aux, context);
            return;
        }
        switch (kind) {
            // The three actor globals a dialog is "about". Each writes the kind too, so the slot
            // knows it holds a party member.
            case KindPrimaryActor:
            case KindPrimaryActorAlt:
                SetActor(table, slot, context.PrimaryActorId, context);
                return;
            case KindSecondaryActor:
                SetActor(table, slot, context.SecondaryActorId, context);
                return;
            case KindTertiaryActor:
                SetActor(table, slot, context.TertiaryActorId, context);
                return;
            // A creature, not a party member — and the slot is marked as such, which is what turns
            // on the renderer's a/an article and possessive rules for this token.
            case KindCreatureName:
                table.Kinds[slot] = DialogSlotTable.CreatureActor;
                table.Names[slot] = context.CreatureNameOf?.Invoke(context.CreatureType) ?? "";
                return;
            case KindObjectName:
                table.Names[slot] = context.ObjectNameOf?.Invoke(context.KeyObjectId) ?? "";
                return;
            case KindShopOrTavernKeeper:
                table.Names[slot] = UiStrings.Get(context.IsRestEncounter
                    ? "base:uistring:npc.tavernkeeper"
                    : "base:uistring:npc.shopkeeper");
                return;
            // Both money sources speak the prose wording — every amount SPOKEN in the game is in
            // sovereigns and royals; gold/silver is screen-only.
            case SourceQuotedMoney:
                table.Names[slot] = MoneyFormatter.Format(context.QuotedAmount,
                    CurrencyStyle.SovereignsAndRoyals);
                return;
            case SourcePartyMoney:
                table.Names[slot] = MoneyFormatter.Format(context.PartyMoneyInRoyals,
                    CurrencyStyle.SovereignsAndRoyals);
                return;
            // Same global as 19, printed as a plain integer. Not money-specific — DDX 1800004 uses
            // it for health points.
            case SourceQuotedNumber:
                table.Names[slot] = context.QuotedAmount.ToString(CultureInfo.InvariantCulture);
                return;
            case SourceGlobal30015:
                table.Names[slot] = context.Global30015.ToString(CultureInfo.InvariantCulture);
                return;
            case SourceGlobal30018:
                table.Names[slot] = context.Global30018.ToString(CultureInfo.InvariantCulture);
                return;
            // Kind 27 writes TWO slots regardless of which was requested: the attribute name into
            // the requested slot and its value into slot 1 (DIALOG.C:531-535). That second write is
            // why DIAL_Z24's "@1" was blank while every other token in the corpus resolved.
            case KindAttributeNameAndValue:
                table.Names[slot] = context.AttributeNameOf(context.Global30015);
                if (SlotCount > 1) {
                    table.Names[1] = (context.AttributeValueOf?.Invoke(context.Global30015) ?? 0)
                        .ToString(CultureInfo.InvariantCulture);
                }
                return;
            case KindAttributeName:
                table.Names[slot] = context.AttributeNameOf(context.Global30018);
                return;
            default:
                return; // unmodelled kind: keep whatever the seeding put here
        }
    }

    private static void SetActor(DialogSlotTable table, int slot, int actorId,
        DialogSlotContext context) {
        table.Kinds[slot] = actorId;
        table.Names[slot] = context.NameOf(actorId);
    }

    /// <summary>
    /// The constrained random picker, <c>dialog_assign_rand_cmbt_name</c> (DIALOG.C:400). Draws a
    /// party member at random and keeps it only if it clears both filters:
    ///
    /// <list type="bullet">
    /// <item>it is not already held by a slot BELOW this one, and</item>
    /// <item>it satisfies the kind's own membership rule — 14 wants one of {2,3,5}, 15 one of
    /// {0,1,4}, 16 one of {1,5}, and 31 wants anyone who is <i>not</i> the chapter speaker. Kind 13
    /// imposes no rule of its own and takes the first draw that is not a duplicate.</item>
    /// </list>
    ///
    /// <para>After <see cref="RandomDrawLimit"/> failed draws it stops and takes a fallback rather
    /// than looping — which is the normal path, not an edge case, whenever the party simply has
    /// nobody in the kind's set.</para>
    /// </summary>
    private static void AssignRandomActor(DialogSlotTable table, int slot, int kind, int aux,
        DialogSlotContext context) {
        int partyCount = context.PartyRoster?.Count ?? 0;
        if (partyCount == 0) {
            return; // the engine returns before writing anything, leaving the slot as it was
        }

        int candidate = -1;
        for (int draw = 0; draw < RandomDrawLimit && candidate < 0; draw++) {
            candidate = context.PartyRoster[Draw(context, partyCount)];
            if (table.IsTakenBelow(slot, candidate) || !SatisfiesKind(kind, candidate, context)) {
                candidate = -1;
            }
        }
        if (candidate < 0) {
            // aux names a slot (one-based) to copy the actor from; zero means the party leader.
            candidate = aux > 0 && aux <= SlotCount
                ? table.Kinds[aux - 1]
                : context.PartyRoster[0];
        }

        table.Kinds[slot] = candidate;
        table.Names[slot] = context.NameOf(candidate);
    }

    private static bool SatisfiesKind(int kind, int candidate, DialogSlotContext context) {
        switch (kind) {
            case 14: return candidate == 2 || candidate == 3 || candidate == 5;
            case 15: return candidate == 0 || candidate == 1 || candidate == 4;
            case 16: return candidate == 1 || candidate == 5;
            case KindRandomNotSpeaker: return candidate != context.ChapterSpeakerId;
            default: return true; // kind 13: any non-duplicate member
        }
    }

    private static int Draw(DialogSlotContext context, int bound) {
        int value = context.Random?.Invoke(bound) ?? 0;
        return value < 0 || value >= bound ? 0 : value;
    }
}
