namespace GameData.Resources.Dialog;

/// <summary>
/// The rounded name plate under the speaker's portrait — <c>dialog_draw_speech_bubble</c>
/// (canassa DIALOG.C:364).
/// </summary>
/// <remarks>
/// <b>Three separate conditions gate it, and dropping any one of them shows it too often.</b> The
/// entry must name a speaker at all, the speaker id must be below
/// <see cref="MaxSpeakerId"/>, and the entry must carry
/// <see cref="DialogEntryFlags.PreserveKeyword"/> — the original looks the name up
/// unconditionally and then <b>throws it away</b> when that flag is clear
/// (<c>if ((record->wFlags &amp; 1) == 0) pSpeakerName = 0;</c>), and the draw routine no-ops on a
/// null or empty string. So the flag is what decides whether the plate appears, even though it
/// reads like a text-formatting flag.
///
/// <para><b>It is centred on the SCREEN, not on the portrait or the panel.</b> Both the plate and
/// its label are positioned from fixed screen x's, so a wider name grows symmetrically about the
/// same point instead of tracking whatever it labels.</para>
///
/// <para><b>The plate and the label are centred two px apart</b> (<see cref="CentreX"/> against
/// <see cref="LabelCentreX"/>). That is in the original and is not a rounding artefact of this
/// port — the label sits fractionally right of the plate's middle.</para>
/// </remarks>
public static class DialogSpeakerNamePill {
    /// <summary>Horizontal scale from original screen px to canonical px.</summary>
    private const int ScaleX = 5;

    /// <summary>Vertical scale from original screen px to canonical px.</summary>
    private const int ScaleY = 6;

    /// <summary>
    /// First speaker id that gets no plate.
    /// </summary>
    /// <remarks>
    /// <c>if (record->wSpeaker_id &lt; 0x46)</c>. Ids at or above it are still speakers with names —
    /// they just narrate without being captioned.
    /// </remarks>
    public const int MaxSpeakerId = 0x46;

    /// <summary>The flag that has to be set for the name to survive its lookup.</summary>
    public const DialogEntryFlags RequiredFlag = DialogEntryFlags.PreserveKeyword;

    /// <summary>Highest speaker id resolved from the party rather than the keyword table.</summary>
    /// <remarks>
    /// <c>askabout_name_or_keyword_lookup</c> splits at <c>id &lt; 7</c>: below it the name is read
    /// out of the character record, at or above it from the keyword table. Both halves are real —
    /// a port that only implements the table silently captions every companion with a topic word.
    /// </remarks>
    public const int MaxPartySpeakerId = 6;

    /// <summary>
    /// Offset from a speaker id to its keyword-table index, for ids past the party.
    /// </summary>
    /// <remarks>
    /// <c>(id + 0x124) * 2 + 2</c> against the table base. The <c>* 2 + 2</c> is the table's own
    /// addressing — a count word followed by 2-byte entries — and is the SAME shape the topic
    /// labels use (<c>(key - 1) * 2 + 2</c>), so both resolve against a plain 0-based index. The
    /// difference between the two is only which index they compute, which is why this is an offset
    /// and not a second lookup.
    /// </remarks>
    public const int KeywordTableOffset = 0x124;

    /// <summary>Whether the speaker's name comes from the party rather than the keyword table.</summary>
    public static bool IsPartySpeaker(int speakerId) =>
        speakerId > 0 && speakerId <= MaxPartySpeakerId;

    /// <summary>Party slot (0-based) a party speaker id names.</summary>
    public static int PartyIndexOf(int speakerId) => speakerId - 1;

    /// <summary>Keyword-table index a non-party speaker id names.</summary>
    public static int KeywordIndexOf(int speakerId) => speakerId + KeywordTableOffset;

    /// <summary>Lowest speaker id resolved from the keyword table.</summary>
    public const int FirstKeywordSpeakerId = MaxPartySpeakerId + 1;

    /// <summary>
    /// Highest speaker id the keyword table can answer for — <b>53, and the bound is real.</b>
    /// </summary>
    /// <remarks>
    /// <b>Established from the shipped KEYWORD.DAT 2026-08-30, and it is not a guess.</b> The file
    /// carries 346 entries, and <c>askabout_name_or_keyword_lookup</c> reads word
    /// <c>id + 0x124</c> of them — so the last id it can reach is <c>345 - 292 = 53</c>. That is
    /// not an accident of sizing: entries 295-298 are EMPTY and the name block starts exactly at
    /// 299, which is <c>FirstKeywordSpeakerId + KeywordTableOffset</c> — "Navon du Sandau" at 299
    /// through "Moredhel" at 345. The table's name block and the id range are the same 47 slots.
    ///
    /// <para><b>So a speaker id above this reads PAST the relocated offset array</b>, into the
    /// string bytes, and hands back a word that was never turned into a pointer. Our resolver looks
    /// the index up in a dictionary and answers null, which is why nothing has gone wrong here — but
    /// it is worth knowing that the original has no valid answer either, rather than assuming we are
    /// missing one.</para>
    /// </remarks>
    public const int LastKeywordSpeakerId = 53;

