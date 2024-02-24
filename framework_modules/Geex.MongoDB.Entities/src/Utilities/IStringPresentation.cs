namespace MongoDB.Entities.Utilities
{
    public interface IStringPresentation
    {
        public string ToString()
        {
            return ((object)this).ToString();
        }
    }
}
