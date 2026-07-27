// NativeMethods.cs — 所有 Win32 P/Invoke、结构体、常量
using System;
using System.Runtime.InteropServices;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

// ===== 结构体 =====
[StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
[StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
[StructLayout(LayoutKind.Sequential)] struct SIZE { public int CX, CY; }
[StructLayout(LayoutKind.Sequential)] struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
[StructLayout(LayoutKind.Sequential)] struct MINMAXINFO { public POINT Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct WNDCLASSEXW { public uint cbSize, style; public IntPtr lpfnWndProc; public int cbClsExtra, cbWndExtra; public IntPtr hInstance, hIcon, hCursor, hbrBackground; public string? lpszMenuName; public string lpszClassName; public IntPtr hIconSm; }
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct CREATESTRUCTW { public IntPtr lpCreateParams, hInstance, hMenu, hwndParent; public int cy, cx, y, x, style; public string lpszName, lpszClass; public uint dwExStyle; }
[StructLayout(LayoutKind.Sequential)] struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public int time; public POINT pt; }

// ===== 常量 =====
static class NativeConstants
{
    public const int WS_EX_LAYERED = 0x00080000, WS_EX_APPWINDOW = 0x00040000;
    public const int WS_POPUP = unchecked((int)0x80000000), WS_VISIBLE = 0x10000000, WS_SYSMENU = 0x00080000;
    public const int SWP_NOZORDER = 0x0004, SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001, SWP_NOACTIVATE = 0x0010;
    public const int ULW_ALPHA = 0x00000002;
    public const byte AC_SRC_OVER = 0x00, AC_SRC_ALPHA = 0x01;
    public const uint SPI_GETWORKAREA = 0x0030;
    public const int HTCLIENT = 1;
    public const int WM_NCCREATE = 0x0081, WM_DESTROY = 0x0002, WM_CLOSE = 0x0010;
    public const int WM_NCHITTEST = 0x0084, WM_GETMINMAXINFO = 0x0024, WM_ACTIVATE = 0x0006;
    public const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, WM_MOUSEMOVE = 0x0200;
    public const int WM_MBUTTONDOWN = 0x0207, WM_MOUSEWHEEL = 0x020A, WM_KEYDOWN = 0x0100;
    public const int WM_MOUSEACTIVATE = 0x0021, WM_TIMER = 0x0113;
    public const int MA_ACTIVATE = 1;
    public const int VK_ESCAPE = 0x1B, VK_LEFT = 0x25, VK_RIGHT = 0x27, VK_CONTROL = 0x11;
    public const double MIN_SCALE = 0.1, MAX_SCALE = 5.0, SCALE_FACTOR = 1.1;
    public const double RESIZE_EDGE = 20.0, OPACITY_STEP = 0.05;
    public const int GLOW_MARGIN = 4;
    public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
    public const string WND_CLASS = "SnapViewWnd";
    public static readonly IntPtr HWND_TOPMOST = new(-1), HWND_NOTOPMOST = new(-2);
}

// ===== P/Invoke =====
static class NativeMethods
{
    [DllImport("kernel32")] public static extern IntPtr GetModuleHandle(string? lp);
    [DllImport("user32")] public static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);
    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateWindowExW(int ex, string cls, string title, int style, int x, int y, int w, int h, IntPtr p, IntPtr m, IntPtr i, IntPtr param);
    [DllImport("user32")] public static extern bool DestroyWindow(IntPtr h);
    [DllImport("user32")] public static extern IntPtr DefWindowProcW(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32")] public static extern bool GetCursorPos(out POINT p);
    [DllImport("user32")] public static extern short GetAsyncKeyState(int k);
    [DllImport("user32")] public static extern IntPtr SetCapture(IntPtr h);
    [DllImport("user32")] public static extern bool ReleaseCapture();
    [DllImport("user32")] public static extern IntPtr SetFocus(IntPtr h);
    [DllImport("user32")] public static extern bool UpdateLayeredWindow(IntPtr h, IntPtr d, ref POINT dp, ref SIZE s, IntPtr m, ref POINT sp, int cr, ref BLENDFUNCTION b, uint f);
    [DllImport("user32")] public static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
    [DllImport("user32")] public static extern bool SystemParametersInfoW(uint a, uint b, ref RECT r, uint f);
    [DllImport("gdi32")] public static extern bool DeleteObject(IntPtr o);
    [DllImport("gdi32")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
    [DllImport("gdi32")] public static extern bool DeleteDC(IntPtr dc);
    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int GetMessageW(out MSG msg, IntPtr h, uint min, uint max);
    [DllImport("user32")] public static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32")] public static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32")] public static extern void PostQuitMessage(int code);
    [DllImport("user32")] public static extern IntPtr LoadCursor(IntPtr h, int id);
    [DllImport("user32")] public static extern IntPtr SetCursor(IntPtr hCursor);
    [DllImport("user32")] public static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);
    [DllImport("user32")] public static extern bool KillTimer(IntPtr hWnd, IntPtr nIDEvent);
    [DllImport("user32", EntryPoint = "SetWindowLongPtrW")]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32", EntryPoint = "GetWindowLongPtrW")]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32", CharSet = CharSet.Unicode)]
    public static extern int MessageBoxW(IntPtr h, string text, string caption, uint type);
}

delegate IntPtr WndProcDelegate(IntPtr h, uint m, IntPtr w, IntPtr l);
