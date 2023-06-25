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
using HotChocolate.Types;
using KuanFang.Rms.MessageManagement.Messages;

using MongoDB.Bson.Serialization;

namespace Geex.Common.Messaging.Core.EntityMapConfigs
{
    public class MessageEntityConfig : EntityConfig<Message>
    {
        protected override void Map(BsonClassMap<Message> map)
        {
            map.Inherit<IMessage>();
            map.AutoMap();
            BsonClassMap.RegisterClassMap<InteractContent>();
            BsonClassMap.RegisterClassMap<ToDoContent>();
        }

        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<Message> descriptor)
        {
            descriptor.BindFieldsImplicitly();
            descriptor.ConfigEntity();
            //descriptor.Field(x => x.FromUserId);
            //descriptor.Field(x => x.MessageType);
            //descriptor.Field(x => x.Content);
            //descriptor.Field(x => x.ToUserIds);
            //descriptor.Field(x => x.Id);
            //descriptor.Field(x => x.Title);
            //descriptor.Field(x => x.Time);
            //descriptor.Field(x => x.Severity);
            descriptor.Implements<InterfaceType<IMessage>>();
        }
    }
}
