using System.Linq;
using System.Threading.Tasks;
using Geex.Abstractions;
using Geex.Extensions.Requests;
using Geex.Extensions.Messaging.Api.Aggregates.Messages;
using Geex.Extensions.Messaging.Core.Aggregates.Messages;
using Geex.Extensions.Requests.Messaging;
using Geex.Gql.Types;
using HotChocolate.Types;
using MediatR;

namespace Geex.Extensions.Messaging.Api.GqlSchemas.Messages
{
    public sealed class MessageQuery : QueryExtension<MessageQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<MessageQuery> descriptor)
        {
            descriptor.Field(x => x.Messages())
            .UseOffsetPaging<ObjectType<Message>>()
            .UseFiltering<IMessage>(x =>
            {
                x.Field(y => y.MessageType);
                x.Field(y => y.Id);
            })
            ;
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;

        public MessageQuery(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        /// <summary>
        /// 列表获取message
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<IMessage>> Messages()
        {
            var result = await this._uow.Request(new QueryRequest<IMessage>());
            return result;
        }

        /// <summary>
        /// 列表获取message
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<IMessage>> UnreadMessages()
        {
            var result = await _uow.Request(new GetUnreadMessagesRequest());
            return result;
        }
    }
}
