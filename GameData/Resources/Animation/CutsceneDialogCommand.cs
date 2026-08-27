namespace GameData.Resources.Animation;

/// <summary>
/// What a cutscene's <c>DialogCommand</c> asks for — <c>anim_show_dialog</c> @0x53df0.
/// </summary>
/// <remarks>
/// <b>The two fields are read together, not independently.</b> <c>Dialog16Id</c> alone does not say
/// what the command does: id 0 is a clear, a book-page animation or nothing at all depending on
/// <c>Arg2</c>. Splitting the pair across separate tests is how a port ends up clearing the dialog
/// plate when it meant to turn a page.
///
/// <para>These rules were spread across four Unity call sites that each re-derived the DDX key by
/// hand (<c>CutsceneFrameProcessor</c> twice, <c>DialogCommandExtensions</c> and
/// <c>DialogResourceLoader</c>), with the <c>1600000</c> base stated a fifth time in
/// <c>TtmExtractor</c>. They are game rules rather than rendering, so they live here where they can
/// be tested without standing up a cutscene.</para>
/// </remarks>
public static class CutsceneDialogCommand {
    /// <summary>What the command resolves to once both fields are read.</summary>
    public enum Kind {
        /// <summary>Nothing to do.</summary>
        None,

        /// <summary>Repaint the dialog plate and clear the text overlay.</summary>
        Clear,

        /// <summary>Advance the book-page turn animation by one step.</summary>
        BookAnimation,

        /// <summary>Show a DDX dialog entry.</summary>
        Display,
    }

    /// <summary>Added to <c>Dialog16Id</c> to reach the global DDX dialog-id catalog.</summary>
    public const int DialogIdBase = 1600000;

    /// <summary>Dialog ids per DDX file — the divisor that picks the <c>DIAL_Z##</c> number.</summary>
    public const int IdsPerFile = 100000;

    /// <summary>The <c>Arg2</c> that means "clear", rather than a book-animation step.</summary>
    public const int ClearArg = 255;

    /// <summary>Highest <c>Arg2</c> the book-page animation uses; the turn runs 0..20.</summary>
    public const int MaxBookStep = 20;

    // Arg2 values for a Display command. Named because three of the six wait for input and three do
    // not, and the split is not a range — see WaitsForInput.
    private const int NarrativeWaitInput = 0;
    private const int SelectFont = 1;
    private const int OpenBook = 2;
    private const int NarrativeAutoAdvance = 3;
    private const int Interactive = 4;
    private const int Simple = 5;

    /// <summary>What this command does.</summary>
    /// <remarks>
    /// <b><c>Dialog16Id == -1</c> is a draw command, not a dialog one.</b> It blits from image slot 2
    /// and is deliberately unimplemented; it must not fall through to the display arm, which would
    /// ask for dialog id 1599999.
    ///
    /// <para><b>The clear test comes before the book-step range</b>, and stays that way even though
    /// <see cref="ClearArg"/> sits outside 0..<see cref="MaxBookStep"/> today. The order is what
    /// keeps a later widening of the step range from silently swallowing the clear.</para>
    /// </remarks>
    public static Kind KindOf(int dialog16Id, int arg2) {
        if (dialog16Id == 0 && arg2 == ClearArg) {
            return Kind.Clear;
        }
        if (dialog16Id == -1) {
            return Kind.None;
        }
        if (dialog16Id == 0 && arg2 >= 0 && arg2 <= MaxBookStep) {
            return Kind.BookAnimation;
        }
        return dialog16Id > 0 ? Kind.Display : Kind.None;
    }

    /// <summary>The global dialog id a <see cref="Kind.Display"/> command names.</summary>
    public static int DialogIdFor(int dialog16Id) => dialog16Id + DialogIdBase;

    /// <summary>
    /// Which DDX file holds a dialog id. Takes the <b>full</b> id, not the command's field.
    /// </summary>
    /// <remarks>
    /// The distinction is the whole reason this is a separate method from
    /// <see cref="DialogIdFor"/>: one call site already holds a resolved id and must not add the
    /// base a second time, while three hold the raw field and must. Passing the wrong one lands in
    /// <c>DIAL_Z16</c> instead of <c>DIAL_Z32</c> and finds nothing.
    /// </remarks>
    public static string DdxKeyFor(int dialogId) =>
        $"DIAL_Z{dialogId / IdsPerFile:D2}.DDX";

    /// <summary>Whether the dialog waits for a keypress rather than advancing on its own.</summary>
    /// <remarks>
    /// <b>Not a range.</b> Of the six modes, 0 (narrative), 4 (interactive) and 5 (simple) wait; 1
    /// (select font), 2 (open book) and 3 (narrative auto-advance) do not. Written as
    /// <c>arg2 != 3</c> or <c>arg2 &gt;= 4</c> it would be wrong for two of the six, and both
    /// readings look tidy enough to survive review.
    /// </remarks>
    public static bool WaitsForInput(int arg2) => arg2 switch {
        NarrativeWaitInput => true,
        Interactive => true,
        Simple => true,
        _ => false,
    };
}
