using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Tests.FeatureTests.TestEntities
{
    public class BatchLoadTestEntity : Entity<BatchLoadTestEntity>
    {
        protected BatchLoadTestEntity()
        {
            ConfigLazyQuery(
                x => x.Children,
                child => child.ParentId == ThisId,
                parents => child => parents.SelectList(x => x.ThisId).Contains(child.ParentId));
            ConfigLazyQuery(
                x => x.FirstChild,
                child => child.ParentId == ThisId && child.ThisId == ThisId + ".1",
                parents => child => parents.SelectList(x => x.ThisId + ".1").Contains(child.ThisId));
        }

        public BatchLoadTestEntity(string thisId) : this()
        {
            ThisId = thisId;
        }

        public string ThisId { get; set; } = default!;
        public IQueryable<BatchLoadTestChildEntity> Children => LazyQuery(() => Children);
        public Lazy<BatchLoadTestChildEntity> FirstChild => LazyQuery(() => FirstChild);
    }

    public class BatchLoadTestChildEntity : Entity<BatchLoadTestChildEntity>
    {
        protected BatchLoadTestChildEntity()
        {
            ConfigLazyQuery(
                x => x.FirstChild,
                child => child.ParentId == ThisId && child.ThisId == ThisId + ".1",
                parents => child => parents.SelectList(x => x.ThisId + ".1").Contains(child.ThisId));
        }

        public BatchLoadTestChildEntity(string thisId, string parentId) : this()
        {
            ThisId = thisId;
            ParentId = parentId;
        }

        public string ThisId { get; set; } = default!;
        public string ParentId { get; set; } = default!;
        public Lazy<BatchLoadTestGrandChildEntity> FirstChild => LazyQuery(() => FirstChild);
    }

    public class BatchLoadTestGrandChildEntity : Entity<BatchLoadTestGrandChildEntity>
    {
        public BatchLoadTestGrandChildEntity(string thisId, string parentId)
        {
            ThisId = thisId;
            ParentId = parentId;
        }

        public string ThisId { get; set; } = default!;
        public string ParentId { get; set; } = default!;
    }
}
