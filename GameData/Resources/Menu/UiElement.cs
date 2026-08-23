namespace GameData.Resources.Menu;

using GameData.Resources.Layout;
using System.Text.Json.Serialization;

[Serializable]
public class UiElement {
    public ElementType ElementType { get; set; }
    public int ActionId { get; set; }
    public bool Visible { get; set; } // 0 = skipped by menu_drawEntry? (hit-test still runs). Hit-only zones backed by an SCX background, e.g. CONTENTS chapter rows, set this 0.
    public int Disabled { get; set; } // 0 = interactive, non-zero = disabled (sub_seg030_97F skips hit-test; menu_type_6_8 dims text; menu_type_3_4 swaps to icon 0x32). FilePicker runtime-overload: the scrollable item count (always 0 in shipped files; the engine sets it when populating).
    public int State { get; set; } // current widget state: Toggle on/off; InputField alt-label switch (state==0 ⇒ LabelAlt); FilePicker scroll position; Preferences value
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public short Field13Offset { get; set; } // string offset; parser fixes it up and the deallocator reads it to free the string buffer, but no rendering or hit-test code reads it — vestigial as a UI field
    public short LabelOffset { get; set; }
    public short LabelAltOffset { get; set; }
    public int IconBase { get; set; } // base bicons index; renderer uses base..base+3 for state/hover variants
    public int Cursor { get; set; } // mouse cursor image id displayed when hovering this element
    public int SoundFlags { get; set; } // bit 0 = suppress press-down sound, bit 1 = suppress click-release sound
    public int ClickSound { get; set; } // custom sound id played on click (overrides default "pound" click); 0 = use default

    // JsonIgnore on the resolved pair below: they are COMPUTED from SoundFlags + ClickSound, both
    // of which are serialized. Emitting them too would put a value in an override file that editing
    // cannot change — the model recomputes on load — which is worse than omitting it. Contrast
    // ZoneTable.TextureBitmap, a resolved value that IS serialized because it is settable and needs
    // context the entry does not carry.
    /// <summary>The two BICONS files every REQ icon comes from.</summary>
    public const string IconFileEven = "BICONS1.BMX";

    /// <inheritdoc cref="IconFileEven"/>
    public const string IconFileOdd = "BICONS2.BMX";

    /// <summary>
    /// The index above which the original bumps by one before splitting the icon index.
    /// </summary>
    public const int IconBumpThreshold = 50;

    /// <summary>
    /// Resolves a combined icon index (an <see cref="IconBase"/> plus the renderer's state offset)
    /// to the resource key of the actual bitmap.
    /// </summary>
    /// <remarks>
    /// <c>sub_seg029_A9</c> @0x2b579: indices above <see cref="IconBumpThreshold"/> are bumped by
    /// one BEFORE the parity test; even goes to <see cref="IconFileEven"/> and odd to
    /// <see cref="IconFileOdd"/>; the sub-image is the index halved.
    ///
    /// <para><b>The offset must be added BEFORE this, never after.</b> The bump makes the mapping
    /// discontinuous, so there is no "base key plus N" a consumer could apply: at base 49 the run is
    /// BICONS2#24, BICONS1#25, BICONS1#26, BICONS2#26 — the file alternation breaks and a sub-image
    /// is skipped. That is why this takes the combined index rather than returning a base key.</para>
    ///
    /// <para><b>How many states an element has is the CONSUMER's business.</b> Toggle and
    /// ImageButton use four (+0 on, +1 on-hovered, +2 off, +3 off-hovered — <c>menu_type_3_4</c>
    /// @0x2b898); ClickArea, InputField and TextLink use two (+0, +1 hovered) and only when
    /// <see cref="State"/> is non-zero (<c>menu_type_0_1_5</c> @0x2b92a), which no shipped element
    /// sets; TextButton reads no icon at all, so its <see cref="IconBase"/> is vestigial.</para>
    ///
    /// <para>A method rather than a property, so it stays out of the serialized surface: it is a
    /// pure function of <see cref="IconBase"/>, which is already emitted.</para>
    /// </remarks>
    public static string IconKeyForCombined(int combined) {
        int index = combined > IconBumpThreshold ? combined + 1 : combined;
        string file = (index & 1) == 0 ? IconFileEven : IconFileOdd;
        return $"{file}#{index >> 1}";
    }

    /// <summary>The default click cue — <c>sound_pound</c>, what an element with no custom sound plays.</summary>
    public const int DefaultClickSoundId = 83;

    /// <summary>
    /// Sound played when the button goes DOWN, or null when this element is silent on press.
    /// </summary>
    /// <remarks>
    /// <b>The original plays TWO cues, gated separately</b> — <c>menu_resolveHoverAndClick</c>
    /// @0x2c97f tests <c>SoundFlags</c> bit 0 before the press cue and bit 1 before the release
    /// cue, and each falls back to <see cref="DefaultClickSoundId"/> when
    /// <see cref="ClickSound"/> is 0. Resolved here so a consumer never has to know which bit gates
    /// which edge, nor that 0 means "the default" rather than "silent".
    ///
    /// <para><b>Whether to play both is the consumer's decision, not this model's.</b> Our Unity UI
    /// deliberately collapses them to a single select cue; that is a port choice about presentation
    /// and it stays there. This pair says what the original would play.</para>
    /// </remarks>
    [JsonIgnore]
    public int? PressSound => (SoundFlags & 1) != 0 ? null : EffectiveSound;

    /// <summary>Sound played when the button is RELEASED, or null when silent on release.</summary>
    /// <inheritdoc cref="PressSound"/>
    [JsonIgnore]
    public int? ReleaseSound => (SoundFlags & 2) != 0 ? null : EffectiveSound;

    [JsonIgnore]
    private int EffectiveSound => ClickSound != 0 ? ClickSound : DefaultClickSoundId;
    public string Label { get; set; }
    public string LabelAlt { get; set; } // alternate label; only rendered by InputField when State == 0 (the toggle-off text)
    public string Field13 { get; set; } // resolved string at Field13Offset; not displayed by any renderer
    public LayoutHint Layout { get; set; } = new();
}
