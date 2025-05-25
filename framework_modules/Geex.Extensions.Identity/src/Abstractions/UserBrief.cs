using Geex.Abstractions;

namespace Geex.Extensions.Identity
{
    public record UserBrief
    {
        public UserBrief(string email, string id, string openId, LoginProviderEnum loginProvider, string phoneNumber, string username, string nickname)
        {
            Email = email;
            Id = id;
            OpenId = openId;
            LoginProvider = loginProvider;
            PhoneNumber = phoneNumber;
            Username = username;
            Nickname = nickname;
        }

        public string? Id { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Username { get; init; }
        public string? Nickname { get; init; }
        public string? Email { get; init; }
        public LoginProviderEnum? LoginProvider { get; init; }
        public string? OpenId { get; init; }
    }
}
