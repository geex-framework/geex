using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using Geex.Common.Messaging.Api.Aggregates.Messages.Inputs;
using Geex.Common.Messaging.Api.GqlSchemas.Messages.Types;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;
using MediatR;
using MongoDB.Entities;

namespace Geex.Common.Messaging.Api.GqlSchemas.Messages
{
    public class MessageQuery : QueryExtension<MessageQuery>
    {
        private readonly IMediator _mediator;

        public MessageQuery(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<MessageQuery> descriptor)
        {
            descriptor.Field(x => x.Messages())
            .UseOffsetPaging<MessageGqlType>()
            .UseFiltering<IMessage>(x =>
            {
                x.Field(y => y.MessageType);
                x.Field(y=>y.Id);
            })
            ;
            base.Configure(descriptor);
        }

        /// <summary>
        /// 列表获取message
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<IMessage>> Messages()
        {
            var result = await this._mediator.Send(new QueryInput<IMessage>());
            return result;
        }

        /// <summary>
        /// 列表获取message
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<IMessage>> UnreadMessages(
            GetUnreadMessagesInput input)
        {
            var result = await _mediator.Send(input);
            return result;
        }
    }
}
