using System;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Driver;

namespace MongoDB.Entities.Tests.Models;

public class BatchLoadEntity : EntityBase<BatchLoadEntity>
{
    public BatchLoadEntity()
    {
        this.ConfigLazyQuery(x => x.Children, x => x.ParentId == ThisId, entities => relatedEntity => entities.Select(y => y.ThisId).ToList().Contains(relatedEntity.ParentId));
        this.ConfigLazyQuery(x => x.FirstChild, x => x.ParentId == ThisId, entities => relatedEntity => entities.Select(y => y.ThisId).ToList().Contains(relatedEntity.ParentId));
    }

    public BatchLoadEntity(string thisId) : this()
    {
        ThisId = thisId;
    }

    public BatchLoadEntity(string thisId, string parentId) : this()
    {
        ThisId = thisId;
        ParentId = parentId;
    }

    public IQueryable<BatchLoadEntity> Children => LazyQuery(() => Children);
    public Lazy<BatchLoadEntity> FirstChild => LazyQuery(() => FirstChild);

    public string ThisId { get; set; }
    public string ParentId { get; set; }

    /// <inheritdoc />
    public override async Task<long> DeleteAsync()
    {
        return await this.Children.DeleteAsync() + await base.DeleteAsync();
    }
}

public class RootEntity : EntityBase<RootEntity>
{
    public RootEntity()
    {
        this.ConfigLazyQuery(x => x.C1, x => x.ParentId == ThisId, entities => relatedEntity => entities.Select(y => y.ThisId).ToList().Contains(relatedEntity.ParentId));
        this.ConfigLazyQuery(x => x.FirstChild, x => x.ParentId == ThisId, entities => relatedEntity => entities.Select(y => y.ThisId).ToList().Contains(relatedEntity.ParentId));
    }

    public RootEntity(string thisId) : this()
    {
        ThisId = thisId;
    }

    public RootEntity(string thisId, string parentId) : this()
    {
        ThisId = thisId;
        ParentId = parentId;
    }

    public IQueryable<IC1> C1 => LazyQuery(() => C1);
    public Lazy<C1Entity> FirstChild => LazyQuery(() => FirstChild);

    public string ThisId { get; set; }
    public string ParentId { get; set; }
    public interface IC1 : IEntityBase
    {
        public string ThisId { get; set; }
        public string ParentId { get; set; }
        public IQueryable<C2Entity> C2 { get; }

    }
    public class C1Entity : EntityBase<C1Entity>, IC1
    {
        public C1Entity()
        {
            this.ConfigLazyQuery<C2Entity>(x => x.C2, x => x.ParentId == ThisId, entities => relatedEntity => entities.Select(y => y.ThisId).ToList().Contains(relatedEntity.ParentId));
        }
        public C1Entity(string thisId) : this()
        {
            ThisId = thisId;
        }

        public C1Entity(string thisId, string parentId) : this()
        {
            ThisId = thisId;
            ParentId = parentId;
        }
        public string ThisId { get; set; }
        public string ParentId { get; set; }
        public IQueryable<C2Entity> C2 => LazyQuery(() => C2);

    }

    public class C2Entity : EntityBase<C2Entity>
    {
        public C2Entity()
        {
            this.ConfigLazyQuery(x => x.C3, x => x.ParentId == ThisId, entities => relatedEntity => entities.Select(y => y.ThisId).ToList().Contains(relatedEntity.ParentId));
        }
        public C2Entity(string thisId) : this()
        {
            ThisId = thisId;
        }

        public C2Entity(string thisId, string parentId) : this()
        {
            ThisId = thisId;
            ParentId = parentId;
        }
        public string ThisId { get; set; }
        public string ParentId { get; set; }
        public IQueryable<C3Entity> C3 => LazyQuery(() => C3);
    }

    public class C3Entity : EntityBase<C3Entity>
    {
        public C3Entity()
        {
        }
        public C3Entity(string thisId) : this()
        {
            ThisId = thisId;
        }

        public C3Entity(string thisId, string parentId) : this()
        {
            ThisId = thisId;
            ParentId = parentId;
        }
        public string ThisId { get; set; }
        public string ParentId { get; set; }
    }

}