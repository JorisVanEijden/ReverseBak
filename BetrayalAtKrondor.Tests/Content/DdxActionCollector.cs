namespace BetrayalAtKrondor.Tests.Content;

using System.Collections.Generic;
using System.Text.Json;

/// <summary>Shared JSON walker for the DDX reference-integrity tests. Dialog actions and branches
/// serialize with a polymorphic <c>$type</c> discriminator; this collects the value of an integer
/// field from every object of a given <c>$type</c>, anywhere in the tree. Used to gather the source
/// side of DDX-originating references (e.g. <c>KeywordChoiceBranch.Keyword</c>, <c>Teleport.DestinationId</c>).</summary>
internal static class DdxActionCollector {
    public static IEnumerable<int> CollectIntFieldByType(JsonElement element, string type, string field) {
        switch (element.ValueKind) {
            case JsonValueKind.Object:
                if (element.TryGetProperty("$type", out JsonElement t)
                    && t.ValueKind == JsonValueKind.String
                    && t.GetString() == type
                    && element.TryGetProperty(field, out JsonElement v)
                    && v.ValueKind == JsonValueKind.Number) {
                    yield return v.GetInt32();
                }
                foreach (JsonProperty prop in element.EnumerateObject()) {
                    foreach (int r in CollectIntFieldByType(prop.Value, type, field)) {
                        yield return r;
                    }
                }
                break;
            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray()) {
                    foreach (int r in CollectIntFieldByType(item, type, field)) {
                        yield return r;
                    }
                }
                break;
        }
    }
}
