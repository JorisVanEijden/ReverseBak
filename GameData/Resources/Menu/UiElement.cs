namespace GameData.Resources.Menu;

using GameData.Resources.Layout;

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
    public int? PressSound => (SoundFlags & 1) != 0 ? null : EffectiveSound;

    /// <summary>Sound played when the button is RELEASED, or null when silent on release.</summary>
    /// <inheritdoc cref="PressSound"/>
    public int? ReleaseSound => (SoundFlags & 2) != 0 ? null : EffectiveSound;

    private int EffectiveSound => ClickSound != 0 ? ClickSound : DefaultClickSoundId;
    public string Label { get; set; }
    public string LabelAlt { get; set; } // alternate label; only rendered by InputField when State == 0 (the toggle-off text)
    public string Field13 { get; set; } // resolved string at Field13Offset; not displayed by any renderer
    public LayoutHint Layout { get; set; } = new();
}
