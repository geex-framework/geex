using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction.ClientNotification;

using HotChocolate.Types;

namespace Geex.Common.Abstraction.ClientNotification
{
    public class DataChangeClientNotify : ClientNotify
    {
        /// <inheritdoc />
        public DataChangeClientNotify(DataChangeType dataChangeType)
        {
            DataChangeType = dataChangeType;
        }

        public DataChangeType DataChangeType { get; set; }

        public class DataChangeClientNotifyGqlConfig : GqlConfig.Object<DataChangeClientNotify>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<DataChangeClientNotify> descriptor)
            {
                descriptor.Implements<InterfaceType<IClientNotify>>();
                base.Configure(descriptor);
            }
        }
    }

}
