namespace TinyMacro;

public enum MacroEventType
{
    MouseClick,
    KeyPress
}

public class MacroEvent
{
    public MacroEventType Type { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public Keys Key { get; init; }
    public long DelayMs { get; init; }

    public static MacroEvent Click(int x, int y, long delayMs)
    {
        return new MacroEvent { Type = MacroEventType.MouseClick, X = x, Y = y, DelayMs = delayMs };
    }

    public static MacroEvent KeyPress(Keys key, long delayMs)
    {
        return new MacroEvent { Type = MacroEventType.KeyPress, Key = key, DelayMs = delayMs };
    }

    public override string ToString()
    {
        return Type == MacroEventType.MouseClick
            ? $"Click at ({X},{Y}) after {DelayMs}ms"
            : $"Key {Key} after {DelayMs}ms";
    }
}
