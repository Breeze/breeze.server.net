// Build for breeze.server.net

// include gulp
var gulp = require('gulp');

var fs   = require('fs-extra');
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
// var _buildSlnDirs = ["../AspNet/", "../AspNetCore/"];
// var _buildSlnDirs = ["../AspNet/"];
var _buildSlnDirs = ["../AspNetCore/"];
var _nugetDirs = _buildSlnDirs.map(function(bsd) {
  return path.join(bsd, "/Nuget.builds/");
});
// ['../AspNet/Nuget.builds/', '../AspNetCore/Nuget.builds/'];
// var _msBuildCmd = 'C:/Windows/Microsoft.NET/Framework/v4.0.30319/MSBuild.exe ';
// var _msBuildCmd = '"C:/Program Files (x86)/MSBuild/14.0/Bin/MsBuild.exe" '; // vs 2015 version of MsBuild
// var _msBuildCmd = '"C:/Program Files (x86)/Microsoft Visual Studio/2017/Professional/MSBuild/15.0/Bin/MSBuild.exe" ' // vs 2017 version of MsBuild
var _msBuildCmd = '"C:/Program Files (x86)/Microsoft Visual Studio/2017/Enterprise/MSBuild/15.0/Bin/MSBuild.exe" '
// var _msBuildOptions = ' /p:Configuration=Release /verbosity:minimal ';
var _msBuildOptions = ' /p:Configuration=Release /verbosity:minimal  /clp:NoSummary;NoItemAndPropertyList;ErrorsOnly';

var _breezeClientVersionNum = getBreezeVersion();
gutil.log('LocalAppData dir: ' + process.env.LOCALAPPDATA);

/**
 * List the available gulp tasks
 */
gulp.task('help', require('gulp-task-listing'));

gulp.task('breezeClientBuild', function(done) {
  execCommands(['gulp'], { cwd: _jsBuildDir }, done);
});

// copy production versions of the breeze.*.js files and adapters into the nuget breeze.client.
gulp.task("copyBreezeJs", ['breezeClientBuild'], function(done) {
  // 'base' arg below allows dir structure from jsBuildDir to be preserved.
  return eventStream.concat(_nugetDirs.map(function(nd) {
    return gulp.src( mapPath( _jsBuildDir, [ 'breeze.*.*', 'adapters/*.*' ]), { base: _jsBuildDir })
       .pipe(gulp.dest(nd + 'Breeze.Client/content/scripts'));
  }));

});

gulp.task("buildAspNetCoreNugets", function(done) {
  const isDirectory = source => fs.lstatSync(source).isDirectory()
  var dirs = glob.sync('../AspNetCore/Breeze.*');
  projDirs = dirs.filter(d => isDirectory(d)); //  && !d.endsWith('EF6'));

  async.eachSeries(projDirs, function (dir, cb) {
    gutil.log(dir);
    var cmd = 'msbuild /t:pack /p:Configuration=Release';
    execCommands( [cmd], { cwd: dir}, cb);
  }, done);
  

});


// look for all .dll files in the nuget dir and try to find
// the most recent production version of the same file and copy
// it if found over the one in the nuget dir.
gulp.task("copyDlls", ['breezeServerBuild'], function() {
  return eventStream.concat(_nugetDirs.map(function(nd) {
    var streams = [];
    gutil.log('copying dlls...')
    updateFiles(nd, streams, ".dll");
    gutil.log('copying XMLs...')
    updateFiles(nd, streams, ".XML");
    gutil.log('copying PDBs...')
    updateFiles(nd, streams, ".pdb");
    return eventStream.concat.apply(null, streams);
  }));
});

// need to run this the first time, because updateFiles only updates files that already exist
gulp.task("initDlls", [], function() {
    // copyToNugetLib("../AspNet/", "Breeze.ContextProvider");
    // copyToNugetLib("../AspNet/", "Breeze.ContextProvider.EF6");
    // copyToNugetLib("../AspNet/", "Breeze.ContextProvider.NH");
    // copyToNugetLib("../AspNet/", "Breeze.WebApi2");
    copyToNugetLib("../AspNetCore/", "Breeze.AspNetCore.NetCore");
    copyToNugetLib("../AspNetCore/", "Breeze.Core");
    copyToNugetLib("../AspNetCore/", "Breeze.Persistence");
    copyToNugetLib("../AspNetCore/", "Breeze.Persistence.EF6");
    copyToNugetLib("../AspNetCore/", "Breeze.Persistence.EFCore");
});

// pathRoot = "../AspNet/" or "../AspNetCore/"
// fileRoot = "Breeze.ContextProvider.EF6" or similar
function copyToNugetLib(pathRoot, fileRoot) {
    var name = fileRoot + ".dll";
    var exts = [".dll", ".pdb", ".XML"];
    var destdir = (pathRoot == "../AspNetCore/") ? fileRoot : fileRoot.replace(/^Breeze/, "Breeze.Server");
    var subdir = (pathRoot == "../AspNetCore/") ? ((fileRoot == "Breeze.Persistence.EF6") ? "net462/" : "netstandard2.0/") : "";
    exts.forEach(function(ext) {
        var sourceFile = pathRoot + fileRoot + "/bin/Release/" + subdir + fileRoot + ext;
        var targetFile = pathRoot + "Nuget.builds/" + destdir + "/lib/" + subdir + fileRoot + ext;
        fs.copy(sourceFile, targetFile, function(err) {
            if (err) {
              gutil.log("Cannot copy " + sourceFile + " to " + targetFile);
              throw err;
            }
            gutil.log("Copied " + sourceFile + " to " + targetFile);
        });
    });
}

