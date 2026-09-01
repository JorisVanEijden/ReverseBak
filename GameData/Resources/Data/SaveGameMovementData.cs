namespace GameData.Resources.Data;

public class SaveGameMovementData {
    public SaveGameMovementData(
        short isAutoTraveling,
        byte subTileStepCount,
        short tileBoundaryCrossed,
        int mapCameraZ
    ) {
        IsAutoTraveling = isAutoTraveling;
        SubTileStepCount = subTileStepCount;
        TileBoundaryCrossed = tileBoundaryCrossed;
        MapCameraZ = mapCameraZ;
    }

    public short IsAutoTraveling { get; }
    public byte SubTileStepCount { get; }
    public short TileBoundaryCrossed { get; }
    /// <summary>
    /// The overhead map's camera height — <b>the player's map zoom</b>. Body offset 55, canassa
    /// <c>lInsetCameraPosZ</c>.
    /// </summary>
    /// <remarks>
    /// <b>Called <c>SavedCameraZPosition</c> until 2026-09-01, which said where it came from rather
    /// than what it is</b> — and the vagueness cost real time: TASK-5 hunted this field under a
    /// third name and concluded it was "the camera Z we already author", confusing it with
    /// <c>PositionZ</c> at offset 29. One name now, matching <c>GameSession.MapCameraZ</c>, which is
    /// the runtime state it restores.
    ///
    /// <para><c>MAP.C:474</c> drops the world camera to this height at a pitch of -90 degrees;
    /// <c>ZONE.C:105</c> reseeds it from the zone default only when the zone changes.</para>
    /// </remarks>
    public int MapCameraZ { get; }
}
