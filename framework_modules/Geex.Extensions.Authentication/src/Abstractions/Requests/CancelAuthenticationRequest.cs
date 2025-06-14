using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record CancelAuthenticationRequest : IRequest<bool>
    {
        public string? UserId { get; set; }

        public CancelAuthenticationRequest(string? userId)
        {
            UserId = userId;
        }
    }
}
