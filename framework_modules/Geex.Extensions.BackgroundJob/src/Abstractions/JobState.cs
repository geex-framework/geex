using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Entities;

namespace Geex.Extensions.BackgroundJob;

[BsonDiscriminator(RootClass = true)]
public class JobState : EntityBase<JobState>
{
    public string JobName { get; set; }
}