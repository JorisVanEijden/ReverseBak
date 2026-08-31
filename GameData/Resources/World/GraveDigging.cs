namespace GameData.Resources.World;

/// <summary>
/// Digging a grave — <c>handle_Grave</c> (ovr190 @0x77ca9).
/// </summary>
/// <remarks>
/// <b>A grave is a CONTAINER AT A LOCATION, not a world object with its own state.</b> The handler
/// looks one up by the item's world position (<c>GetContainerAtLocation(zone, x, y)</c>) and does
/// nothing at all unless it finds one of type <c>fixedWorldItem</c> carrying dialog data. So the
/// grave's behaviour lives in the zone's container table, and a port that hangs it off the clicked
/// entity finds nothing to read.
/// </remarks>
/// <remarks>
/// <b>CONSUMED since 2026-08-31.</b> <c>GraveInteractionHandler</c> runs these rules on a real
/// click, and <c>InteractionProfileTable</c> carries the <c>("grave", empty profile)</c> row that
/// names the behaviour. The trap arm goes through <c>HotspotService.FireTrapEncounterAt</c>.
/// </remarks>
public static class GraveDigging {
    /// <summary>The tool the party must be carrying, and which is spent.</summary>
    /// <remarks>
    /// <b>*** 83, NOT 30. ***</b> This read <c>0x1e</c> until 2026-08-31, which is the <b>Light
    /// Crossbow</b> — a grave built on it would have refused to dig for want of a bow. The push at
    /// <c>handle_Grave</c> +0x14B assembles as <c>6A 53</c>, so the immediate is <c>0x53</c> = 83,
    /// and object 83 in the shipped OBJINFO.DAT is named "Shovel". IDA renders it through an enum
    /// member also called <c>Shovel</c>, which is why the disassembly reads correctly either way and
    /// the wrong number could sit here unnoticed.
    ///
    /// <para>Counted across the party with <see cref="Inventory.InventoryQuery.AnyHolds"/>, which
    /// already exists — the grave does not need a query of its own.</para>
    /// </remarks>
    public const int ShovelObjectId = 0x53;

    /// <summary>Shown when the grave has no dialog of its own, and when a trap spawn comes back empty.</summary>
    public const int NothingHereDialog = 154;

    /// <summary>Shown for a SECONDARY click: reading the tombstone.</summary>
    /// <remarks>"Not ordinarily given to morbid moods, @4 allowed himself to study the tombstone".</remarks>
    public const int ExamineDialog = 173;

    /// <summary>Shown when nobody carries a shovel.</summary>
    /// <remarks>
    /// "…I'm not too comfortable with the idea of accidentally digging up dead people! Besides, we
    /// need a shovel. We'd ruin our swords digging."
    /// </remarks>
    public const int NoShovelDialog = 66;

    /// <summary>Dug it up and the coffin was empty.</summary>
    /// <remarks>"…\"That's strange,\" @0 said. \"No body.\""</remarks>
    public const int EmptyCoffinDialog = 67;

    /// <summary>Dug it up and there was only a corpse.</summary>
    /// <remarks>"…\"Just a body,\" he gagged."</remarks>
    public const int JustABodyDialog = 68;

    /// <summary>What the dialog-data flags say is under the grave.</summary>
    /// <remarks>
    /// <b>These three bits are also what makes a grave DIGGABLE at all.</b> The handler tests
    /// <c>flags &amp; 2</c>, <c>&amp; 4</c> and <c>&amp; 8</c> together up front and takes the
    /// examine-only path when none is set — so a grave with no outcome bit cannot be dug, whatever
    /// else it carries.
    /// </remarks>
    [System.Flags]
    public enum Contents {
        None = 0,

        /// <summary>Bit 1 — the container's own contents; the handler opens it.</summary>
        Loot = 2,

        /// <summary>Bit 2 — a corpse and nothing else (<see cref="JustABodyDialog"/>).</summary>
        Body = 4,

        /// <summary>Bit 3 — an empty box (<see cref="EmptyCoffinDialog"/>).</summary>
        Empty = 8,
    }

    /// <summary>Whether the grave can be dug at all, or only read.</summary>
    public static bool IsDiggable(int dialogFlags) =>
        (dialogFlags & (int)(Contents.Loot | Contents.Body | Contents.Empty)) != 0;

