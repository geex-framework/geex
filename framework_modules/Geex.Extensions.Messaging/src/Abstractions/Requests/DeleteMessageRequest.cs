using MediatX;

namespace Geex.Extensions.Messaging.Requests
{
    public record DeleteMessageRequest(string MessageId) : IRequest<bool>;
}
