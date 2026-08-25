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

    /// <summary>
    /// Height of the player's eye while walking this zone — 230 in every outdoor zone, 250
    /// underground.
    /// </summary>
    /// <remarks>
    /// <b>This is the world view's camera, and there are two others to keep it apart from.</b>
    /// <see cref="CameraZPosition"/> a few fields down is the OVERHEAD MAP's camera height, and
    /// START.DAT carries a third pair for the combat arena. All three are heights of a camera in
    /// this zone; only this one is what the player looks through while moving.
    ///
    /// <para>Confusing it with START.DAT's 1024 tilts the walking view visibly wrong, which is how
    /// the mix-up gets caught — but only after it has shipped, so prefer the branch: the explore
    /// camera reads THIS, and nothing else does.</para>
    /// </remarks>
    public uint DefaultCameraZ { get; set; }

    /// <summary>
    /// Pitch of the player's eye while walking, in 16-bit angle units — 280 outdoors (near level),
    /// 0 underground.
    /// </summary>
    /// <inheritdoc cref="DefaultCameraZ"/>
    public ushort DefaultCameraPitch { get; set; }

    public ZoneFlags Flags { get; set; }
    public byte SkyColor { get; set; }
    public byte GroundColor { get; set; }

    /// <summary>Lowest the overhead map's camera may be zoomed — 23000 outdoors, 6000 underground.</summary>
    /// <inheritdoc cref="CameraZPosition"/>
    public uint MapMinZ { get; set; }

    /// <summary>
    /// Starting height of the OVERHEAD MAP's camera — 133000 outdoors, 18000 underground.
    /// </summary>
    /// <remarks>
    /// <b>Not the walking camera</b> — see <see cref="DefaultCameraZ"/> for that. This one is the
    /// bird's-eye view, and it sits between <see cref="MapMinZ"/> and <see cref="MapMaxZ"/> because
    /// those two and <see cref="MapZoomStep"/> are the range the player zooms it through. The
    /// engine remembers the current value across zones and only re-seeds it from here when the zone
    /// actually CHANGES, so it is a starting height rather than a fixed one.
    /// </remarks>
    public uint CameraZPosition { get; set; }

    /// <summary>Highest the overhead map's camera may be zoomed — 203000 outdoors, 120000 underground.</summary>
    /// <inheritdoc cref="CameraZPosition"/>
    public uint MapMaxZ { get; set; }

    /// <summary>How far one zoom press moves the overhead map's camera.</summary>
    /// <inheritdoc cref="CameraZPosition"/>
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
