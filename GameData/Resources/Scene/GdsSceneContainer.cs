namespace GameData.Resources.Scene;

/// <summary>
/// Where a location scene's own stats live — the container behind a shop, temple, inn or tavern.
/// </summary>
/// <remarks>
/// <b>A location's container is not found at the party's feet.</b> <c>gds_loadSceneFile</c> @0x4d878
/// resolves it with <c>GetContainerAtLocation(zone 15, X = scene number, Y = scene letter)</c> and
/// stores it on the scene — so for these containers the location fields are the scene's identity,
/// not world coordinates, and zone 15 is a dedicated container zone with nothing in it but these.
///
/// <para>Looking the container up by where the party is standing finds nothing at all, because the
/// party is never standing at (70, 1). That failure is silent: the arm that wanted the container
/// simply does nothing.</para>
///
/// <para>Which of the block's readings applies — shop markup, temple tier, inn rate — is decided by
/// the hotspot's action code, not by the container. See <c>SaveGameContainerShopData</c>.</para>
/// </remarks>
public static class GdsSceneContainer {
    /// <summary>The zone every location's container is filed under.</summary>
    public const int Zone = 15;

    /// <summary>The container "x" for a scene: its number.</summary>
    public static int LocationX(int sceneNumber) => sceneNumber;

    /// <summary>The container "y" for a scene: its letter, counting from 1.</summary>
    public static int LocationY(int sceneLetter) => sceneLetter;
}
