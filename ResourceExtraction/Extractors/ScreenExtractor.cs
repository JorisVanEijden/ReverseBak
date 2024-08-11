namespace ResourceExtraction.Extractors;

using GameData.Resources.Image;

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

        var screen = new BackgroundImage(screenId) {
            BitMapData = bitMapData,
            Width = hiRes ? 640 : 320,
            Height = hiRes ? 350 : 200
        };

        return screen;
    }
}