namespace Geex.Abstractions.Authorization
{
    public class UserSessionCache : IHasId
    {
        public string userId { get; set; }
        //public Dictionary<string, string> claims { get; set; }
        public string token { get; set; }

        /// <inheritdoc />
        string IHasId.Id => userId;
    }
}
