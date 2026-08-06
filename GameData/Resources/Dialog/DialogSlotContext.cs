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

    /// <summary>The party purse in royals, for text-variable source 20.</summary>
    public int PartyMoneyInRoyals { get; set; }

    /// <summary>The engine's quoted-amount global (30014), for sources 19 and 21.</summary>
    public int QuotedAmount { get; set; }

    /// <summary>The engine's <c>RND(n)</c>: a value in <c>[0, n)</c>. Injected so the seeding is
    /// reproducible under test — the picker genuinely re-rolls on every dialog play, so a narration
    /// line can name a different companion each time it is shown, and that is faithful.</summary>
    public Func<int, int> Random { get; set; }

    /// <summary>The name for a member id, or empty when the id is out of range.</summary>
    public string NameOf(int actorId) =>
        actorId >= 0 && actorId < ActorNames.Count ? ActorNames[actorId] ?? "" : "";
}
