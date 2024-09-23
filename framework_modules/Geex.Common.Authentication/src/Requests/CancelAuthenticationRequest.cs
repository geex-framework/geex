using MediatR;

namespace Geex.Common.Requests.Authentication
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
