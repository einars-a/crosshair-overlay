@echo off
rem Rebuilds CrosshairOverlay.exe with the C# compiler that ships with Windows.
cd /d "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /out:CrosshairOverlay.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll CrosshairOverlay.cs
if errorlevel 1 (echo Build failed. & pause) else (echo Built CrosshairOverlay.exe)
