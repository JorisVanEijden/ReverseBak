namespace GameData.Resources.World;

using System;

/// <summary>
/// The original's perspective scale, and the Unity vertical FOV that reproduces it.
/// </summary>
/// <remarks>
/// <b>Two views ship with DIFFERENT scales, and that is the whole point of this class.</b> The
/// renderer projects a point at horizontal offset <c>X</c> and depth <c>Z</c> to
/// <c>X * (1 &lt;&lt; zoom) / Z</c> VGA pixels from the view centre (canassa
/// <c>project_world_to_screen</c>, <c>PROJECT.C</c>), and <c>zoom</c> is a per-view field. The game
/// builds two <c>ViewContext</c>s and never changes either one's zoom afterwards:
/// <list type="bullet">
/// <item><c>g_active_window</c> — built in <c>boot_start_dat_load</c>, whose last read lands on the
/// struct's first member, so its zoom is <b>START.DAT's</b> <see cref="Config.StartData.ProjectionShift"/>
/// = 9. This is the view <c>world_render_view</c> renders travel and combat through.</item>
/// <item><c>g_world_widget</c> — built in <c>zone_subsystem_init</c>, which never writes zoom, so it
/// keeps <c>VIEW_ZOOM_DEFAULT</c> = 7. This is the view the <b>map screens</b> render through
/// (<c>world_render_scene_dispatch</c>'s full-redraw branch), and the one
/// <c>world_render_record_marker_dot</c> scales the locator's dots by.</item>
/// </list>
/// So the map and the locator see <b>four times</b> as much ground as the travel view at the same
/// camera height. A port that gives every screen the travel camera's FOV shows a quarter of the map.
///
/// <para><b>Why the view's width does not appear below.</b> Matching the original horizontally means
/// <c>tan(halfFovH) = (w/2) / (1 &lt;&lt; shift)</c> for a VGA rect <c>w x h</c>. That rect is
/// rendered into a canonical <c>5w x 6h</c> texture, so
/// <c>tan(halfFovV) = tan(halfFovH) * 6h / 5w = 0.6h / (1 &lt;&lt; shift)</c> — the width cancels,
/// and the 0.6 is the 6:5 VGA pixel aspect. The travel view (h = 101, shift 9) comes out at 13.50°,
/// which is the hand-calibrated constant this replaces, so nothing about travel moves.</para>
/// </remarks>
public static class WorldProjection {
    /// <summary>START.DAT's shift, used by the travel and combat view.</summary>
    public const int TravelProjectionShift = 9;

    /// <summary><c>VIEW_ZOOM_DEFAULT</c>, used by the overhead map and the locator inset.</summary>
    public const int MapProjectionShift = 7;

    /// <summary>VGA's 6:5 pixel aspect, halved — see the class remarks.</summary>
    private const double PixelAspectHalf = 0.6;

    /// <summary>
    /// The Unity camera vertical FOV, in degrees, that reproduces the original's projection for a
    /// view whose VGA rectangle is <paramref name="viewHeightVgaPx"/> tall.
    /// </summary>
    public static double VerticalFovDegrees(double viewHeightVgaPx, int projectionShift) {
        if (viewHeightVgaPx <= 0) {
            throw new ArgumentOutOfRangeException(nameof(viewHeightVgaPx));
        }
        if (projectionShift is < 0 or > 15) {
            throw new ArgumentOutOfRangeException(nameof(projectionShift));
        }
        double tanHalf = PixelAspectHalf * viewHeightVgaPx / (1 << projectionShift);

        return 2.0 * Math.Atan(tanHalf) * (180.0 / Math.PI);
    }
}
