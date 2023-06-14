using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Entities.InnerQuery
{
    /// <summary>
    /// Document used internally to handle projections that don't map to a document.
    /// For example, ".Select(c => c.Age)" returns a collection of integers with no
    /// matching name fields.  In this case, we'd use a document of the format
    /// {_result_:25} internally within the pipeline to represent an integer(s)
    /// </summary>
    [BsonIgnoreExtraElements]
    internal class PipelineDocument<T>
    {
        // ReSharper disable once UnassignedField.Compiler
        // The following propety name must match the value of PipelineDocumentResultName.
        // I could do this via reflection but I'm a lazy.
        public T _result_ { get; set; }
    }
}
