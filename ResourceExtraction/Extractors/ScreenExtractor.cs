namespace ResourceExtraction.Extractors;

using GameData.Resources.Image;

using ResourceExtraction.Imaging;

using System.IO;
using System.Text;

public class ScreenExtractor : ExtractorBase<BackgroundImage> {
    public override BackgroundImage Extract(string screenId, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        ushort signature = resourceReader.ReadUInt16();
        bool hiRes = signature != 0x27B6;
        if (hiRes) {
            // Hi-res screens have no signature
            resourceStream.Seek(-2, SeekOrigin.Current);
        }
        byte[] bitMapData = DecompressToByteArray(resourceReader);

        // Hi-res screens use 4 bits per pixel, we need to convert them to 8 bits per pixel.
        if (hiRes) {
            var tempArray = new byte[bitMapData.Length * 2];
            var j = 0;
            foreach (byte colors in bitMapData) {
                tempArray[j++] = (byte)(colors >> 4);
                tempArray[j++] = (byte)(colors & 0x0F);
            }
            bitMapData = tempArray;
        }

        // Correct DOS non-square pixels to square pixels. SCX data is row-major.
        int correctedWidth;
        int correctedHeight;
        if (hiRes) {
            // The only 640x350 EGA screen (BOOK.SCX): x2 horizontally, vertical resamples 350->960 for 4:3.
            correctedWidth = 1280;
            correctedHeight = 960;
            bitMapData = AspectCorrection.ResampleNearest(bitMapData, 640, 350, correctedWidth, correctedHeight);
        } else {
            bitMapData = AspectCorrection.CorrectVga(bitMapData, 320, 200, columnMajor: false, out correctedWidth, out correctedHeight);
        }

        var screen = new BackgroundImage(screenId) {
            BitMapData = bitMapData,
            Width = correctedWidth,
            Height = correctedHeight
        };

        return screen;
    }
}