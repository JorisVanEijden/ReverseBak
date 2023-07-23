namespace BetrayalAtKrondor.Overrides.Libraries;

using Spice86.Core.Emulator.CPU;
using Spice86.Core.Emulator.Memory;
using Spice86.Shared.Utils;

using System.Text.RegularExpressions;

public partial class CFunctions {
    private static readonly Regex FormatRegex = PrintFRegex();
    private readonly Memory _memory;
    private readonly Stack _stack;
    private readonly State _state;

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

            var formatSpecification = new FormatSpecification(match);
            object arg;
            // Use the information from the format specification to read the argument from the stack
            switch (formatSpecification.SizePrefix) {
                case SizePrefix.Long:
                    arg = _stack.Peek32(stackPos);
                    stackPos += 4;
                    break;
                case SizePrefix.Far:
                    ushort offset = _stack.Peek16(stackPos);
                    ushort segment = _stack.Peek16(stackPos + 2);
                    arg = MemoryUtils.ToPhysicalAddress(segment, offset);
                    stackPos += 4;
                    break;
                case SizePrefix.Short:
                case SizePrefix.Near:
                    arg = _stack.Peek16(stackPos);
                    stackPos += 2;
                    break;
                case SizePrefix.None:
                default:
                    arg = _stack.Peek16(stackPos);
                    stackPos += 2;
                    break;
            }

            // Convert pointer to string
            if (formatSpecification.Formatter == FormatterType.String) {
                arg = GetString(formatSpecification, arg);
            }

            segments.Add(formatSpecification.Format(arg));

            lastEnd = match.Index + match.Value.Length;
        }

        if (lastEnd < format.Length) {
            segments.Add(format[lastEnd..]);
        }

        return string.Join(null, segments);
    }

    private object GetString(FormatSpecification specification, object arg) {
        uint address;
        if (specification.SizePrefix == SizePrefix.Far) {
            address = (uint)arg;
        } else {
            address = MemoryUtils.ToPhysicalAddress(_state.DS, (ushort)arg);
        }
        return _memory.GetZeroTerminatedString(address, int.MaxValue);
    }

    [GeneratedRegex(@"\%([\#\-\+0 ]*)(\d*|\*)(?:\.(\d+|\*))?([hlFN])?([dioxXucseEfgGp%])", RegexOptions.Compiled)]
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

    private enum FormatterType {
        SignedInt, // d, i
        UnsignedInt, // u  
        UnsignedOct, // o
        UnsignedHex, // x    
        UnsignedHexUpper, // X  
        Float, // f  
        Scientific, // e
        ScientificUpper, // E
        General, // g
        GeneralUpper, // G
        Char, // c
        String, // s
        Percent, // %
        Pointer // p
    }

    private enum SizePrefix {
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
        public SizePrefix SizePrefix { get; set; }
        public string? FormattingString { get; set; }
        public FormatterType Formatter { get; set; }

        public void Parse(Match match) {
            string flagsParameter = match.Groups[1].Value;
            string widthParameter = match.Groups[2].Value;
            string precisionParameter = match.Groups[3].Value;
            string prefixParameter = match.Groups[4].Value;
            string typeParameter = match.Groups[5].Value;

            Flags = ParseFlags(flagsParameter);
            Width = ParseWidth(widthParameter);
            Precision = ParsePrecision(precisionParameter);
            SizePrefix = ParseSizePrefix(prefixParameter);
            Formatter = ParseType(typeParameter);
        }

        private static FormatterType ParseType(string typeParameter) {
            return typeParameter[0] switch {
                'd' => FormatterType.SignedInt,
                'i' => FormatterType.SignedInt,
                'u' => FormatterType.UnsignedInt,
                'o' => FormatterType.UnsignedOct,
                'x' => FormatterType.UnsignedHex,
                'X' => FormatterType.UnsignedHexUpper,
                'f' => FormatterType.Float,
                'e' => FormatterType.Scientific,
                'E' => FormatterType.ScientificUpper,
                'g' => FormatterType.General,
                'G' => FormatterType.GeneralUpper,
                'c' => FormatterType.Char,
                's' => FormatterType.String,
                'p' => FormatterType.Pointer,
                '%' => FormatterType.Percent,
                _ => throw new ArgumentOutOfRangeException(nameof(typeParameter), typeParameter, "invalid type parameter")
            };
        }

        private static SizePrefix ParseSizePrefix(string sizePrefixParameter) {
            return sizePrefixParameter switch {
                "h" => SizePrefix.Short,
                "l" => SizePrefix.Long,
                "F" => SizePrefix.Far,
                "N" => SizePrefix.Near,
                _ => SizePrefix.None
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

        private static int ToSigned(uint value) {
            return value <= int.MaxValue ? (int)value : -(int)(uint.MaxValue - value + 1);
        }

        private static int ToSigned(ushort value) {
            return value <= short.MaxValue ? (short)value : -(short)(ushort.MaxValue - value + 1);
        }

        public string Format(object obj) {
            return Formatter switch {
                FormatterType.SignedInt => FormatNumber("d", SizePrefix == SizePrefix.Long ? ToSigned((uint)obj) : ToSigned((ushort)obj)),
                FormatterType.UnsignedInt => FormatNumber("d", obj),
                FormatterType.UnsignedOct => FormatString(Convert.ToString((long)obj, 8)),
                FormatterType.UnsignedHex => FormatNumber("x", obj),
                FormatterType.UnsignedHexUpper => FormatNumber("X", obj),
                FormatterType.Float => FormatNumber("f", obj),
                FormatterType.Scientific => FormatNumber("e", obj),
                FormatterType.ScientificUpper => FormatNumber("E", obj),
                FormatterType.General => FormatNumber("g", obj),
                FormatterType.GeneralUpper => FormatNumber("G", obj),
                FormatterType.Char => FormatString(((char)obj).ToString()),
                FormatterType.String => FormatString(obj.ToString() ?? string.Empty),
                FormatterType.Pointer => FormatString("0x" + Convert.ToString((int)obj, 16)),
                FormatterType.Percent => "%",
                _ => throw new ArgumentOutOfRangeException(nameof(Formatter), Formatter, "Invalid format specifier")
            };
        }

        private string FormatNumber(string specifier, object value) {
            string precision = (Precision.HasValue ? Precision.ToString() : string.Empty) ?? string.Empty;
            string str = $"{{0:{specifier}{precision}}}";
            str = string.Format(str, value);

            if (Flags.HasFlag(Flags.ForceSign)
                && str[0] != '-'
                && Formatter != FormatterType.UnsignedHex
                && Formatter != FormatterType.UnsignedHexUpper) {
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
                return Flags.HasFlag(Flags.LeftAligned) ? value.PadRight(Width.Value) : value.PadLeft(Width.Value);
            }
            return value;
        }
    }
}