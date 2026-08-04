# AGENTS.md — SnapView 开发记忆

## 项目概览

SnapView 是一个极简的 Windows 图片查看器。核心行为类似 Snipaste 的"钉图"功能：以无边框分层窗口显示图片，支持无限缩放、透明度调节、置顶、同目录导航。使用者通常将其设为默认看图程序，双击图片即打开。

技术路线：.NET 8 Console App（`WinExe`），零 WPF/零 WinForms，全部通过 P/Invoke 调用 Win32 API（`user32.dll` + `gdi32.dll`）实现窗口创建、消息循环、渲染。GDI+（`System.Drawing.Common`）负责位图缩放和描边绘制，`UpdateLayeredWindow` 负责输出到屏幕。

## 维护纪律

1. **每次功能新增或 bug 修复必须同步更新本文件末尾的经验日志**，记录日期、变更内容和吸取的教训。
2. **每次改动后必须将 `installer.iss` 中的 `AppVersion` 递增**（如 `1.0` → `1.1`），确保安装包版本与代码一致。
3. **提交信息格式**：`type: brief summary`，type 可选 `feat` / `fix` / `perf` / `refactor` / `docs` / `chore`。
4. **README.md** 在新增用户可感知的功能后同步更新 Usage/Controls 部分。
5. **所有渲染相关改动必须在真机上以超过屏幕分辨率的高度测试**——WPF 时代的裁切问题是一个深刻教训。

## 技术栈与命令

| 项目 | 说明 |
|------|------|
| 运行时 | .NET 8, `net8.0` TFM |
| 输出类型 | `WinExe`（无控制台黑框） |
| 发布 | `dotnet publish -c Release -o publish`, csproj 已配置 `<SelfContained>true` + `<PublishSingleFile>true` |
| 依赖 | `System.Drawing.Common`（GDI+） |
| 打包 | Inno Setup 6, `installer.iss` → `build_installer.ps1` |
| 安装包构建 | `powershell -ExecutionPolicy Bypass -File .\build_installer.ps1` |

## 核心文件与职责

```
src/
├── Program.cs         入口：窗口类注册、CreateWindowEx、消息循环
├── NativeMethods.cs   所有 Win32 结构体、常量、P/Invoke 声明
├── ImageWindow.cs     单窗口的运行时状态（位图缓存、缩放、拖拽等字段）
├── WindowManager.cs   WndProc——消息路由到 Rendering / ImageLoader
├── Rendering.cs       SetScale + Render（GDI+ 绘制 + UpdateLayeredWindow）
└── ImageLoader.cs     图片加载、文件夹枚举、GIF 解析、导航
installer.iss          Inno Setup 安装脚本
build_installer.ps1    一键构建安装包
app.ico                exe 图标
```

## 架构约定（重要）

### 窗口创建：`CreateWindowEx` + `WS_EX_LAYERED`

- 项目**不创建 WPF `Window`**、不使用 `HwndSource`。
- 窗口通过 `CreateWindowExW` 直接创建，样式 `WS_POPUP | WS_VISIBLE | WS_SYSMENU`，扩展样式 `WS_EX_LAYERED | WS_EX_APPWINDOW`。
- 窗口类 `WND_CLASS = "SnapViewWnd"` 在 `Program.Main` 中注册，`cbWndExtra` 预留一个指针用于存储 `ImageWindow*`（GCHandle）。

### 消息循环与输入

- 使用标准 Win32 消息循环 `GetMessageW` / `TranslateMessage` / `DispatchMessageW`。
- WndProc 在 `WindowManager.WndProc` 中实现，作为静态委托传给 `WNDCLASSEXW.lpfnWndProc`。
- 输入处理**完全在 WndProc 中完成**，不经过任何 UI 框架的抽象层。

### 渲染管道：GDI+ → UpdateLayeredWindow

- `Rendering.Render()` 创建目标大小 `Bitmap`（`Format32bppPArgb`），用 GDI+ 缩放并绘制源位图 + 描边，再取 `HBITMAP` 通过 `UpdateLayeredWindow(ULW_ALPHA)` 输出。
- 透明度通过 `BLENDFUNCTION.SourceConstantAlpha` 实现，**不使用** `SetLayeredWindowAttributes`（二者互斥，混用会导致黑屏）。
- 窗口尺寸 = 图片尺寸 + `GLOW_MARGIN * 2`。描边绘制在图片区域外侧的 margin 中，不覆盖图片像素。
- 原始尺寸 `OrigW`/`OrigH` 用于缩放计算，保持不变；加载时大图预缩到 4096px 上限以优化性能。