    /// <summary>
    /// What a completed dig produces — <b>tested in bit order, first match wins</b>.
    /// </summary>
    /// <remarks>
    /// The handler is a chain: <c>if (flags &amp; 2) open the container; else if (flags &amp; 4)
    /// dialog 68; else dialog 67</c>. So a grave flagged Loot AND Body opens and never says a word
    /// about the body, and the final arm is an <c>else</c> rather than a test of bit 3 — anything
    /// diggable that is not Loot or Body reads as an empty coffin.
    /// </remarks>
    public static Contents OutcomeFor(int dialogFlags) {
        if ((dialogFlags & (int)Contents.Loot) != 0) {
            return Contents.Loot;
        }
        return (dialogFlags & (int)Contents.Body) != 0 ? Contents.Body : Contents.Empty;
    }

    /// <summary>The dialog a non-loot outcome shows.</summary>
    public static int DialogFor(Contents outcome) =>
        outcome == Contents.Body ? JustABodyDialog : EmptyCoffinDialog;

    /// <summary>
    /// <b>A TRAPPED grave can only be dug from its OWN tile.</b>
    /// </summary>
    /// <param name="graveWorldX">The grave item's world position.</param>
    /// <param name="graveWorldY"><inheritdoc cref="PartyIsCloseEnough" path="/param[@name='graveWorldX']"/></param>
    /// <param name="partyTileX">The tile the party is standing in.</param>
    /// <param name="partyTileY"><inheritdoc cref="PartyIsCloseEnough" path="/param[@name='partyTileX']"/></param>
    /// <remarks>
    /// Only when the container's encounter data has <c>firesTrapEncounter</c> set. The handler
    /// divides the grave's world coordinates by <see cref="WorldPlacement.TileSize"/> and compares
    /// both against the current tile, bailing <b>silently</b> if either differs — no dialog, no
    /// sound, nothing. An untrapped grave has no such test and can be dug from wherever it is
    /// clickable.
    ///
    /// <para>Worth stating because the failure is invisible: a player clicking a trapped grave from
    /// the neighbouring tile gets no response at all, which reads as a broken hotspot.</para>
    /// </remarks>
    public static bool PartyIsCloseEnough(long graveWorldX, long graveWorldY,
        int partyTileX, int partyTileY) =>
        graveWorldX / WorldPlacement.TileSize == partyTileX
        && graveWorldY / WorldPlacement.TileSize == partyTileY;

    /// <summary>
    /// <b>The shovel is checked AFTER the confirm, not before it.</b>
    /// </summary>
    /// <remarks>
    /// The order is: click sound, confirm dialog, <i>then</i>
    /// <c>CountItemInWholeParty(Shovel)</c>. So the game asks whether you want to dig and only then
    /// tells you that you cannot — and <see cref="NoShovelDialog"/>'s text is written for exactly
    /// that moment ("Besides, we need a shovel"). Checking first would be tidier and would skip a
    /// line the game means you to read.
    /// </remarks>
    public static bool ShovelIsCheckedAfterTheConfirm => true;

    /// <summary>
    /// <b>Declining the confirm aborts the dig.</b>
    /// </summary>
    /// <remarks>
    /// <c>dialog_Show(dialogId, 1)</c>'s non-zero return jumps to the exit. The grave's own dialog
    /// is therefore a question, not a description.
    /// </remarks>
    public static bool ConfirmCanBeDeclined => true;

    /// <summary>
    /// <b>The shovel is SPENT, and spent whatever the dig turns up.</b>
    /// </summary>
    /// <remarks>
    /// <c>useItem(Shovel)</c> runs before the outcome branch, so an empty coffin costs the same as a
    /// full one. It is a use, not a check: digging consumes a charge of the tool.
    /// </remarks>
    public static bool DiggingSpendsTheShovel => true;

    /// <summary>
    /// <b>A trapped grave is DISPOSED before its trap is spawned.</b>
    /// </summary>
    /// <remarks>
    /// The handler reads the encounter's x/y, calls <c>disposeContainer</c>, nulls its pointer and
    /// only then spawns from TRAP.DAT — then re-fetches the container at the location, because the
    /// spawn may have put a new one there. Spawning first would leave the old grave in place beside
    /// whatever the trap created.
    /// </remarks>
    public static bool TrapDisposesTheGraveFirst => true;
}
