namespace _org_._proj_._mod_.Api.Aggregates._aggregate_s
{
    /// <summary>
    /// this is a aggregate root of this module, we name it the same as the module feel free to change it to its real name
    /// </summary>
    public interface I_aggregate_
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