### 动画 (GIF)

- GIF 帧数和延迟从 `PropertyTagFrameDelay` (0x5100) 读取。
- 帧切换通过 `SetTimer` + `WM_TIMER`（ID=2）驱动，区别于提示定时器（ID=1）。
- 渲染前调用 `SelectActiveFrame(FrameDimension.Time, frameIndex)` 切换到当前帧。

### 自然排序

- 文件列表使用 `StrCmpLogicalW`（shlwapi.dll）排序，与 Windows 资源管理器的"按名称排序"行为一致。

## 已知坑（务必避免）

1. **不要用 WPF Window**：`AllowsTransparency="True"` 的 WPF 窗口在尺寸超过屏幕物理分辨率时，`DrawImage` 渲染位图会被裁切。这是 WPF 渲染管线的硬限制，无法通过 hook 绕过。当前项目曾经历多次 WPF 方案失败后才迁移到纯 Win32 架构。

2. **不要混用 `UpdateLayeredWindow` 和 `SetLayeredWindowAttributes`**：前者使用 per-pixel alpha（`ULW_ALPHA`），后者使用全局 alpha（`LWA_ALPHA`），混用会导致窗口变黑或渲染异常。透明度应通过 `BLENDFUNCTION.SourceConstantAlpha` 控制。

3. **不要用 `Marshal.WriteIntPtr(hwnd, 0, ...)` 写窗口额外字节**：`hwnd` 是句柄而非内存地址，直接写入会触发 `AccessViolationException`。必须用 `SetWindowLongPtr` / `GetWindowLongPtr`。

4. **`SetWindowPos` 改变 Z 序时不要带 `SWP_NOZORDER`**：该标志会忽略 `hWndInsertAfter` 参数，导致 `HWND_TOPMOST` 不生效。

5. **`WM_ACTIVATE` 中不要调用 `SetFocus`**：会导致焦点变化循环。正确的键盘焦点设置时机是 `WM_MOUSEACTIVATE`。

6. **切图导航后必须显式调用 `Rendering.Render()`**：`SetScale` 内部有"相同缩放值跳过渲染"的优化，导航时新旧图片缩放值可能碰巧相同，导致窗口未刷新。

7. **GDI+ 的 `PixelFormat32bppPARgb` 是预乘 Alpha**：`GetHbitmap` 时传 `Color.FromArgb(0,0,0,0)` 作为背景色，否则透明区域会残留垃圾色。

## 测试技巧

- **超屏幕高度渲染测试**：缩放图片至窗口高度超过屏幕高度，确认图片和描边均无裁切。
- **透明 PNG 测试**：用带 Alpha 通道的 PNG 验证透明区域 + Ctrl+Click 白色背景切换。
- **GIF 动画测试**：确认多帧 GIF 正常播放，切图后定时器正确重置。
- **自然排序测试**：文件夹内放置 `1.jpg, 2.jpg, 10.jpg, 11.jpg`，确认导航顺序为 1→2→10→11。
- **安装包测试**：`build_installer.ps1` 构建后安装，检查文件关联和卸载是否正常。
- **置顶测试**：中键切换置顶，打开其他窗口确认 SnapView 保持在最前；再次中键取消。
- **透明度+缩放不冲突测试**：Ctrl+滚轮调透明度后再滚轮缩放，确认不黑屏。

## 经验更新日志

### 2025-07-27 — 初始版本总结
- WPF `AllowsTransparency="True"` 的 `DrawImage` 裁切问题是 WPF 框架的硬限制，`WM_GETMINMAXINFO`、`WM_WINDOWPOSCHANGING`、`WM_NCHITTEST` 等 hook 均无法解决图片内容的裁切。
- `UpdateLayeredWindow` + GDI+ 是分层窗口下唯一可靠的位图渲染路径。
- Win32 消息循环 + 自定义 WndProc 虽然代码量大，但消除了框架层的所有不确定行为。
- `StrCmpLogicalW` 比字母排序更符合用户直觉。
- 大图预缩（max 4096px）对性能提升显著，且不影响视觉质量——显示器的物理像素远小于此。
