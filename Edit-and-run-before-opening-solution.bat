@echo off

REM Location of your SpaceEngineers.exe
mklink /J Bin64 "H:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64"

REM Location of your workshop
mklink /J workshop "H:\SteamLibrary\steamapps\workshop"

pause
