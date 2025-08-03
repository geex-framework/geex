using Geex.Validation;
using System.Threading;
using System.Threading.Tasks;
using Geex.Storage;

namespace Geex.Extensions.Messaging.Core.Entities
{
    public class MessageDistribution : Entity<MessageDistribution>
    {
        public MessageDistribution(string messageId, string toUserId)
        {
            this.MessageId = messageId;
            this.ToUserId = toUserId;
        }

        public string ToUserId { get; set; }
        public string MessageId { get; set; }
        /// <summary>
        /// 是否已读
        /// bug: 未读消息最好放入redis, 避免全表遍历
        /// </summary>
        public bool IsRead { get; set; }
        public override async Task<ValidationResult> Validate(CancellationToken cancellation = default)
        {
            return ValidationResult.Success;
        }
    }
}
