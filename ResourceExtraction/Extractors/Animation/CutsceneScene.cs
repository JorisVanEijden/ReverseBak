using System.Collections.Generic;

namespace ResourceExtraction.Extractors.Animation;

public class CutsceneScene {
    public ushort SceneNumber { get; }
    public List<CutsceneCommand> Commands { get; }

    public CutsceneScene(ushort sceneNumber, List<CutsceneCommand> commands) {
        SceneNumber = sceneNumber;
        Commands = commands;
    }

    public override string ToString() {
        return $"Scene {SceneNumber:X4}";
    }
}