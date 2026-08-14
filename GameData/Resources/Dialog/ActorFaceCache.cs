namespace GameData.Resources.Dialog;

/// <summary>
/// The portrait cache behind a dialog's speaker face — IDA <c>LoadActorFace</c> (ovr144 @0x4aa15)
/// and <c>disposeActorFaces</c> (@0x4a9a2).
/// </summary>
public static class ActorFaceCache {
    /// <summary>Portraits held at once. Fixed — there is no growth path.</summary>
    public const int Slots = 6;

    /// <summary>Actor numbers at and above this have no portrait at all.</summary>
    public const int FirstFacelessActor = 49;

    /// <summary>Value stamped into the loaded palette's first byte.</summary>
    public const int PreparedPaletteMarker = 0x3f;

    /// <summary>Format of a portrait's bitmap.</summary>
    public const string BitmapFormat = "ACT{0:D3}{1}.BMP";

    /// <summary>Format of a portrait's palette.</summary>
    public const string PaletteFormat = "ACT{0:D3}.PAL";

    /// <summary>Whether this actor has a portrait to load.</summary>
    /// <remarks>
    /// <b>The high-numbered actors simply have no face</b>, and the function says so by nulling the
    /// bitmap and palette rather than by failing. A caller that treats a null portrait as an error
    /// will reject a perfectly ordinary speaker.
    /// </remarks>
    public static bool HasFace(int actorNumber) => actorNumber < FirstFacelessActor;

    /// <summary>
    /// The bitmap a portrait loads from.
    /// </summary>
    /// <param name="alternate">
    /// Whether to take the alternate portrait. The original selects it on a <b>negative</b>
    /// argument, not on a flag.
    /// </param>
    /// <remarks>
    /// <b>An actor can have two faces and only one palette.</b> The alternate adds an "A" to the
    /// bitmap name — and the palette name has no such variant, so both portraits are drawn through
    /// the same colours. Loading a per-variant palette would be the natural thing to build and there
    /// is no file for it.
    /// </remarks>
    public static string BitmapNameFor(int actorNumber, bool alternate) =>
        string.Format(BitmapFormat, actorNumber, alternate ? "A" : string.Empty);

    /// <summary>The palette a portrait loads from — shared by both variants.</summary>
    public static string PaletteNameFor(int actorNumber) =>
        string.Format(PaletteFormat, actorNumber);

    /// <summary>
    /// Whether a palette has been through <c>LoadActorFace</c>.
    /// </summary>
    /// <remarks>
    /// The loader stamps <see cref="PreparedPaletteMarker"/> into the palette's first byte and
    /// <c>ShowDialogWithFace</c> tests for exactly that before using it — so the marker is a
    /// handshake between the two, not a colour. Writing the palette through faithfully means
    /// preserving a byte that is not really colour data.
    /// </remarks>
    public static bool PaletteIsPrepared(int firstPaletteByte) =>
        firstPaletteByte == PreparedPaletteMarker;

    /// <summary>
    /// Which slot a request lands in, given what the cache already holds.
    /// </summary>
    /// <param name="cachedActors">The actor number in each slot; 0 for empty.</param>
    /// <returns>The slot index, or -1 when the cache is full of other actors.</returns>
    /// <remarks>
    /// <b>A hit returns immediately; a miss takes the LAST empty slot, not the first.</b> The scan
    /// runs all six either way and keeps overwriting its candidate, so a cache with holes fills from
    /// the back. Nothing depends on it, but a port that takes the first free slot will not match a
    /// trace.
    ///
    /// <para><b>And the original does not handle a full cache.</b> Its candidate starts at -1 and is
    /// used unguarded, so a seventh distinct speaker writes <i>before</i> the table. It never bites
    /// because the cache is emptied at every scene change and no scene has seven speakers — but this
    /// port returns -1 rather than reproducing an out-of-bounds write.</para>
    /// </remarks>
    public static int SlotFor(int actorNumber, int[] cachedActors) {
        if (cachedActors == null) {
            return -1;
        }

        var free = -1;
        for (var slot = 0; slot < cachedActors.Length && slot < Slots; slot++) {
            if (cachedActors[slot] == actorNumber) {
                return slot;
            }
            if (cachedActors[slot] == 0) {
                free = slot;
            }
        }

        return free;
    }

    /// <summary>
    /// <b>A faceless actor is not cached.</b>
    /// </summary>
    /// <remarks>
    /// The no-face path nulls the slot's bitmap and palette but never records the actor number, so
    /// the slot stays free and the lookup is repeated on every request. Caching the negative result
    /// would be the obvious improvement and would leave a slot permanently occupied by an actor with
    /// nothing in it.
    /// </remarks>
    public static bool IsRemembered(int actorNumber) => HasFace(actorNumber);

    /// <summary>
    /// <b>Disposal is all or nothing.</b>
    /// </summary>
    /// <remarks>
    /// Every slot's bitmap and palette are freed, the table itself is released and the pointer
    /// cleared. There is no per-actor eviction anywhere — which is why a scene transition is the
    /// only thing that reclaims a portrait.
    /// </remarks>
    public static bool EvictsIndividually => false;
}
