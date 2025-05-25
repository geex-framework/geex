using MongoDB.Entities;

namespace Geex.Abstractions.Migrations
{
    /// <summary>
    /// Represents a migration history item in the database
    /// </summary>
    [Name("_migration_history_")]
    public class Migration : EntityBase<Migration>
    {
        public long Number { get; set; }
        public string Name { get; set; }
        public double TimeTakenSeconds { get; set; }
    }
}
