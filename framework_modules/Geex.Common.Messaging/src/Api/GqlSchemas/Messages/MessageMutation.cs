using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using Geex.Common.Messaging.Api.Aggregates.Messages.Inputs;
using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using MediatR;
using MongoDB.Entities;

namespace Geex.Common.Messaging.Api.GqlSchemas.Messages
{
    public class MessageMutation : MutationExtension<MessageMutation>
    {
        private readonly IMediator _mediator;

        public MessageMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        /// <summary>
        /// 标记消息已读
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> MarkMessagesRead(
            MarkMessagesReadInput input)
        {
            var result = await this._mediator.Send(input);
            return true;
        }
        /// <summary>
        /// 删除消息分配
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> DeleteMessageDistributions(
            DeleteMessageDistributionsInput input)
        {
            var result = await _mediator.Send(input);
            return true;
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> SendMessage(
            SendNotificationMessageRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }

        /// <summary>
        /// 创建消息
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IMessage> CreateMessage(
            CreateMessageRequest input)
        {
            var result = await _mediator.Send(input);
            return result;
        }

        /// <summary>
        /// 编辑消息
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> EditMessage(
            EditMessageRequest input)
        {
            var result = await _mediator.Send(input);
            return true;
        }

    }
}
