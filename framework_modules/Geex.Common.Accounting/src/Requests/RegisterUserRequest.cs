using MediatR;

// ReSharper disable once CheckNamespace
namespace Geex.Common.Requests.Accounting
{
    public record RegisterUserRequest : IRequest
    {
        public string Password { get; set; }
        public string Username { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}