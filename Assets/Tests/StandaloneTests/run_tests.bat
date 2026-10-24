@echo off
echo =======================================================
echo Compiling and Running Curve Shot Physics & Math Tests...
echo =======================================================
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:MathAndPhysicsTests.exe MathAndPhysicsTests.cs
if %errorlevel% neq 0 (
    echo Compilation Failed!
    pause
    exit /b %errorlevel%
)
MathAndPhysicsTests.exe
pause
