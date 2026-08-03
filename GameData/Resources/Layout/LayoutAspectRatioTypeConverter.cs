namespace GameData.Resources.Layout;

using System;
using System.ComponentModel;
using System.Globalization;

/// <summary>Lets Newtonsoft.Json (which has no built-in <c>[JsonConverter]</c> support and
/// ignores System.Text.Json attributes) read/write <see cref="LayoutAspectRatio"/> as a bare
/// string. Newtonsoft honours <see cref="TypeConverter"/> natively for scalar (de)serialization,
/// so this is the Newtonsoft-side counterpart of <see cref="LayoutAspectRatioJsonConverter"/>
/// (the STJ side) — both delegate to <see cref="LayoutAspectRatio.Parse"/>/
/// <see cref="object.ToString"/> rather than duplicating the parsing logic.</summary>
public class LayoutAspectRatioTypeConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string text ? LayoutAspectRatio.Parse(text) : base.ConvertFrom(context, culture, value);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        destinationType == typeof(string) && value is LayoutAspectRatio ratio
            ? ratio.ToString()
            : base.ConvertTo(context, culture, value, destinationType);
}
