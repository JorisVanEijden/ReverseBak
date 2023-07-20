namespace BetrayalAtKrondor.Overrides.Libraries;

using Spice86.Core.Emulator.CPU;
using Spice86.Core.Emulator.Memory;
using Spice86.Shared.Utils;

using System.Text.RegularExpressions;

public partial class CFunctions {
    private static readonly Regex FormatRegex = PrintFRegex();
    private readonly State _state;
    private readonly Stack _stack;
    private readonly Memory _memory;

    public CFunctions(Cpu cpu, Memory memory) {
        _state = cpu.State;
        _stack = cpu.Stack;
        _memory = memory;
    }

    public string _sprintf(string format) {
        var segments = new List<string>();
        MatchCollection matches = FormatRegex.Matches(format);
        if (matches.Count == 0)
            return string.Empty;

        int lastEnd = 0;
        int stackPos = 8;

        foreach (Match match in matches) {
            if (lastEnd < match.Index) {
                string item = format.Substring(lastEnd, match.Index - lastEnd);
                segments.Add(item);
            }

            var specification = new FormatSpecification(match);
            object arg;
            switch (specification.Length) {
                case LengthType.Long:
                    arg = _stack.Peek32(stackPos);
                    stackPos += 4;
                    break;
                case LengthType.Far:
                    ushort offset = _stack.Peek16(stackPos);
                    ushort segment = _stack.Peek16(stackPos + 2);
                    arg = MemoryUtils.ToPhysicalAddress(segment, offset);
                    stackPos += 4;
                    break;
                case LengthType.Short:
                case LengthType.Near:
                    arg = _stack.Peek16(stackPos);
                    stackPos += 2;
                    break;
                case LengthType.None:
                default:
                    arg = _stack.Peek16(stackPos);
                    stackPos += 2;
                    break;
            }

            if (specification.Specifier == SpecifierType.String) {
                uint address = specification.Length == LengthType.Far
                    ? (uint)arg
                    : MemoryUtils.ToPhysicalAddress(_state.DS, (ushort)arg);
                arg = _memory.GetZeroTerminatedString(address, int.MaxValue);
            }

            // if (!specification.Width.HasValue) {
            //     specification.Width = _stack.Peek16(stackPos);
            //     stackPos += 2;
            // }
            // if (!specification.Precision.HasValue) {
            //     specification.Precision = _stack.Peek16(stackPos);
            //     stackPos += 2;
            // }

            segments.Add(specification.Format(arg));

            lastEnd = match.Index + match.Value.Length;
        }

        if (lastEnd < format.Length) {
            segments.Add(format[lastEnd..]);
        }

        return string.Join(null, segments);
    }

    [GeneratedRegex(@"\%(\d*\$)?([\#\-\+0 ]*)(\d*|\*)(?:\.(\d+|\*))?([hlFN])?(\[(.+)\])?([dioxXucseEfgGp%])", RegexOptions.Compiled)]
    private static partial Regex PrintFRegex();

    [Flags]
    private enum Flags {
        None = 0, // <the default>
        LeftAligned = 1, // -
        LeadingZeroFill = 2, // 0
        ForceSign = 4, // +
        InvisiblePlusSign = 8, // <space>
        Alternate = 16 // #
    }

    private enum SpecifierType {
        SignedInt, // d, i
        UnsignedInt, // u  
        UnsignedOct, // o
        UnsignedHex, // h    
        UnsignedHexUpper, // H  
        Float, // f  
        FloatUpper, // F
        Scientific, // e
        ScientificUpper, // E
        General, // g
        GeneralUpper, // G
        Char, // c
        String, // s
        Percent, // %
        Pointer // p
    }

    private enum LengthType {
        None,
        Short, // 16 bit number
        Long, // 32 bit number
        Near, // 16 bit pointer
        Far // 32 bit pointer
    }

    private class FormatSpecification {
        public FormatSpecification(Match match) {
            Parse(match);
        }

        public Flags Flags { get; set; }
        public int? Width { get; set; }
        public int? Precision { get; set; }
        public LengthType Length { get; set; }
        public string? FormattingString { get; set; }
        public SpecifierType Specifier { get; set; }

        public void Parse(Match match) {
            string flagsParameter = match.Groups[2].Value;
            string widthParameter = match.Groups[3].Value;
            string precisionParameter = match.Groups[4].Value;
            string lengthParameter = match.Groups[5].Value;
            string unknownParameter = match.Groups[6].Value;
            string formattingString = match.Groups[7].Value;
            string specifierParameter = match.Groups[8].Value;

            Flags = ParseFlags(flagsParameter);
            Width = ParseWidth(widthParameter);
            Precision = ParsePrecision(precisionParameter);
            Length = ParseLength(lengthParameter);

            FormattingString = formattingString;
            Specifier = ParseSpecifier(specifierParameter);
        }

