@echo off
REM Batch file to import breeze.js files from various sources
REM Should run a fresh before tests to get the latest.
REM .NET projects run this automatically for each build
REM Assumes the source directories are in a specific directory relative to this batch file

set execDir=%~dp0
set breezeDir=%execDir%..\..\
echo breezeDir is '%breezeDir%'
set breezeJsDir=%breezeDir%breeze.js\
set breezeJsBuildDir=%breezeJsDir%build\
set breezeJsSrcDir=%breezeJsDir%src\
set breezeJsLabsDir=%breezeDir%breeze.js.labs\
set jsbreeze=%execDir%breeze.js.tests\breeze\
set jslibs=%execDir%breeze.js.tests\libs\


IF EXIST %breezeJsDir% GOTO breezeSourceExists
@echo Cannot find '%breezeJsDir%'; no files copied
GOTO :done

:breezeSourceExists
@echo on
@echo Copying 'breeze.*.js' from '%breezeJsBuildDir%' to '%jsbreeze%'
COPY "%breezeJsBuildDir%breeze.*.js" "%jsbreeze%" /Y

@echo Copying 'breeze.dataService.mongo.js' from '%breezeJsSrcDir%' to '%jsbreeze%'
COPY "%breezeJsSrcDir%breeze.dataService.mongo.js" "%jsbreeze%" /Y

@echo Copying '*.js' from '%breezeJsLabsDir%' to '%jsbreeze%'
COPY "%breezeJsLabsDir%*.js" "%jsbreeze%" /Y
MOVE /Y "%jsbreeze%ngMidwayTester.js"  "%jslibs%"

:done
