using System;
using System.Collections.Generic;
using Geex.MultiTenant;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Geex.Extensions.Identity.Core.Entities;

public sealed class UserChangeValue
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public List<string> Before { get; set; } = new();
    public List<string> After { get; set; } = new();
}

public sealed class UserChangeRecord : Entity<UserChangeRecord>, ITenantFilteredEntity
{
    public string UserId { get; set; } = "";
    public string? TenantCode { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? ActorId { get; set; }
    public string ActorName { get; set; } = "";
    public string Source { get; set; } = "";
    public List<UserChangeValue> Changes { get; set; } = new();

    public sealed class Mapping : BsonConfig<UserChangeRecord>
    {
        protected override void Map(BsonClassMap<UserChangeRecord> map, BsonIndexConfig<UserChangeRecord> indexes)
        {
            map.AutoMap();
            indexes.MapEntityDefaultIndex();
            indexes.MapIndex(x => x.Ascending(y => y.TenantCode).Ascending(y => y.UserId).Descending(y => y.OccurredAt));
        }
    }

    public sealed class Graph : GqlConfig.Object<UserChangeRecord>
    {
        protected override void Configure(IObjectTypeDescriptor<UserChangeRecord> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.Id);
            descriptor.Field(x => x.CreatedOn);
            descriptor.Field(x => x.ModifiedOn);
            descriptor.Field(x => x.UserId);
            descriptor.Field(x => x.OccurredAt);
            descriptor.Field(x => x.ActorId);
            descriptor.Field(x => x.ActorName);
            descriptor.Field(x => x.Source);
            descriptor.Field(x => x.Changes);
        }
    }
}
