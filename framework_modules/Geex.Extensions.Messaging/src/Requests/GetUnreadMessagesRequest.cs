using System.Linq;
using Geex.Extensions.Messaging.Api.Aggregates.Messages;
using MediatR;

namespace Geex.Extensions.Requests.Messaging
{
    public record GetUnreadMessagesRequest : IRequest<IQueryable<IMessage>>
    {
    }
}
