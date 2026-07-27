# SnapView 安装包构建脚本
# 需要先安装 Inno Setup: https://jrsoftware.org/isdl.php

$ErrorActionPreference = "Stop"

# 1. 编译项目
Write-Host "[1/3] 编译项目..." -ForegroundColor Cyan
dotnet publish -c Release -o publish --self-contained true -p:PublishSingleFile=true -p:RuntimeIdentifier=win-x64

# 2. 查找 Inno Setup 编译器（兼容所有安装位置和版本）
$iscc = Get-ChildItem -Path `
    "${env:ProgramFiles(x86)}\Inno Setup*",
    "${env:ProgramFiles}\Inno Setup*",
    "${env:LocalAppData}\Programs\Inno Setup*" `
    -Filter ISCC.exe -Recurse -ErrorAction SilentlyContinue `
    | Select-Object -First 1 -ExpandProperty FullName

if (-not $iscc) {
    Write-Host "错误: 未找到 Inno Setup，请从 https://jrsoftware.org/isdl.php 下载安装" -ForegroundColor Red
    exit 1
}

# 3. 构建安装包
Write-Host "[2/3] 构建安装包..." -ForegroundColor Cyan
& $iscc installer.iss

Write-Host "[3/3] 完成！安装包位置: installer\SnapView_Setup.exe" -ForegroundColor Green
