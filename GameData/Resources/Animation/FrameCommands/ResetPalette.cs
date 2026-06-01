namespace GameData.Resources.Animation.FrameCommands;

// TTM 0x0400 (no arguments): re-apply the current palette to the VGA DAC,
// cancelling any active fade or palette cycle. Verified in
// anim_executeFrameFunctions (IDA loc_ovr153_D2D @ 0x534ad): resets the palette
// range then calls SetPaletteOrDefault(anim_pCurrentPalette).
public class ResetPalette : FrameCommand {
    public override string ToString() {
        return $"{nameof(ResetPalette)}();";
    }
}
