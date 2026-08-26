namespace ResourceExtractor.Extractors;

using GameData.Resources.Image;

using ResourceExtraction.Compression;

using ResourceExtractor.Compression;

public abstract class ExtractorBase {
    internal const int FileNameLength = 13;
    private const int TagLength = 4;
    internal const int DosCodePage = 437;
    internal const bool Debug = false;
    protected static string Indent = string.Empty;

    protected static void Log(string message) {
        if (Debug)
            Console.WriteLine(Indent + message);
    }

   
    /// <summary>Reads one compressed block — see <see cref="CompressedBlock"/>.</summary>
    /// <remarks>
    /// A wrapper, not an implementation: the read itself is shared with the other project's
    /// <c>ExtractorBase</c>, which held a byte-for-byte copy of it until 2026-08-26. Kept as a
    /// protected member so nothing deriving from this base had to change.
    /// </remarks>
    protected static byte[] DecompressToByteArray(BinaryReader resourceReader, long length = 0) =>
        CompressedBlock.Read(resourceReader, length);
}