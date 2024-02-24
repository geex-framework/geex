namespace MongoDB.Entities.Tests.Models
{
    public interface IInheritanceEntity : IEntityBase
    {
        public string Name { get; set; }
    }
    public class InheritanceEntity : EntityBase<InheritanceEntity>, IInheritanceEntity
    {
        public string Name { get; set; }
        public virtual string DisplayName => Name;
    }

    public class InheritanceEntityChild : InheritanceEntity
    {
        /// <inheritdoc />
        public override string DisplayName => "1" + Name;
    }
}
