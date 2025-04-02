namespace ResourceExtraction.Extractors;

using GameData.Resources.Audio;
using System.IO;

public class SoundExtractor : ExtractorBase<SoundEffect> {
    public override SoundEffect Extract(string id, Stream resourceStream) {
        throw new System.NotImplementedException();
    }
}