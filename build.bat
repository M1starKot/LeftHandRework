@echo off
title Build SPTLeftStanceWallFix v1.0.0 (com.m1starkot.leftstancewallfix)
echo Building SPTLeftStanceWallFix v1.0.0...
dotnet build "%~dp0SPTLeftStanceWallFix.csproj" -c Release
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed! If your SPT is not in L:\Games\SPT, run:
    echo dotnet build -c Release -p:SPTPath="C:\Path\To\SPT"
    pause
    exit /b 1
)
powershell -Command "Compress-Archive -Path '%~dp0bin\Release\BepInEx' -DestinationPath '%~dp0com.m1starkot.leftstancewallfix-v1.0.0.zip' -Force"
echo.
echo [SUCCESS] Built DLL: bin\Release\BepInEx\plugins\SPTLeftStanceWallFix.dll
echo [SUCCESS] Release ZIP: com.m1starkot.leftstancewallfix-v1.0.0.zip
pause
