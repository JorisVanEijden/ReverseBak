namespace BetrayalAtKrondor.Tests.Audio;

using GameData.Resources.Audio;
using Xunit;

/// <summary>
/// How a sound variant's kind is decided — there is no format field in the file.
/// </summary>
public class AudioFormatTests {
    [Fact]
    public void TheSENTINELChannelIsTheWholeDiscriminator() {
        // 0xFE is far outside the 0..15 a MIDI channel can be, so it is a marker rather than a
        // channel the synth ever addresses. It was a bare magic number in the extractor.
        Assert.Equal(0xFE, AudioChannel.DigitisedSample);
        Assert.True(AudioChannel.DigitisedSample > 15);
    }

    [Fact]
    public void BothParserLoopsEndOnTheSameByte() {
        // One nested inside the other — variants and the tracks within a variant.
        Assert.Equal(0xFF, AudioChannel.EndOfList);
        Assert.NotEqual(AudioChannel.EndOfList, AudioChannel.DigitisedSample);
    }

    [Fact]
    public void TheFormatIsReadBackOffThePayload() {
        Assert.Equal(AudioFormat.Wav, new AudioDataResource { WavData = new byte[1] }.Format);
        Assert.Equal(AudioFormat.Midi, new AudioDataResource { MidiData = new byte[1] }.Format);
    }

    [Fact]
    public void AVariantCarryingBOTHIsASample() {
        // A variant really can hold both: the sentinel channel and the real ones are read in the
        // same loop, so a digitised sample can arrive alongside midi tracks. The sample plays.
        var both = new AudioDataResource { WavData = new byte[1], MidiData = new byte[1] };
        Assert.Equal(AudioFormat.Wav, both.Format);
    }

    [Fact]
    public void AnEmptyVariantHasNoFormatRatherThanADefaultOne() {
        // Null, not Midi. Defaulting would have a silent variant claim to be a MIDI track and get
        // handed to the synth.
        Assert.Null(new AudioDataResource().Format);
    }
}
