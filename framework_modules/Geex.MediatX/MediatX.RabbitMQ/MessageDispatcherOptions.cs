using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MediatX.RabbitMQ
{
    /// <summary>
    /// Represents the options for configuring the message dispatcher.
    /// </summary>
    public class MessageDispatcherOptions
    {
        /// <summary>
        /// Gets or sets the name of the host.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the user name. </summary> <value>
        /// The user name. </value>
        /// /
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the virtual host.
        /// </summary>
        /// <value>
        /// The virtual host.
        /// </value>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// Gets or sets the port number.
        /// </summary>
        /// <value>
        /// The port number.
        /// </value>
        public int Port { get; set; } = 5601;

        /// <summary>
        /// Gets or sets the name of the queue.
        /// </summary>
        /// <value>
        /// The name of the queue.
        /// </value>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the object should be automatically deleted.
        /// </summary>
        public bool AutoDelete = false;

        /// <summary>
        /// The Durable variable represents the durability status of an object.
        /// If Durable is set to true, it means the object is durable, otherwise, it is not.
        /// </summary>
        /// <remarks>
        /// Durability is a characteristic that specifies whether an object is able to withstand wear, decay, or damage over time.
        /// Setting Durable to true indicates that the object is designed to be long-lasting and can resist various forms of deterioration.
        /// Conversely, setting Durable to false suggests that the object is not intended to have a long lifespan or may be susceptible to damage.
        /// </remarks>
        public bool Durable = true;

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
        public bool DeDuplicationEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the serializer settings for JSON serialization and deserialization.
        /// </summary>
        public JsonSerializerOptions SerializerSettings { get; set; }

        public Dictionary<Type, Type[]> NotificationHandlerTypes { get; set; } = new Dictionary<Type, Type[]>();

        /// Represents the options for message dispatcher.
        /// /
        public MessageDispatcherOptions()
        {
        }
    }
}
