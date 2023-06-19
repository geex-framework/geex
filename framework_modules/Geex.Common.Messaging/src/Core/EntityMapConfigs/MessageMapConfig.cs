using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Bson;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Messaging.Api.Aggregates.Messages;
using Geex.Common.Messaging.Core.Aggregates.Messages;

using KuanFang.Rms.MessageManagement.Messages;

using MongoDB.Bson.Serialization;

namespace Geex.Common.Messaging.Core.EntityMapConfigs
{
    public class MessageMapConfig : EntityMapConfig<Message>
    {
        public override void Map(BsonClassMap<Message> map)
        {
            map.Inherit<IMessage>();
            map.AutoMap();
            BsonClassMap.RegisterClassMap<InteractContent>();
            BsonClassMap.RegisterClassMap<ToDoContent>();
        }
    }
}
