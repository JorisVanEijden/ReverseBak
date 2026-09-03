namespace BetrayalAtKrondor.Tests.Dialog;

using GameData.Resources.Dialog.Actions;
using Xunit;

/// <summary>
/// What a dialog's <c>PlayAudio</c> id means.
/// </summary>
public class DialogAudioTests {
    [Fact]
    public void IdZeroIsSILENT_onTheBuildWeTarget() {
        // *** THE GUARD IS CD-ONLY, AND IT COVERS 157 SHIPPED INSTANCES. ***
        // audio_sfx_play_n_times opens `#ifdef V102CD / if (sfx_id < 1) return 0;` — the floppy has
        // no such test. Id 0 is the most common single value in the shipped PlayAudio actions and
        // there is no sound 0 in the extracted corpus, so playing them would be 157 misses a
        // playthrough.
        Assert.Equal(DialogAudio.Kind.Silent, DialogAudio.KindOf(0));
        Assert.Equal(DialogAudio.Kind.Silent, DialogAudio.KindOf(-1));
    }

    [Fact]
    public void TheSplitIsAtAThousand() {
        Assert.Equal(DialogAudio.Kind.Sound, DialogAudio.KindOf(1));
        Assert.Equal(DialogAudio.Kind.Sound, DialogAudio.KindOf(999));
        Assert.Equal(DialogAudio.Kind.Song, DialogAudio.KindOf(1000));
        Assert.Equal(DialogAudio.Kind.Song, DialogAudio.KindOf(1041));
    }

    [Fact]
    public void TheThreePassesAreDistinctAndDefaultToTheFirst() {
        // The default matters: it is 572 of the 779 shipped instances, so a port that ignored
        // Timing would still be right most of the time and wrong in a way nobody would trace.
        Assert.Equal(0, (int)PlayAudioTiming.BeforeActions);
        Assert.Equal(1, (int)PlayAudioTiming.WithActions);
        Assert.Equal(2, (int)PlayAudioTiming.AfterActions);
        Assert.Equal(PlayAudioTiming.BeforeActions, new PlayAudioAction().Timing);
    }
}
