namespace GameData.Resources.Credits;

using System.Collections.Generic;

// CRED.DAT — the intro credits text, scrolled by ShowCredits (KRONDOR.EXE
// 0x40934) over the BLANK.SCX parchment. Entry [0] of the on-disk string table
// is the title; the remaining entries are consumed as (role, name) pairs that
// scroll up through the parchment window. See LoadCRED.DAT (0x40520) for the
// binary layout and scrollCredits (0x405f1) for the layout/easter-egg rules
// that the extractor folds into the flags below.
public class CreditsData : IResource {
    public CreditsData(string id) {
        Id = id;
    }

    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;

    // Centered title drawn above the scroll region ("CREDITS").
    public string Title { get; set; } = string.Empty;

    // Ordered list of scrolling lines (one per (role, name) pair).
    public List<CreditLine> Lines { get; set; } = [];
}

public class CreditLine {
    // Left column (heading), e.g. "PROGRAMMING:". Empty for a continuation line
    // (an additional name under the previous role) or a spacer. For a Centered
    // line this holds the single centered string.
    public string Role { get; set; } = string.Empty;

    // Right column (name), e.g. "Steve Cordon". Empty for a spacer or a Centered
    // line.
    public string Name { get; set; } = string.Empty;

    // Final closing block ("Based on the Midkemian Universe…"): a single string
    // centered rather than laid out in columns. scrollCredits centers pairs
    // whose left index exceeds count-5.
    public bool Centered { get; set; }

    // Hidden 'LOVELY LADY:' credit (role begins "LO"): scrollCredits collapses
    // it to zero height except on attract passes where cycle % 30 == 8.
    public bool RareReveal { get; set; }

    // 'Nels Bruckner' sparkle (name[0]=='N' && name[5]=='B'): rendered in a
    // per-frame random colour when cycle % 30 == 8 or the N key is held.
    public bool Sparkle { get; set; }
}
