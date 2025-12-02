@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
echo 正在编译 CWF 模组...

REM 清理之前的编译结果（可选，dotnet build 会自动处理）
REM if exist "bin" rmdir /s /q "bin"
REM if exist "obj" rmdir /s /q "obj"

REM 编译项目（Release 配置）
dotnet build CWF.csproj -c Release

if %ERRORLEVEL% NEQ 0 (
    echo 编译失败！
    pause
    exit /b 1
)

echo 编译成功！

REM 查找编译后的 DLL 文件
set "DLL_PATH="
if exist "obj\Release\net48\CWF.dll" (
    set "DLL_PATH=obj\Release\net48\CWF.dll"
) else if exist "obj\Debug\net48\CWF.dll" (
    set "DLL_PATH=obj\Debug\net48\CWF.dll"
) else (
    echo 错误：找不到编译后的 DLL 文件！
    pause
    exit /b 1
)

REM 目标路径
set "TARGET_PATH=C:\SteamLibrary\steamapps\workshop\content\294100\3550585103\Assemblies\net48\CWF.dll"

REM 检查目标目录是否存在
for %%F in ("%TARGET_PATH%") do set "TARGET_DIR=%%~dpF"

if not exist "!TARGET_DIR!" (
    echo 创建目标目录: !TARGET_DIR!
    mkdir "!TARGET_DIR!"
)

REM 复制文件
echo 正在复制 DLL 到目标位置...
copy /Y "!DLL_PATH!" "%TARGET_PATH%"

if %ERRORLEVEL% NEQ 0 (
    echo 复制失败！
    pause
    exit /b 1
)

echo 成功！DLL 已复制到: %TARGET_PATH%
endlocal
pause

