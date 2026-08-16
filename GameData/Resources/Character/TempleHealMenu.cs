namespace GameData.Resources.Character;

/// <summary>
/// Driving the temple healing screen — what a cure costs, what it does, and where "next" goes.
/// Faithful port of the loop in <c>charscreen_temple_heal_menu</c> @0x5877e (ovr160), from 0x58846.
/// The gate in front of it is <see cref="TempleHealEntry"/>; the affliction pricing is
/// <see cref="TempleHeal"/>.
/// </summary>
public static class TempleHealMenu {
    /// <summary>The party cannot pay the quote.</summary>
    public const int CannotAffordDialog = 1300076;

    /// <summary>The cure itself — hours of chanting, and waking free of it.</summary>
    public const int CuredDialog = 1300078;

    /// <summary>Right-clicking a button explains it.</summary>
    public const int ButtonHelpDialog = 1300074;

    /// <summary>Right-clicking the character shows their description instead of button help.</summary>
    public const int CharacterDescriptionDialog = 105;

    /// <summary>
    /// The bill for one character.
    /// </summary>
    /// <param name="afflictionPrice">What <see cref="TempleHeal"/> charges for their conditions.</param>
    /// <param name="healthStaminaDeficit">How far below maximum their health-and-stamina sits.</param>
    /// <param name="mode">The service mode; see <see cref="TempleHealEntry.ModeThatTreatsWounds"/>.</param>
    /// <remarks>
    /// <b>The mode that treats wounds also bills for them</b>, at one royal per missing point, added
    /// straight onto the affliction price. That is the whole of the difference between the modes:
    /// not a discount or a surcharge but an extra line on the bill, which is also why that mode will
    /// see a merely-wounded party at all.
    /// </remarks>
    public static long BillFor(long afflictionPrice, int healthStaminaDeficit, int mode) =>
        mode == TempleHealEntry.ModeThatTreatsWounds
            ? afflictionPrice + healthStaminaDeficit
            : afflictionPrice;

    /// <summary>Conditions the cure touches — the whole table, from 0 to this bound exclusive.</summary>
    public const int ConditionCount = 7;

    /// <summary>
    /// The one beneficial condition. Never charged for, and <b>granted</b> by a cure rather than
    /// cleared.
    /// </summary>
    public const int HealingCondition = 4;

    /// <summary>Healing granted by an ordinary cure.</summary>
    public const int HealingGrantedByCure = 20;

    /// <summary>Healing granted when the service also mends wounds.</summary>
    public const int HealingGrantedByFullCure = 100;

    /// <summary>The amount that clears a condition outright.</summary>
    public const int ClearAmount = -100;

    /// <summary>
    /// What a cure applies to each condition.
    /// </summary>
    /// <remarks>
    /// <b>Everything is cleared except Healing, which is handed out.</b> The loop runs over all seven
    /// conditions with the same call and only the amount differs — so a port that "cures" by
    /// clearing the lot would strip the one condition the priest is there to give, and the character
    /// would walk out with no regeneration at all.
    /// </remarks>
    public static int CureAmountFor(int conditionIndex, int mode) {
        if (conditionIndex != HealingCondition) {
            return ClearAmount;
        }

        return mode == TempleHealEntry.ModeThatTreatsWounds
            ? HealingGrantedByFullCure
            : HealingGrantedByCure;
    }

    /// <summary>Whether the cure also restores health and stamina outright.</summary>
    /// <remarks>Applied as a saturating <c>0x7FFF</c>, so it is "to full" rather than a fixed amount.</remarks>
    public static bool RestoresHealth(int mode) => mode == TempleHealEntry.ModeThatTreatsWounds;

    // ---- moving between characters -------------------------------------------------------------

    /// <summary>
    /// Whether curing someone moves on by itself.
    /// </summary>
    /// <remarks>
    /// It does: the cure arm ends by setting the pending action to <see cref="TempleHealEntry.NextActionId"/>
    /// and falling into it. So paying for one character advances to the next who needs something,
    /// and the screen closes on its own once the last of them is done — the player never has to
    /// press Next or Done in the ordinary case.
    /// </remarks>
    public static bool CureAdvances => true;

    /// <summary>
    /// Where "next player" goes.
    /// </summary>
    /// <param name="current">The slot being shown.</param>
    /// <param name="partyCount">Active party size.</param>
    /// <param name="needsHealing">Whether the member in a slot prices above zero.</param>
    /// <returns>The next slot to show, or <paramref name="partyCount"/> to close the screen.</returns>
    /// <remarks>
    /// <b>It skips anyone who needs nothing.</b> Not "the next member" but "the next member with
    /// something to cure" — the scan advances while the price is zero. And it does not wrap: running
    /// off the end closes the screen, which together with <see cref="CureAdvances"/> is what makes
    /// healing a party a single pass rather than a tour.
    /// </remarks>
    public static int NextNeedy(int current, int partyCount, System.Func<int, bool> needsHealing) {
        int slot = current;
        do {
            slot++;
        }
        while (slot < partyCount && !needsHealing(slot));

        return slot;
    }

    /// <summary>Whether a "next" landed past the end, which closes the screen.</summary>
    public static bool ClosesAfter(int slot, int partyCount) => slot >= partyCount;

    /// <summary>
    /// The party slot an action id selects directly, or -1 when it selects none.
    /// </summary>
    /// <remarks>
    /// The screen's portrait row picks a member outright, alongside the sequential Next. Ignored
    /// during combat, where the caller is looking at one combatant and may not switch — see
    /// <see cref="SelectionIsLockedInCombat"/>.
    /// </remarks>
    public static int PartySlotForAction(int actionId) =>
        ActiveParty.SlotForAction(actionId, FirstPortraitActionId);

    /// <summary>Action id of the first portrait in the row.</summary>
    public const int FirstPortraitActionId = 2;

    /// <inheritdoc cref="ActiveParty.Slots"/>
    public const int MaxPortraits = ActiveParty.Slots;

    /// <summary>
    /// Whether switching character is refused while a fight is on.
    /// </summary>
    /// <remarks>
    /// Both the portrait row and Next are gated on it, so in combat the screen shows exactly one
    /// combatant and the only ways out are curing them or Done.
    /// </remarks>
    public static bool SelectionIsLockedInCombat => true;

    /// <summary>Which help a right-click asks for.</summary>
    /// <remarks>
    /// The character's own description for the portrait and the Next control, button help for
    /// everything else — so right-clicking the person tells you about the person.
    /// </remarks>
    public static int HelpDialogFor(int actionId) =>
        actionId == TempleHealEntry.NextActionId || actionId == PortraitActionId
            ? CharacterDescriptionDialog
            : ButtonHelpDialog;

    /// <summary>The attribute-sheet area that shares the character's description help.</summary>
    public const int PortraitActionId = 57;
}
