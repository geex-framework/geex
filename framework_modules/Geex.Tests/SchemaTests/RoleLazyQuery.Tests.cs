using Geex.Extensions.Identity.Core.Entities;
using MongoDB.Entities.Utilities;
using Shouldly;

namespace Geex.Tests.SchemaTests;

public class RoleLazyQueryTests
{
    [Fact]
    public void UsersCanBePlannedBeforeCreatingOrAttachingARole()
    {
        var property = typeof(Role).GetProperty(nameof(Role.Users))!;
        var config = new BatchLoadConfig();

        var usersConfig = config.RegisterBatchLoad(property, typeof(Role));

        config.GetSubConfig(property, typeof(Role)).ShouldBeSameAs(usersConfig);
        LazyQueryMetadataRegistry.IsRegistered(typeof(Role), nameof(Role.Users)).ShouldBeTrue();
    }
}
