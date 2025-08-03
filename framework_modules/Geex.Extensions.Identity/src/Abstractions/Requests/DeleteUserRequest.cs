using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record DeleteUserRequest : IRequest<bool>
    {
        public string Id { get; set; }
    }
}
