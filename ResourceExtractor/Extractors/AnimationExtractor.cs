namespace ResourceExtractor.Extractors;

using ResourceExtractor.Extensions;
using ResourceExtractor.Resources.Animation;

using System.Text;

internal class AnimationExtractor : ExtractorBase {
    public static AnimationResource Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var animation = new AnimationResource();
        string tag = ReadTag(resourceReader);
        if (tag != "VER") {
            throw new InvalidDataException($"Expected VER tag, got {tag}");
        }
        int verSize = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in VER tag");
        }
        animation.Version = resourceReader.ReadZeroTerminatedString();
        tag = ReadTag(resourceReader);
        if (tag != "ADS") {
            throw new InvalidDataException($"Expected ADS tag, got {tag}");
        }
        uint adsSize = resourceReader.ReadUInt32();
        if ((adsSize | 0x8000) == 0) {
            throw new InterestingDataException("Ads size does not have 0x8000 in ADS tag");
        }
        tag = ReadTag(resourceReader);
        if (tag != "RES") {
            throw new InvalidDataException($"Expected RES tag, got {tag}");
        }
        uint resSize = resourceReader.ReadUInt32();
        int nrOfResEntries = resourceReader.ReadUInt16();
        animation.ResourceFiles = new Dictionary<int, string>(nrOfResEntries);
        for (int i = 0; i < nrOfResEntries; i++) {
            int id = resourceReader.ReadUInt16();
            string fileName = resourceReader.ReadZeroTerminatedString();
            animation.ResourceFiles.Add(id, fileName);
        }

        tag = ReadTag(resourceReader);
        if (tag != "SCR") {
            throw new InvalidDataException($"Expected SCR tag, got {tag}");
        }
        uint scrSize = resourceReader.ReadUInt32();
        animation.ScriptBytes = DecompressToByteArray(resourceReader, scrSize);

        return animation;
    }
}

internal class InterestingDataException : Exception {
    public InterestingDataException(string message) {
        throw new Exception(message);
    }
}