﻿namespace Geex.Extensions.Messaging.Core.Entities
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

        //public class ToDoContentGqlConfig : GqlConfig.Object<ToDoContent>
        //{
        //    /// <inheritdoc />
        //    protected override void Configure(IObjectTypeDescriptor<ToDoContent> descriptor)
        //    {
        //        descriptor.Implements<IMessageContent.IMessageContentGqlType>();
        //        descriptor.BindFieldsImplicitly();
        //        base.Configure(descriptor);
        //    }
        //}
    }
}
