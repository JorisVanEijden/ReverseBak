namespace GameData;

public static class PaletteMapping {

    public static string? GetPaletteFor(string image, int subImage = -1) {
        image = StripThreeLetterExtension(image).ToUpper();

        // Sub-image-specific overrides: a few BICONS icons are authored for a screen-specific palette
        // rather than the shared OPTIONS UI palette. The Contents-screen Exit button graphic — normal
        // frame BICONS1 #66, hover frame BICONS2 #66 — is drawn under CONTENTS.PAL in the original
        // (icons render under the active screen palette, which is CONTENTS.PAL on that screen).
        if (subImage == 66 && (image == "BICONS1" || image == "BICONS2")) {
            return "CONTENTS.PAL";
        }

        // Zone slot bitmaps (Z##SLOT#.BMX — the object/wall textures sampled by Flags&0x10 faces)
        // render under the zone palette, like the Z##L terrain atlas. They have no per-slot .PAL, so
        // the default same-name lookup fails. (RE: resource_loadZoneDataFiles @0x7313b loads them per zone.)
        if (image.Length > 4 && image[0] == 'Z' && char.IsDigit(image[1]) && char.IsDigit(image[2])
            && image.Substring(3).StartsWith("SLOT")) {
            return image.Substring(0, 3) + ".PAL";
        }

        return image switch {
            "BICONS1" => "OPTIONS.PAL",
            "BICONS2" => "OPTIONS.PAL",
            "BLANK" => "CREDITS.PAL",
            "BOOK" => "BOOK.PAL",
            // Synthesized parchment variants (even = BOOK.SCX, odd = vertically flipped) share BOOK.PAL.
            "BOOK_EVEN" => "BOOK.PAL",
            "BOOK_ODD" => "BOOK.PAL",
            "C11" => "C11B.PAL",
            "C11A1" => "C11A.PAL",
            "C11A2" => "C11A.PAL",
            "C11B" => "C11B.PAL",
            "C12A" => "C12A.PAL",
            "C12A_BAK" => "C12A.PAL",
            "C12A_MAG" => "C12A.PAL",
            "C12A_PUG" => "C12A.PAL",
            "C12B_ARC" => "C12B.PAL",
            "C12B_GOR" => "C12B.PAL",
            "C12B_SRL" => "C12A.PAL",
            "C42" => "C42.PAL",
            "C61A_TLK" => "C61B.PAL",
            "C61B_BAK" => "C61B.PAL",
            "CAST" => "OPTIONS.PAL",
            "CFRAME" => "OPTIONS.PAL",
            "CHAPTER" => "CHAPTER.PAL",
            // Travel compass strip — drawn in the FRAME chrome's compass slot, so it shares the
            // travel-HUD UI palette (same as FRAME). COMPASS.PAL does not exist in the archive.
            "COMPASS" => "OPTIONS.PAL",
            "CONT2" => "CONTENTS.PAL",
            "CONTENTS" => "CONTENTS.PAL",
            "CREDITS" => "CREDITS.PAL",
            "DIALOG" => "OPTIONS.PAL",
            "ENCAMP" => "OPTIONS.PAL",
            "FCOMBAT" => "OPTIONS.PAL",
            "FMAP_ICN" => "FULLMAP.PAL",
            "FRAME" => "OPTIONS.PAL",
            "FULLMAP" => "FULLMAP.PAL",
            // Party portrait heads — stamped onto the FRAME chrome (frame.scx) by addHeads in the
            // original, so they share the travel-HUD UI palette (same as FRAME/COMPASS). HEADS.PAL
            // does not exist in the archive.
            "HEADS" => "OPTIONS.PAL",
            "INT_BORD" => "INT_DYN.PAL",
            "INVSHP1" => "INVENTOR.PAL",
            "INVSHP2" => "INVENTOR.PAL",
            // Container-type images (dead body / chest / etc.) drawn in the loot screen's detail
            // window under the inventory palette (sub_ovr158_3D0 @0x56420).
            "INVMISC" => "INVENTOR.PAL",
            "INT_BOOK" => "INT_TITL.PAL",
            "INT_BUNT" => "INT_DYN.PAL",
            "INT_LGHT" => "INT_DYN.PAL",
            "INT_MENU" => "INT_MENU.PAL",
            "INVENTOR" => "INVENTOR.PAL",
            "OPTIONS0" => "OPTIONS.PAL",
            "OPTIONS1" => "OPTIONS.PAL",
            "OPTIONS2" => "OPTIONS.PAL",
            // Mouse cursors have no dedicated palette — the original blits them with the
            // active screen palette. Use the UI palette (same as BICONS/DIALOG/FRAME); the
            // arrow/hourglass/label glyphs use standard indices that read correctly under it.
            "POINTER" => "OPTIONS.PAL",
            "POINTERG" => "OPTIONS.PAL",
            "PUZZLE" => "PUZZLE.PAL",
            "RIFTMAP" => "OPTIONS.PAL",
            "Z01L" => "Z01.PAL",
            "Z02L" => "Z02.PAL",
            "Z03L" => "Z03.PAL",
            "Z04L" => "Z04.PAL",
            "Z05L" => "Z05.PAL",
            "Z06L" => "Z06.PAL",
            "Z07L" => "Z07.PAL",
            "Z08L" => "Z08.PAL",
            "Z09L" => "Z09.PAL",
            "Z10L" => "Z10.PAL",
            "Z11L" => "Z11.PAL",
            "Z12L" => "Z12.PAL",
            _ => null
        };
    }

    private static string StripThreeLetterExtension(string fileName) {
        if (fileName.Length > 4 && fileName[^4] == '.') {
            return fileName[..^4];
        }
        return fileName;
    }

}