namespace ResourceExtraction.Tests.Animation;

using ResourceExtraction.Extractors.Animation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

/// <summary>
/// The TTM opcode stream's one structural invariant: <b>the low nibble of an opcode is its argument
/// count, in 16-bit words</b>. The original never looks at what a command means to know how far to
/// step — <c>ttmscript_run_block</c> does a bare <c>FP_OFF(w) += low * 2</c> — so the nibble is the
/// format, not a convention.
///
/// <para>Our extractor has no such skip: it advances only by what each case happens to read. That is
/// fine while every case reads the right number, and silently corrupts every command after it the
/// moment one does not — the stream desyncs mid-frame and the following bytes are read as opcodes.
/// This test asserts the property the extractor relies on but never states.</para>
/// </summary>
public class TtmOpcodeArityTests {
    /// <summary>Low nibble that means "a string argument follows" rather than a word count.</summary>
    private const int StringArgumentNibble = 0xf;

    /// <summary>Opcode that closes a frame; the loop consumes it without dispatching.</summary>
    private const ushort EndOfFrame = 0x0FF0;

    /// <summary>
    /// The opcodes whose low nibble is <see cref="StringArgumentNibble"/>. Their length lives in the
    /// data rather than the opcode, so the arity rule cannot cover them — they are listed here so a
    /// newly added one has to be looked at rather than quietly joining the exempt set.
    /// </summary>
    private static readonly ushort[] KnownStringOpcodes =
        { 0xC02F, 0xF01F, 0xF02F, 0xF04F, 0xF05F };

    /// <summary>Every opcode the extractor claims, discovered by asking it.</summary>
    private static IEnumerable<ushort> HandledOpcodes() {
        for (var opcode = 0; opcode <= ushort.MaxValue; opcode++) {
            if (opcode == EndOfFrame) {
                continue;
            }
            if (Consumed((ushort)opcode, out _)) {
                yield return (ushort)opcode;
            }
        }
    }

    /// <summary>
    /// Runs one command against a zero-filled buffer and reports how many bytes it read.
    /// </summary>
    /// <returns>False when the extractor does not handle this opcode at all.</returns>
    private static bool Consumed(ushort opcode, out long bytes) {
        bytes = 0;
        // Generous, so an over-reading case reports its real appetite instead of hitting the end.
        using var stream = new MemoryStream(new byte[256]);
        using var reader = new BinaryReader(stream, Encoding.ASCII);
        try {
            TtmExtractor.GetFrameCommand(opcode, reader);
        } catch (Exception) {
            return false;
        }
        bytes = stream.Position;

        return true;
    }

    [Fact]
    public void EveryHandledOpcodeReadsExactlyTheWordsItsLowNibbleDeclares() {
        var wrong = new List<string>();
        var handled = 0;

        foreach (ushort opcode in HandledOpcodes()) {
            handled++;
            int nibble = opcode & 0xf;
            if (nibble == StringArgumentNibble) {
                continue; // Variable-length by definition — see the string-opcode test below.
            }

            Consumed(opcode, out long bytes);
            if (bytes != nibble * 2) {
                wrong.Add($"0x{opcode:X4} declares {nibble} word(s) but read {bytes} byte(s)");
            }
        }

        Assert.True(handled > 0, "no opcodes were discovered — GetFrameCommand's contract changed");
        Assert.Empty(wrong);
    }

    [Fact]
    public void TheHandledSetIsTheSizeWeThinkItIs() {
        // A tripwire, not a spec: if this moves, a command was added or removed and the arity check
        // above is the thing to confirm still passes for it.
        var handled = 0;

        foreach (ushort unused in HandledOpcodes()) {
            handled++;
        }

        Assert.Equal(55, handled);
    }

    [Fact]
    public void TheOnlyOpcodesExemptFromTheArityRuleAreTheStringOnes() {
        var exempt = new List<ushort>();

        foreach (ushort opcode in HandledOpcodes()) {
            if ((opcode & 0xf) == StringArgumentNibble) {
                exempt.Add(opcode);
            }
        }

        Assert.Equal(KnownStringOpcodes, exempt);
    }

    [Theory]
    [InlineData((ushort)0x1301, (ushort)0xC051)] // play sound
    [InlineData((ushort)0x1311, (ushort)0xC061)] // stop sound
    public void TheTwoAliasedSoundOpcodesProduceTheSameCommand(ushort alias, ushort canonical) {
        // ttmscript_run_block rewrites the opcode before dispatch (TTM.C:352), so these are not two
        // commands that happen to look alike — they are one command with two spellings.
        using var aliasStream = new MemoryStream(new byte[16]);
        using var canonicalStream = new MemoryStream(new byte[16]);

        var fromAlias = TtmExtractor.GetFrameCommand(alias, new BinaryReader(aliasStream));
        var fromCanonical = TtmExtractor.GetFrameCommand(canonical, new BinaryReader(canonicalStream));

        Assert.Equal(fromCanonical.GetType(), fromAlias.GetType());
        Assert.Equal(canonicalStream.Position, aliasStream.Position);
    }

    [Fact]
    public void AnUnknownOpcodeIsRefusedRatherThanSkipped() {
        // There is no length to skip by without the nibble, and guessing would desync the stream.
        // Failing loudly is why the shipped files can be trusted to contain only handled opcodes.
        using var stream = new MemoryStream(new byte[16]);

        Assert.ThrowsAny<Exception>(() => TtmExtractor.GetFrameCommand(0x0FFF, new BinaryReader(stream)));
    }
}
