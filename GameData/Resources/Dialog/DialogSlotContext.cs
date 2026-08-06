namespace GameData.Resources.Dialog;

using System;
using System.Collections.Generic;

/// <summary>
/// Everything <see cref="DialogSlotPopulator"/> needs to fill a slot, gathered from live game state
/// so the populator itself stays a pure function of its inputs. The engine reads these straight out
/// of dseg; here they are handed in, which is also what makes the seeding testable.
/// </summary>
public sealed class DialogSlotContext {
    /// <summary>The active party's member ids, in roster order — the engine's
    /// <c>party_roster[0..party_count)</c>. These are ids into <see cref="ActorNames"/>, NOT slot
    /// or portrait positions, and the random picker's constraints are expressed in them.</summary>
    public IReadOnlyList<int> PartyRoster { get; set; } = Array.Empty<int>();

    /// <summary>Every character's name, indexed by member id (not by roster position).</summary>
    public IReadOnlyList<string> ActorNames { get; set; } = Array.Empty<string>();

    /// <summary>Global 30005: the chapter's designated speaker. <c>GetGlobalValue</c> @0x42399
    /// resolves it as "member 3 if he is in the active party, else the chapter's own entry in the
    /// 9-byte table at 0x3a53e". Slot 4's standing default, and the member slot 0 is forbidden
    /// from picking.</summary>
    public int ChapterSpeakerId { get; set; }

    /// <summary>The actor a bare <c>@</c> (one not followed by a digit) names — the engine's
    /// <c>nEvtArgActor0</c>, global 30004.</summary>
    public int CurrentActorId { get; set; }

    /// <summary>Global 30004 — the dialog's primary actor (kinds 10 and 30, and the actor a bare
    /// <c>@</c> names). Same value as <see cref="CurrentActorId"/>; both names are kept because the
    /// engine reads it through two different paths.</summary>
    public int PrimaryActorId { get; set; }

    /// <summary>The dialog's secondary actor (kind 11) — TEMP.GAM +0x0598, the last actor a status
    /// effect touched.</summary>
    public int SecondaryActorId { get; set; }

    /// <summary>The dialog's tertiary actor (kind 12) — TEMP.GAM +0x059A.</summary>
    public int TertiaryActorId { get; set; }

    /// <summary>The creature the dialog is about (kind 17) — TEMP.GAM +0x05A0, an index into
    /// MNAMES.DAT.</summary>
    public int CreatureType { get; set; }

    /// <summary>The object the dialog is about (kind 18) — TEMP.GAM +0x05A2.</summary>
    public int KeyObjectId { get; set; }

    /// <summary>The party purse in royals, for text-variable source 20.</summary>
    public int PartyMoneyInRoyals { get; set; }

    /// <summary>The engine's quoted-amount global (30014), for sources 19 and 21.</summary>
    public int QuotedAmount { get; set; }

    /// <summary>Global 30015, printed as a bare number by kind 22.</summary>
    public int Global30015 { get; set; }

    /// <summary>Global 30018, printed as a bare number by kind 23.</summary>
    public int Global30018 { get; set; }

    /// <summary>Whether the NPC being spoken to runs an inn rather than a shop — the engine's
    /// <c>g_bIsRestEncounter</c>, set from the container's nightly rate (CMBINV.C:67). Kind 28 says
    /// "tavernkeeper" when true and "shopkeeper" when false.</summary>
    public bool IsRestEncounter { get; set; }

    /// <summary>Creature id → name (MNAMES.DAT), for kind 17. Null yields an empty name.</summary>
    public Func<int, string> CreatureNameOf { get; set; }

    /// <summary>Object id → name (OBJINFO.DAT), for kind 18. Null yields an empty name.</summary>
    public Func<int, string> ObjectNameOf { get; set; }

    /// <summary>The engine's <c>RND(n)</c>: a value in <c>[0, n)</c>. Injected so the seeding is
    /// reproducible under test — the picker genuinely re-rolls on every dialog play, so a narration
    /// line can name a different companion each time it is shown, and that is faithful.</summary>
    public Func<int, int> Random { get; set; }

    /// <summary>The name for a member id, or empty when the id is out of range.</summary>
    public string NameOf(int actorId) =>
        actorId >= 0 && actorId < ActorNames.Count ? ActorNames[actorId] ?? "" : "";
}
