using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Tests.SchemaTests.TestEntities
{
    public interface IBatchLoadGraphQLEntity : IEntityBase
    {
        string ThisId { get; }
        string ParentId { get; }
        IQueryable<IBatchLoadGraphQLEntity> Children { get; }
        Lazy<IBatchLoadGraphQLEntity> FirstChild { get; }
    }

    public class BatchLoadGraphQLEntity : Entity<BatchLoadGraphQLEntity>, IBatchLoadGraphQLEntity
    {
        public BatchLoadGraphQLEntity()
        {
            ConfigLazyQuery(
                x => x.Children,
                x => x.ParentId == ThisId,
                children => parent => children.Select(y => y.ThisId).ToList().Contains(parent.ParentId));
            ConfigLazyQuery(
                x => x.FirstChild,
                x => x.ParentId == ThisId,
                children => parent => children.Select(y => y.ThisId).ToList().Contains(parent.ParentId));
        }

        public BatchLoadGraphQLEntity(string thisId) : this()
        {
            ThisId = thisId;
        }

        public BatchLoadGraphQLEntity(string thisId, string parentId) : this()
        {
            ThisId = thisId;
            ParentId = parentId;
        }

        IQueryable<IBatchLoadGraphQLEntity> IBatchLoadGraphQLEntity.Children => Children;

        Lazy<IBatchLoadGraphQLEntity> IBatchLoadGraphQLEntity.FirstChild =>
            new(() => FirstChild.Value);

        public IQueryable<BatchLoadGraphQLEntity> Children => LazyQuery(() => Children);
        public Lazy<BatchLoadGraphQLEntity> FirstChild => LazyQuery(() => FirstChild);

        public string ThisId { get; set; } = default!;
        public string ParentId { get; set; } = default!;
    }
}
