using MongoDB.Entities;

namespace Geex.Extensions.Authentication
{
    public interface IAuthUser : IEntityBase
    {
        string? PhoneNumber { get; set; }
        string Username { get; set; }
        string? Nickname { get; set; }
        string? Email { get; set; }
        LoginProviderEnum LoginProvider { get; set; }
        string? OpenId { get; set; }
        public bool IsEnable { get; set; }
        void ChangePassword(string originPassword, string newPassword);
        IAuthUser SetPassword(string? password);
        bool CheckPassword(string password);
    }
}
