namespace Geex.Extensions.Authentication
{
    public class UserSessionVersion : IHasId
    {
        public string UserId { get; set; } = string.Empty;
        public long Version { get; set; }

        string IHasId.Id => UserId;
    }
}
