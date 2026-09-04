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
                int argCount = CutsceneCommand.GetCommandArgCount(cmd);
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
}