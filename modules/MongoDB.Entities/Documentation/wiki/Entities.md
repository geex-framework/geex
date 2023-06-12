# Define entities

add the import statement shown below and create your entities by inheriting the `Entity` base class.

```csharp
using MongoDB.Entities;

public class Book : Entity
{
    public string Title { get; set; }
}
```

# Ignore properties

if there are some properties on entities you don't want persisted to mongodb, simply use the `IgnoreAttribute` 
```csharp
public class Book : Entity
{
    [Ignore]
    public string SomeProperty { get; set; }
}
```

# Customize collection names
by default, mongodb collections will use the names of the entity classes. you can customize the collection names by decorating your entities with the `NameAttribute` as follows:
```csharp
[Name("Writer")]
public class Author : Entity
{
    ...
}
```

# Optional auto-managed properties
there are 2 optional interfaces `ICreatedOn` & `IModifiedOn` that you can add to entity class definitions like so:
```csharp
public class Book : Entity, ICreatedOn, IModifiedOn
{
    public string Title { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}
```
if your entity classes implements these interfaces, the library will automatically set the appropriate values so you can use them for sorting operations and other queries.

# The IEntity interface

if for whatever reason, you're unable to inherit the `Entity` base class, you can simply implement the `IEntity` interface to make your classes compatible with the library like so:
```csharp
public class Book : IEntity
{
    [BsonId, ObjectId]
    public string ID { get; set; }
    
    public string GenerateNewID() 
        => ObjectId.GenerateNewId().ToString();
}
```

# Customizing the ID format
the default format of the IDs automatically generated for new entities is `ObjectId`. if you'd like to change the format of the ID, simply override the `GenerateNewID` method of the `Entity` class or implement the `IEntity` interface and place the logic for generating new IDs inside the `GenerateNewID` method. 

make sure to only return truly unique strings in order to avoid mongodb server from complaining as there's a unique index on the ID field. 

if implementing `IEntity`, don't forget to decorate the ID property with the `[BsonId]` attribute.
```csharp
public class Book : IEntity
{
    [BsonId]
    public string ID { get; set; }

    public string GenerateNewID()
        => $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}";
}
```

> [!note]
> the type of the ID property cannot be changed to something other than `string`. PRs are welcome for removing this limitation.