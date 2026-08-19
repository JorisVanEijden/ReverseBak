namespace GameData.Resources.Scene;

/// <summary>
/// What a left click on a hotspot actually does — the action-code arms of <c>GDS_RunScene</c>
/// (ovr149 @0x4de9d), read from the dispatch at 0x4e367.
///
/// <para><see cref="GdsSceneInteraction"/> decides <i>whether</i> a click is an action and whether
/// the hotspot's dialog runs first; this is the table that runs afterwards.</para>
/// </summary>
public static class GdsActionDispatch {
    /// <summary>What an action code opens or does.</summary>
    public enum ActionKind {
        /// <summary>Nothing beyond the dialog that may already have been shown.</summary>
        DialogOnly,

        /// <summary>Move to another sub-scene of this location.</summary>
        SubScene,

        /// <summary>Hand the location's container to the party.</summary>
        Container,

        /// <summary>Rest at an inn.</summary>
        Inn,

        /// <summary>Perform for the house — the barding minigame.</summary>
        Barding,

        /// <summary>The buy/repair screen.</summary>
        ShopScreen,

        /// <summary>The teleport destination menu.</summary>
        Teleport,

        /// <summary>The shop's service menu, which loops until dismissed.</summary>
        ShopServices,

        /// <summary>End the chapter.</summary>
        EndChapter,

        /// <summary>Not a code this scene loop handles.</summary>
        Unhandled,
    }

    /// <summary>
    /// The arm an action code takes.
    /// </summary>
    /// <remarks>
    /// The original tests the code with a run of independent <c>if</c>s rather than a switch, so the
    /// order here is presentational only — no code reaches two arms.
    /// </remarks>
    public static ActionKind KindOf(int actionCode) {
        switch (actionCode) {
            case 2: return ActionKind.DialogOnly;
            case 3:
            case 4: return ActionKind.SubScene;
            // *** THREE CODES, ONE BEHAVIOUR. *** 5, 6 and 8 are byte-for-byte the same arm
            // (0x4e406, 0x4e367, 0x4e39d): the same call with the same arguments, the same backdrop
            // reload and the same two flags. Whatever they meant to the authors, the engine does
            // not tell them apart, so neither does this.
            case 5:
            case 6:
            case 8: return ActionKind.Container;
            case 7: return ActionKind.Inn;
            case 9: return ActionKind.Barding;
            case 16: return ActionKind.ShopScreen;
            case 11: return ActionKind.Teleport;
            case 13: return ActionKind.ShopServices;
            case 15: return ActionKind.EndChapter;
            default: return ActionKind.Unhandled;
        }
    }

    /// <summary>
    /// <b>Action code 10 is dead — the test for it is discarded.</b>
    /// </summary>
    /// <remarks>
    /// The shop arm reads <c>cmp di, 0Ah</c> immediately followed by <c>cmp di, 10h</c>, and only
    /// then branches. The second compare overwrites the first one's flags, so nothing can act on the
    /// <c>== 10</c> result and only 16 enters the body. A correctly compiled <c>if (di == 10 || di ==
    /// 16)</c> would have a <c>jz</c> to the body between them; it is missing.
    ///
    /// <para><b>No shipped hotspot uses it.</b> Across all 118 scenes the codes that appear are 2,
    /// 3, 4, 5, 6, 7, 8, 9, 11, 13, 15 and 16 — 10 is absent — so the defect cannot be triggered by
    /// clicking anything. The one remaining route in is
    /// <see cref="GdsSceneRules.OutcomeFor"/>, which maps a dialog result of <c>-5</c> onto code 10;
    /// whether any GDS dialog actually returns -5 is <b>not</b> established here.</para>
    ///
    /// <para>Recorded rather than repaired: <see cref="KindOf"/> maps 10 to
    /// <see cref="ActionKind.Unhandled"/>, matching what the original does rather than what it reads
    /// as having meant.</para>
    /// </remarks>
    public static bool ActionCode10IsDead => true;

