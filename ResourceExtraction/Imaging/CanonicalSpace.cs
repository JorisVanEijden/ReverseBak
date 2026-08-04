namespace ResourceExtraction.Imaging;

using GameData.Resources.Animation;
using GameData.Resources.Animation.FrameCommands;
using GameData.Resources.Book;
using GameData.Resources.Dialog;
using GameData.Resources.Dialog.Actions;
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
        // REQ_PREF / REQ_MAIN ship Width = Height = 1 as a "full screen
        // canvas size" sentinel (their entries use absolute coordinates).
        ui.Width = ui.Width > 1 ? AspectCorrection.ScaleVgaX(ui.Width) : 1600;
        ui.Height = ui.Height > 1 ? AspectCorrection.ScaleVgaY(ui.Height) : 1200;
        ui.XOffset = AspectCorrection.ScaleVgaX(ui.XOffset);
        ui.YOffset = AspectCorrection.ScaleVgaY(ui.YOffset);
        foreach (UiElement entry in ui.MenuEntries) {
            entry.XPosition = AspectCorrection.ScaleVgaX(entry.XPosition);
            entry.YPosition = AspectCorrection.ScaleVgaY(entry.YPosition);
            entry.Width = AspectCorrection.ScaleVgaX(entry.Width);
            entry.Height = AspectCorrection.ScaleVgaY(entry.Height);
        }
        // The frame is the coordinate space the rects above resolve against — separate from
        // ui.Width/Height, which is the screen's own box (e.g. REQ_CAMP's 1470x606 panel).
        ui.Frame = AspectCorrection.CanonicalFrame();
    }

    public static void Apply(LabelSet labels) {
        foreach (Label label in labels.Labels) {
            label.XPosition = AspectCorrection.ScaleVgaX(label.XPosition);
            label.YPosition = AspectCorrection.ScaleVgaY(label.YPosition);
        }
    }

    public static void Apply(BookResource book) {
        foreach (Page page in book.Pages) {
            page.XOffset = AspectCorrection.ScaleEgaX(page.XOffset);
            page.YOffset = AspectCorrection.ScaleEgaY(page.YOffset);
            page.Width = AspectCorrection.ScaleEgaX(page.Width);
            page.Height = AspectCorrection.ScaleEgaY(page.Height);
            foreach (BookImage image in page.Images) {
                image.X = AspectCorrection.ScaleEgaX(image.X);
                image.Y = AspectCorrection.ScaleEgaY(image.Y);
            }
            foreach (Paragraph paragraph in page.Paragraphs) {
                paragraph.XOffset = AspectCorrection.ScaleEgaX(paragraph.XOffset);
                paragraph.YOffset = AspectCorrection.ScaleEgaY(paragraph.YOffset);
                paragraph.Width = AspectCorrection.ScaleEgaX(paragraph.Width);
                paragraph.StartIndent = AspectCorrection.ScaleEgaX(paragraph.StartIndent);
                paragraph.LineSpacing = AspectCorrection.ScaleEgaY(paragraph.LineSpacing);
                paragraph.InterParagraphSpacing = AspectCorrection.ScaleEgaY(paragraph.InterParagraphSpacing);
            }
            foreach (ReservedArea area in page.ReservedAreas) {
                area.X = AspectCorrection.ScaleEgaX(area.X);
                area.Y = AspectCorrection.ScaleEgaY(area.Y);
                area.X2 = AspectCorrection.ScaleEgaX(area.X2);
                area.Y2 = AspectCorrection.ScaleEgaY(area.Y2);
            }
        }
    }

    public static void Apply(Dialog dialog) {
        foreach (DialogEntry entry in dialog.Entries) {
            foreach (DialogActionBase action in entry.Actions) {
                if (action is ResizeDialogAction resize) {
                    resize.Left = AspectCorrection.ScaleVgaX(resize.Left);
                    resize.Top = AspectCorrection.ScaleVgaY(resize.Top);
                    resize.Width = AspectCorrection.ScaleVgaX(resize.Width);
                    resize.Height = AspectCorrection.ScaleVgaY(resize.Height);
                }
            }
        }
    }

    /// <summary>
    /// Stamps the dialog surface's design frame onto the (synthesized) DIALSTYL.DAT table — the
    /// space <see cref="DialogStyle.DefaultArea"/> and every <see cref="DialogLayout"/> value
    /// resolve in, and the one place <see cref="GameData.Resources.Layout.LayoutFit"/> can be
    /// stated for dialogs.
    ///
    /// <para>Nothing else is rescaled: unlike a REQ or a DDX, this table is not parsed from
    /// original bytes — GameData carries its rows already in canonical px (see the row comments,
    /// which record the VGA numbers and the x5/x6 factors applied to them). What GameData cannot
    /// carry is the canonical frame itself, because the dimensions are derived from
    /// <see cref="AspectCorrection"/>, which lives on this side of the boundary. Hence a stamp
    /// rather than a type default — and hence EVERY path that hands out a shipped table has to
    /// come through here (the archive provider, and the override merge baseline on the Unity
    /// side), or an unmentioned frame arrives at the stage as 0x0.</para>
    /// </summary>
    public static void Apply(DialogStyleTable styleTable) {
        styleTable.Frame = AspectCorrection.CanonicalFrame();
    }

    public static void Apply(AnimationResource animation) {
        foreach (Frame frame in animation.Frames) {
            foreach (FrameCommand command in frame.Commands) {
                // IArea covers area commands, scaled draws, and screen transitions: scale X/Y/Width/Height.
                if (command is IArea area) {
                    area.X = AspectCorrection.ScaleVgaX(area.X);
                    area.Y = AspectCorrection.ScaleVgaY(area.Y);
                    area.Width = AspectCorrection.ScaleVgaX(area.Width);
                    area.Height = AspectCorrection.ScaleVgaY(area.Height);
                }
                // Plain image draws (no Width/Height): scale position only.
                else if (command is DrawImageBase image) {
                    image.X = AspectCorrection.ScaleVgaX(image.X);
                    image.Y = AspectCorrection.ScaleVgaY(image.Y);
                }
            }
        }
    }
}
