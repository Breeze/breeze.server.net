module.exports = function(grunt) {

  var path = require('path');
  var tempDir = '../_temp/';
  var jsBuildDir = '../../../Breeze.js/build/';
  var jsSrcDir = '../../../Breeze.js/src/'
  var nugetDir = '../../Nuget.builds/'
  var msBuild = 'C:/Windows/Microsoft.NET/Framework/v4.0.30319/MSBuild.exe ';
  var msBuildOptions = ' /p:Configuration=Release /verbosity:minimal ';
 
  var versionNum = getBreezeVersion();

  grunt.file.write(tempDir + 'version.txt', 'Version: ' + versionNum);
  grunt.log.writeln('localAppData: ' + process.env.LOCALAPPDATA);
  
  var nugetPackageNames = [
 	   'Breeze.Client',
     'Breeze.WebApi', 
     'Breeze.WebApi2.EF6',
     'Breeze.WebApi2.NH',
	   'Breeze.Server.WebApi2',
     'Breeze.Server.ContextProvider.EF6',
     'Breeze.Server.ContextProvider.NH',
     'Breeze.Server.ContextProvider'
	];
  
  var breezeDlls = [
    'Breeze.WebApi', 
    'Breeze.WebApi.EF', 
    'Breeze.WebApi.NH',
    'Breeze.ContextProvider', 
    'Breeze.ContextProvider.EF6',
    'Breeze.ContextProvider.NH',
    'Breeze.WebApi2'
  ];
  
  var tempPaths = [
     'bin','obj', 'packages','*_Resharper*','*.suo'
  ];
	 
  // Project configuration.
  grunt.initConfig({
    pkg: grunt.file.readJSON('package.json'),
    
    // run the breeze.js grunt scripts - args here are dummies.
    buildBreezeJs: { foo: "bar" },
    
	  msBuild: {
      // build the Breeze.Build sln ( which includes all of the other projects) 
      source: {
        // 'src' here just for 'newer' functionality
        src: '../../Breeze.*/**',
        msBuildOptions: msBuildOptions,
        solutionFileNames: ['../../Breeze.Build.sln']
      },
    },
    
    clean: {
      options: {
        // uncomment to test
        // 'no-write': true,
        force: true,
      },
      // remove all previously build .nupkgs
      nupkgs: [ nugetDir + '**/*.nupkg']
    },
    
    copy: {
      // copy all nuget packages into the local nuget test dir
      testNupkg: {
        files: [ { 
          expand: true, 
          cwd: nugetDir, 
          src: ['**/*.nupkg' ], 
          flatten: true,
          dest: process.env.LOCALAPPDATA + '/Nuget/Cache' 
        }]
      }, 
    },

    updateFiles: {
      // copy breeze.*.js files to each of the nuget sources
      nugetScripts: { 
        src: [ jsBuildDir + 'breeze.*.js'] ,
        destFolders: [ nugetDir ]
      },
      // copy breeze dll files to each of the nuget sources
      nugetLibs: {
        src: breezeDlls.map(function(x) {
          return '../../' + x + '/*.dll';
        }),
        destFolders: [ nugetDir]
      }
    },
    
    // build all nuget packages 
    buildNupkg: {
      build: { src: [ nugetDir + '**/Default.nuspec' ] }
    },
    
    // deploy all nuget packages
    deployNupkg: {
      base: { src: [ nugetDir + '**/*.nupkg'] }
    },
    
    // debugging tool
    listFiles: {
      samples: {
        src: [ nugetDir + '**/Default.nuspec']
      }
    },
   
  });

  grunt.loadNpmTasks('grunt-exec');
  grunt.loadNpmTasks('grunt-contrib-copy');
  grunt.loadNpmTasks('grunt-contrib-clean');
  grunt.loadNpmTasks('grunt-contrib-compress');
  grunt.loadNpmTasks('grunt-newer');

  
  grunt.registerMultiTask('buildBreezeJs', 'build breeze.xxx.js files', function() {
    runExec('buildJsFiles', {
      cwd:  jsBuildDir + "/grunt/",
      cmd: 'grunt'
    });   
  });
   
  grunt.registerMultiTask('msBuild', 'Execute MsBuild', function( ) {
    // dynamically build the exec tasks
    grunt.log.writeln('msBuildOptions: ' + this.data.msBuildOptions);
    var that = this;
    
    this.data.solutionFileNames.forEach(function(solutionFileName) {
      execMsBuild(solutionFileName, that.data);
    });
  });  
  
  grunt.registerMultiTask('updateFiles', 'update files to latest version', function() {
    var that = this;
    this.files.forEach(function(fileGroup) {
      fileGroup.src.forEach(function(srcFileName) {
        grunt.log.writeln('Updating from: ' + srcFileName);
        var baseName = path.basename(srcFileName);
        that.data.destFolders.forEach(function(df) {
          var destPattern = df + '/**/' + baseName;
          var destFiles = grunt.file.expand(destPattern);
          destFiles.forEach(function(destFileName) {
            grunt.log.writeln('           to: ' + destFileName);
            grunt.file.copy(srcFileName, destFileName);
          });
        });
      });
    });
  });
  
   grunt.registerMultiTask('buildNupkg', 'package nuget files', function() {   
    this.files.forEach(function(fileGroup) {
      fileGroup.src.forEach(function(fileName) {
        packNuget(fileName);
      });
    });
  });
  
  grunt.registerMultiTask('deployNupkg', 'deploy nuget package', function() {   
    this.files.forEach(function(fileGroup) {
      fileGroup.src.forEach(function(fileName) {
        grunt.log.writeln('Deploy: ' + fileName);
        var folderName = path.dirname(fileName);
        runExec('deployNupkg', {
          cmd: 'nuget push ' + fileName 
        });
      });
    });
  });
  
  // for debugging file patterns
  grunt.registerMultiTask('listFiles', 'List files', function() {
    grunt.log.writeln('target: ' + this.target);
    
    this.files.forEach(function(fileGroup) {
      fileGroup.src.forEach(function(fileName) {
        grunt.log.writeln('file: ' + fileName);
      });
    });
  });

  grunt.registerTask('buildRelease', 
   ['newer:msBuild:source' ]);
  grunt.registerTask('packageNuget',   
   [ 'buildBreezeJs', 'clean:nupkgs', 'newer:updateFiles', 'buildNupkg', 'copy:testNupkg']);
  
  grunt.registerTask('default', ['buildRelease', 'packageNuget']);
  
  // ------------------- local functions ----------------------
    
  function getBreezeVersion() {
     var versionFile = grunt.file.read( jsSrcDir + '_head.jsfrag');    
     var regex = /\s+version:\s*"(\d.\d\d*.?\d*)"/
     var matches = regex.exec(versionFile);
     
     if (matches == null) {
        throw new Error('Version number not found');
     }
     // matches[0] is entire version string - [1] is just the capturing group.
     var versionNum = matches[1];
     grunt.log.writeln('version: ' + versionNum);
     return versionNum;
  }
  
  function join(a1, a2) {
    var result = [];
    a1.forEach(function(a1Item) {
      a2.forEach(function(a2Item) {
        result.push(a1Item + '**/' + a2Item);
      });
    });
    return result;
  }
  
  function packNuget(nuspecFileName) {
    var folderName = path.dirname(nuspecFileName);
    grunt.log.writeln('Nuspec folder: ' + folderName);
    
    var text = grunt.file.read(nuspecFileName);
    var folders = folderName.split('/');
    var folderId = folders[folders.length-1];
    
    text = text.replace(/{{version}}/g, versionNum);
    text = text.replace(/{{id}}/g, folderId);
    var destFileName = folderName + '/' + folderId + '.nuspec';
    grunt.log.writeln('nuspec file: ' + destFileName);
    grunt.file.write(destFileName, text);
    // 'nuget pack $folderName.nuspec'
    runExec('nugetpack', {
      cwd: folderName,
      cmd: 'nuget pack ' + folderId + '.nuspec'
    });   

  }

  function execMsBuild(solutionFileName, config ) {
    grunt.log.writeln('Executing solution build for: ' + solutionFileName);
    
    var cwd = path.dirname(solutionFileName);
    var baseName = path.basename(solutionFileName);
    var rootCmd = msBuild + '"' + baseName +'"' + config.msBuildOptions + ' /t:' 
    
    runExec('msBuildClean', {
      cwd: cwd,
      cmd: rootCmd + 'Clean'
    });
    runExec('msBuildRebuild', {
      cwd: cwd,
      cmd: rootCmd + 'Rebuild'
    });

  }
  
  var index = 0;
  
  function runExec(name, config) {
    var name = name+'-'+index++;
    grunt.config('exec.' + name, config);
    grunt.task.run('exec:' + name);
  }
  
  function log(err, stdout, stderr, cb) {
    if (err) {
      grunt.log.write(err);
      grunt.log.write(stderr);
      throw new Error('Failed');
    }

    grunt.log.write(stdout);

    cb();
  }


};