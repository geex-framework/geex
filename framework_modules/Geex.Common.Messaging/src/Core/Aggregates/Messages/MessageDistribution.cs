using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Storage;
using Geex.Common.Abstractions;

namespace Geex.Common.Messaging.Core.Aggregates.Messages
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
        public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
        {
            return ValidationResult.Success;
        }
    }
}
