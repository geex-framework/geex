using System.Linq;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using MediatR;

namespace Geex.Common.Requests.Messaging
{
    public class GetUnreadMessagesRequest : IRequest<IQueryable<IMessage>>
    {
    }
}