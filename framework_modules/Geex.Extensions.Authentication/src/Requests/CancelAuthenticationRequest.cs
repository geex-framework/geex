using MediatR;

namespace Geex.Extensions.Requests.Authentication
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
