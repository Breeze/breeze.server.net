// Build for breeze.server.net

// include gulp
var gulp = require('gulp');

var fs   = require('fs');
var path = require('path');
var glob = require('glob');
var async = require('async');
var del = require('del');
const zip = require('gulp-zip');
var eventStream = require('event-stream');

// include plug-ins
var gutil = require('gulp-util');
var flatten = require('gulp-flatten');

//var concat  = require('gulp-concat');
//var rename  = require('gulp-rename');
//var newer   = require('gulp-newer');

var _tempDir = './_temp/';
var _jsSrcDir = '../../Breeze.js/src/'
var _jsBuildDir = '../../Breeze.js/build/';
var _nugetDir = '../Nuget.builds/'
// var _msBuildCmd = 'C:/Windows/Microsoft.NET/Framework/v4.0.30319/MSBuild.exe ';
var _msBuildCmd = '"C:/Program Files (x86)/MSBuild/14.0/Bin/MsBuild.exe" '; // vs 2015 version of MsBuild
var _msBuildOptions = ' /p:Configuration=Release /verbosity:minimal ';

var _versionNum = getBreezeVersion();
gutil.log('LocalAppData dir: ' + process.env.LOCALAPPDATA);

/**
 * List the available gulp tasks
 */
gulp.task('help', require('gulp-task-listing'));

gulp.task('breezeClientBuild', function(done) {
  execCommands(['gulp'], { cwd: _jsBuildDir }, done);
});

// copy production versions of the breeze.*.js files and adapters into the nuget breeze.client.
gulp.task("copyBreezeJs", ['breezeClientBuild'], function() {
  // 'base' arg in next line allows dir structure from jsBuildDir to be preserved.
  return gulp.src( mapPath( _jsBuildDir, [ 'breeze.*.*', 'adapters/*.*' ]), { base: _jsBuildDir })
    .pipe(gulp.dest(_nugetDir + 'Breeze.Client/content/scripts'));
});


// look for all .dll files in the nuget dir and try to find
// the most recent production version of the same file and copy
// it if found over the one in the nuget dir.
gulp.task("copyDlls", ['breezeServerBuild'], function() {
  var streams = [];
  gutil.log('copying dlls...')
  updateFiles(streams, ".dll");
  gutil.log('copying XMLs...')
  updateFiles(streams, ".XML");
  gutil.log('copying PDBs...')
  updateFiles(streams, ".pdb");
  return eventStream.concat.apply(null, streams);
});

// create a zip file of all the .dll and .xml files
gulp.task("zipDlls", ["copyDlls"], function() {
    var fileNames = glob.sync(_nugetDir + '**/*.XML')
        .concat(glob.sync(_nugetDir + '**/*.dll'))
        .concat(glob.sync(_nugetDir + '**/*.pdb'));
    return gulp.src(fileNames)
        .pipe(zip('breeze-server-net.zip'))
        .pipe(gulp.dest('../build'));
});

// for each file in nuget dir, copy existing file from the release dir.
// @param streams[] - array that will be filled with streams
// @param ext - file extension (with .) of files to copy.
function updateFiles(streams, ext) {
  var fileNames = glob.sync(_nugetDir + '**/*' + ext);
  fileNames.forEach(function(fileName) {
    var baseName = path.basename(fileName, ext);
    var src = '../' + baseName +  '/bin/release/' + baseName + ext
    if (fs.existsSync(src)) {
      var dest = path.dirname(fileName);
      gutil.log("Processing " + fileName);
      streams.push(gulp.src(src).pipe(gulp.dest(dest)));
    } else {
      gutil.log("skipped: " + src);
    }
  });
}

gulp.task('breezeServerBuild', function(done) {
  var solutionFileName = '../Breeze.AspNet.Build.sln';
  msBuildSolution(solutionFileName, done);
});

gulp.task('nugetClean', function() {
  var src = _nugetDir + '**/*.nupkg';
  del.sync(src, { force: true} );
//  return gulp.src(src, { read: false }) // much faster
//      .pipe(rimraf());
});

gulp.task('nugetPack', ['copyBreezeJs', 'copyDlls', 'nugetClean'], function(done) {
  gutil.log('Packing nugets...');
  var fileNames = glob.sync(_nugetDir + '**/Default.nuspec');
  async.each(fileNames, function (fileName, cb) {
    packNuget(fileName, cb);
  }, done);
});

// Deploy to nuget on local machine - NuGet 3.3+
gulp.task('nugetTestDeploy', ['nugetPack'], function(done) {
  gutil.log('Deploying Nugets...');
  var src = _nugetDir + '**/*.nupkg';
  var dest = process.env.LOCALAPPDATA + '/Nuget/Test';
  var fileNames = glob.sync( src);
  async.each(fileNames, function (fileName, cb) {
    gutil.log('Deploying nuspec file: ' + fileName);
    var cmd = 'nuget add ' + fileName + ' -Source ' + dest;
    execCommands([cmd], null, cb);
  }, done);
});



