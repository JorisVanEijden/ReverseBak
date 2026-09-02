namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

/// <summary>
/// That <c>ScaleX</c>/<c>ScaleY</c> really are a fraction of the screen.
/// </summary>
/// <remarks>
/// <b>Two bare doubles whose name says the opposite of what they hold.</b> They read as a
/// multiplier; they are the image's size relative to the screen, so 1.0 means full-width. A consumer
/// that guesses wrong does not crash — it draws at the wrong size, which is how a 16x16 test image
/// came to fill a whole cutscene buffer on 2026-09-02.
///
/// <para>Pinned against the shipped corpus rather than one sample, because the claim is about a
/// CONVENTION: if an extractor change ever made the field a real multiplier, every consumer would
/// silently resize and nothing else would notice.</para>
///
/// <para>Skips rather than fails when <c>generated/</c> is absent, the same contract the other
/// corpus tests use.</para>
/// </remarks>
public class ImageScaleConventionTests {
    /// <summary>The canonical screen the extracted widths are expressed in.</summary>
    private const double CanonicalWidth = 1600;

    private const double CanonicalHeight = 1200;

    private static IEnumerable<string> ImageJson(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, "BMX"), "*.json", SearchOption.AllDirectories)
            .Where(f => char.IsDigit(Path.GetFileNameWithoutExtension(f)[0]));

    [Fact]
    public void ScaleIsTheImagesSizeAsAFractionOfTheScreen() {
        string? root = GeneratedCorpus.FindDir("BMX");
        if (root == null) {
            return;
        }

        var checkedCount = 0;
        foreach (string file in ImageJson(root).Take(4000)) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(file));
            JsonElement e = doc.RootElement;
            if (!e.TryGetProperty("Width", out JsonElement w)
                || !e.TryGetProperty("ScaleX", out JsonElement sx)
                || !e.TryGetProperty("Height", out JsonElement h)
                || !e.TryGetProperty("ScaleY", out JsonElement sy)) {
                continue;
            }

            Assert.Equal(w.GetInt32() / CanonicalWidth, sx.GetDouble(), 6);
            Assert.Equal(h.GetInt32() / CanonicalHeight, sy.GetDouble(), 6);
            checkedCount++;
        }

        // Non-vacuous: an empty enumeration would otherwise pass silently.
        Assert.True(checkedCount > 100, $"expected a corpus, checked {checkedCount}");
    }
}
