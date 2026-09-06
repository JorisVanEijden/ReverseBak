namespace GameData.Resources.Dialog;

/// <summary>
/// Resolves a dialog record's <see cref="DialogEntry.ActorNumber"/>, which is a <b>sentinel</b> for
/// "whoever is speaking right now" as often as it is a literal actor id.
/// </summary>
/// <remarks>
/// <b>255 does not mean "nobody".</b> <c>ExecuteDialog</c> rewrites <c>record-&gt;wSpeaker_id</c> at
/// the top of its record loop, before anything is drawn and before the name is looked up
/// (canassa DIALOG.C:882-897):
/// <code>
/// if (id == 0xfe) id = gstate_event_read(0x7535) + 1;   /* the chapter's speaker */
/// if (id == 0xff)      id = partySpeaker;
/// else if (id == 0xfd) id = npcSpeaker;
/// else if (id >= 0xf0) { ...combat speaker slots... }
/// else if (id != 0 &amp;&amp; id &lt; 7) partySpeaker = id;   /* a party member LATCHES */
/// else if (id != 0)             npcSpeaker  = id;
/// </code>
/// so the lookup only ever sees a concrete id. Reading 255 literally is how the ask-about page lost
/// both its portrait and its "&lt;name&gt; asked about:" heading: the id fell past the name table and
/// answered nothing, and the absence looked deliberate enough to be documented as faithful.
///
/// <para><b>The two latches are why this is an object and not a function.</b> A record naming a
/// party member (1..6) or an NPC does not merely draw that speaker, it becomes the answer for every
/// later 0xff / 0xfd in the same conversation. <see cref="Begin"/> starts a conversation, matching
/// <c>partySpeaker = gstate_event_read(0x7535) + 1; npcSpeaker = 0;</c> at DIALOG.C:834.</para>
///
/// <para><b>Sentinel use in the shipped data</b>, over all 8203 entries of the 32 DDX files:
/// 0xff 221 times, 0xfe 15, 0xfd once, and 0xf0..0xf5 192 times. The first three are resolved here.
/// <see cref="IsCombatSpeakerSlot"/> is NOT: those index the combat speaker-kind table
/// (<c>g_speaker_kinds</c>, DIALOG.C:467) that this port has no equivalent of yet, and answering
/// them with a guess would caption the wrong character rather than none.</para>
/// </remarks>
public sealed class DialogSpeakerSentinel {
    /// <summary>"The party member currently speaking" — the running <see cref="PartySpeaker"/>.</summary>
    public const int CurrentPartySpeaker = 0xFF;

    /// <summary>"The chapter's designated speaker" — global 30005 + 1, and it latches.</summary>
    public const int ChapterSpeaker = 0xFE;

    /// <summary>"The NPC currently speaking" — the running <see cref="NpcSpeaker"/>.</summary>
    public const int CurrentNpcSpeaker = 0xFD;

    /// <summary>First of the combat speaker-kind slots (0xf0..0xfc), which this port cannot resolve.</summary>
    public const int FirstCombatSpeakerSlot = 0xF0;

    /// <summary>Ids 1..6 are the party roster; 7 and up are NPCs and keyword-table names.</summary>
    public const int LastPartyActorNumber = 6;

    /// <summary>The party member a later <see cref="CurrentPartySpeaker"/> resolves to.</summary>
    public int PartySpeaker { get; private set; }

    /// <summary>The NPC a later <see cref="CurrentNpcSpeaker"/> resolves to.</summary>
    public int NpcSpeaker { get; private set; }

    /// <summary>Whether <paramref name="speakerId"/> indexes the combat speaker-kind table.</summary>
    /// <remarks>
    /// Deliberately excludes the three named sentinels above it, which are tested first in the
    /// original's ladder and are not table indices.
    /// </remarks>
    public static bool IsCombatSpeakerSlot(int speakerId) =>
        speakerId >= FirstCombatSpeakerSlot && speakerId < CurrentNpcSpeaker;

    /// <summary>
    /// Starts a conversation: <paramref name="chapterSpeaker"/> is global 30005, whose +1 seeds the
    /// party latch.
    /// </summary>
    public void Begin(int chapterSpeaker) {
        PartySpeaker = chapterSpeaker + 1;
        NpcSpeaker = 0;
    }

    /// <summary>
    /// The concrete actor id this record speaks as, updating the latches exactly as the original's
    /// record loop does. 0 for a record with no speaker, and for an unresolved combat slot.
    /// </summary>
    public int Resolve(int rawActorNumber, int chapterSpeaker) {
        // The high byte is a portrait frame, not part of the id.
        int id = rawActorNumber & 0xFF;

        // NOT an else-if in the original either: 0xfe resolves to a concrete party id first and
        // then falls through the ladder below, which is what makes it latch like a literal 1..6.
        if (id == ChapterSpeaker) {
            id = chapterSpeaker + 1;
        }

        if (id == CurrentPartySpeaker) {
            return PartySpeaker;
        }

        if (id == CurrentNpcSpeaker) {
            return NpcSpeaker;
        }

        if (IsCombatSpeakerSlot(id)) {
            return 0;
        }

        if (id > 0 && id <= LastPartyActorNumber) {
            PartySpeaker = id;
        } else if (id > 0) {
            NpcSpeaker = id;
        }

        return id;
    }
}
