using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using Confluent.Kafka;

namespace MediatX.Kafka
{
    /// <summary>
    /// Represents the options for the <see cref="MessageDispatcher"/>.
    /// </summary>
    public class MessageDispatcherOptions
    {
        public int? TopicPartition { get; set; }
        public string BootstrapServers { get; set; }
        public Offset Offset { get; set; } = Offset.End;

        /// <summary>
        /// Gets or sets the settings used for JSON serialization.
        /// </summary>
        /// <value>
        /// The JSON serializer settings.
        /// </value>
        public JsonSerializerOptions SerializerSettings { get; set; }

        /// <summary>
        /// Retrieves the admin client configuration.
        /// </summary>
        /// <returns>
        /// An instance of the <see cref="AdminClientConfig"/> class containing the admin client configuration.
        /// </returns>
        public AdminClientConfig GetAdminConfig()
        {
            return new AdminClientConfig()
            {
                BootstrapServers = this.BootstrapServers,
            };
        }

        /// <summary>
        /// Gets the producer config.
        /// </summary>
        /// <remarks>
        /// Use this method to retrieve the producer configuration.
        /// </remarks>
        /// <returns>
        /// The producer configuration.
        /// </returns>
        public ProducerConfig GetProducerConfig()
        {
            return new ProducerConfig()
            {
                BootstrapServers = this.BootstrapServers
            };
        }

        /// <summary>
        /// Returns a new instance of the ConsumerConfig class with the following properties set:
        /// - BootstrapServers: The bootstrap servers to connect to
        /// - EnableAutoCommit: Whether to automatically commit consumed messages
        /// - EnableAutoOffsetStore: Whether to automatically store the consumer's offsets
        /// - SessionTimeoutMs: The timeout for a consumer's session, in milliseconds
        /// - AutoOffsetReset: The strategy to use when there is no initial offset in Kafka or if the current offset does not exist anymore on the server
        /// </summary>
        /// <returns>
        /// A new instance of the ConsumerConfig class with the specified properties set.
        /// </returns>
        public ConsumerConfig GetConsumerConfig()
        {
            return new ConsumerConfig()
            {
                BootstrapServers = this.BootstrapServers,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = true,
                SessionTimeoutMs = 45000,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }


        /// <summary>
        /// Represents the options for a message dispatcher.
        /// </summary>
        public MessageDispatcherOptions(JsonSerializerOptions jsonSerializerOptions)
        {
            SerializerSettings = jsonSerializerOptions;
        }
    }
}
