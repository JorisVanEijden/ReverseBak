namespace ResourceExtractor.Extractors;

using ResourceExtractor.Extensions;

using System.Text;

internal class AnimationExtractor : ExtractorBase {
    public static void Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        
        var animation = new AnimationResource();
        string tag = ReadTag(resourceReader);
        if (tag != "VER") {
            throw new InvalidDataException($"Expected VER tag, got {tag}");
        }
        int size = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in VER tag");
        }
        animation.Version = new string(resourceReader.ReadChars(size));
        tag = ReadTag(resourceReader);
        if (tag != "ADS") {
            throw new InvalidDataException($"Expected ADS tag, got {tag}");
        }
        size = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x8000) {
            throw new InterestingDataException("UInt16 after size is not 0x8000 in ADS tag");
        }
        tag = ReadTag(resourceReader);
        if (tag != "RES") {
            throw new InvalidDataException($"Expected RES tag, got {tag}");
        }
        size = resourceReader.ReadUInt16();
        if (resourceReader.ReadUInt16() != 0x0000) {
            throw new InterestingDataException("UInt16 after size is not 0x0000 in RES tag");
        }
        var numberOfEntries = resourceReader.ReadUInt16();
        for (int i = 0; i < numberOfEntries; i++) {
            var id = resourceReader.ReadUInt16();
            var name = resourceReader.ReadZeroTerminatedString();
        }
    }
}

internal class InterestingDataException : Exception {
    public InterestingDataException(string message) {
        throw new Exception(message);
    }
}

internal class AnimationResource {
    public string Version { get; set; }
}