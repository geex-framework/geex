using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Authentication;
using Geex.Extensions.Messaging.Core.Entities;
using Geex.Extensions.Messaging.Requests;
using HotChocolate.Subscriptions;

using MediatX;

namespace Geex.Extensions.Messaging.Core.Handlers
{
    public class MessageHandler :
        ICommonHandler<IMessage, Message>,
        IRequestHandler<DeleteMessageDistributionsRequest>,
        IRequestHandler<DeleteMessageRequest, bool>,
        IRequestHandler<MarkMessagesReadRequest>,
        IRequestHandler<SendNotificationMessageRequest>,
        IRequestHandler<CreateMessageRequest, IMessage>,
        IRequestHandler<EditMessageRequest>,
        IRequestHandler<GetUnreadMessagesRequest, IQueryable<Message>>
    {
        public IUnitOfWork Uow { get; }
        public ICurrentUser CurrentUser { get; }
        public LazyService<ITopicEventSender> Sender { get; }

        public MessageHandler(IUnitOfWork uow, ICurrentUser currentUser, LazyService<ITopicEventSender> sender)
        {
            Uow = uow;
            this.CurrentUser = currentUser;
            Sender = sender;
        }

        public async Task Handle(DeleteMessageDistributionsRequest request, CancellationToken cancellationToken)
        {
            await Uow.DeleteAsync<MessageDistribution>(x => request.UserIds.Contains(x.ToUserId) && request.MessageId == request.MessageId, cancellationToken);
        }

        public async Task<bool> Handle(DeleteMessageRequest request, CancellationToken cancellationToken)
        {
            _ = Uow.Query<Message>().FirstOrDefault(x => x.Id == request.MessageId
                    && (CurrentUser.IsSuperAdmin
                        || x.FromUserId == CurrentUser.UserId
                        || Uow.Query<MessageDistribution>().Any(d => d.MessageId == request.MessageId && d.ToUserId == CurrentUser.UserId)))
                ?? throw new BusinessException(GeexExceptionType.NotFound, message: "Message not found.");

            await Uow.DeleteAsync<MessageDistribution>(x => x.MessageId == request.MessageId, cancellationToken);
            await Uow.DeleteAsync<Message>(x => x.Id == request.MessageId, cancellationToken);
            return true;
        }

        public async Task Handle(MarkMessagesReadRequest request, CancellationToken cancellationToken)
        {
            await Uow.DbContext.Update<MessageDistribution>().Match(x => request.MessageIds.Contains(x.MessageId) && request.UserId == x.ToUserId).Modify(x => x.IsRead, true).ExecuteAsync(cancellationToken);
        }

        public Task<IQueryable<Message>> Handle(GetUnreadMessagesRequest request, CancellationToken cancellationToken)
        {
            var messageIds = Uow.Query<MessageDistribution>()
                .Where(x => x.IsRead == false && x.ToUserId == CurrentUser.UserId)
                .Select(x => x.MessageId)
                .ToList();
            return Task.FromResult(Uow.Query<Message>().Where(x => messageIds.Contains(x.Id)));
        }

        public async Task Handle(SendNotificationMessageRequest request, CancellationToken cancellationToken)
        {
            var message = Uow.Query<Message>().First(x => x.Id == request.MessageId);
            await message.DistributeAsync(request.ToUserIds.ToArray());

            await Uow.ClientNotify(new NewMessageClientNotify(message), request.ToUserIds.ToArray());
        }

        public async Task<IMessage> Handle(CreateMessageRequest request, CancellationToken cancellationToken)
        {
            var message = new Message(request.Text, request.Severity);
            Uow.Attach(message);
            return message;
        }

        public async Task Handle(EditMessageRequest request, CancellationToken cancellationToken)
        {
            var message = await Uow.Query<Message>().OneAsync(request.Id);
            if (!string.IsNullOrEmpty(request.Text))
            {
                message.Title = request.Text;
            }
            if (request.Severity.HasValue)
            {
                message.Severity = request.Severity.Value;
            }
            if (request.MessageType.HasValue)
            {
                message.MessageType = request.MessageType.Value;
            }
        }
    }
}
