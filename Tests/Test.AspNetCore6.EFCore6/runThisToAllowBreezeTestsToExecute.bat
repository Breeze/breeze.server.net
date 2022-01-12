rem This batch file assumes that the breeze.js repo is at the same level as this repo.
rem create a link to the breezeTests
mklink /j breezeTests ..\..\..\breeze.js\test
rem create a local copy of breeze.js
mkdir breezeTests\breeze
copy ..\..\..\breeze.js\build\breeze.debug.js breezeTests\breeze\