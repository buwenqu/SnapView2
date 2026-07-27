// Program.cs — 入口、窗口类注册、创建、消息循环
using System;
using System.IO;
using System.Runtime.InteropServices;
using static NativeConstants;
using static NativeMethods;

class Program
{
    static IntPtr _hInstance;
    static WndProcDelegate? _wpDelegate;

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            MessageBoxW(IntPtr.Zero, "请将图片文件拖放到此程序图标上，\n或将其设为默认看图程序后双击图片。", "SnapView", 0);
            return;
        }

        _hInstance = GetModuleHandle(null);
        _wpDelegate = WindowManager.WndProc;
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wpDelegate),
            hInstance = _hInstance,
            hCursor = LoadCursor(IntPtr.Zero, 32512),
            hbrBackground = IntPtr.Zero,
            lpszClassName = WND_CLASS,
            cbWndExtra = IntPtr.Size
        };
        if (RegisterClassExW(ref wc) == 0)
            throw new InvalidOperationException("RegisterClassEx failed");

        foreach (var file in args)
        {
            if (!File.Exists(file)) continue;
            var win = new ImageWindow();
            if (!ImageLoader.Load(win, file)) continue;

            int w = Math.Max(1, (int)Math.Ceiling(win.OrigW * win.Scale) + GLOW_MARGIN * 2);
            int h = Math.Max(1, (int)Math.Ceiling(win.OrigH * win.Scale) + GLOW_MARGIN * 2);

            var gc = GCHandle.Alloc(win);
            win.Hwnd = CreateWindowExW(
                WS_EX_LAYERED | WS_EX_APPWINDOW,
                WND_CLASS, "SnapView",
                WS_POPUP | WS_VISIBLE | WS_SYSMENU,
                0, 0, w, h, IntPtr.Zero, IntPtr.Zero, _hInstance,
                GCHandle.ToIntPtr(gc));

            if (win.Hwnd == IntPtr.Zero) { gc.Free(); continue; }

            // GIF 动画：启动帧切换定时器
            ImageLoader.StartGifTimer(win);

            RECT wa = default;
            SystemParametersInfoW(SPI_GETWORKAREA, 0, ref wa, 0);
            int cx = Math.Max(0, (wa.Right - wa.Left - w) / 2);
            int cy = Math.Max(0, (wa.Bottom - wa.Top - h) / 2);
            SetWindowPos(win.Hwnd, IntPtr.Zero, cx, cy, 0, 0,
                SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);

            Rendering.Render(win);
        }

        while (GetMessageW(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
    }
}
