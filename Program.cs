using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

[assembly: SupportedOSPlatform("windows")]

// ===== Win32 结构体 =====
[StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
[StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
[StructLayout(LayoutKind.Sequential)] struct SIZE { public int CX, CY; }
[StructLayout(LayoutKind.Sequential)] struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
[StructLayout(LayoutKind.Sequential)] struct MINMAXINFO { public POINT Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct WNDCLASSEXW { public uint cbSize, style; public IntPtr lpfnWndProc; public int cbClsExtra, cbWndExtra; public IntPtr hInstance, hIcon, hCursor, hbrBackground; public string? lpszMenuName; public string lpszClassName; public IntPtr hIconSm; }
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] struct CREATESTRUCTW { public IntPtr lpCreateParams, hInstance, hMenu, hwndParent; public int cy, cx, y, x, style; public string lpszName, lpszClass; public uint dwExStyle; }
[StructLayout(LayoutKind.Sequential)] struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public int time; public POINT pt; }

// ===== 窗口实例数据 =====
class ImageWindow
{
    public IntPtr Hwnd;
    public Bitmap? SrcBitmap;
    public int OrigW, OrigH;
    public double Scale = 1.0, Opacity = 1.0;
    public bool Topmost, Active;
    public bool Dragging, Resizing;
    public int DragStartX, DragStartY, WinStartX, WinStartY;
    public double ResizeStartScale;
    public int ResizeStartX;
    public string? CurrentFolder;
    public readonly List<string> ImageFiles = new();
    public int CurrentIndex = -1;
    // 缩放百分比提示
    public bool ShowScaleHint;
    public IntPtr ScaleHintTimer;
}

class Program
{
    // ===== Win32 常量 =====
    const int WS_EX_LAYERED = 0x00080000, WS_EX_APPWINDOW = 0x00040000;
    const int WS_POPUP = unchecked((int)0x80000000), WS_VISIBLE = 0x10000000, WS_SYSMENU = 0x00080000;
    const int SWP_NOZORDER = 0x0004, SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001, SWP_NOACTIVATE = 0x0010;
    const int ULW_ALPHA = 0x00000002;
    const byte AC_SRC_OVER = 0x00, AC_SRC_ALPHA = 0x01;
    const uint SPI_GETWORKAREA = 0x0030;
    const int HTCLIENT = 1;
    const int WM_NCCREATE = 0x0081, WM_DESTROY = 0x0002, WM_CLOSE = 0x0010;
    const int WM_NCHITTEST = 0x0084, WM_GETMINMAXINFO = 0x0024, WM_ACTIVATE = 0x0006;
    const int WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, WM_MOUSEMOVE = 0x0200;
    const int WM_MBUTTONDOWN = 0x0207, WM_MOUSEWHEEL = 0x020A, WM_KEYDOWN = 0x0100;
    const int WM_MOUSEACTIVATE = 0x0021;
    const int MA_ACTIVATE = 1;
    const int WM_TIMER = 0x0113;
    const int VK_ESCAPE = 0x1B, VK_LEFT = 0x25, VK_RIGHT = 0x27, VK_CONTROL = 0x11;
    const double MIN_SCALE = 0.1, MAX_SCALE = 5.0, SCALE_FACTOR = 1.1;
    const double RESIZE_EDGE = 20.0, OPACITY_STEP = 0.05;
    const int GLOW_MARGIN = 4; // 外发光边距（窗口 = 图片 + 2*GLOW_MARGIN）
    static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };
    const string WND_CLASS = "SnapViewWnd";

    // ===== P/Invoke =====
    [DllImport("kernel32")] static extern IntPtr GetModuleHandle(string? lp);
    [DllImport("user32")] static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);
    [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr CreateWindowExW(int ex, string cls, string title, int style, int x, int y, int w, int h, IntPtr p, IntPtr m, IntPtr i, IntPtr param);
    [DllImport("user32")] static extern bool DestroyWindow(IntPtr h);
    [DllImport("user32")] static extern IntPtr DefWindowProcW(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32")] static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32")] static extern short GetAsyncKeyState(int k);
    [DllImport("user32")] static extern IntPtr SetCapture(IntPtr h);
    [DllImport("user32")] static extern bool ReleaseCapture();
    [DllImport("user32")] static extern IntPtr SetFocus(IntPtr h);
    [DllImport("user32")] static extern bool UpdateLayeredWindow(IntPtr h, IntPtr d, ref POINT dp, ref SIZE s, IntPtr m, ref POINT sp, int cr, ref BLENDFUNCTION b, uint f);
    [DllImport("user32")] static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
    [DllImport("user32")] static extern bool SystemParametersInfoW(uint a, uint b, ref RECT r, uint f);
    [DllImport("gdi32")] static extern bool DeleteObject(IntPtr o);
    [DllImport("gdi32")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32")] static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
    [DllImport("gdi32")] static extern bool DeleteDC(IntPtr dc);
    [DllImport("user32", CharSet = CharSet.Unicode)]
    static extern int GetMessageW(out MSG msg, IntPtr h, uint min, uint max);
    [DllImport("user32")] static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32")] static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32")] static extern void PostQuitMessage(int code);
    [DllImport("user32")] static extern IntPtr LoadCursor(IntPtr h, int id);
    [DllImport("user32")] static extern IntPtr SetCursor(IntPtr hCursor);
    [DllImport("user32")] static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);
    [DllImport("user32")] static extern bool KillTimer(IntPtr hWnd, IntPtr nIDEvent);
    [DllImport("user32", EntryPoint = "SetWindowLongPtrW")]
    static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32", EntryPoint = "GetWindowLongPtrW")]
    static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32", CharSet = CharSet.Unicode)]
    static extern int MessageBoxW(IntPtr h, string text, string caption, uint type);

    static readonly IntPtr HWND_TOPMOST = new(-1), HWND_NOTOPMOST = new(-2);
    static readonly IntPtr IDC_ARROW = new(32512), IDC_SIZENWSE = new(32642);
    static IntPtr _hInstance;
    static WndProcDelegate? _wpDelegate;
    delegate IntPtr WndProcDelegate(IntPtr h, uint m, IntPtr w, IntPtr l);

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            MessageBoxW(IntPtr.Zero, "请将图片文件拖放到此程序图标上，\n或将其设为默认看图程序后双击图片。", "SnapView", 0);
            return;
        }

        _hInstance = GetModuleHandle(null);
        _wpDelegate = WndProc;
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
            if (!LoadImage(win, file)) continue;

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

            RECT wa = default;
            SystemParametersInfoW(SPI_GETWORKAREA, 0, ref wa, 0);
            int cx = Math.Max(0, (wa.Right - wa.Left - w) / 2);
            int cy = Math.Max(0, (wa.Bottom - wa.Top - h) / 2);
            SetWindowPos(win.Hwnd, IntPtr.Zero, cx, cy, 0, 0,
                SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);

            Render(win);
        }

        while (GetMessageW(out MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
    }

    static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp)
    {
        if (msg == WM_NCCREATE)
        {
            var cs = Marshal.PtrToStructure<CREATESTRUCTW>(lp);
            var gp = cs.lpCreateParams;
            SetWindowLongPtr(hwnd, 0, gp); // GWLP_USERDATA = 0 (cbWndExtra = IntPtr.Size)
            return DefWindowProcW(hwnd, msg, wp, lp);
        }

        var gcp = GetWindowLongPtr(hwnd, 0);
        var w = (ImageWindow?)GCHandle.FromIntPtr(gcp).Target;
        if (w == null) return DefWindowProcW(hwnd, msg, wp, lp);

        switch (msg)
        {
            case WM_NCHITTEST:
                return (IntPtr)HTCLIENT;

            case WM_GETMINMAXINFO:
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lp);
                mmi.MaxTrackSize.X = mmi.MaxSize.X = 100000;
                mmi.MaxTrackSize.Y = mmi.MaxSize.Y = 100000;
                Marshal.StructureToPtr(mmi, lp, true);
                return IntPtr.Zero;

            case WM_MOUSEACTIVATE:
                SetFocus(hwnd);
                return (IntPtr)MA_ACTIVATE;

            case WM_ACTIVATE:
                w.Active = (wp != IntPtr.Zero && (wp.ToInt32() & 0xFFFF) != 0);
                Render(w);
                return IntPtr.Zero;

            case WM_LBUTTONDOWN:
                {
                    int mx = (short)(lp.ToInt32() & 0xFFFF);
                    int my = (short)((lp.ToInt32() >> 16) & 0xFFFF);
                    SetCapture(hwnd);
                    GetWindowRect(hwnd, out RECT r);
                    if (mx >= r.Right - r.Left - RESIZE_EDGE && my >= r.Bottom - r.Top - RESIZE_EDGE)
                    {
                        w.Resizing = true; w.ResizeStartScale = w.Scale;
                        GetCursorPos(out POINT cp); w.ResizeStartX = cp.X;
                    }
                    else
                    {
                        w.Dragging = true;
                        GetCursorPos(out POINT cp);
                        w.DragStartX = cp.X; w.DragStartY = cp.Y;
                        w.WinStartX = r.Left; w.WinStartY = r.Top;
                    }
                    return IntPtr.Zero;
                }

            case WM_MOUSEMOVE:
                if (w.Dragging)
                {
                    GetCursorPos(out POINT cp);
                    SetWindowPos(hwnd, IntPtr.Zero,
                        w.WinStartX + cp.X - w.DragStartX,
                        w.WinStartY + cp.Y - w.DragStartY,
                        0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE);
                }
                else if (w.Resizing)
                {
                    GetCursorPos(out POINT cp);
                    SetScale(w, w.ResizeStartScale + (double)(cp.X - w.ResizeStartX) / w.OrigW);
                }
                else
                {
                    int mx2 = (short)(lp.ToInt32() & 0xFFFF);
                    int my2 = (short)((lp.ToInt32() >> 16) & 0xFFFF);
                    int cw = Math.Max(1, (int)Math.Ceiling(w.OrigW * w.Scale));
                    int ch = Math.Max(1, (int)Math.Ceiling(w.OrigH * w.Scale));
                    bool se = mx2 >= cw - RESIZE_EDGE && my2 >= ch - RESIZE_EDGE;
                    SetCursor(LoadCursor(IntPtr.Zero, se ? 32642 : 32512));
                }
                return IntPtr.Zero;

            case WM_LBUTTONUP:
                ReleaseCapture();
                if (w.Resizing) { w.Resizing = false; Render(w); }
                w.Dragging = false;
                return IntPtr.Zero;

            case WM_MBUTTONDOWN:
                w.Topmost = !w.Topmost;
                SetWindowPos(hwnd, w.Topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
                    0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                Render(w);
                return IntPtr.Zero;

            case WM_MOUSEWHEEL:
                {
                    int delta = (short)((wp.ToInt64() >> 16) & 0xFFFF);
                    if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
                    {
                        w.Opacity = Math.Max(0.1, Math.Min(1.0, w.Opacity + (delta > 0 ? OPACITY_STEP : -OPACITY_STEP)));
                        w.ShowScaleHint = true;
                        KillTimer(hwnd, w.ScaleHintTimer);
                        w.ScaleHintTimer = SetTimer(hwnd, (IntPtr)1, 500, IntPtr.Zero);
                        Render(w);
                    }
                    else
                    {
                        SetScale(w, w.Scale * (delta > 0 ? SCALE_FACTOR : 1.0 / SCALE_FACTOR));
                    }
                    return IntPtr.Zero;
                }

            case WM_KEYDOWN:
                switch (wp.ToInt32())
                {
                    case VK_ESCAPE: DestroyWindow(hwnd); return IntPtr.Zero;
                    case VK_LEFT: Navigate(w, -1); return IntPtr.Zero;
                    case VK_RIGHT: Navigate(w, 1); return IntPtr.Zero;
                }
                return IntPtr.Zero;

            case WM_TIMER:
                KillTimer(hwnd, wp);
                w.ShowScaleHint = false;
                Render(w);
                return IntPtr.Zero;

            case WM_CLOSE:
                DestroyWindow(hwnd);
                return IntPtr.Zero;

            case WM_DESTROY:
                w.SrcBitmap?.Dispose();
                var gch = GCHandle.FromIntPtr(GetWindowLongPtr(hwnd, 0));
                gch.Free();
                PostQuitMessage(0);
                return IntPtr.Zero;
        }
        return DefWindowProcW(hwnd, msg, wp, lp);
    }

    // ===== 缩放 =====
    static void SetScale(ImageWindow w, double s)
    {
        if (s < MIN_SCALE) s = MIN_SCALE; if (s > MAX_SCALE) s = MAX_SCALE;
        if (Math.Abs(s - w.Scale) < 0.0001) return;
        w.Scale = s;
        GetWindowRect(w.Hwnd, out RECT r);
        int nw = Math.Max(1, (int)Math.Ceiling(w.OrigW * s) + GLOW_MARGIN * 2);
        int nh = Math.Max(1, (int)Math.Ceiling(w.OrigH * s) + GLOW_MARGIN * 2);
        SetWindowPos(w.Hwnd, IntPtr.Zero, r.Left, r.Top, nw, nh,
            SWP_NOZORDER | SWP_NOACTIVATE);

        // 显示缩放百分比，0.5 秒后自动消失
        w.ShowScaleHint = true;
        KillTimer(w.Hwnd, w.ScaleHintTimer);
        w.ScaleHintTimer = SetTimer(w.Hwnd, (IntPtr)1, 500, IntPtr.Zero);

        Render(w);
    }

    // ===== 渲染 =====
    static void Render(ImageWindow w)
    {
        if (w.SrcBitmap == null || w.Hwnd == IntPtr.Zero) return;
        int imgW = Math.Max(1, (int)Math.Ceiling(w.OrigW * w.Scale));
        int imgH = Math.Max(1, (int)Math.Ceiling(w.OrigH * w.Scale));
        int ww = imgW + GLOW_MARGIN * 2;
        int hh = imgH + GLOW_MARGIN * 2;

        try
        {
            using var dest = new Bitmap(ww, hh, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(dest);

            // 图片渲染在中央（四周留 GLOW_MARGIN 给外发光）
            if (w.Resizing) { g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.HighSpeed; }
            else { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality; }
            g.DrawImage(w.SrcBitmap, GLOW_MARGIN, GLOW_MARGIN, imgW, imgH);

            // 描边颜色：置顶→绿色系，普通→蓝色系；激活→亮，未激活→暗
            bool act = w.Active; bool top = w.Topmost;
            int r, gv, b;
            if (top) { r = 0; gv = act ? 200 : 140; b = act ? 80 : 50; }
            else { r = 30; gv = act ? 200 : 144; b = 255; }
            Rectangle imgRect = new(GLOW_MARGIN, GLOW_MARGIN, imgW, imgH);

            for (int i = GLOW_MARGIN - 1; i >= 0; i--)
            {
                float t = (float)i / GLOW_MARGIN;                      // 0(外) → 1(内)
                int a = (int)(30 + 150 * t * t);                       // 外30 → 内180
                using var pen = new Pen(Color.FromArgb(a, r, gv, b), 1);
                g.DrawRectangle(pen, imgRect.Left - i, imgRect.Top - i,
                    imgRect.Width + i * 2 - 1, imgRect.Height + i * 2 - 1);
            }

            // 最内层一道细实线描边（紧贴图片边缘）
            using var edgePen = new Pen(Color.FromArgb(act ? 220 : 200, r, gv, b), 1);
            g.DrawRectangle(edgePen, imgRect.Left - 1, imgRect.Top - 1,
                imgRect.Width + 1, imgRect.Height + 1);

            // 缩放百分比提示
            if (w.ShowScaleHint)
            {
                string text1 = $"大小：{(int)(w.Scale * 100)}%";
                string text2 = $"不透明度：{(int)(w.Opacity * 100)}%";
                using var font = new Font("Microsoft YaHei", 14, FontStyle.Regular);
                var sz1 = g.MeasureString(text1, font);
                var sz2 = g.MeasureString(text2, font);
                float maxW = Math.Max(sz1.Width, sz2.Width);
                int tx = GLOW_MARGIN + 6, ty = GLOW_MARGIN + 6;
                int tw = (int)maxW + 12, th = (int)(sz1.Height + sz2.Height) + 8;
                using var bgBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
                g.FillRectangle(bgBrush, tx, ty, tw, th);
                using var textBrush = new SolidBrush(Color.White);
                g.DrawString(text1, font, textBrush, tx + 6, ty + 4);
                g.DrawString(text2, font, textBrush, tx + 6, ty + 4 + sz1.Height);
            }

            var hBmp = dest.GetHbitmap(Color.FromArgb(0, 0, 0, 0));
            var sdc = GetDC(IntPtr.Zero);
            var mdc = CreateCompatibleDC(sdc);
            var oldBmp = SelectObject(mdc, hBmp);

            GetWindowRect(w.Hwnd, out RECT wr);
            var dp = new POINT { X = wr.Left, Y = wr.Top };
            var sz = new SIZE { CX = ww, CY = hh };
            var sp = new POINT();
            var bl = new BLENDFUNCTION { BlendOp = AC_SRC_OVER, SourceConstantAlpha = (byte)(w.Opacity * 255), AlphaFormat = AC_SRC_ALPHA };
            UpdateLayeredWindow(w.Hwnd, IntPtr.Zero, ref dp, ref sz, mdc, ref sp, 0, ref bl, ULW_ALPHA);

            SelectObject(mdc, oldBmp);
            DeleteObject(hBmp);
            DeleteDC(mdc);
            ReleaseDC(IntPtr.Zero, sdc);
        }
        catch { }
    }

    // ===== 图片加载 =====
    static bool LoadImage(ImageWindow w, string path)
    {
        w.SrcBitmap?.Dispose(); w.SrcBitmap = null;
        try { w.SrcBitmap = new Bitmap(path); w.OrigW = w.SrcBitmap.Width; w.OrigH = w.SrcBitmap.Height; }
        catch (Exception ex) { MessageBoxW(IntPtr.Zero, $"无法打开图片:\n{ex.Message}", "SnapView", 0); return false; }

        RECT wa = default;
        SystemParametersInfoW(SPI_GETWORKAREA, 0, ref wa, 0);
        double mw = (wa.Right - wa.Left) * 0.8, mh = (wa.Bottom - wa.Top) * 0.8;
        w.Scale = 1.0;
        if (w.OrigW > mw || w.OrigH > mh)
            w.Scale = Math.Min(mw / w.OrigW, mh / w.OrigH);

        BuildList(w, path);
        return true;
    }

    // ===== 文件夹浏览 =====
    static void BuildList(ImageWindow w, string currentFile)
    {
        w.ImageFiles.Clear();
        var dir = Path.GetDirectoryName(Path.GetFullPath(currentFile)) ?? "";
        w.CurrentFolder = dir;
        try
        {
            w.ImageFiles.AddRange(Directory.EnumerateFiles(dir)
                .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase));
            w.CurrentIndex = w.ImageFiles.FindIndex(f =>
                string.Equals(f, currentFile, StringComparison.OrdinalIgnoreCase));
        }
        catch { w.ImageFiles.Add(currentFile); w.CurrentIndex = 0; }
    }

    static void Navigate(ImageWindow w, int delta)
    {
        if (w.ImageFiles.Count == 0 || w.CurrentIndex < 0) return;
        int i = w.CurrentIndex + delta;
        if (i < 0) i = w.ImageFiles.Count - 1;
        if (i >= w.ImageFiles.Count) i = 0;
        if (i == w.CurrentIndex) return;
        LoadImage(w, w.ImageFiles[i]);
        SetScale(w, w.Scale);
        Render(w); // 即使缩放值相同也必须重绘（切到了新图片）
    }
}
