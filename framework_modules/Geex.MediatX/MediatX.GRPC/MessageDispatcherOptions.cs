using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MediatX.GRPC
{
    public class MessageDispatcherOptions
    {
        /// <summary>
        /// Gets or sets the time-to-live value for deduplication.
        /// </summary>
        /// <remarks>
        /// The DeDuplicationTTL property determines the amount of time, in milliseconds, that an item can be considered duplicate
        /// before it is removed from the deduplication cache. The default value is 5000 milliseconds (5 seconds).
        /// </remarks>
        /// <value>
        /// The time-to-live value for deduplication.
        /// </value>
        public int DeDuplicationTTL { get; set; } = 5000;

        /// <summary>
        /// Gets or sets a value indicating whether duplicate entries are enabled for deduplication.
        /// </summary>
        /// <value>
        /// <c>true</c> if deduplication is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool DeDuplicationEnabled { get; set; } = true;

        public Dictionary<Type, RemoteServiceDefinition> RemoteTypeServices { get; set; } = new();

        public JsonSerializerOptions SerializerSettings { get; set; }
        /// <summary>
        /// Represents the options for a message dispatcher.
        /// </summary>
        public MessageDispatcherOptions(JsonSerializerOptions jsonSerializerOptions)
        {
            SerializerSettings = jsonSerializerOptions;
        }
    }
}
