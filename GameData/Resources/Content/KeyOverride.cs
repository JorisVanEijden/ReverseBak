namespace GameData.Resources.Content;

/// <summary>Records that a later source overrode an already-present key. Not an error (mods override
/// intentionally); the Unity binding logs these — the rule stays logger-free.</summary>
public readonly record struct KeyOverride(string Key, string FromSource, string ToSource);
