using System.Linq;

using Geex.Storage;

using MongoDB.Entities;

namespace Geex.Tests.SchemaTests.TestEntities
{
    public class AutoBatchLoadTestEntity : Entity<AutoBatchLoadTestEntity>
    {
        protected AutoBatchLoadTestEntity()
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

        public AutoBatchLoadTestEntity(string thisId) : this()
        {
            ThisId = thisId;
        }

        public string ThisId { get; set; } = default!;
        public IQueryable<AutoBatchLoadChildEntity> Children => LazyQuery(() => Children);
        public Lazy<AutoBatchLoadChildEntity> FirstChild => LazyQuery(() => FirstChild);
    }

    public class AutoBatchLoadChildEntity : Entity<AutoBatchLoadChildEntity>
    {
        protected AutoBatchLoadChildEntity()
        {
            ConfigLazyQuery(
                x => x.FirstChild,
                child => child.ParentId == ThisId && child.ThisId == ThisId + ".1",
                parents => child => parents.SelectList(x => x.ThisId + ".1").Contains(child.ThisId));
            ConfigLazyQuery(
                x => x.ResettableGrandChild,
                child => child.ParentId == ThisId && child.ThisId == ThisId + ".1",
                parents => child => parents.SelectList(x => x.ThisId + ".1").Contains(child.ThisId));
        }

        public AutoBatchLoadChildEntity(string thisId, string parentId) : this()
        {
            ThisId = thisId;
            ParentId = parentId;
        }

        public string ThisId { get; set; } = default!;
        public string ParentId { get; set; } = default!;
        public Lazy<AutoBatchLoadGrandChildEntity> FirstChild => LazyQuery(() => FirstChild);
        public ResettableLazy<AutoBatchLoadGrandChildEntity> ResettableGrandChild => LazyQuery(() => ResettableGrandChild);
    }

    public class AutoBatchLoadGrandChildEntity : Entity<AutoBatchLoadGrandChildEntity>
    {
        public AutoBatchLoadGrandChildEntity(string thisId, string parentId)
        {
            ThisId = thisId;
            ParentId = parentId;
        }

        public string ThisId { get; set; } = default!;
        public string ParentId { get; set; } = default!;
    }
}
