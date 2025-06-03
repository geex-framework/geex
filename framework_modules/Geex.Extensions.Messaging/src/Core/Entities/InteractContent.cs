namespace Geex.Extensions.Messaging.Core.Entities
{
    public class InteractContent : IMessageContent
    {
        public string _ { get; set; }
        //public class InteractContentGqlConfig : GqlConfig.Object<InteractContent>
        //{
        //    /// <inheritdoc />
        //    protected override void Configure(IObjectTypeDescriptor<InteractContent> descriptor)
        //    {
        //        descriptor.Implements<IMessageContent.IMessageContentGqlType>();
        //        descriptor.BindFieldsImplicitly();
        //        base.Configure(descriptor);
        //    }
        //}
    }
}
