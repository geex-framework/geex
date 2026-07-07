using System.Linq;
using Geex.Extensions.Messaging.Core.Entities;
using MediatX;

namespace Geex.Extensions.Messaging.Requests
{
    public record GetUnreadMessagesRequest : IRequest<IQueryable<Message>>
    {
    }
}
