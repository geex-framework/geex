using System;
using Geex.Extensions.Messaging.Core.Entities;

namespace Geex.Extensions.Messaging.ClientNotification;

public sealed class ClientNotifyEnvelope
{
    public string Kind { get; set; } = "";
    public string Value { get; set; } = "";
    public DateTimeOffset CreatedOn { get; set; }

    public static ClientNotifyEnvelope From(ClientNotify notification) => new()
    {
        Kind = notification switch
        {
            NewMessageClientNotify => nameof(NewMessageClientNotify),
            DataChangeClientNotify => nameof(DataChangeClientNotify),
            _ => throw new NotSupportedException($"Unsupported notification: {notification.GetType().Name}")
        },
        Value = notification switch
        {
            NewMessageClientNotify message => message.Message.Id,
            DataChangeClientNotify change => change.DataChangeType.Value,
            _ => ""
        },
        CreatedOn = notification.CreatedOn
    };
}
