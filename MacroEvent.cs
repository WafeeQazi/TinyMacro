namespace TinyMacro;

public enum MacroEventType
{
    MouseClick,
    MiddleClick,
    RightClick,
    Scroll,
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

    public static MacroEvent Click(int x, int y, long delayMs)
    {
        return new MacroEvent { Type = MacroEventType.MouseClick, X = x, Y = y, DelayMs = delayMs };
    }

    public static MacroEvent MiddleClick(int x, int y, long delayMs)
    {
        return new MacroEvent { Type = MacroEventType.MiddleClick, X = x, Y = y, DelayMs = delayMs };
    }

    public static MacroEvent RightClick(int x, int y, long delayMs)
    {
        return new MacroEvent { Type = MacroEventType.RightClick, X = x, Y = y, DelayMs = delayMs };
    }

    public static MacroEvent Scroll(int x, int y, int wheelDelta, long delayMs)
    {
        return new MacroEvent { Type = MacroEventType.Scroll, X = x, Y = y, WheelDelta = wheelDelta, DelayMs = delayMs };
    }

    public static MacroEvent KeyPress(Keys key, long delayMs)
    {
        return new MacroEvent { Type = MacroEventType.KeyPress, Key = key, DelayMs = delayMs };
    }

    public override string ToString()
    {
        return Type switch
        {
            MacroEventType.MouseClick => $"Left click at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.MiddleClick => $"Middle click at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.RightClick => $"Right click at ({X},{Y}) after {DelayMs}ms",
            MacroEventType.Scroll => $"Scroll {WheelDelta} at ({X},{Y}) after {DelayMs}ms",
            _ => $"Key {Key} after {DelayMs}ms"
        };
    }
}
