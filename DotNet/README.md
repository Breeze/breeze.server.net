# Readme for DotNet 5,6,7...

This folder contain code and setup for building and deploying of Breeze for DotNet 5, 6, 7 and hopefully beyond.

The idea is to have a single C# codebase that can be used for multiple build targets.  However, nothing fancy is done to support
multi-targetted Nugets.  Instead, we search-and-replace values in the .csproj values to configure the projects for the desired target.

## How

Each project subfolder contains a `{name}.csproj.xml` file which has tokens where version-specific information belongs.

The `tools/setup.js` script reads each *.csproj.xml file, replaces the tokens, and writes out a .csproj file.

## So...

Before you open the solution file (.sln), first run

    node tools/setup.js {n}

Where {n} is the dotnet version that you want to use.  The {n} argument corresponds to a set of replacement values, 
defined in setup.js, that are used when creating the .csproj files