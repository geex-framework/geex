using System.Linq;
using System.Threading.Tasks;
using Geex.Extensions.Messaging.Core.Entities;
using Geex.Extensions.Messaging.Requests;
using Geex.Gql.Types;
using Geex.Requests;
using HotChocolate.Types;

namespace Geex.Extensions.Messaging.Gql
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
