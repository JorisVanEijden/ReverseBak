namespace GameData.Resources.Layout;

using System;
using System.Globalization;
using System.Text.Json.Serialization;

/// <summary>
/// A width:height proportion, written in data as <c>"10:9"</c>. Attach one to a
/// <see cref="LayoutHint"/> whose size is given in percentages so the element keeps its
/// proportions when the window aspect changes — percent-of-width and percent-of-height are
/// different units, so painted art sized purely in percent would otherwise stretch.
/// </summary>
[JsonConverter(typeof(LayoutAspectRatioJsonConverter))]
public readonly record struct LayoutAspectRatio {
    public LayoutAspectRatio(float width, float height) {
        if (!IsUsable(width) || !IsUsable(height)) {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                $"Aspect ratio components must be finite and positive; got {width}:{height}.");
        }

        Width = width;
        Height = height;
    }

    public float Width { get; }

    public float Height { get; }

    /// <summary>Width divided by height.</summary>
    /// <remarks>
    /// The constructor rejects non-finite and non-positive components, so any ratio built
    /// through <see cref="Parse"/> or the constructor is safe. A <c>default</c> struct value
    /// bypasses both — C# guarantees every struct a zero value — so this throws rather than
    /// returning NaN, which would otherwise flow silently into layout arithmetic.
    /// </remarks>
    public float Ratio =>
        Height > 0f
            ? Width / Height
            : throw new InvalidOperationException(
                "This LayoutAspectRatio is a default (uninitialised) value and has no ratio. " +
                "Build one via LayoutAspectRatio.Parse or its constructor.");

    private static bool IsUsable(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;

    public static LayoutAspectRatio Parse(string text) =>
        TryParse(text, out LayoutAspectRatio result)
            ? result
            : throw new FormatException($"'{text}' is not an aspect ratio. Expected \"<width>:<height>\" with both parts positive.");

    public static bool TryParse(string? text, out LayoutAspectRatio result) {
        result = default;
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }

        string[] parts = text!.Split(':');
        if (parts.Length != 2) {
            return false;
        }

        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float width) ||
            !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float height)) {
            return false;
        }

        // float.TryParse accepts "NaN"/"Infinity"/"-Infinity" whatever the NumberStyles, and a
        // non-finite ratio is meaningless as geometry — reject rather than propagate it.
        if (float.IsNaN(width) || float.IsInfinity(width) ||
            float.IsNaN(height) || float.IsInfinity(height)) {
            return false;
        }

        // Zero or negative components are rejected rather than clamped: Ratio divides by
        // Height, and a "0:9" in data is a mistake we want surfaced, not silently absorbed.
        if (width <= 0f || height <= 0f) {
            return false;
        }

        result = new LayoutAspectRatio(width, height);
        return true;
    }

    public override string ToString() =>
        Width.ToString("0.####", CultureInfo.InvariantCulture) + ":" +
        Height.ToString("0.####", CultureInfo.InvariantCulture);
}
