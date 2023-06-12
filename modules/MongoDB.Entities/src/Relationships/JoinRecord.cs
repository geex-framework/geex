using MongoDB.Bson;

namespace MongoDB.Entities
{
    /// <summary>
    /// Represents a parent-child relationship between two entities.
    /// <para>TIP: The ParentId and ChildId switches around for many-to-many relationships depending on the side of the relationship you're accessing.</para>
    /// </summary>
    public class JoinRecord : EntityBase<JoinRecord>
    {
        /// <summary>
        /// The Id of the parent IEntity for both one-to-many and the owner side of many-to-many relationships.
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// The Id of the child IEntity in one-to-many relationships and the Id of the inverse side IEntity in many-to-many relationships.
        /// </summary>
        public string ChildId { get; set; }
    }
}
