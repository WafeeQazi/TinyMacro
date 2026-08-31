using System.Runtime.InteropServices;

namespace TinyMacro;

public class MouseHook
{
    public event Action<Point>? LeftDown;
    public event Action<Point>? LeftUp;
    public event Action<Point>? MiddleDown;
    public event Action<Point>? MiddleUp;
    public event Action<Point>? RightDown;
    public event Action<Point>? RightUp;
    public event Action<Point, int>? Scroll;

    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hookId = IntPtr.Zero;

    public MouseHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero)
            return;

        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
    }

    public void Stop()
    {
        if (_hookId == IntPtr.Zero)
            return;

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var point = new Point(hookStruct.pt.x, hookStruct.pt.y);
            var message = (int)wParam;

            switch (message)
            {
                case WM_LBUTTONDOWN:
                    LeftDown?.Invoke(point);
                    break;
                case WM_LBUTTONUP:
                    LeftUp?.Invoke(point);
                    break;
                case WM_MBUTTONDOWN:
                    MiddleDown?.Invoke(point);
                    break;
                case WM_MBUTTONUP:
                    MiddleUp?.Invoke(point);
                    break;
                case WM_RBUTTONDOWN:
                    RightDown?.Invoke(point);
                    break;
                case WM_RBUTTONUP:
                    RightUp?.Invoke(point);
                    break;
                case WM_MOUSEWHEEL:
                    var delta = (short)((hookStruct.mouseData >> 16) & 0xffff);
                    Scroll?.Invoke(point, delta);
                    break;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
