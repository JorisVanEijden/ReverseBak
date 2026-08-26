namespace ResourceExtraction.Extractors;

using GameData.Resources;
using ResourceExtraction.Compression;
using ResourceExtraction.Extensions;
using ResourceExtractor.Compression;
using System;
using System.Collections.Generic;
using System.IO;

public abstract class ExtractorBase<T> where T : IResource {
    internal const int DosCodePage = 437;
    internal const bool Debug = true;
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

    public abstract T Extract(string id, Stream resourceStream);

    protected static string ReadAlignedString(BinaryReader reader) {
        string text = reader.ReadZeroTerminatedString();
        if ((text.Length & 1) == 0) {
            reader.ReadByte();
        }

        return text;
    }
}