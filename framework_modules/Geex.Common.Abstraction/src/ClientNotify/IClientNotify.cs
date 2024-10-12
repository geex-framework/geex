﻿using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

using Geex.Common.Abstractions;

using HotChocolate.Types;

namespace Geex.Common.Abstraction.ClientNotification
{
    public abstract class ClientNotify
    {
        protected ClientNotify()
        {
        }
        public class ClientNotifyGqlConfig : GqlConfig.Interface<ClientNotify>
        {
            /// <inheritdoc />
            protected override void Configure(IInterfaceTypeDescriptor<ClientNotify> descriptor)
            {
                base.Configure(descriptor);
            }
        }
        public DateTimeOffset CreatedOn { get; protected set; } = DateTimeOffset.Now;
    }
}
