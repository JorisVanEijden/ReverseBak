namespace ResourceExtraction.Extractors;

using ResourceExtraction.Extensions;

using System.Collections.Generic;
using System.IO;
using System.Text;

public class ArchiveExtractor {
    private const string ResourceFileName = "KRONDOR.001";
    private const int DosCodePage = 437;
    private const int FileNameLength = 13;

    private readonly Dictionary<string, (long, uint)> _dictionary;
    private readonly string _resourceFilePath;

    public ArchiveExtractor(string gameDirectory) {
        _resourceFilePath = Path.Combine(gameDirectory, ResourceFileName);
        _dictionary = GetDictionary();
    }

    public Dictionary<string, (long, uint)> GetDictionary() {
        using FileStream resourceFile = File.OpenRead(_resourceFilePath);
        using var resourceReader = new BinaryReader(resourceFile, Encoding.GetEncoding(DosCodePage));
        long resourceFileLength = resourceFile.Length;

        var offsets = new Dictionary<string, (long, uint)>();
        while (resourceFile.Position < resourceFileLength) {
            string fileName = new(resourceReader.ReadChars(FileNameLength));
            fileName = fileName.Trim('\0').ToUpper();
            uint fileSize = resourceReader.ReadUInt32();
            long fileOffset = resourceFile.Position;
            resourceFile.Seek(fileSize, SeekOrigin.Current);

            offsets.Add(fileName, (fileOffset, fileSize));
        }

        return offsets;
    }

    public Stream GetResourceStream(string filename) {
        string filePath = Path.Combine(_resourceFilePath, filename);
        if (File.Exists(filePath)) {
            return File.OpenRead(filePath);
        }
        if (_dictionary.TryGetValue(filename.ToUpper(), out (long offset, uint size) entry)) {
            return GetResourceStream(_resourceFilePath, entry.offset, entry.size);
        }

        throw new KeyNotFoundException($"Resource {filename} not found.");
    }

    public static Stream GetResourceStream(string filePath, long offset, uint size) {
        FileStream resourceFile = File.OpenRead(filePath);
        resourceFile.Seek(offset, SeekOrigin.Begin);

        var buffer = new byte[size];
        resourceFile.ReadExactly(buffer);

        return new MemoryStream(buffer);
    }
}