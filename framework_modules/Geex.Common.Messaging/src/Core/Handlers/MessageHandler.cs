using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Enumerations;
using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstractions;
using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using Geex.Common.Messaging.Api.Aggregates.Messages.Inputs;
using Geex.Common.Messaging.Api.GqlSchemas.Messages;
using Geex.Common.Messaging.Core.Aggregates;
using Geex.Common.Messaging.Core.Aggregates.FrontendCalls;
using Geex.Common.Messaging.Core.Aggregates.Messages;

using HotChocolate.Subscriptions;

using MediatR;

using MongoDB.Entities;

namespace Geex.Common.Messaging.Core.Handlers
{
    public class MessageHandler :
        ICommonHandler<IMessage, Message>,
        IRequestHandler<DeleteMessageDistributionsInput>,
        IRequestHandler<MarkMessagesReadInput>,
        IRequestHandler<SendNotificationMessageRequest>,
        IRequestHandler<CreateMessageRequest, IMessage>,
        IRequestHandler<EditMessageRequest>,
    IRequestHandler<GetUnreadMessagesInput, IEnumerable<IMessage>>
    {
        public DbContext DbContext { get; }
        public LazyService<ClaimsPrincipal> ClaimsPrincipal { get; }
        public LazyService<ITopicEventSender> Sender { get; }

        public MessageHandler(DbContext dbContext, LazyService<ClaimsPrincipal> claimsPrincipal, LazyService<ITopicEventSender> sender)
        {
            DbContext = dbContext;
            this.ClaimsPrincipal = claimsPrincipal;
            Sender = sender;
        }

        public async Task Handle(DeleteMessageDistributionsInput request, CancellationToken cancellationToken)
        {
            var res = await DbContext.DeleteAsync<MessageDistribution>(x => request.UserIds.Contains(x.ToUserId) && request.MessageId == x.MessageId, cancellationToken);
            return;
        }

        public async Task Handle(MarkMessagesReadInput request, CancellationToken cancellationToken)
        {
            await DbContext.Update<MessageDistribution>().Match(x => request.MessageIds.Contains(x.MessageId) && request.UserId == x.ToUserId).Modify(x => x.IsRead, true).ExecuteAsync(cancellationToken);
            return;
        }

        public async Task<IEnumerable<IMessage>> Handle(GetUnreadMessagesInput request, CancellationToken cancellationToken)
        {
            var claimsPrincipal = ClaimsPrincipal.Value;
            var messageDistributions = DbContext.Query<MessageDistribution>().Where(x => x.IsRead == false && x.ToUserId == claimsPrincipal.FindUserId()).ToList();
            var messageIds = messageDistributions.Select(x => x.MessageId);
            var messages = DbContext.Query<Message>().Where(x => messageIds.Contains(x.Id)).ToList();
            return messages;
        }

        public async Task Handle(SendNotificationMessageRequest request, CancellationToken cancellationToken)
        {
            var message = DbContext.Query<Message>().First(x => x.Id == request.MessageId);
            await message.DistributeAsync(request.ToUserIds.ToArray());

            foreach (var toUserId in request.ToUserIds)
            {
                await Sender.Value.SendAsync<IFrontendCall>($"{nameof(MessageSubscription.OnFrontendCall)}:{toUserId}", new FrontendCall(FrontendCallType.NewMessage, JsonSerializer.SerializeToNode(new { message.Content, message.FromUserId, message.MessageType, message.Severity })), cancellationToken);
            }

            return;
        }

        public async Task<IMessage> Handle(CreateMessageRequest request, CancellationToken cancellationToken)
        {
            var message = new Message(request.Text, request.Severity);
            DbContext.Attach(message);
            await message.SaveAsync(cancellation: cancellationToken);
            return message;
        }

        public async Task Handle(EditMessageRequest request, CancellationToken cancellationToken)
        {
            var message = await DbContext.Query<Message>().OneAsync(request.Id);
            if (!request.Text.IsNullOrEmpty())
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
            await message.SaveAsync(cancellation: cancellationToken);
            return;
        }
    }
}