// for each file in nuget dir, copy existing file from the release dir.
// @param streams[] - array that will be filled with streams
// @param ext - file extension (with .) of files to copy.
function updateFiles(nugetDir, streams, ext) {
  var fileNames = glob.sync(nugetDir + '**/*' + ext);
  gutil.log("Copying " + fileNames.length + " files from /bin/release dir into " + nugetDir);
  fileNames.forEach(function(fileName) {
    var baseName = path.basename(fileName, ext);
    var src;
    if (_buildSlnDirs.some(function(dir) {
      src = dir + baseName +  '/bin/release/' + baseName + ext;
      // gutil.log("test: " + src);
      if (fs.existsSync(src)) return true;
      src = dir + baseName +  '/bin/release/netstandard2.0/' + baseName + ext;
      // gutil.log("test: " + src);
      if (fs.existsSync(src)) return true;
      src = dir + baseName +  '/bin/release/net462/' + baseName + ext;
      return fs.existsSync(src);
    })) {
      var dest = path.dirname(fileName);
      gutil.log("Copying " + src + " to " + dest);
      streams.push(gulp.src(src).pipe(gulp.dest(dest)));
    } else {
      gutil.log("skipped: " + src);
    }
  });
}

// create a zip file of all the .dll and .xml files
gulp.task("zipDlls", ["copyDlls"], function() {
  return eventStream.concat(_nugetDirs.map(function(nd) {
    var fileNames = glob.sync(nd + '**/*.XML')
        .concat(glob.sync(nd + '**/*.dll'))
        .concat(glob.sync(nd + '**/*.pdb'));
    return gulp.src(fileNames)
        .pipe(zip('breeze-server-net.zip'))
        .pipe(gulp.dest('../build'));
  }));
});

gulp.task('breezeServerBuild', function(done) {
  var solutionFileNames = [];
  _buildSlnDirs.forEach(function(bsd) {
    [].push.apply(solutionFileNames, glob.sync(bsd + "*.sln"));
  });
  async.eachSeries(solutionFileNames, function(sfn, cb1) {
    sfn = path.normalize(sfn);
    gutil.log('Building solution: ' + sfn );
    msBuildSolution(sfn, cb1);
  }, done);
});

gulp.task('nugetClean', function() {
  _nugetDirs.forEach(function(nd) {
    var src = nd + '**/*.nupkg';
    del.sync(src, { force: true} );
    //  return gulp.src(src, { read: false }) // much faster
    //      .pipe(rimraf());
  });
});

gulp.task('nugetPack', ['copyBreezeJs', 'copyDlls', 'nugetClean'], function(done) {
  async.eachSeries(_nugetDirs, function(nd, cb1) {
    var version;
    versionFileName = path.resolve(nd, '../version.txt');
    serverVersion = fs.readFileSync(versionFileName, { encoding: 'utf8'});

    var fileNames = glob.sync(nd + '**/Default.nuspec');
    async.eachSeries(fileNames, function (fileName, cb2) {
      if (fileName.toLowerCase().indexOf('breeze.client') != -1) {
        version = _breezeClientVersionNum;
      } else {
        version = serverVersion;
      }
      gutil.log('Packing nuget for ' + fileName + ' version: ' + version);
      packNuget(fileName, version, cb2);
    }, cb1);
  }, done);
});

// Deploy to nuget on local machine - NuGet 3.3+
gulp.task('nugetTestDeploy', ['nugetPack'], function(done) {
  async.eachSeries(_nugetDirs, function(nd, cb1) {
    gutil.log('Deploying Test Nugets...');
    var src = nd + '**/*.nupkg';
    var dest = process.env.LOCALAPPDATA + '/Nuget/Test';
    var fileNames = glob.sync( src);
    async.eachSeries(fileNames, function (fileName, cb2) {
      gutil.log('Deploying nuspec file: ' + fileName);
      var cmd = 'nuget add ' + fileName + ' -Source ' + dest;
      execCommands([cmd], null, cb2);
    }, cb1);
  }, done);
});


// should ONLY be called manually after testing locally installed nugets from nugetPack step.
// deliberately does NOT have a dependency on nugetPack
gulp.task('nugetDeploy', function(done) {
  async.eachSeries(_nugetDirs, function(nd, cb1) {
    gutil.log('Deploying Nugets...');
    var src = nd + '**/*.nupkg';
    var fileNames = glob.sync( src);
    async.eachSeries(fileNames, function (fileName, cb2) {
      gutil.log('Deploying nuspec file: ' + fileName);
      var cmd = 'nuget push ' + fileName + ' -Source https://www.nuget.org';

      execCommands([ cmd], { shouldThrow: false }, cb2);

    }, cb1);
  }, done);
});

gulp.task('nugetDeployClient', function(done) {
  async.eachSeries(_nugetDirs, function(nd, cb1) {
    gutil.log('Deploying Nugets...');
    var src = nd + '**/Breeze.Client.*.nupkg';
    var fileNames = glob.sync( src);
    async.eachSeries(fileNames, function (fileName, cb2) {
      gutil.log('Deploying nuspec file: ' + fileName);
      var cmd = 'nuget push ' + fileName + ' -Source https://www.nuget.org';
      execCommands([ cmd], null, cb2);
    }, cb1);
  }, done);
});

gulp.task('default', ['nugetTestDeploy'] , function() {

});

function packNuget(nuspecFileName, version, execCb) {
  var folderName = path.dirname(nuspecFileName);
  var text = fs.readFileSync(nuspecFileName, { encoding: 'utf8'});
  var folders = folderName.split('/');
  var folderId = folders[folders.length-1];

  text = text.replace(/{{version}}/g, version);
  text = text.replace(/{{clientVersion}}/g, _breezeClientVersionNum);
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

