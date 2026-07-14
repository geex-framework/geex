using MediatX;

namespace Geex.Extensions.Authentication.Requests
{
    public record EnsureLocalLoginRequest : IRequest<bool>
    {
        public string UserId { get; set; }
    }
}
