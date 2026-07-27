// Rendering.cs — 缩放 + GDI+ 渲染 + UpdateLayeredWindow
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static NativeConstants;
using static NativeMethods;

static class Rendering
{
    public static void SetScale(ImageWindow w, double s)
    {
        if (s < MIN_SCALE) s = MIN_SCALE; if (s > MAX_SCALE) s = MAX_SCALE;
        if (Math.Abs(s - w.Scale) < 0.0001) return;
        w.Scale = s;
        GetWindowRect(w.Hwnd, out RECT r);
        int nw = Math.Max(1, (int)Math.Ceiling(w.OrigW * s) + GLOW_MARGIN * 2);
        int nh = Math.Max(1, (int)Math.Ceiling(w.OrigH * s) + GLOW_MARGIN * 2);
        SetWindowPos(w.Hwnd, IntPtr.Zero, r.Left, r.Top, nw, nh,
            SWP_NOZORDER | SWP_NOACTIVATE);

        w.ShowScaleHint = true;
        KillTimer(w.Hwnd, w.ScaleHintTimer);
        w.ScaleHintTimer = SetTimer(w.Hwnd, (IntPtr)1, 500, IntPtr.Zero);

        Render(w);
    }

    public static void Render(ImageWindow w)
    {
        if (w.SrcBitmap == null || w.Hwnd == IntPtr.Zero) return;
        int imgW = Math.Max(1, (int)Math.Ceiling(w.OrigW * w.Scale));
        int imgH = Math.Max(1, (int)Math.Ceiling(w.OrigH * w.Scale));
        int ww = imgW + GLOW_MARGIN * 2;
        int hh = imgH + GLOW_MARGIN * 2;

        try
        {
            // GIF 动画：切换到当前帧
            if (w.IsGif && w.GifFrameCount > 1)
                w.SrcBitmap.SelectActiveFrame(FrameDimension.Time, w.GifFrame);

            using var dest = new Bitmap(ww, hh, PixelFormat.Format32bppPArgb);
            using var g = Graphics.FromImage(dest);

            if (w.Resizing) { g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.HighSpeed; }
            else { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality; }
            g.DrawImage(w.SrcBitmap, GLOW_MARGIN, GLOW_MARGIN, imgW, imgH);

            bool act = w.Active; bool top = w.Topmost;
            int r, gv, b;
            if (top) { r = 0; gv = act ? 200 : 140; b = act ? 80 : 50; }
            else { r = 30; gv = act ? 200 : 144; b = 255; }
            Rectangle imgRect = new(GLOW_MARGIN, GLOW_MARGIN, imgW, imgH);

            for (int i = GLOW_MARGIN - 1; i >= 0; i--)
            {
                float t = (float)i / GLOW_MARGIN;
                int a = (int)(30 + 150 * t * t);
                using var pen = new Pen(Color.FromArgb(a, r, gv, b), 1);
                g.DrawRectangle(pen, imgRect.Left - i, imgRect.Top - i,
                    imgRect.Width + i * 2 - 1, imgRect.Height + i * 2 - 1);
            }

            using var edgePen = new Pen(Color.FromArgb(act ? 220 : 200, r, gv, b), 1);
            g.DrawRectangle(edgePen, imgRect.Left - 1, imgRect.Top - 1,
                imgRect.Width + 1, imgRect.Height + 1);

            if (w.ShowScaleHint)
            {
                string t1 = $"大小：{(int)(w.Scale * 100)}%";
                string t2 = $"不透明度：{(int)(w.Opacity * 100)}%";
                using var font = new Font("Microsoft YaHei", 14, FontStyle.Regular);
                var sz1 = g.MeasureString(t1, font);
                var sz2 = g.MeasureString(t2, font);
                float mw = Math.Max(sz1.Width, sz2.Width);
                int tx = GLOW_MARGIN + 6, ty = GLOW_MARGIN + 6;
                int tw = (int)mw + 12, th = (int)(sz1.Height + sz2.Height) + 8;
                using var bg = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
                g.FillRectangle(bg, tx, ty, tw, th);
                using var fg = new SolidBrush(Color.White);
                g.DrawString(t1, font, fg, tx + 6, ty + 4);
                g.DrawString(t2, font, fg, tx + 6, ty + 4 + sz1.Height);
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
}
