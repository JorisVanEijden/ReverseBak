namespace BetrayalAtKrondor;

using Spice86.Core.Emulator.InterruptHandlers.Input.Mouse;

public class GameEngine : IGameEngine {
    private readonly IMouseDriver _mouseDriver;

    public GameEngine(IMouseDriver mouseDriver) {
        _mouseDriver = mouseDriver;
    }

    public void SetMouseCursorArea(int minX, int minY, int maxX, int maxY) {
        _mouseDriver.CurrentMinX = minX;
        _mouseDriver.CurrentMinY = minY;
        _mouseDriver.CurrentMaxX = maxX;
        _mouseDriver.CurrentMaxY = maxY;
    }
}