    /// <summary>
    /// Whether this speaker id names anybody at all.
    /// </summary>
    /// <remarks>
    /// The three arms of <c>askabout_name_or_keyword_lookup</c>: 0 answers null, 1..6 a party
    /// member, 7..53 a keyword-table name. <b>Everything above 53 answers nothing</b> — including
    /// 255, which the shipped DDX uses 221 times and which 19 of the 21 ChoiceMenu entries carry.
    ///
    /// <para>The name is thrown away again unless the entry sets <see cref="RequiredFlag"/>
    /// (DIALOG.C:968), which is why an unresolvable id is harmless on most of those 221 and not on
    /// the ChoiceMenu ones, where it is the "&lt;name&gt; asked about:" heading that goes missing.
    /// Two of the twenty-one resolve, and both are party members.</para>
    /// </remarks>
    public static bool ResolvesToAName(int speakerId) =>
        IsPartySpeaker(speakerId)
        || (speakerId >= FirstKeywordSpeakerId && speakerId <= LastKeywordSpeakerId);

    /// <summary>Whether this entry captions its speaker.</summary>
    public static bool ShowsFor(int speakerId, DialogEntryFlags flags) =>
        speakerId > 0 && speakerId < MaxSpeakerId && (flags & RequiredFlag) != 0;

    /// <summary>Whether this entry captions its speaker.</summary>
    public static bool ShowsFor(DialogEntry entry) =>
        entry != null && ShowsFor(entry.ActorNumber & 0xFF, entry.Flags);

    /// <summary>Narrowest the plate is allowed to get, in canonical px.</summary>
    /// <remarks>0x37 original px — a short name gets a plate no smaller than this.</remarks>
    public const int MinWidth = 0x37 * ScaleX;

    /// <summary>Total slack around the label, in canonical px.</summary>
    public const int LabelPadding = 10 * ScaleX;

    /// <summary>Radius of the plate's rounded ends, in canonical px.</summary>
    /// <remarks>
    /// The original draws two r=7 circles centred ON the bar's ends, so they overhang it by a full
    /// radius either side — see <see cref="OuterWidth"/>.
    /// </remarks>
    public const int CapRadius = 7 * ScaleX;

    /// <summary>Screen x the plate is centred on, in canonical px.</summary>
    public const int CentreX = 0x9E * ScaleX;

    /// <summary>Screen x the LABEL is centred on, in canonical px — two px right of the plate.</summary>
    public const int LabelCentreX = 0xA0 * ScaleX;

    /// <summary>Top edge, in canonical px.</summary>
    public const int Top = 0x69 * ScaleY;

    /// <summary>Bottom edge, in canonical px.</summary>
    /// <remarks>
    /// From the lower rule at <c>y + 0x77</c>, not from the filled bar's height: the bar is drawn
    /// 14 px tall from 0x69 and the rule that closes the plate is one px below its last row.
    /// </remarks>
    public const int Bottom = 0x77 * ScaleY;

    /// <summary>Height, in canonical px.</summary>
    public const int Height = Bottom - Top;

    /// <summary>Baseline the label is drawn from, in canonical px.</summary>
    public const int LabelTop = 0x6C * ScaleY;

    /// <summary>How far the drop shadow is offset, in canonical px.</summary>
    /// <remarks>
    /// One px right and one px down in the original — drawn as a separate pass in
    /// <see cref="ShadowPen"/> beneath both the plate and the label.
    /// </remarks>
    public const int ShadowOffsetX = 1 * ScaleX;

    /// <summary>How far the drop shadow is offset downwards, in canonical px.</summary>
    public const int ShadowOffsetY = 1 * ScaleY;

    /// <summary>Pen the plate is filled with.</summary>
    public const int FillPen = 0x0B;

    /// <summary>Pen the plate's outline and its top and bottom rules are drawn in.</summary>
    public const int EdgePen = 0x0F;

    /// <summary>Pen both drop shadows are drawn in.</summary>
    public const int ShadowPen = 1;

    /// <summary>Pen the label is drawn in.</summary>
    public const int LabelPen = 0x0A;

    /// <summary>
    /// Width of the plate's straight middle for a label of <paramref name="labelWidth"/> canonical
    /// px.
    /// </summary>
    public static int BarWidth(float labelWidth) {
        int padded = (int)(labelWidth + LabelPadding);
        return padded < MinWidth ? MinWidth : padded;
    }

    /// <summary>Full width including both rounded ends, in canonical px.</summary>
    /// <remarks>
    /// <b>Wider than <see cref="BarWidth"/> by a diameter, not by nothing.</b> The caps are centred
    /// on the bar's ends rather than tucked inside it, so each adds a full radius. Sizing a
    /// rounded rectangle to the bar width alone draws a plate two radii too narrow.
    /// </remarks>
    public static int OuterWidth(float labelWidth) => BarWidth(labelWidth) + (2 * CapRadius);

    /// <summary>Left edge of the plate, in canonical px.</summary>
    public static int Left(float labelWidth) => CentreX - (OuterWidth(labelWidth) / 2);

    /// <summary>Left edge of the label, in canonical px.</summary>
    public static int LabelLeft(float labelWidth) => LabelCentreX - (int)(labelWidth / 2);
}
