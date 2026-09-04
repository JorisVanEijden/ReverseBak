namespace ResourceExtraction.Extractors.Animation;

public class CutsceneCommand {
    public ushort Token { get; }
    public ushort[] Arguments { get; }

    public CutsceneCommand(ushort token, ushort[] arguments) {
        Token = token;
        Arguments = arguments;
    }

    public override string ToString() {
        switch (Token) {
            // Conditions
            case 0x1030:
                return $"IF NOT PLAYED scene_{Arguments[1]}";
            case 0x1330:
                return $"IF NOT PLAYED scene_{Arguments[1]}";
            case 0x1350:
                return $"IF PLAYED scene_{Arguments[1]}";
            case 0x13A0:
                return $"IF CHAPTER <= {Arguments[0]}";
            case 0x13B0:
                return $"IF CHAPTER >= {Arguments[0]}";
            // Operators
            case 0x1420:
                return "AND";
            // End conditions
            case 0x1510:
                return "END IF";
            case 0x1500:
                return "ELSE";
            case 0x1520:
                return "END IF";
            // Commands
            case 0x2000:
                return $"CONTINUE scene_{Arguments[1]}";
            case 0x2005:
                return $"START scene_{Arguments[1]}";
            case 0x2010:
                return $"STOP scene_{Arguments[1]}";
            case 0xFFFF:
                return "END OF SCRIPT";
            default:
                return $"UNKNOWN_COMMAND 0x{Token:X4}, {string.Join(", ", Arguments)}";
        }
    }

    /// <summary>
    /// How many inline UInt16 arguments follow an opcode. The parser uses this to find the NEXT
    /// opcode, so an entry that is too small makes it read arguments as opcodes and desynchronise
    /// the rest of the scene.
    /// </summary>
    /// <remarks>
    /// <b>This was two tables until 2026-09-04</b> — a fuller private copy in
    /// <c>AdsScriptBuilder</c> feeding the raw command dump, and a sparser one here feeding the
    /// script the extractor actually emits. They agreed on every opcode that ships (so no output was
    /// ever wrong), but the arrangement meant the debug path could skip an opcode's arguments
    /// correctly while the real path desynchronised on it. Unified on the fuller list; measured
    /// output is byte-identical.
    ///
    /// <para>Only the thirteen opcodes with a case in <see cref="ToString"/> occur in shipped data
    /// (<c>AdsOpcodeInventoryTests</c> pins this). The remaining entries are unreached, so their
    /// counts are unverified against the binary and cost nothing if wrong — they exist so that data
    /// which is not the shipped corpus, such as a mod, parses rather than derails. The one that WAS
    /// checked is <c>0x1430</c>: it shares a jump-table target with <c>0x1420</c>, so it is
    /// genuinely a zero-argument opcode and its absence here is correct.</para>
    /// </remarks>
    public static int GetCommandArgCount(ushort cmd) {
        return cmd switch {
            0x2000 or 0x2005 => 4, // Scene management commands (only second argument used)
            0x2010 or 0x2015 or 0x2020 or 0x4000 or 0x4010 => 3,
            0x1010 or 0x1020 or 0x1030 or 0x1040 or 0x1050 or 0x1060 or 0x1070
                or 0x1310 or 0x1320 or 0x1330 or 0x1340 or 0x1350 or 0x1360 or 0x1370 => 2,
            0xF010 or 0xF200 or 0xF210 or 0x1080 or 0x1380 or 0x1390
                or 0x13A0 or 0x13A1 or 0x13B0 or 0x13B1 or 0x13C0 or 0x13C1 or 0x3020 => 1,
            // Logical AND, ELSE, block ends and end-of-script take no arguments, as does everything
            // else the engine dispatches.
            _ => 0
        };
    }
}