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

    public static int GetCommandArgCount(ushort cmd) {
        return cmd switch {
            0x2000 or 0x2005 => 4, // Scene management commands (only second argument used)
            0x2010 => 3, // Scene management commands (only second argument used)
            0x1030 or 0x1330 or 0x1350 => 2, // Scene condition commands (first argument ignored)
            0x13A0 or 0x13B0 => 1, // Chapter comparison commands
            0x1420 or 0x1500 or 0x1520 or 0x1510 or 0xFFFF => 0, // Logical AND, ELSE, block ends, and end of script have no arguments
            _ => 0
        };
    }
}