        private static SpecifierType ParseSpecifier(string specifierParameter) {
            return specifierParameter[0] switch {
                'd' => SpecifierType.SignedInt,
                'i' => SpecifierType.SignedInt,
                'u' => SpecifierType.UnsignedInt,
                'o' => SpecifierType.UnsignedOct,
                'x' => SpecifierType.UnsignedHex,
                'X' => SpecifierType.UnsignedHexUpper,
                'f' => SpecifierType.Float,
                'F' => SpecifierType.FloatUpper,
                'e' => SpecifierType.Scientific,
                'E' => SpecifierType.ScientificUpper,
                'g' => SpecifierType.General,
                'G' => SpecifierType.GeneralUpper,
                'c' => SpecifierType.Char,
                's' => SpecifierType.String,
                'p' => SpecifierType.Pointer,
                '%' => SpecifierType.Percent,
                _ => throw new ArgumentOutOfRangeException(nameof(specifierParameter), specifierParameter, "invalid specifier")
            };
        }

        private static LengthType ParseLength(string lengthParameter) {
            return lengthParameter switch {
                "h" => LengthType.Short,
                "l" => LengthType.Long,
                "F" => LengthType.Far,
                "N" => LengthType.Near,
                _ => LengthType.None
            };
        }

        private static int? ParsePrecision(string precisionParameter) {
            if (string.IsNullOrEmpty(precisionParameter) || precisionParameter == "*")
                return null;
            if (int.TryParse(precisionParameter, out int precision))
                return precision;
            throw new ArgumentException("invalid precision parameter, should be * or integer", nameof(precisionParameter));
        }

        private static Flags ParseFlags(string flagsParameter) {
            Flags flags = Flags.None;
            if (flagsParameter.Contains('-'))
                flags |= Flags.LeftAligned;
            if (flagsParameter.Contains('+'))
                flags |= Flags.ForceSign;
            if (flagsParameter.Contains('#'))
                flags |= Flags.Alternate;
            if (flagsParameter.Contains('0'))
                flags |= Flags.LeadingZeroFill;
            if (flagsParameter.Contains(' ') && !flags.HasFlag(Flags.ForceSign)) {
                flags |= Flags.InvisiblePlusSign;
            }
            return flags;
        }

        private static int? ParseWidth(string? widthParameter) {
            if (string.IsNullOrEmpty(widthParameter) || widthParameter == "*")
                return null;
            if (int.TryParse(widthParameter, out int width))
                return width;
            throw new ArgumentException("invalid width parameter, should be * or integer", nameof(widthParameter));
        }

        public string Format(object obj) {
            return Specifier switch {
                SpecifierType.SignedInt => FormatNumber("d", obj),
                SpecifierType.UnsignedInt => FormatNumber("d", obj),
                SpecifierType.UnsignedOct => FormatString(Convert.ToString((long)obj, 8)),
                SpecifierType.UnsignedHex => FormatNumber("x", obj),
                SpecifierType.UnsignedHexUpper => FormatNumber("X", obj),
                SpecifierType.Float => FormatNumber("f", obj),
                SpecifierType.FloatUpper => FormatNumber("F", obj),
                SpecifierType.Scientific => FormatNumber("e", obj),
                SpecifierType.ScientificUpper => FormatNumber("E", obj),
                SpecifierType.General => FormatNumber("g", obj),
                SpecifierType.GeneralUpper => FormatNumber("G", obj),
                SpecifierType.Char => obj is int c ? FormatString(((char)c).ToString()) : FormatString(obj.ToString() ?? string.Empty),
                SpecifierType.String => FormatString(obj.ToString() ?? string.Empty),
                SpecifierType.Pointer => FormatString("0x" + Convert.ToString((int)obj, 16)),
                SpecifierType.Percent => "%",
                _ => throw new ArgumentOutOfRangeException(nameof(Specifier), Specifier, "Invalid format specifier")
            };
        }

        private string FormatNumber(string specifier, object value) {
            string str = "{0:" + specifier + (Precision.HasValue ? Precision.ToString() : string.Empty) + "}";
            str = string.Format(str, value);

            if (Flags.HasFlag(Flags.ForceSign)
                && str[0] != '-'
                && Specifier != SpecifierType.UnsignedHex
                && Specifier != SpecifierType.UnsignedHexUpper) {
                str = "+" + str;
            }

            if (Width.HasValue && str.Length < Width.Value) {
                if (Flags.HasFlag(Flags.LeftAligned))
                    return str.PadRight(Width.Value, ' ');
                if (Flags.HasFlag(Flags.LeadingZeroFill) && (str[0] == '-' || str[0] == '+')) {
                    return str[0] + str[1..].PadLeft(Width.Value - 1, '0');
                }
                return str.PadLeft(Width.Value, Flags.HasFlag(Flags.LeadingZeroFill) ? '0' : ' ');
            }
            return str;
        }

        private string FormatString(string value) {
            if (Width.HasValue && Width.Value > value.Length) {
                return Flags.HasFlag(Flags.LeftAligned)
                    ? value.PadRight(Width.Value)
                    : value.PadLeft(Width.Value);
            }
            return value;
        }
    }
}