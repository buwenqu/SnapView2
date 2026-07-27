// ImageWindow.cs — 窗口实例数据
using System;
using System.Collections.Generic;
using System.Drawing;

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
    public bool ShowScaleHint;
    public IntPtr ScaleHintTimer;
    // GIF 动画
    public bool IsGif;
    public int GifFrameCount;
    public int GifFrame;
    public int[]? GifDelays; // 每帧延迟（毫秒）
    public IntPtr GifTimer;    // 白色背景
    public bool ShowWhiteBg;
    // 当前文件名（用于切换提示）
    public string? CurrentFileName;
}
