using System.Threading.Tasks;
using Geex.Extensions.Messaging.Requests;
using Geex.Gql.Types;

namespace Geex.Extensions.Messaging.Gql
{
    public sealed class MessageMutation : MutationExtension<MessageMutation>
    {
        private readonly IUnitOfWork _uow;

        public MessageMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        /// <summary>
        /// 标记消息已读
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> MarkMessagesRead(
            MarkMessagesReadRequest request)
        {
            await this._uow.Request(request);
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
            await _uow.Request(request);
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
            await _uow.Request(request);
            return true;
        }

        /// <summary>
        /// 创建消息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IMessage> CreateMessage(CreateMessageRequest request) => await _uow.Request(request);

        /// <summary>
        /// 编辑消息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<bool> EditMessage(
            EditMessageRequest request)
        {
            await _uow.Request(request);
            return true;
        }

    }
}
