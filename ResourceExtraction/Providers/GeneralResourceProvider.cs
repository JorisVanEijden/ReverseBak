namespace ResourceExtraction.Providers;

using GameData.Resources;
using GameData.Resources.Data;
using GameData.Resources.Dialog;
using GameData.Resources.Image;
using ResourceExtraction.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ResourceType = ResourceExtraction.ResourceType;

public class GeneralResourceProvider : IResourceProvider {
    private const string ResourceFileName = "KRONDOR.001";
    private const int DosCodePage = 437;
    private const int FileNameLength = 13;

    private readonly IDictionary<string, (long, uint)> _dictionary;
    private readonly string _resourceFilePath;

    public GeneralResourceProvider(string gameDirectory) {
        _resourceFilePath = Path.Combine(gameDirectory, ResourceFileName);
        _dictionary = GetDictionary();
    }

    public IDictionary<string, (long, uint)> GetDictionary(ResourceType? type = null) {
        if (type.HasValue && type.Value != ResourceType.General) {
            throw new ArgumentException($"Invalid resource type: {type.Value}. Expected: {ResourceType.General}");
        }
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

    public ResourceExtraction.ResourceType ResourceType {
        get => ResourceExtraction.ResourceType.General;
    }

    public bool CanProvideResource(string resourceId) {
        // Synthesized (not an archive member) — provided whenever asked, like BookParchment.
        if (string.Equals(resourceId, ChapterCatalog.ResourceId, System.StringComparison.OrdinalIgnoreCase)) {
            return true;
        }
        // The dialog style table likewise has no archive member — the original kept it in the
        // executable's data segment, and GameData carries the rows as code. Provided whenever
        // asked so the faithful table is reachable with or without a mod override.
        if (string.Equals(resourceId, DialogStyleTable.ResourceId, System.StringComparison.OrdinalIgnoreCase)) {
            return true;
        }
        // Derived book-parchment variants (BOOK_EVEN.SCX / BOOK_ODD.SCX) are synthesized from
        // BOOK.SCX, so we can provide them whenever the source is available.
        if (BookParchment.IsVariant(resourceId)) {
            return CanProvideResource(BookParchment.Source);
        }
        string filePath = Path.Combine(Path.GetDirectoryName(_resourceFilePath)!, resourceId);
        return File.Exists(filePath) || _dictionary.ContainsKey(resourceId.ToUpper());
    }

    public Stream GetResourceStream(string filename) {
        string filePath = Path.Combine(Path.GetDirectoryName(_resourceFilePath)!, filename);
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

    public void ExtractAllResources() {
        foreach (KeyValuePair<string, (long, uint)> kvp in _dictionary) {
            var s = GetResourceStream(_resourceFilePath, kvp.Value.Item1, kvp.Value.Item2);
            using var outStream = File.Create(Path.Combine(Path.GetDirectoryName(_resourceFilePath)!, kvp.Key));
            s.CopyTo(outStream);
        }
    }

    public T GetResource<T>(string resourceId) where T : IResource {
        // Synthesized chapter catalog: probe the archive for each chapter's parts (see ChapterCatalogBuilder).
        if (typeof(T) == typeof(ChapterCatalog)) {
            return (T)(IResource)ChapterCatalogBuilder.Build(resourceId, this);
        }

        // Synthesized dialog style table: the seven dialogTypeData rows at 0x3a831 live in
        // GameData as code (see DialogStyleTable), so the shipped table needs no archive read.
        if (typeof(T) == typeof(DialogStyleTable)) {
            return (T)(IResource)DialogStyleTable.CreateShipped(resourceId);
        }

        // Derived book-parchment variants: even = BOOK.SCX as-is, odd = vertically flipped
        // (mirrors the DOS bok_DrawPage page-parity behaviour). Synthesized from the source
        // image rather than read from the archive. See BookParchment.
        if (BookParchment.IsVariant(resourceId)) {
            BackgroundImage source = GetResource<BackgroundImage>(BookParchment.Source);
            return (T)(IResource)BookParchment.Build(resourceId, source);
        }

        // Get the extractor for the requested resource type
        var extractor = ExtractorFactory.GetExtractor<T>();

        // Get the resource stream
        using var stream = GetResourceStream(resourceId);

        // Extract and return the resource
        return extractor.Extract(resourceId, stream);
    }
}

