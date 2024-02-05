namespace ResourceExtractor.Extractors;

using ResourceExtractor.Resources.Book;

using System.Text;

internal class BokExtractor : ExtractorBase {
    public BookResource Extract(string filePath) {
        using FileStream resourceFile = File.OpenRead(filePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));

        var book = new BookResource();

        int dataSize = resourceReader.ReadInt32();
        int nrOfPages = resourceReader.ReadUInt16();
        int[] pageOffsets = new int[nrOfPages];
        for (int i = 0; i < nrOfPages; i++) {
            pageOffsets[i] = resourceReader.ReadInt32();
        }

        return book;
    }
}