    /// <summary>
    /// <b>A failed barding turns into a sub-scene transition.</b>
    /// </summary>
    /// <param name="bardingSucceeded">What the barding routine returned.</param>
    /// <returns>The code that actually runs.</returns>
    /// <remarks>
    /// The barding arm rewrites the action to 3 when the routine returns zero, so failing to perform
    /// does not simply do nothing — it walks the party out through the scene's own transition. A port
    /// that treats failure as a no-op leaves them standing in a room the original would have moved
    /// them out of.
    /// </remarks>
    public static int ActionAfterBarding(bool bardingSucceeded) => bardingSucceeded ? 9 : 3;

    /// <summary>
    /// Which sub-scene a transition goes to.
    /// </summary>
    /// <param name="actionCode">3 or 4.</param>
    /// <param name="sceneNextLetter">The scene's own next-letter field.</param>
    /// <param name="hotspotNextLetter">The hotspot's next-letter field.</param>
    /// <remarks>
    /// <b>The two transition codes read the letter from different places.</b> Code 3 takes the
    /// <i>scene's</i> next letter — one destination shared by every hotspot that uses it — and code 4
    /// takes the <i>hotspot's</i> own. Reading one field for both collapses every exit in a location
    /// onto the same destination.
    /// </remarks>
    public static int TransitionLetter(int actionCode, int sceneNextLetter, int hotspotNextLetter) =>
        actionCode == 3 ? sceneNextLetter : hotspotNextLetter;

    /// <summary>
    /// Whether a transition leaves the location entirely rather than moving within it.
    /// </summary>
    /// <remarks>
    /// A letter of zero or less ends the scene loop. That is how a location's exit hotspot is
    /// authored — not a distinct action code, just a transition with no destination.
    /// </remarks>
    public static bool TransitionLeavesTheLocation(int letter) => letter <= 0;

    /// <summary>
    /// <b>A transition replays the scene's transition animation and drops both palettes.</b>
    /// </summary>
    /// <remarks>
    /// Codes 3 and 4 play the scene's transition animation and then clear the current palette
    /// <i>and</i> the animation palette — two separate pointers, both zeroed, so whatever draws next
    /// must establish its own.
    /// </remarks>
    public static bool TransitionPlaysTheTransitionAnimation => true;

    // ---------------------------------------------------------------- the visit counter

    /// <summary>The value the per-hotspot counter stops at.</summary>
    public const int VisitCountCap = 100;

    /// <summary>
    /// The hotspot's visit count after another action on it.
    /// </summary>
    /// <remarks>
    /// <b>Every hotspot counts how often it has been used, and the count saturates.</b> The dispatch
    /// adds one while the count is below <see cref="VisitCountCap"/> and adds zero at or above it, so
    /// it climbs to 100 and stops rather than wrapping the byte.
    ///
    /// <para>It is not bookkeeping: the count is published to a global <b>before</b> the hotspot's
    /// dialog runs (see <see cref="VisitCountIsPublishedBeforeTheDialog"/>), so dialogs can and do
    /// branch on whether this is the player's first visit. Dropping it makes those branches always
    /// take the first-time arm.</para>
    /// </remarks>
    public static int NextVisitCount(int current) =>
        current < VisitCountCap ? current + 1 : current;

    /// <summary>
    /// <b>The visit count reaches the dialog through a global, not an argument.</b>
    /// </summary>
    /// <remarks>
    /// Written sign-extended into the same global the shop arm later overwrites with the shop type,
    /// so its lifetime is one action. A port that sets it late, or not at all, changes which branch
    /// the hotspot's dialog takes.
    /// </remarks>
    public static bool VisitCountIsPublishedBeforeTheDialog => true;

    // ---------------------------------------------------------------- the inn

    /// <summary>The location whose nightly rate is hard-coded rather than taken from its container.</summary>
    public const int ScriptedInnSceneNumber = 62;

    /// <inheritdoc cref="ScriptedInnSceneNumber"/>
    public const int ScriptedInnSceneLetter = 5;

    /// <summary>The global that decides that inn's rate.</summary>
    public const int ScriptedInnFlag = 56092;

    /// <summary>Rate once the flag is set.</summary>
    public const int ScriptedInnDiscountedRate = 10;

    /// <summary>Rate while it is clear.</summary>
    public const int ScriptedInnStandardRate = 72;

