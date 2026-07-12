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
            });
            descriptor.Field(x => x.UnreadMessages())
                .UseOffsetPaging<ObjectType<Message>>()
                .Authorize();
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;

        public MessageQuery(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        public async Task<IQueryable<IMessage>> Messages()
        {
            return await this._uow.Request(new QueryRequest<IMessage>());
        }

        public async Task<IQueryable<Message>> UnreadMessages()
        {
            return await _uow.Request(new GetUnreadMessagesRequest());
        }
    }
}
