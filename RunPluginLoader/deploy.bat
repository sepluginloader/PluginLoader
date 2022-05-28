@echo off
Rem Run this script with the full file path as argument

if [%1] == [] goto eof

set workshop="%~dp0..\workshop\content\244850\2407984968\%~n1"
set counter=1

Rem Wait for the file to be ready
:waitfile
2>nul (
 >>%workshop% (call )
) && (goto copyfile) || (echo File is in use.)
set /a counter=counter+1
echo Trying attempt #%counter%
ping -n 6 127.0.0.1 >nul
goto waitfile

Rem Copy the file to the target location
:copyfile
echo Copying file.
copy /y "%~1" %workshop%
copy /y "%~1" "%appdata%\SpaceEngineers\Mods\Plugin Loader\%~n1"

:eof