// should ONLY be called manually after testing locally installed nugets from nugetPack step.
// deliberately does NOT have a dependency on nugetPack
gulp.task('nugetDeploy', function(done) {
  gutil.log('Deploying Nugets...');
  var src = _nugetDir + '**/*.nupkg';
  var fileNames = glob.sync( src);
  async.each(fileNames, function (fileName, cb) {
    gutil.log('Deploying nuspec file: ' + fileName);
    var cmd = 'nuget push ' + fileName + ' -Source https://www.nuget.org';
    execCommands([ cmd], null, cb);
  }, done);
});

gulp.task('nugetDeployClient', function(done) {
  gutil.log('Deploying Nugets...');
  var src = _nugetDir + '**/Breeze.Client.*.nupkg';
  var fileNames = glob.sync( src);
  async.each(fileNames, function (fileName, cb) {
    gutil.log('Deploying nuspec file: ' + fileName);
    var cmd = 'nuget push ' + fileName + ' -Source https://www.nuget.org';
    execCommands([ cmd], null, cb);
  }, done);
});

gulp.task('default', ['nugetTestDeploy'] , function() {

});

function packNuget(nuspecFileName, execCb) {
  var folderName = path.dirname(nuspecFileName);
  var text = fs.readFileSync(nuspecFileName, { encoding: 'utf8'});
  var folders = folderName.split('/');
  var folderId = folders[folders.length-1];

  text = text.replace(/{{version}}/g, _versionNum);
  text = text.replace(/{{id}}/g, folderId);
  var destFileName = folderName + '/' + folderId + '.nuspec';
  gutil.log('Packing nuspec file: ' + destFileName);
  fs.writeFileSync(destFileName, text);
  // 'nuget pack $folderName.nuspec'
  var cmd = 'nuget pack ' + folderId + '.nuspec'
  execCommands([ cmd], { cwd: folderName }, execCb);
}

function getBreezeVersion() {
  var versionFile = fs.readFileSync( _jsSrcDir + '_head.jsfrag');
  var regex = /\s+version:\s*"(\d.\d\d*.?\d*.?\d*)"/
  var matches = regex.exec(versionFile);

  if (matches == null) {
    throw new Error('Breeze client version number not found');
  }
  // matches[0] is entire version string - [1] is just the capturing group.
  var versionNum = matches[1];
  gutil.log("Breeze client version from: " + _jsSrcDir + ' is: ' + versionNum);
  return versionNum;
}



function msBuildSolution(solutionFileName, done) {
  if (!fs.existsSync(solutionFileName)) {
    throw new Error(solutionFileName + ' does not exist');
  }
  var baseName = path.basename(solutionFileName);
  var rootCmd = _msBuildCmd + '"' + baseName +'"' + _msBuildOptions + ' /t:'
  var nuGetRestoreCmd = 'nuget.exe restore '  + '"' + baseName +'"';

  var cmds = [nuGetRestoreCmd, rootCmd + 'Clean', rootCmd + 'Rebuild'];
  var cwd = path.dirname(solutionFileName);
  execCommands(cmds, { cwd: cwd},  done);
}


// utilities
// added options are: shouldLog
// cb is function(err, stdout, stderr);
function execCommands(cmds, options, cb) {
  options = options || {};
  options.shouldThrow = options.shouldThrow == null ? true : options.shouldThrow;
  options.shouldLog = options.shouldLog == null ? true : options.shouldLog;
  if (!cmds || cmds.length == 0) cb(null, null, null);
  var exec = require('child_process').exec;  // just to make it more portable.
  if (options.shouldLog && options.cwd){
    gutil.log('executing command ' + cmds[0] + ' in directory ' + options.cwd);
  }
  exec(cmds[0], options, function(err, stdout, stderr) {
    if (err == null) {
      if (options.shouldLog) {
        gutil.log('cmd: ' + cmds[0]);
        gutil.log('stdout: ' + stdout);
      }
      if (cmds.length == 1) {
        cb(err, stdout, stderr);
      } else {
        execCommands(cmds.slice(1), options, cb);
      }
    } else {
      if (options.shouldLog) {
        gutil.log('exec error on cmd: ' + cmds[0]);
        gutil.log('exec error: ' + err);
        if (stdout) gutil.log('stdout: ' + stdout);
        if (stderr) gutil.log('stderr: ' + stderr);
      }
      if (err && options.shouldThrow) throw err;
      cb(err, stdout, stderr);
    }
  });
}

function mapPath(dir, fileNames) {
  return fileNames.map(function(fileName) {
    return dir + fileName;
  });
};

