using System.Linq;
using System.Threading.Tasks;

using Geex.Gql.Attributes;
using Geex.Storage;

using HotChocolate.Types;

using MongoDB.Entities;

namespace Geex.Tests.SchemaTests.TestEntities
{
    public interface IBatchLoadTestEntity : IEntityBase
    {
        string ThisId { get; }
        string ParentId { get; }
        IQueryable<IBatchLoadTestEntity> Children { get; }
    }

    public class BatchLoadTestEntity : Entity<BatchLoadTestEntity>, IBatchLoadTestEntity
    {
        public BatchLoadTestEntity()
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

        public BatchLoadTestEntity(string thisId) : this()
        {
            ThisId = thisId;
        }

        public BatchLoadTestEntity(string thisId, string parentId) : this()
        {
            ThisId = thisId;
            ParentId = parentId;
        }

        IQueryable<IBatchLoadTestEntity> IBatchLoadTestEntity.Children => Children;

        public IQueryable<BatchLoadTestEntity> Children => LazyQuery(() => Children);
        public Lazy<BatchLoadTestEntity> FirstChild => LazyQuery(() => FirstChild);

        [AutoBatchLoadDependsOn(nameof(Children))]
        public int ChildCount => Children.Count();

        public string ThisId { get; set; } = default!;
        public string ParentId { get; set; } = default!;

        public class BatchLoadTestEntityGqlConfig : GqlConfig.Object<BatchLoadTestEntity>
        {
            protected override void Configure(IObjectTypeDescriptor<BatchLoadTestEntity> descriptor)
            {
                descriptor.Implements<InterfaceType<IBatchLoadTestEntity>>();
                descriptor.Field(x => x.Children).Name("childNodes");
            }
        }
    }
}
