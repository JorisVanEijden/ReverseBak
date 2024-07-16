namespace ResourceExtraction.Extractors.Animation;

using GameData.Resources.Animation;
using System.Collections.Generic;
using System.IO;

public static class AdsScriptBuilder {
    public static readonly HashSet<ushort> SeenCommands = [];

    public static Dictionary<int, List<AdsScriptCall>> CreateFrom(byte[] scriptBytes) {
        var script = new Dictionary<int, List<AdsScriptCall>>();
        using var scriptReader = new BinaryReader(new MemoryStream(scriptBytes));
        while (scriptReader.BaseStream.Position < scriptReader.BaseStream.Length) {
            int index = scriptReader.ReadUInt16();
            ushort cmd = scriptReader.ReadUInt16();
            SeenCommands.Add(cmd);
            var commands = new List<AdsScriptCall>();
            while (cmd != 0xFFFF) {
                int argCount = GetCommandArgCount(cmd);
                List<string> arguments = new(argCount);
                for (var i = 0; i < argCount; i++) {
                    arguments.Add($"{scriptReader.ReadUInt16():X4}");
                }
                commands.Add(new AdsScriptCall {
                    Function = $"{cmd:X4}",
                    Arguments = arguments
                });
                cmd = scriptReader.ReadUInt16();
                SeenCommands.Add(cmd);
            }
            script[index] = commands;
        }

        return script;
    }

    private static int GetCommandArgCount(ushort cmd) {
        return cmd switch {
            8192 or 8197 => 4,
            8208 or 8213 or 8224 or 16384 or 16400 => 3,
            4112 or 4128 or 4144 or 4160 or 4176 or 4192 or 4208 or 4880 or 4896 or 4912 or 4928 or 4944 or 4960 or 4976 => 2,
            61456 or 61952 or 61968 or 4224 or 4992 or 5008 or 5024 or 5025 or 5040 or 5041 or 5056 or 5057 or 12320 => 1,
            _ => 0
        };
    }
}