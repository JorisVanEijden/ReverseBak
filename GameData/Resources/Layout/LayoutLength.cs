namespace GameData.Resources.Layout;

using System;
using System.Globalization;
using System.Text.Json.Serialization;

/// <summary>Which space a <see cref="LayoutLength"/> is measured in.</summary>
public enum LayoutLengthUnit {
    /// <summary>Size determined by content or by the layout engine.</summary>
    Auto,

    /// <summary>Design-frame pixels (the canonical 1600x1200 space). Resolution-independent:
    /// the whole frame scales to the window, so a px value is virtual, not physical.</summary>
    Px,

    /// <summary>Percentage of the parent's corresponding axis. Note that percent-of-width and
    /// percent-of-height are different units, so a shape sized purely in percent does not keep
    /// its proportions when the aspect ratio changes — pair with LayoutAspectRatio when it must.</summary>
    Percent
}

/// <summary>
/// A layout length with its unit, written in data as a bare string: <c>"200px"</c>,
/// <c>"12.5%"</c>, <c>"auto"</c>. Extractors emit <see cref="LayoutLengthUnit.Px"/> in
/// design-frame units so extracted layout reproduces the original exactly; override
/// authors use <see cref="LayoutLengthUnit.Percent"/> where content should reflow.
/// </summary>
[JsonConverter(typeof(LayoutLengthJsonConverter))]
public readonly record struct LayoutLength(float Value, LayoutLengthUnit Unit) {
    public static LayoutLength Auto => new(0f, LayoutLengthUnit.Auto);

    public static LayoutLength Px(float value) => new(value, LayoutLengthUnit.Px);

    public static LayoutLength Percent(float value) => new(value, LayoutLengthUnit.Percent);

    public static LayoutLength Parse(string text) =>
        TryParse(text, out LayoutLength result)
            ? result
            : throw new FormatException(
                $"'{text}' is not a layout length. Expected \"<number>px\", \"<number>%\", or \"auto\".");

    public static bool TryParse(string? text, out LayoutLength result) {
        result = Auto;
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }

        string trimmed = text!.Trim();
        if (string.Equals(trimmed, "auto", StringComparison.OrdinalIgnoreCase)) {
            result = Auto;
            return true;
        }

        LayoutLengthUnit unit;
        string number;
        if (trimmed.EndsWith("%", StringComparison.Ordinal)) {
            unit = LayoutLengthUnit.Percent;
            number = trimmed.Substring(0, trimmed.Length - 1);
        } else if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase)) {
            unit = LayoutLengthUnit.Px;
            number = trimmed.Substring(0, trimmed.Length - 2);
        } else {
            return false;
        }

        // InvariantCulture deliberately: layout data is machine-written and must parse
        // identically regardless of the machine's locale.
        if (!float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) {
            return false;
        }

        // float.TryParse accepts "NaN"/"Infinity"/"-Infinity" whatever the NumberStyles, and a
        // non-finite length is meaningless as geometry — reject rather than propagate it.
        if (float.IsNaN(value) || float.IsInfinity(value)) {
            return false;
        }

        result = new LayoutLength(value, unit);
        return true;
    }

    public override string ToString() {
        if (Unit == LayoutLengthUnit.Auto) {
            return "auto";
        }

        string number = Value.ToString("0.####", CultureInfo.InvariantCulture);
        return Unit == LayoutLengthUnit.Percent ? number + "%" : number + "px";
    }
}
