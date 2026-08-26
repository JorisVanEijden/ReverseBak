namespace ResourceExtraction.Compression;

using ResourceExtraction.Extensions;
// CompressionType declares the ResourceExtractor.Compression namespace despite living in
// this folder — a pre-existing quirk, not a typo here.
using ResourceExtractor.Compression;
using System.IO;

/// <summary>
/// Reads one of the archive's compressed blocks: a type byte, a decompressed size, then the
/// payload.
/// </summary>
/// <remarks>
/// <b>Lives here rather than on an extractor base because there are two of those.</b>
/// <c>ResourceExtraction</c> and <c>ResourceExtractor</c> each carry an <c>ExtractorBase</c>, and
/// both held a byte-for-byte copy of this read — the library's used by the screen and ADS
/// extractors, the CLI's by the OVL one. Two copies of a format header is exactly the thing that
/// drifts silently: a fix to one leaves the other reading the old shape.
///
/// <para>The two bases keep their own thin <c>DecompressToByteArray</c> wrappers, so nothing that
/// derives from either had to change.</para>
/// </remarks>
public static class CompressedBlock {
    /// <summary>
    /// Reads the block at the reader's current position.
    /// </summary>
    /// <param name="reader">Positioned on the block's type byte.</param>
    /// <param name="length">
    /// Bytes the block occupies, or 0 to read to the end of the stream. This bounds the COMPRESSED
    /// payload — the decompressed size comes from the header — which is what lets a block be read
    /// out of the middle of an archive member.
    /// </param>
    /// <remarks>
    /// <b>The size in the header is trusted for the output buffer, and the read is exact.</b> A
    /// block whose payload decompresses to fewer bytes than it claims fails here rather than
    /// yielding a half-filled buffer that reads as valid data further up.
    /// </remarks>
    public static byte[] Read(BinaryReader reader, long length = 0) {
        long endPosition = length == 0
            ? reader.BaseStream.Length
            : reader.BaseStream.Position + length;
        var compressionType = (CompressionType)reader.ReadByte();
        var decompressedSize = (int)reader.ReadUInt32();

        ICompression compression = CompressionFactory.Create(compressionType);
        Stream decompressedStream = compression.Decompress(reader.BaseStream, endPosition);

        var decompressedDataBuffer = new byte[decompressedSize];
        decompressedStream.ReadExactly(decompressedDataBuffer);

        return decompressedDataBuffer;
    }
}
