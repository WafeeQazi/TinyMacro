namespace TinyMacro;

public enum MacroEventType
{
    LeftDown,
    LeftUp,
    MiddleDown,
    MiddleUp,
    RightDown,
    RightUp,
    Scroll,
    MouseMove,
    KeyPress
}

public class MacroEvent
{
    public MacroEventType Type { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public Keys Key { get; init; }
    public int WheelDelta { get; init; }
    public long DelayMs { get; init; }
    public long HoldMs { get; init; }

    public static MacroEvent LeftDown(int x, int y, long delayMs) =>
        new() { Type = MacroEventType.LeftDown, X = x, Y = y, DelayMs = delayMs };

    public static MacroEvent LeftUp(int x, int y, long delayMs) =>
        new() { Type = MacroEventType.LeftUp, X = x, Y = y, DelayMs = delayMs };

    public static MacroEvent MiddleDown(int x, int y, long delayMs) =>
        new() { Type = MacroEventType.MiddleDown, X = x, Y = y, DelayMs = delayMs };

    public static MacroEvent MiddleUp(int x, int y, long delayMs) =>
        new() { Type = MacroEventType.MiddleUp, X = x, Y = y, DelayMs = delayMs };

    public static MacroEvent RightDown(int x, int y, long delayMs) =>
        new() { Type = MacroEventType.RightDown, X = x, Y = y, DelayMs = delayMs };

    public static MacroEvent RightUp(int x, int y, long delayMs) =>
        new() { Type = MacroEventType.RightUp, X = x, Y = y, DelayMs = delayMs };

    public static MacroEvent Scroll(int x, int y, int wheelDelta, long delayMs) =>
        new() { Type = MacroEventType.Scroll, X = x, Y = y, WheelDelta = wheelDelta, DelayMs = delayMs };

    public static MacroEvent Move(int x, int y, long delayMs) =>
        new() { Type = MacroEventType.MouseMove, X = x, Y = y, DelayMs = delayMs };

    public static MacroEvent KeyPress(Keys key, long delayMs, long holdMs) =>
        new() { Type = MacroEventType.KeyPress, Key = key, DelayMs = delayMs, HoldMs = holdMs };

    public override string ToString()
    {
        return Type switch
        {
            MacroEventType.LeftDown => $"Left down at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.LeftUp => $"Left up at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.MiddleDown => $"Middle down at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.MiddleUp => $"Middle up at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.RightDown => $"Right down at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.RightUp => $"Right up at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.Scroll => $"Scroll {WheelDelta} at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.MouseMove => $"Move to ({X},{Y}) after {DelayMs}ms",
            _ => $"Key {Key} after {DelayMs}ms, held {HoldMs}ms"
        };
    }
}
