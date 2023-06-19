using System.Linq;
using Geex.Common.Abstraction.Gql;
using Geex.Common.Messaging.Api.Aggregates.Messages;

using MediatR;

namespace Geex.Common.Messaging.Api.GqlSchemas.Messages
{
    public class GetUnreadMessagesInput : IRequest<IQueryable<IMessage>>, IEmptyObject
    {
        public string _ { get; set; }
    }
}