    /// <summary>
    /// The nightly rate to write into a container before resting, or null to leave it alone.
    /// </summary>
    /// <remarks>
    /// <b>One inn's price is a story flag, not data.</b> The rest arm overwrites the stored cost for
    /// exactly one location — checked on the scene number and letter together — and leaves every
    /// other inn's container value untouched. It cannot be read out of the files, which is why it
    /// lives here.
    /// </remarks>
    public static int? ScriptedInnRate(int sceneNumber, int sceneLetter, bool flagSet) {
        if (sceneNumber != ScriptedInnSceneNumber || sceneLetter != ScriptedInnSceneLetter) {
            return null;
        }
        return flagSet ? ScriptedInnDiscountedRate : ScriptedInnStandardRate;
    }

    // ---------------------------------------------------------------- temple services

    /// <summary>The result that ends the service loop.</summary>
    public const int ServiceMenuExitResult = 3;

    /// <summary>The dialog result that opens the healing screen.</summary>
    /// <remarks>
    /// <c>charscreen_temple_heal_menu</c> @0x5877e, called with the container's
    /// <c>ShopkeeperSkill</c> as its price percentage and <c>ShopType</c> as its mode (0x4e599).
    ///
    /// <para><b>These numbers are branch POSITIONS, not keyword flags.</b> The service dialog
    /// carries no <c>SetReturnValue</c>, so what comes back is the choice menu's own answer — the
    /// index of the option clicked (<c>ShowDialogChoiceMenu</c> @0x4b54c). In the shipped temple
    /// dialog those options run Talk, Cure, Bless, Done, which is what makes 1 the heal and 3 the
    /// way out. Reading them as the flags those options carry (269, 272, 271, 268) matches nothing
    /// and quietly ends the loop on the first click.</para>
    /// </remarks>
    public const int HealingService = 1;

    /// <summary>The dialog result that opens the weapon blessing.</summary>
    /// <remarks>
    /// @0x4f589, called with <c>ShopType</c>, <c>MarkupPercentage</c>, <c>MaxHagglingDiscount</c>
    /// and <c>MarkDownPercentage</c> (0x4e5c6) — which for a temple are the unused id, the flat fee,
    /// the price percentage and the blessing tier. See <c>TempleBlessing</c>.
    /// </remarks>
    public const int BlessingService = 2;

    /// <summary>
    /// <b>The service menu is a loop, not one screen.</b>
    /// </summary>
    /// <param name="dialogResult">What the service dialog returned.</param>
    /// <returns>False once the loop ends.</returns>
    /// <remarks>
    /// The arm re-shows the hotspot's own dialog until it stops naming a service, running the heal
    /// for a result of 1 and the blessing for 2 and looping after either. So a player buys several
    /// services in a visit without the location redrawing between them — a port that shows the
    /// dialog once makes every service a separate trip.
    ///
    /// <para><b>This asks "did it name a service", where the original asks "was it not 3".</b> The
    /// two agree on every result the shipped dialogs produce, which is 1, 2 or 3. They differ on
    /// results the original never sees — and a port has one the original does not: a dialog that
    /// resolves to nothing at all. Looping on that would re-show the dialog that just failed to
    /// resolve, forever, with no way out.</para>
    /// </remarks>
    public static bool ServiceMenuContinues(int dialogResult) =>
        dialogResult == HealingService || dialogResult == BlessingService;

    // ---------------------------------------------------------------- returning to the location

    /// <summary>
    /// Whether the location redraws itself after the action.
    /// </summary>
    /// <param name="staysInTheScene">
    /// The action set the stay flag — every arm except a transition does.
    /// </param>
    /// <remarks>
    /// The loop tail replays the scene's <b>idle</b> animation and re-renders the scene's own dialog
    /// text, which is what puts the location back after a shop or a description. A transition clears
    /// the flag instead, because the scene it would redraw is the one being left.
    /// </remarks>
    public static bool RedrawsTheLocationAfterwards(bool staysInTheScene) => staysInTheScene;

    /// <summary>
    /// Whether returning fades out and clears first.
    /// </summary>
    /// <param name="showedAFullScreen">
    /// The action opened a screen of its own — the container, the inn and the shop arms all set this.
    /// </param>
    /// <remarks>
    /// Only the arms that took over the display fade and clear before the location is redrawn;
    /// something that merely spoke redraws straight over itself. Fading unconditionally puts a black
    /// flash after every click.
    /// </remarks>
    public static bool FadesBeforeRedraw(bool showedAFullScreen) => showedAFullScreen;
}
