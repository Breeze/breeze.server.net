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
set jslibs=%execDir%breeze.js.tests\libs\


IF EXIST %breezeJsDir% GOTO breezeSourceExists
@echo Cannot find '%breezeJsDir%'; no files copied
GOTO :done

:breezeSourceExists
@echo on
@echo Copying 'breeze.*.js' from '%breezeJsBuildDir%' to '%jslibs%'
COPY "%breezeJsBuildDir%breeze.*.js" "%jslibs%" /Y

@echo Copying 'breeze.dataService.mongo.js' from '%breezeJsSrcDir%' to '%jslibs%'
COPY "%breezeJsSrcDir%breeze.dataService.mongo.js" "%jslibs%" /Y

@echo Copying '*.js' from '%breezeJsLabsDir%' to '%jslibs%'
COPY "%breezeJsLabsDir%*.js" "%jslibs%" /Y

:done
