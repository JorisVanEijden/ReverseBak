namespace GameData.Resources.Content;

/// <summary>A <see cref="ContentReference"/> whose target key is absent from its catalog — a
/// dangling reference that would silently corrupt under additive merge. Reported by
/// <see cref="ReferenceValidator"/>.</summary>
public readonly record struct BrokenReference(string FromKey, string TargetCatalog, string TargetKey);
