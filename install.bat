@echo off
setlocal enabledelayedexpansion

set "TaskName=Cafeteria Notifier"
set "TaskPath=\%TaskName%"

REM Set the executable name and path.
REM By default, uses CafeteriaNotifier.exe in the same folder as this script.
REM You can change ExecutablePath below if your .exe is elsewhere.
REM %~dp0 expands to the folder where this batch file is located.
set "ExecutablePath=%~dp0CafeteriaNotifier.exe"

REM Check if the executable exists
if not exist "%ExecutablePath%" (
    echo %ExecutablePath% not found. Either place CafeteriaNotifier.exe in the same folder as this script or edit the script to point to the correct location.
    pause
    goto :eof
)

REM Check if task exists
schtasks /Query /TN "%TaskPath%" >nul 2>&1
if !errorlevel!==0 (
    echo Task "%TaskName%" already exists.
    choice /M "The task is already installed. Remove it?"
    if !errorlevel!==1 (
        schtasks /Delete /TN "%TaskPath%" /F
        echo Task deleted.
    ) else (
        echo Task not deleted.
        pause
        goto :eof
    )
)

REM Clean parameters
set phone=
set token=
set balanceLimit=

REM Prompt for parameters
set /p phone="Enter phone (Probably not your phone number. See 'Arguments' in README.md): "
if "!phone!"=="" goto :eof
set /p token="Enter token: "
if "!token!"=="" goto :eof
set /p balanceLimit="Enter balance limit (Default 200): " || set balanceLimit=200

REM Check if balanceLimit is an integer
if not 1%balanceLimit% EQU +1%balanceLimit% (
    echo Balance limit must be an integer number, was "%balanceLimit%".
    pause
    goto :eof
)

REM Create the task (run only on weekdays: MON,TUE,WED,THU,FRI)
echo Creating task "%TaskName%"...
schtasks /Create /TN "%TaskPath%" /TR "\"%ExecutablePath%\" %phone% %token% %balanceLimit%" /SC WEEKLY /ST 10:00 /D MON,TUE,WED,THU,FRI
if %errorlevel%==0 (
    echo Task created successfully.
) else (
    echo Failed to create task.
)

pause