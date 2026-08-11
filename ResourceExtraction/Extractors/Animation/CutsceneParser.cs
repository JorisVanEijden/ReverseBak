using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceExtraction.Extractors.Animation;

public class CutsceneParser {
    private readonly byte[] _data;
    private int _position;

    public CutsceneParser(byte[] data) {
        _data = data;
        _position = 0;
    }

    public List<CutsceneScene> Parse() {
        var scenes = new List<CutsceneScene>();

        while (_position < _data.Length) {
            // Read scene number (2 bytes)
            ushort sceneNumber = ReadUInt16();

            var commands = new List<CutsceneCommand>();
            while (_position < _data.Length) {
                var command = ParseNextCommand();

                // If we hit end of script, break this scene's command loop
                if (command.Token == 0xFFFF) {
                    break;
                }
                commands.Add(command);
            }

            scenes.Add(new CutsceneScene(sceneNumber, commands));

            // If we've reached the end of the data, stop parsing
            if (_position >= _data.Length) {
                break;
            }
        }

        return scenes;
    }

    private CutsceneCommand ParseNextCommand() {
        // Read token (2 bytes)
        ushort token = ReadUInt16();

        // Get number of arguments for this token
        int argCount = CutsceneCommand.GetCommandArgCount(token);

        // Read arguments
        var arguments = new ushort[argCount];
        for (int i = 0; i < argCount; i++) {
            arguments[i] = ReadUInt16();
        }

        return new CutsceneCommand(token, arguments);
    }

    private ushort ReadUInt16() {
        if (_position + 1 >= _data.Length)
            throw new InvalidOperationException("Unexpected end of data while reading UInt16");

        // Read bytes in little-endian order (least significant byte first)
        var value = (ushort)(_data[_position++] | _data[_position++] << 8);

        return value;
    }

    public Dictionary<int, string> ToHumanReadableScript() {
        _position = 0; // Reset position before parsing
        List<CutsceneScene> scenes = Parse();
        var sb = new StringBuilder();

        var scripts = new Dictionary<int, string>();

        foreach (var scene in scenes) {
            sb.Clear();
            var indentLevel = 0;
            var blockStack = new Stack<int>();

            for (var i = 0; i < scene.Commands.Count; i++) {
                var command = scene.Commands[i];

                // Handle END_IF and END_SCENE - reduce indent before writing
                if (command.Token is 0x1510 or 0x1520) // END_IF or END_OF_SCRIPT
                {
                    if (blockStack.Count > 0) {
                        indentLevel = blockStack.Pop();
                    }
                }

                // Handle ELSE - same indentation as the matching IF
                if (command.Token == 0x1500) // ELSE
                {
                    if (blockStack.Count > 0) {
                        indentLevel = blockStack.Peek();
                    }
                }

                // Special handling for AND - append to previous line
                if (command.Token == 0x1420) // AND
                {
                    // Remove the newline from the previous line
                    if (sb.Length >= Environment.NewLine.Length) {
                        sb.Length -= Environment.NewLine.Length;
                    }
                    sb.Append(" AND");

                    // Get the next command (IF condition)
                    if (i + 1 < scene.Commands.Count) {
                        var nextCommand = scene.Commands[i + 1];
                        if ((nextCommand.Token & 0xFF00) == 0x1300 || nextCommand.Token == 0x1030) {
                            sb.Append(" " + nextCommand);
                            i++; // Skip the next command since we've handled it
                        }
                    }
                    sb.AppendLine();
                } else {
                    // Add indentation for non-AND commands
                    sb.Append(new string(' ', indentLevel * 4));
                    sb.AppendLine(command.ToString());
                }

                // Handle IF conditions - increase indent for the next command
                if ((command.Token & 0xFF00) == 0x1300 || command.Token == 0x1030) {
                    blockStack.Push(indentLevel);
                    indentLevel++;
                }

                // After ELSE, increase indent for the next command
                if (command.Token == 0x1500) {
                    indentLevel++;
                }
            }
            sb.AppendLine();

            scripts.Add(scene.SceneNumber, sb.ToString());
        }

        return scripts;
    }
}