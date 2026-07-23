namespace GameData.Resources.Content;

/// <summary>An edge in the content graph: entry <see cref="FromKey"/> references the entry
/// <see cref="TargetKey"/> in catalog <see cref="TargetCatalog"/>. Emitted by an extractor's
/// reference declaration in place of a raw index/offset, so additive merges stay safe.</summary>
public readonly record struct ContentReference(string FromKey, string TargetCatalog, string TargetKey);
