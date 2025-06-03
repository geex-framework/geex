using System.Linq;
using MediatR;

namespace Geex.Extensions.Messaging.Requests
{
    public record GetUnreadMessagesRequest : IRequest<IQueryable<IMessage>>
    {
    }
}
