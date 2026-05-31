namespace ResourceExtraction.Extractors;

using GameData.Resources.Config;

using System.IO;

/// <summary>
/// Parses the original game's 5-byte preferences blob (KRONDOR.CFG, and the
/// shipped DEFAULT.DAT / CDEFAULT.DAT defaults) into the engine-independent
/// <see cref="Preferences"/> model.
///
/// Layout (verified against dialog_Preferences @ 0x6f33f and the `config`
/// struct):
///   byte 0  stepSize       (0..2)
///   byte 1  turnSize       (0..2)
///   byte 2  levelOfDetail  (0..3)
///   byte 3  textSpeed      (0..2)
///   byte 4  flags bitfield: 0x01 Sound, 0x02 GameMusic, 0x04 CombatMusic,
///                           0x08 Introduction, 0x10 CdMusic
/// DEFAULT.DAT  = 02 01 02 00 0F (Cd off); CDEFAULT.DAT = 02 01 02 00 1F.
/// </summary>
public class PreferencesExtractor : ExtractorBase<Preferences> {
    private const int SoundFlag = 0x01;
    private const int GameMusicFlag = 0x02;
    private const int CombatMusicFlag = 0x04;
    private const int IntroductionFlag = 0x08;
    private const int CdMusicFlag = 0x10;

    public override Preferences Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var stepSize = reader.ReadByte();
        var turnSize = reader.ReadByte();
        var levelOfDetail = reader.ReadByte();
        var textSpeed = reader.ReadByte();
        var flags = reader.ReadByte();

        return new Preferences(id) {
            StepSize = (StepSize)stepSize,
            TurnSize = (TurnSize)turnSize,
            DetailLevel = (DetailLevel)levelOfDetail,
            TextSpeed = (TextSpeed)textSpeed,
            Sound = (flags & SoundFlag) != 0,
            GameMusic = (flags & GameMusicFlag) != 0,
            CombatMusic = (flags & CombatMusicFlag) != 0,
            Introduction = (flags & IntroductionFlag) != 0,
            CdMusic = (flags & CdMusicFlag) != 0
        };
    }
}
