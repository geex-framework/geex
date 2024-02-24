using MediatR;

namespace Geex.Common.Accounting.Requests
{
    public record RegisterUserRequest : IRequest
    {
        public string Password { get; set; }
        public string Username { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}