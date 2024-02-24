using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using Geex.Common.Messaging.Requests;
using MediatR;

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
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> MarkMessagesRead(
            MarkMessagesReadRequest request)
        {
            await this._mediator.Send(request);
            return true;
        }
        /// <summary>
        /// 删除消息分配
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> DeleteMessageDistributions(
            DeleteMessageDistributionsRequest request)
        {
            await _mediator.Send(request);
            return true;
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> SendMessage(
            SendNotificationMessageRequest request)
        {
            await _mediator.Send(request);
            return true;
        }

        /// <summary>
        /// 创建消息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IMessage> CreateMessage(
            CreateMessageRequest request)
        {
            var result = await _mediator.Send(request);
            return result;
        }

        /// <summary>
        /// 编辑消息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> EditMessage(
            EditMessageRequest request)
        {
            await _mediator.Send(request);
            return true;
        }

    }
}
