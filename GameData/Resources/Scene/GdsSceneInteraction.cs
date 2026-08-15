namespace GameData.Resources.Scene;

/// <summary>
/// What a click on a location does before any action code runs — the click-routing and examine
/// arms of <c>GDS_RunScene</c> (ovr149 @0x4de9d).
///
/// <para>The action-code table itself is <see cref="GdsHotspot.ActionCode"/>'s business; this is
/// the layer above it, and it is where a port gets the mouse wrong rather than the outcome.</para>
/// </summary>
public static class GdsSceneInteraction {
    /// <summary>Which arm a click on a location takes.</summary>
    public enum Click {
        /// <summary>Not a hotspot — the poll returned a plain menu action.</summary>
        NotAHotspot,

        /// <summary>Left button: run the hotspot's action.</summary>
        Act,

        /// <summary>Right button: describe the hotspot.</summary>
        Examine,
    }

    /// <summary>
    /// Routes a polled action id and mouse button.
    /// </summary>
    /// <param name="actionId">What the input poll returned.</param>
    /// <param name="rightButton">The click was the secondary button.</param>
    /// <remarks>
    /// <b>Right-click is examine and it is the only way to look at anything.</b> The scene shares one
    /// input poll with every REQ menu and separates the two arms purely on which button came back,
    /// so a port that wires locations to left-click alone silently deletes every description in the
    /// game — no error, just a right button that does nothing.
    ///
    /// <para>Ids below <see cref="GdsSceneRules.HotspotActionIdBase"/> are not hotspots at all;
    /// they leave this loop.</para>
    /// </remarks>
    public static Click ClickFor(int actionId, bool rightButton) {
        if (actionId < GdsSceneRules.HotspotActionIdBase) {
            return Click.NotAHotspot;
        }
        return rightButton ? Click.Examine : Click.Act;
    }

    /// <summary>The hotspot a polled action id refers to, or -1.</summary>
    public static int HotspotIndexFor(int actionId) =>
        actionId < GdsSceneRules.HotspotActionIdBase ? -1 : actionId - GdsSceneRules.HotspotActionIdBase;

    /// <summary>
    /// Whether a right-click produces anything at all.
    /// </summary>
    /// <remarks>
    /// A hotspot with no examine dialog falls straight back to the poll — it is not an error and
    /// there is no "nothing to see here" message. So examine being silent on some hotspots is
    /// faithful, not a missing string.
    /// </remarks>
    public static bool HasExamine(GdsHotspot hotspot) => hotspot != null && hotspot.ExamineDialogId != 0;

    /// <summary>How an examine description is presented.</summary>
    public enum ExamineStyle {
        /// <summary>Drawn into the scene itself.</summary>
        InScene,

        /// <summary>Opened as the ordinary dialog window.</summary>
        DialogWindow,
    }

    /// <summary>The dialog type that always opens the dialog window.</summary>
    public const int WindowedDialogType = 6;

    /// <summary>
    /// Which presentation an examine dialog gets.
    /// </summary>
    /// <param name="dialogType">The loaded dialog's type byte.</param>
    /// <param name="branchCount">How many branches it has.</param>
    /// <remarks>
    /// <b>Examine has two presentations, not one.</b> A type-6 dialog, or any dialog with branches,
    /// opens the full dialog window; everything else is drawn straight into the scene. Sending all
    /// of them through one path is wrong in whichever direction it is taken — a window for every
    /// glance at a signpost, or a branching conversation rendered as flat text with no way to answer.
    /// </remarks>
    public static ExamineStyle ExamineStyleFor(int dialogType, int branchCount) =>
        dialogType == WindowedDialogType || branchCount != 0
            ? ExamineStyle.DialogWindow
            : ExamineStyle.InScene;

    /// <summary>
    /// <b>The in-scene presentation invalidates the palette; the windowed one does not.</b>
    /// </summary>
    /// <remarks>
    /// Only the in-scene arm clears the current-palette pointer, so whatever draws next must reload
    /// it. Distinct from <see cref="GdsSceneRules.InvalidatesPalette"/>, which is the same effect
    /// reached through a dialog <i>result</i> — two independent routes to the same reload.
    /// </remarks>
    public static bool ExamineInvalidatesPalette(ExamineStyle style) => style == ExamineStyle.InScene;

    /// <summary>
    /// <b>A shop's repair categories are published to a global before its examine dialog runs.</b>
    /// </summary>
    /// <remarks>
    /// When the location has a container, the examine arm reads the shop block's repair-category
    /// mask into a global first, so the description can branch on what this shop will mend. A port
    /// that shows the dialog without setting it gets whichever text the previous shop left behind.
    /// </remarks>
    public static bool ExaminePublishesRepairCategories => true;

    /// <summary>
    /// The action code that runs <b>without</b> showing the hotspot's action dialog first.
    /// </summary>
    /// <remarks>
    /// Every other code with an action dialog shows it and lets its result override the code (see
    /// <see cref="GdsSceneRules.OutcomeFor"/>). Thirteen is exempt because it picks its own dialog
    /// from the shop's services — showing the hotspot's first would ask the question twice.
    /// </remarks>
    public const int ActionCodeThatSkipsItsDialog = 13;

    /// <summary>
    /// Whether the hotspot's action dialog is shown before the action code is dispatched.
    /// </summary>
    /// <param name="hotspot">The clicked hotspot.</param>
    public static bool ShowsActionDialogFirst(GdsHotspot hotspot) =>
        hotspot != null
        && hotspot.ActionDialogId != 0
        && hotspot.ActionCode != ActionCodeThatSkipsItsDialog;

    /// <summary>
    /// <b>The action code is a signed byte.</b>
    /// </summary>
    /// <remarks>
    /// Sign-extended on load (<c>cbw</c>), so a code above 127 would arrive negative and match none
    /// of the dispatch arms. No shipped scene relies on it — the codes in use are 2..16 — but it
    /// fixes the width, which is what a port needs to agree on.
    /// </remarks>
    public static int NormalizeActionCode(int rawByte) {
        int b = rawByte & 0xFF;
        return b > 127 ? b - 256 : b;
    }
}
