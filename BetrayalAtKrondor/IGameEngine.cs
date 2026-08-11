namespace BetrayalAtKrondor;

public interface IGameEngine {
    string? DataPath { get; set; }
    void SetMouseCursorArea(int minX, int minY, int maxX, int maxY);
}