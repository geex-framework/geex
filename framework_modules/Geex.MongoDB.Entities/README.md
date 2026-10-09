[![license](https://img.shields.io/github/license/dj-nitehawk/MongoDB.Entities?color=blue&label=license&logo=Github&style=flat-square)](https://github.com/dj-nitehawk/MongoDB.Entities/blob/master/README.md) [![nuget](https://img.shields.io/nuget/v/MongoDB.Entities?label=version&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![nuget](https://img.shields.io/nuget/dt/MongoDB.Entities?color=blue&label=downloads&logo=NuGet&style=flat-square)](https://www.nuget.org/packages/MongoDB.Entities) [![tests](https://img.shields.io/azure-devops/tests/RyanGunner/MongoDB%20Entities/4?color=blue&label=tests&logo=Azure%20DevOps&style=flat-square)](https://dev.azure.com/RyanGunner/MongoDB%20Entities/_build/latest?definitionId=4) [![discord](https://img.shields.io/discord/768493765995921449?color=blue&label=discord&logo=discord&logoColor=white&style=flat-square)](https://discord.com/invite/CM5mw2G)


# MongoDB.Entities
A light-weight .net standard library which simplifies access to mongodb by abstracting away the official .net mongodb driver and providing some additional features on top of it. The API is clean and intuitive resulting in less lines of code that is more human friendly than driver code.

## Geex query expressions

`ExpressionSimplifier` translates array-backed `MemoryExtensions.Contains` calls to `Enumerable.Contains`. The three-argument overload is translated when the comparer is explicitly `null`, preserving default equality for .NET 10 enumeration queries. Calls with an explicit comparer retain their original expression.

## Lazy navigation and BatchLoad

Configure navigation properties with `ConfigLazyQuery` in the entity's parameterless constructor. Public, protected and private constructors are supported. The framework initializes missing navigation metadata when a manual `BatchLoad` or GraphQL automatic batch load first plans a query, including mapped interfaces and inherited navigation properties. Entity static constructors and startup warm-up instances are unnecessary.

Cold initialization constructs one detached instance per concrete type and publishes its metadata only after construction succeeds. Keep the parameterless constructor limited to instance initialization and navigation configuration; it must not query a database or require a `DbContext`. Data sources are not evaluated, and lazy query objects and expressions remain owned by each real entity. Successful initialization and initialization failures are cached. Already registered navigation metadata does not require another instance. Entities without a parameterless constructor must register their navigation metadata before query planning and still configure lazy queries on their real instances.

GraphQL automatic batch loading uses `GeexCoreModuleOptions.AutoBatchLoad` when the field has no setting. An explicit `UseAutoBatchLoad(true)` or `UseAutoBatchLoad(false)` overrides that default.

Automatic navigation plans use the compiled GraphQL selections and each object type's CLR `RuntimeType`. Interface implementations, renamed GraphQL types, fragments, aliases and directives retain their selection scope. The BSON storage root does not need a GraphQL object type. Manual `BatchLoad`/`ThenBatchLoad` paths merge with automatic paths, including nested paths omitted from the GraphQL selection.

Execution groups compatible navigation queries by the actual entity type and `DbContext`, preserving the navigation's parent and related generic types. Framework default sources with equivalent batch rules can share a batch. Custom sources and rules that capture instance state are evaluated separately to preserve each instance's data source and filtering semantics. Scalar `Count`/`LongCount` execution skips batch preloading while retaining the context's local entity view. Offset pagination retains Hot Chocolate's standard extra-row lookahead.

Regression tests are in `test/TestLazyQueryMetadata.cs`, `test/TestBatchLoadColdStart.cs` and `test/TestBatchLoadPolymorphism.cs`, alongside the existing BatchLoad tests and `Geex.Tests/FeatureTests/CoreBatchLoad.Api.Tests.cs`. From the repository root, run the PowerShell 7 runner below. It snapshots the source, builds both test projects and their dependencies, and runs the selected tests inside Linux .NET 10 SDK containers with dedicated MongoDB and Redis instances. No host ports are published. The databases and profiling state are disposable and isolated. Each run saves its scope, source hash, discovered case IDs, build logs and TRX results under `.test-evidence/<runId>/`, and checks that the planned and executed case sets match.

```powershell
./scripts/testing/test-batchload.ps1
```

The runner reads the local NuGet cache and restores missing packages from NuGet. An independent reviewer uses `-Stage INDEPENDENT_ACCEPTANCE` for a separate run of this scope. These regressions cover framework behavior and the GraphQL HTTP API; they do not replace deployed application or final project acceptance.

## More Info:
please visit the official website for detailed documentation:
## [https://mongodb-entities.com](https://mongodb-entities.com)
