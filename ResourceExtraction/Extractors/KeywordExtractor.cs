namespace ResourceExtraction.Extractors;

using GameData.Resources.Data;

using ResourceExtraction.Extensions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class KeywordExtractor : ExtractorBase<KeywordList> {
    public override KeywordList Extract(string id, Stream resourceStream) {
        using var resourceReader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));

        int size = resourceReader.ReadInt16();
        int amount = resourceReader.ReadInt16();

        var offsets = new int[amount];
        for (var i = 0; i < amount; i++) {
            offsets[i] = resourceReader.ReadInt16();
            Console.WriteLine($"{offsets[i]:X4}");
        }
        var keywords = new Dictionary<int, string>(amount);
        for (var i = 0; i < amount; i++) {
            resourceReader.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
            keywords[i] = resourceReader.ReadZeroTerminatedString();
        }

        return new KeywordList(id, keywords);
    }
}