using MediatR;

namespace Geex.Common.Authentication.Requests
{
    public class CancelAuthenticationRequest : IRequest<bool>
    {
        public string? UserId { get; set; }

        public CancelAuthenticationRequest(string? userId)
        {
            UserId = userId;
        }
    }
}
