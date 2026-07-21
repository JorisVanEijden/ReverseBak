namespace GameData.Resources.Content;

/// <summary>One (key, value) contribution from a source, keyed by a <see cref="ContentKey"/>.</summary>
public readonly record struct ContentEntry<T>(string Key, T Value);
