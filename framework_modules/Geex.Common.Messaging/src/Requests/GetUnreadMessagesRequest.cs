using System.Linq;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using MediatR;

namespace Geex.Common.Messaging.Requests
{
    public class GetUnreadMessagesRequest : IRequest<IQueryable<IMessage>>
    {
    }
}