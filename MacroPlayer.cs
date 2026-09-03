using System.Runtime.InteropServices;

namespace TinyMacro;

public static class MacroPlayer
{
    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint SPI_GETKEYBOARDSPEED = 0x000A;
    private const uint SPI_GETKEYBOARDDELAY = 0x0016;

    public static void MoveTo(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public static void LeftDown() => SendMouseFlag(MOUSEEVENTF_LEFTDOWN);
    public static void LeftUp() => SendMouseFlag(MOUSEEVENTF_LEFTUP);
    public static void MiddleDown() => SendMouseFlag(MOUSEEVENTF_MIDDLEDOWN);
    public static void MiddleUp() => SendMouseFlag(MOUSEEVENTF_MIDDLEUP);
    public static void RightDown() => SendMouseFlag(MOUSEEVENTF_RIGHTDOWN);
    public static void RightUp() => SendMouseFlag(MOUSEEVENTF_RIGHTUP);

    public static void Scroll(int wheelDelta)
    {
        var inputs = new INPUT[1];
        inputs[0] = CreateMouseInput(MOUSEEVENTF_WHEEL, (uint)wheelDelta);
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public static void KeyDown(Keys key) => SendKeyboardFlag(key, false);
    public static void KeyUp(Keys key) => SendKeyboardFlag(key, true);

    public static int GetKeyRepeatDelayMs()
    {
        uint delaySetting = 0;
        SystemParametersInfo(SPI_GETKEYBOARDDELAY, 0, ref delaySetting, 0);
        return (int)((delaySetting + 1) * 250);
    }

    public static int GetKeyRepeatIntervalMs()
    {
        uint speedSetting = 0;
        SystemParametersInfo(SPI_GETKEYBOARDSPEED, 0, ref speedSetting, 0);
        var charsPerSecond = 2.5 + speedSetting * (30.0 - 2.5) / 31.0;
        return Math.Max((int)(1000 / charsPerSecond), 1);
    }

    private static void SendMouseFlag(uint flag)
    {
        var inputs = new INPUT[1];
        inputs[0] = CreateMouseInput(flag, 0);
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyboardFlag(Keys key, bool keyUp)
    {
        var inputs = new INPUT[1];
        inputs[0] = CreateKeyboardInput((ushort)key, keyUp);
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT CreateMouseInput(uint flags, uint mouseData)
    {
        return new INPUT
        {
            type = INPUT_MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT { dx = 0, dy = 0, mouseData = mouseData, dwFlags = flags, time = 0, dwExtraInfo = IntPtr.Zero }
            }
        };
    }

    private static INPUT CreateKeyboardInput(ushort vk, bool keyUp)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public static void BeginHighResolutionTiming() => TimeBeginPeriod(1);
    public static void EndHighResolutionTiming() => TimeEndPeriod(1);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
    private static extern uint TimeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
    private static extern uint TimeEndPeriod(uint uMilliseconds);
}
