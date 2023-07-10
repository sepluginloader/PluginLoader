@echo off
Rem Run this script with the full file path as argument

if [%1] == [] goto eof

set se_folder=%~dp0..\Bin64
set counter=1

Rem Wait for the file to be ready
:waitfile
2>nul (
 >>%se_folder%\%~nx1 (call )
) && (goto copyfile) || (echo File is in use.)
set /a counter=counter+1
echo Trying attempt #%counter%
ping -n 6 127.0.0.1 >nul
goto waitfile

Rem Copy the file to the target location
:copyfile
echo Copying file.
for /R "%~dp1" %%F in (*.dll) DO copy /y "%%F" "%se_folder%\%%~nxF"

:eof