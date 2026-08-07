namespace BetrayalAtKrondor.Tests.Text;

using ResourceExtraction.Extractors.Exe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

public class ExeStringReaderTests {
    // A fixed-width table: NUL-padded entries at a constant stride, exactly like the
    // attribute table at 0x37930 (stride 15) and conditions at 0x37897 (stride 23).
    private static byte[] Exe(string prefix, IEnumerable<string> entries, int stride) {
        var bytes = new List<byte>(Encoding.ASCII.GetBytes(prefix));
        foreach (string e in entries) {
            byte[] raw = Encoding.ASCII.GetBytes(e);
            bytes.AddRange(raw);
            for (int i = raw.Length; i < stride; i++) {
                bytes.Add(0);
            }
        }
        return bytes.ToArray();
    }

    [Fact]
    public void ReadsAFixedWidthTableByStride() {
        byte[] exe = Exe("junk", new[] { "Health", "Stamina", "Speed" }, 15);
        IReadOnlyList<string> t = ExeStringReader.ReadTable(exe, "Health", 15, 3);
        Assert.Equal(new[] { "Health", "Stamina", "Speed" }, t);
    }

    // The anchor must be a whole NUL-terminated entry, not a substring: "Health" must not
    // match inside "HealthPotion".
    [Fact]
    public void AnchorDoesNotMatchASubstring() {
        byte[] exe = Exe("", new[] { "HealthPotion", "Health" }, 15);
        IReadOnlyList<string> t = ExeStringReader.ReadTable(exe, "Health", 15, 1);
        Assert.Equal("Health", t[0]);
    }

    [Fact]
    public void MissingAnchorThrowsNamingIt() {
        byte[] exe = Exe("", new[] { "Other" }, 15);
        InvalidDataException ex = Assert.Throws<InvalidDataException>(
            () => ExeStringReader.ReadTable(exe, "Health", 15, 1));
        Assert.Contains("Health", ex.Message);
    }

    [Fact]
    public void TableRunningPastTheEndThrows() {
        byte[] exe = Exe("", new[] { "Health" }, 15);
        Assert.Throws<InvalidDataException>(() => ExeStringReader.ReadTable(exe, "Health", 15, 4));
    }

    // The 1993 compiler did not pool duplicate literals, so the same text appears at
    // several addresses and each call site is keyed separately. occurrence is 0-based.
    [Fact]
    public void SingleSelectsTheRequestedOccurrence() {
        byte[] exe = Exe("", new[] { "Quarrel", "Other", "Quarrel" }, 10);
        Assert.Equal("Quarrel", ExeStringReader.ReadSingle(exe, "Quarrel", 1));
    }

    [Fact]
    public void SingleThrowsWhenTheOccurrenceIsAbsent() {
        byte[] exe = Exe("", new[] { "Quarrel" }, 10);
        InvalidDataException ex = Assert.Throws<InvalidDataException>(
            () => ExeStringReader.ReadSingle(exe, "Quarrel", 1));
        Assert.Contains("Quarrel", ex.Message);
    }
}
