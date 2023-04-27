# [Breeze](http://breeze.github.io/doc-main/) Data Management for [.NET Servers](http://breeze.github.io/doc-net/)

**Breeze** is a library that helps you manage data in rich client applications. If you store data in a database, query and save those data as complex object graphs, and share these graphs across multiple screens of your JavaScript or C# client, Breeze is for you.

Client-side querying, caching, dynamic object graphs, change tracking and notification, model validation, batch save, offline … all part of rich data management with Breeze.  Breeze clients communicate with any remote service that speaks HTTP and JSON.

**Breeze** lets you develop applications using the same powerful idioms on the client and server. You can

- query with a rich query syntax
- navigate the graph of related entities
- track changes as you add/change/delete entities
- perform client-side validation
- save all changes in a single transaction
- use the same entity model on the server and client

## Install from NuGet

### .NET Core

For .NET Core (2 through 7) and Entity Framework Core (2 through 7), find the following packages in NuGet.

> Note: Version 7.1 or later of each package is for .NET 5, 6, and 7, whereas Version 3.x is for .NET Core 3 and Version 1.x is for .NET Core 2.

- [Breeze.AspNetCore.NetCore](https://www.nuget.org/packages/Breeze.AspNetCore.NetCore/)
- [Breeze.Persistence.EFCore](https://www.nuget.org/packages/Breeze.Persistence.EFCore/) (support for EF Core)
- [Breeze.Persistence.NH](https://www.nuget.org/packages/Breeze.Persistence.NH/) (support for NHibernate)
- [Breeze.Core](https://www.nuget.org/packages/Breeze.Core/)
- [Breeze.Persistence](https://www.nuget.org/packages/Breeze.Persistence/)

For a typical EFCore application, you would install the first two packages.  For an NHibernate application, install the first package and Breeze.Persistence.NH.  The last two packages are dependencies that are automatically installed by the other packages.

### .NET Framework

See the [docs](http://breeze.github.io/doc-net/nuget-packages.html) for .NET 4.x NuGet packages

## Documentation 

See the [docs](http://breeze.github.io/doc-net/breeze-server-core) for more info about what Breeze does and how to use it.

Set the [release notes](http://breeze.github.io/doc-net/release-notes.html) for changes in the latest version.

## Examples

See some [examples](https://github.com/Breeze/northwind-demo) of how to use Breeze .NET server with clients written in Angular, Aurelia, React, and Vue in the [Northwind-Demo](https://github.com/Breeze/northwind-demo).

See the [TempHire](https://github.com/Breeze/temphire.angular) application for a richer example showing proper architectural patterns.

## Sources

The sources for this package are in the [breeze.server.net](https://github.com/Breeze/breeze.server.net) repo.  Please file issues and pull requests against that repo.

## Upgrading from .NET Framework to Core

The underlying concepts are the same, but there are a few major changes

 - The `ContextProvider` class is now `PersistenceManager`.
 - The `[BreezeController]` and `[EnableBreezeQuery]` attributes have been replaced by `[BreezeQueryFilter]` attribute.
 - Breeze JSON query format is preferred over OData.  You will need to use Breeze 2.x on the client, and use the UriBuilderJsonAdapter (not the UriBuilderOdataAdapter).

See the [Northwind-Demo](https://github.com/Breeze/northwind-demo) for steps to set up a new .NET Core server.

See the [UPGRADE](https://github.com/Breeze/breeze-client/blob/master/UPGRADE.md) document for information on upgrading Breeze Client from 1.x to 2.x.

## Building Breeze

The recent sources and solutions are in the [breeze.server.net](https://github.com/Breeze/breeze.server.net) repo under the  **DotNet** folder.  Building is just a matter of:

1. Installing the required version of .NET SDK
2. Opening the appropriate solution in Visual Studio
3. Restoring NuGet packages
4. Rebuilding the solution

Test solutions are in the **Tests/Test.AspNetCore.EFCore** folder.

---

If you have discovered a bug or missing feature, please create an issue in the [breeze.server.net github repo](https://github.com/Breeze/breeze.server.net).

If you have questions about using Breeze, please ask on [Stack Overflow](https://stackoverflow.com/questions/tagged/breeze).

If you need help developing your application, please contact us at [IdeaBlade](mailto:info@ideablade.com).
