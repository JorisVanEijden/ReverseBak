namespace ResourceExtraction.Imaging;

using GameData.Resources.Label;
using GameData.Resources.Menu;

/// <summary>
/// Rescales engine-independent layout coordinates from the original DOS spaces
/// (VGA 320x200 / EGA 640x350) into the canonical aspect-true space (1600x1200 / 1280x960),
/// matching the pixel correction done by <see cref="AspectCorrection"/>. Applied at the end
/// of each extractor so every consumer receives canonical-space coordinates only.
/// </summary>
public static class CanonicalSpace {
    public static void Apply(UserInterface ui) {
        ui.XPosition = AspectCorrection.ScaleVgaX(ui.XPosition);
        ui.YPosition = AspectCorrection.ScaleVgaY(ui.YPosition);
        ui.Width = AspectCorrection.ScaleVgaX(ui.Width);
        ui.Height = AspectCorrection.ScaleVgaY(ui.Height);
        ui.XOffset = AspectCorrection.ScaleVgaX(ui.XOffset);
        ui.YOffset = AspectCorrection.ScaleVgaY(ui.YOffset);
        foreach (UiElement entry in ui.MenuEntries) {
            entry.XPosition = AspectCorrection.ScaleVgaX(entry.XPosition);
            entry.YPosition = AspectCorrection.ScaleVgaY(entry.YPosition);
            entry.Width = AspectCorrection.ScaleVgaX(entry.Width);
            entry.Height = AspectCorrection.ScaleVgaY(entry.Height);
        }
    }

    public static void Apply(LabelSet labels) {
        foreach (Label label in labels.Labels) {
            label.XPosition = AspectCorrection.ScaleVgaX(label.XPosition);
            label.YPosition = AspectCorrection.ScaleVgaY(label.YPosition);
        }
    }
}
