/** Set up the projects for a given .NET version */
const bu = require('./build-utils');

const versionblurb = 'Note: Version 7.x of this package is for .NET 7,  whereas Version 6.x is for .NET Core 6, Version 5.x is for .NET Core 5, Version 3.x is for .NET Core 3 and Version 1.x is for .NET Core 2.';
// const reporoot = 'C:\\GitHub\\breeze.server.net'; // Jay
const reporoot = 'C:\\git\\Breeze\\breeze.server.net'; // Steve

/** Files containing search tokens that need replacing.  Filenames will have '.xml' removed. */
const files = [
  'Breeze.AspNetCore.NetCore/Breeze.AspNetCore.NetCore.csproj.xml',
  'Breeze.Core/Breeze.Core.csproj.xml',
  'Breeze.Persistence/Breeze.Persistence.csproj.xml',
  'Breeze.Persistence.EFCore/Breeze.Persistence.EFCore.csproj.xml',
  'Breeze.Persistence.NH/Breeze.Persistence.NH.csproj.xml',
];

/** Search tokens that need replacing */
const search = [ '${target}', '${version}', '${versionblurb}', '${tags}', '${efversion}', '${reporoot}' ];

/** Replacement tokens for a given version */
const replaceMap = {
  '5': [ 'net5.0', '5.0.6.0', versionblurb, 'Net5', '5.0.5', reporoot ],
  '6': [ 'net6.0', '6.0.2.0', versionblurb, 'Net6', '6.0.1', reporoot ],
  '7': [ 'net7.0', '7.0.1.0', versionblurb, 'Net7', '7.0.1', reporoot ],
};

const arg = bu.getArg();

if (!arg || !replaceMap[arg]) {
  const keys = Object.keys(replaceMap).join(", ");
  const msg = "First arg must be .NET version to target: " + keys;
  throw new Error(msg);
}

const replace = replaceMap[arg];
console.log("Using version " + arg, replace);

// create '*.{n}.csproj' from '*.csproj.xml' and replace tokens with values
for (var i=0; i<files.length; i++) {
  const infile = files[i];
  const outfile = infile.replace('.csproj.xml', '.' + arg + '.csproj');
  bu.replaceInFile(infile, search, replace, outfile);
}

