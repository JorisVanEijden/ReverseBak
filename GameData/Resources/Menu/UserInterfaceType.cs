namespace GameData.Resources.Menu;

/// <summary>
/// The REQ screen's interaction model (the original's <c>menuData.type</c>), which drives how
/// keyboard input is handled. Ground truth: <c>menuData.type</c> in <c>menu_pollInput</c> @0x2c704 /
/// <c>menu_resolveHoverAndClick</c> @0x2c97f / <c>menu_sub_2C22C</c> @0x2c22c.
/// </summary>
public enum UserInterfaceType {
    /// <summary>Standard menu: generic focus-warp navigation, first-letter accelerators, and Enter
    /// activates the focused widget (the vast majority of REQ screens — options, inventory, combat …).</summary>
    Menu = 0x0000,

    /// <summary>A popup that saves and restores the screen region behind it (DOS
    /// <c>gfx_readAreaToBuffer</c>). No REQ file uses it (runtime-only); superseded in the Unity
    /// port by UIDocument layering.</summary>
    Overlay = 0x0001,

    /// <summary>The screen owns arrows and Enter: plain arrow scancodes and Enter are routed to the
    /// screen's own handler instead of the generic focus/accelerator model (SAVE/LOAD file dialogs,
    /// MAP/CMAP/ZONE, the teleport screens).</summary>
    InteractiveScreen = 0x0002
}
