[![license](https://img.shields.io/github/license/dj-nitehawk/MongoDB.Entities?color=blue&label=license&logo=Github&style=flat-square)](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/README.md) [![nuget](https://img.shields.io/nuget/v/MongoDB.Entities?label=version&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![nuget](https://img.shields.io/nuget/dt/MongoDB.Entities?color=blue&label=downloads&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![tests](https://img.shields.io/azure-devops/tests/RyanGunner/MongoDB%20Entities/4?color=blue&label=tests&logo=Azure%20DevOps&style=flat-square)](https://dev.azure.com/RyanGunner/MongoDB%20Entities/_build/latest?definitionId=4) [![discord](https://img.shields.io/discord/768493765995921449?color=blue&label=discord&logo=discord&logoColor=white&style=flat-square)](https://discord.com/invite/CM5mw2G)


# MongoDB.Entities
A light-weight .net standard library which simplifies access to mongodb by abstracting away the official .net mongodb driver and providing some additional features on top of it. The API is clean and intuitive resulting in less lines of code that is more human friendly than driver code.

## Geex query expressions

`ExpressionSimplifier` translates array-backed `MemoryExtensions.Contains` calls to `Enumerable.Contains`. The three-argument overload is translated when the comparer is explicitly `null`, preserving default equality for .NET 10 enumeration queries. Calls with an explicit comparer retain their original expression.

## Lazy navigation and BatchLoad

Configure navigation properties with `ConfigLazyQuery` in the entity's parameterless constructor. Public, protected and private constructors are supported. The framework initializes missing navigation metadata when a manual `BatchLoad` or GraphQL automatic batch load first plans a query, including mapped interfaces and inherited navigation properties. Entity static constructors and startup warm-up instances are unnecessary.

Cold initialization constructs one detached instance per concrete type and publishes its metadata only after construction succeeds. Keep the parameterless constructor limited to instance initialization and navigation configuration; it must not query a database or require a `DbContext`. Data sources are not evaluated, and lazy query objects and expressions remain owned by each real entity. Successful initialization and initialization failures are cached. Already registered navigation metadata does not require another instance. Entities without a parameterless constructor must register their navigation metadata before query planning and still configure lazy queries on their real instances.

GraphQL automatic batch loading uses `GeexCoreModuleOptions.AutoBatchLoad` when the field has no setting. An explicit `UseAutoBatchLoad(true)` or `UseAutoBatchLoad(false)` overrides that default.

Regression tests are in `test/TestLazyQueryMetadata.cs` and `test/TestBatchLoadColdStart.cs`, alongside the existing BatchLoad tests. Run the following inside a Linux .NET 10 SDK container connected to a dedicated MongoDB instance at `localhost:27017`. The test command builds the project and its dependencies before running. The integration tests use `mongodb-entities-test` and MongoDB profiling, so the database must be disposable and isolated.

```powershell
dotnet test framework_modules/Geex.MongoDB.Entities/test/Tests.csproj --configuration Release --filter 'FullyQualifiedName~MongoDB.Entities.Tests.TestBatchLoad|FullyQualifiedName~MongoDB.Entities.Tests.TestLazyQueryMetadata'
```

## More Info:
please visit the official website for detailed documentation:
## [https://mongodb-entities.com](https://mongodb-entities.com)
