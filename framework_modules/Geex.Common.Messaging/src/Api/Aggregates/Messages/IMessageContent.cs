using Geex.Abstractions;
using HotChocolate.Types;

namespace KuanFang.Rms.MessageManagement.Messages
{
    public interface IMessageContent
    {
        public string _ { get; set; }

        public class IMessageContentGqlType : GqlConfig.Object<IMessageContent>
        {
            protected override void Configure(IObjectTypeDescriptor<IMessageContent> descriptor)
            {
                // Implicitly binding all fields, if you want to bind fields explicitly, read more about hot chocolate
                descriptor.BindFieldsImplicitly();
                base.Configure(descriptor);
            }
        }
    }
}
