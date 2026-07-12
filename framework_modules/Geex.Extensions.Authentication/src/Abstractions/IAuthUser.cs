using MongoDB.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Geex.Extensions.Authentication
{
    public interface IAuthUser : IEntityBase
    {
        string? PhoneNumber { get; set; }
        string Username { get; set; }
        string? Nickname { get; set; }
        string? Email { get; set; }
        public bool IsEnable { get; set; }
        void ChangePassword(string originPassword, string newPassword);
        IAuthUser SetPassword(string? password);
        bool CheckPassword(string password);
        Task InvalidateSessionsCacheAsync(CancellationToken cancellationToken = default);
        Task RevokeSessionsAsync(CancellationToken cancellationToken = default);
    }
}
