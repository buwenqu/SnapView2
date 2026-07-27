// WindowManager.cs — WndProc（窗口消息处理）
using System;
using System.Runtime.InteropServices;
using static NativeConstants;
using static NativeMethods;

static class WindowManager
{
    public static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp)
    {
        if (msg == WM_NCCREATE)
        {
            var cs = Marshal.PtrToStructure<CREATESTRUCTW>(lp);
            SetWindowLongPtr(hwnd, 0, cs.lpCreateParams);
            return DefWindowProcW(hwnd, msg, wp, lp);
        }

        var gcp = GetWindowLongPtr(hwnd, 0);
        var w = (ImageWindow?)GCHandle.FromIntPtr(gcp).Target;
        if (w == null) return DefWindowProcW(hwnd, msg, wp, lp);

        switch (msg)
        {
            case WM_NCHITTEST: return (IntPtr)HTCLIENT;
            case WM_MOUSEACTIVATE: SetFocus(hwnd); return (IntPtr)MA_ACTIVATE;

            case WM_GETMINMAXINFO:
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lp);
                mmi.MaxTrackSize.X = mmi.MaxSize.X = 100000;
                mmi.MaxTrackSize.Y = mmi.MaxSize.Y = 100000;
                Marshal.StructureToPtr(mmi, lp, true);
                return IntPtr.Zero;

            case WM_ACTIVATE:
                w.Active = (wp != IntPtr.Zero && (wp.ToInt32() & 0xFFFF) != 0);
                Rendering.Render(w);
                return IntPtr.Zero;

            case WM_LBUTTONDOWN:
                {
                    int mx = (short)(lp.ToInt32() & 0xFFFF), my = (short)((lp.ToInt32() >> 16) & 0xFFFF);
                    SetCapture(hwnd); GetWindowRect(hwnd, out RECT r);
                    if (mx >= r.Right - r.Left - RESIZE_EDGE && my >= r.Bottom - r.Top - RESIZE_EDGE)
                    { w.Resizing = true; w.ResizeStartScale = w.Scale; GetCursorPos(out POINT cp); w.ResizeStartX = cp.X; }
                    else { w.Dragging = true; GetCursorPos(out POINT cp); w.DragStartX = cp.X; w.DragStartY = cp.Y; w.WinStartX = r.Left; w.WinStartY = r.Top; }
                    return IntPtr.Zero;
                }

            case WM_MOUSEMOVE:
                if (w.Dragging) { GetCursorPos(out POINT cp); SetWindowPos(hwnd, IntPtr.Zero, w.WinStartX + cp.X - w.DragStartX, w.WinStartY + cp.Y - w.DragStartY, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_NOACTIVATE); }
                else if (w.Resizing) { GetCursorPos(out POINT cp); Rendering.SetScale(w, w.ResizeStartScale + (double)(cp.X - w.ResizeStartX) / w.OrigW); }
                else
                {
                    int mx2 = (short)(lp.ToInt32() & 0xFFFF), my2 = (short)((lp.ToInt32() >> 16) & 0xFFFF);
                    int cw = Math.Max(1, (int)Math.Ceiling(w.OrigW * w.Scale) + GLOW_MARGIN * 2), ch = Math.Max(1, (int)Math.Ceiling(w.OrigH * w.Scale) + GLOW_MARGIN * 2);
                    SetCursor(LoadCursor(IntPtr.Zero, mx2 >= cw - RESIZE_EDGE && my2 >= ch - RESIZE_EDGE ? 32642 : 32512));
                }
                return IntPtr.Zero;

            case WM_LBUTTONUP:
                ReleaseCapture(); if (w.Resizing) { w.Resizing = false; Rendering.Render(w); }
                w.Dragging = false; return IntPtr.Zero;

            case WM_MBUTTONDOWN:
                w.Topmost = !w.Topmost; SetWindowPos(hwnd, w.Topmost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                Rendering.Render(w); return IntPtr.Zero;

            case WM_MOUSEWHEEL:
                {
                    int delta = (short)((wp.ToInt64() >> 16) & 0xFFFF);
                    if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
                    {
                        w.Opacity = Math.Max(0.1, Math.Min(1.0, w.Opacity + (delta > 0 ? OPACITY_STEP : -OPACITY_STEP)));
                        w.ShowScaleHint = true; KillTimer(hwnd, w.ScaleHintTimer); w.ScaleHintTimer = SetTimer(hwnd, (IntPtr)1, 500, IntPtr.Zero); Rendering.Render(w);
                    }
                    else Rendering.SetScale(w, w.Scale * (delta > 0 ? SCALE_FACTOR : 1.0 / SCALE_FACTOR));
                    return IntPtr.Zero;
                }

            case WM_KEYDOWN:
                switch (wp.ToInt32()) { case VK_ESCAPE: DestroyWindow(hwnd); return IntPtr.Zero; case VK_LEFT: ImageLoader.Navigate(w, -1); return IntPtr.Zero; case VK_RIGHT: ImageLoader.Navigate(w, 1); return IntPtr.Zero; }
                return IntPtr.Zero;

            case WM_TIMER:
                if (wp == (IntPtr)1) // 缩放/透明度提示定时器
                {
                    KillTimer(hwnd, wp);
                    w.ShowScaleHint = false;
                    Rendering.Render(w);
                }
                else if (wp == (IntPtr)2 && w.IsGif) // GIF 帧切换定时器
                {
                    w.GifFrame = (w.GifFrame + 1) % w.GifFrameCount;
                    ImageLoader.StartGifTimer(w);
                    Rendering.Render(w);
                }
                return IntPtr.Zero;

            case WM_CLOSE: DestroyWindow(hwnd); return IntPtr.Zero;
            case WM_DESTROY: w.SrcBitmap?.Dispose(); GCHandle.FromIntPtr(GetWindowLongPtr(hwnd, 0)).Free(); PostQuitMessage(0); return IntPtr.Zero;
        }
        return DefWindowProcW(hwnd, msg, wp, lp);
    }
}
