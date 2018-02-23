@echo off
for /F "usebackq tokens=1,2 delims==" %%i in (`wmic os get LocalDateTime /VALUE 2^>NUL`) do if '.%%i.'=='.LocalDateTime.' set ldt=%%j
set timestamp=%ldt:~0,4%-%ldt:~4,2%-%ldt:~6,2% %ldt:~8,2%.%ldt:~10,2%

set "git=git"
for /f %%i in ('%git% remote') do set remote=%%i

set "tag=%1"
set "message=Release %tag% (%timestamp%)"

echo Creating Git Tag for release:
echo     Remote:   %remote%
echo     Tag:      %tag%
echo     Message:  %message%

%git% tag -a "%tag%" -m "%message%"
%git% push %remote% tag "%tag%"
