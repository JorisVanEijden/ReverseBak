namespace ResourceExtraction.Tests.Extractors;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using GameData.Resources.Cursor;
using Xunit;

/// <summary>Guards the hand-authored generated/POINTER/cursor-map.json against the GameCursor enum and
/// the extracted CursorSet image counts. Skips when the committed artifacts aren't present.</summary>
public class CursorMapTests {
    private static string? FindGenerated(string relative) {
        string? dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir)) {
            string candidate = Path.Combine(dir, "generated", relative);
            if (File.Exists(candidate)) {
                return candidate;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    [SkippableFact]
    public void EveryGameCursor_HasRow_WithInRangeIndexForItsSet() {
        string? mapPath = FindGenerated(Path.Combine("POINTER", "cursor-map.json"));
        Skip.If(mapPath == null, "generated/POINTER/cursor-map.json not found");

        using JsonDocument map = JsonDocument.Parse(File.ReadAllText(mapPath!));
        JsonElement sets = map.RootElement.GetProperty("sets");
        JsonElement cursors = map.RootElement.GetProperty("cursors");

        // image count per set name (e.g. "POINTER" -> 3) read from the extracted CursorSet JSON.
        var imageCount = new Dictionary<string, int>();
        foreach (JsonProperty set in sets.EnumerateObject()) {
            string? setJson = FindGenerated(Path.Combine("POINTER", $"{set.Name}.json"));
            Skip.If(setJson == null, $"generated/POINTER/{set.Name}.json not found");
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(setJson!));
            imageCount[set.Name] = doc.RootElement.GetProperty("Images").GetArrayLength();
        }

        foreach (GameCursor cursor in Enum.GetValues(typeof(GameCursor))) {
            Assert.True(cursors.TryGetProperty(cursor.ToString(), out JsonElement row),
                $"GameCursor.{cursor} has no row in cursor-map.json");
            string setName = row.GetProperty("set").GetString()!;
            int index = row.GetProperty("index").GetInt32();
            Assert.True(imageCount.ContainsKey(setName), $"{cursor}: unknown set '{setName}'");
            Assert.InRange(index, 0, imageCount[setName] - 1);
        }
    }
}
