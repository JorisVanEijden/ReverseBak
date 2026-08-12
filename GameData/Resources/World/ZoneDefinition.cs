namespace GameData.Resources.World;

public class ZoneDefinition : IResource
{
    public ZoneDefinition(string id) { Id = id; }
    /// <summary>
    /// What kind of place this zone is — the first field of <c>Z##DEF.DAT</c>, read into the
    /// original's <c>g_game_mode</c> / IDA's <c>zoneLocation</c> by <c>zone_load</c>.
    /// <list type="bullet">
    ///   <item><b>0</b> — ordinary outdoor zone (1, 3–8).</item>
    ///   <item><b>1</b> — zones 2 and 9. Distinguished only in <c>sky_draw_ground_band</c>, which
    ///   picks a different horizon strip for it. What the category actually means is not established,
    ///   so it is deliberately left unnamed rather than guessed at.</item>
    ///   <item><b>2</b> — underground (zones 10, 11, 12). Every test in the engine is against this
    ///   value; see <see cref="IsUnderground"/>.</item>
    /// </list>
    /// </summary>
    public short ZoneLocation { get; set; }

    /// <summary>
    /// Whether this zone is underground. Drives a lot more than lighting: the party's step is
    /// quartered, the interaction-range table switches to its underground block, and the combat grid
    /// shrinks from 8×13 to 8×7.
    /// </summary>
    public bool IsUnderground => ZoneLocation == UndergroundZoneLocation;

    /// <summary>The <see cref="ZoneLocation"/> value that means underground.</summary>
    public const short UndergroundZoneLocation = 2;
    public short ZonePointer { get; set; }
    public uint DefaultCameraZ { get; set; }
    public ushort DefaultCameraPitch { get; set; }
    public ZoneFlags Flags { get; set; }
    public byte SkyColor { get; set; }
    public byte GroundColor { get; set; }
    public uint MapMinZ { get; set; }
    public uint CameraZPosition { get; set; }
    public uint MapMaxZ { get; set; }
    public uint MapZoomStep { get; set; }
    public short RmpResourceCount { get; set; }
    public short SpriteFogDivisor { get; set; }
    public uint SpriteFogNearDistance { get; set; }
    public uint Unused26 { get; set; }
    public short PolygonFogDivisor { get; set; }
    public uint PolygonFogNearDistance { get; set; }
    public uint FarClipDistance { get; set; }
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
