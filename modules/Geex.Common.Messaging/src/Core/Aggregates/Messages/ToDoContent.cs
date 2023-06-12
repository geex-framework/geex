using System;
using KuanFang.Rms.MessageManagement.Messages;

namespace Geex.Common.Messaging.Core.Aggregates.Messages
{
    public class ToDoContent : IMessageContent
    {
        public ToDoContent(TodoType todoType, object meta)
        {
            this.TodoType = todoType;
            this.Meta = meta;
        }

        public string Detail { get; set; }
        public TodoType TodoType { get; set; }
        public object Meta { get; }
        public string _ { get; set; }
    }
}
