namespace ResourceExtraction.Extractors.Animation;

using GameData.Resources.Animation;
using System.Collections.Generic;
using System.IO;

public static class AdsScriptBuilder {
    public static HashSet<ushort> SeenCommands = [];

    public static Dictionary<int, List<AdsScriptCall>> CreateDebug(byte[] scriptBytes) {
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

    public static Dictionary<int, string> CreateFrom(byte[] scriptBytes) {
        var parser = new CutsceneParser(scriptBytes);
        return parser.ToHumanReadableScript();
    }

    private static int GetCommandArgCount(ushort cmd) {
        return cmd switch {
            0x2000 or 0x2005 => 4,
            0x2010 or 0x2015 or 0x2020 or 0x4000 or 0x4010 => 3,
            0x1010 or 0x1020 or 0x1030 or 0x1040 or 0x1050 or 0x1060 or 0x1070 or 0x1310 or 0x1320 or 0x1330 or 0x1340 or 0x1350 or 0x1360 or 0x1370 => 2,
            0xF010 or 0xF200 or 0xF210 or 0x1080 or 0x1380 or 0x1390 or 0x13A0 or 0x13A1 or 0x13B0 or 0x13B1 or 0x13C0 or 0x13C1 or 0x3020 => 1,
            _ => 0
        };
    }
}