using System.Linq;
using MediatX;

namespace Geex.Extensions.Messaging.Requests
{
    public record GetUnreadMessagesRequest : IRequest<IQueryable<IMessage>>
    {
    }
}
