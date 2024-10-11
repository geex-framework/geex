using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

using Geex.Common.Abstractions;

using HotChocolate.Types;

namespace Geex.Common.Abstraction.ClientNotification
{
    public abstract class ClientNotify : IClientNotify
    {
        protected ClientNotify()
        {
        }
        public DateTimeOffset CreatedOn { get; protected set; } = DateTimeOffset.Now;

        public class ClientNotifyGqlConfig : GqlConfig.Interface<IClientNotify>
        {
            /// <inheritdoc />
            protected override void Configure(IInterfaceTypeDescriptor<IClientNotify> descriptor)
            {
                base.Configure(descriptor);
            }
        }


    }

    public interface IClientNotify
    {
        public DateTimeOffset CreatedOn { get; }
    }
}
