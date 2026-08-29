namespace BetrayalAtKrondor.Tests.Character;

using System;
using System.IO;
using System.Linq;
using GameData.Resources.Character;
using Xunit;

/// <summary>
/// Where the stat-modifier block lives, and that we read it correctly (TASK-251).
/// </summary>
/// <remarks>
/// <b>Deliberately NOT only a round trip.</b> The sibling TASK-203 offset test exists because a
/// reader and writer sharing one offset constant stayed self-consistent while the offset was wrong,
/// and the defect survived for exactly as long as nothing looked at it from outside. So the load
/// straight — see <see cref="TheBlockIsEmptyInTheSHIPPEDSaveAndGarbageOneHeaderEarly"/>.
/// </remarks>
public class ActorStatModifiersBlockTests {
    [Fact]
    public void THEOFFSETIsWhereTheConditionRanksEnd() {
        // Derived, not restated: ranks start at 0x2CC and run 6 characters x 7 bytes. TASK-203
        // already pinned that they end here with no trailing gap, so this block must start there.
        const int ranksStart = 0x2cc;
        const int ranksSize = 6 * 7;

        Assert.Equal(ranksStart + ranksSize, ActorStatModifiers.BodyOffset);
        Assert.Equal(672, ActorStatModifiers.BlockSize);
    }

    [Fact]
    public void ASLOTRoundTripsThroughTheBlock() {
        var body = new byte[ActorStatModifiers.BodyOffset + ActorStatModifiers.BlockSize];
        var slots = new ActorStatModifiers.Slot[48];
        slots[ActorStatModifiers.IndexOf(2, 3)] =
            new ActorStatModifiers.Slot(0x0833, 0x0040, -7, 1234u, 5678u);

        Assert.True(ActorStatModifiers.Save(slots, body));
        ActorStatModifiers.Slot[] back = ActorStatModifiers.Load(body);

        ActorStatModifiers.Slot one = back[ActorStatModifiers.IndexOf(2, 3)];
        Assert.Equal(0x0833, one.Flags);
        Assert.Equal(0x0040, one.StatMask);
        Assert.Equal((short)-7, one.Value);
        Assert.Equal(1234u, one.AppliedAt);
        Assert.Equal(5678u, one.ExpiresAt);
        Assert.All(back.Where((_, i) => i != ActorStatModifiers.IndexOf(2, 3)),
            s => Assert.True(s.IsEmpty));
    }

    [Fact]
    public void ANEGATIVEValueSurvives_BecauseItIsSignedAndTheOthersAreNot() =>
        // Value is the only signed field in the slot. Round-tripping it through ushort would turn
        // every penalty into a large bonus.
        Assert.Equal((short)-32000, RoundTrip(new ActorStatModifiers.Slot(1, 1, -32000, 0, 0)).Value);

    [Fact]
    public void ASHORTBodyYieldsNoModifiersRatherThanThrowing() {
        Assert.All(ActorStatModifiers.Load(new byte[10]), s => Assert.True(s.IsEmpty));
        Assert.All(ActorStatModifiers.Load(null), s => Assert.True(s.IsEmpty));
        Assert.False(ActorStatModifiers.Save(new ActorStatModifiers.Slot[48], new byte[10]));
    }

    [Fact]
    public void TheBlockIsEmptyInTheSHIPPEDSaveAndGarbageOneHeaderEarly() {
        // *** THE ASSERTION A ROUND TRIP CANNOT MAKE. *** BodyOffset is an offset into the TEMP.GAM
        // BODY; a SAVE##.GAM puts a 100-byte header in front. Read correctly, SAVE02's block is 48
        // cleanly empty slots. Read as a FILE offset -- the natural mistake -- it lands on the
        // condition ranks and their neighbours and yields slots that look populated, with values
        // like 10023 and expiry stamps in the billions. Only the shipped bytes separate those.
        byte[]? save = ReadSave();
        if (save == null) {
            return;   // skip-if-absent, like the other game-data tests
        }

        const int headerSize = 100;
        byte[] body = save.Skip(headerSize).ToArray();

        Assert.All(ActorStatModifiers.Load(body), s => Assert.True(s.IsEmpty,
            "a clean save should carry no active stat modifiers"));

        ActorStatModifiers.Slot[] misread = ActorStatModifiers.Load(save);
        Assert.Contains(misread, s => !s.IsEmpty);
    }

    private static ActorStatModifiers.Slot RoundTrip(ActorStatModifiers.Slot slot) {
        var body = new byte[ActorStatModifiers.BodyOffset + ActorStatModifiers.BlockSize];
        var slots = new ActorStatModifiers.Slot[48];
        slots[0] = slot;
        ActorStatModifiers.Save(slots, body);
        return ActorStatModifiers.Load(body)[0];
    }

    private static byte[]? ReadSave() {
        foreach (string root in new[] { "../../../../..", "../../../../../.." }) {
            string p = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, root, "OriginalGame", "GAMES", "dir.G01", "SAVE02.GAM"));
            if (File.Exists(p)) {
                return File.ReadAllBytes(p);
            }
        }
        return null;
    }
}
