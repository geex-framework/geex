using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace MediatX.Kafka
{
  /// <summary>
  /// Represents a Kafka message with generic type T.
  /// </summary>
  /// <typeparam name="T">The type of message.</typeparam>
  public class KafkaMessage<T>
  {
    /// <summary>
    /// Gets or sets the <typeparamref name="T"/> message.
    /// </summary>
    /// <remarks>
    /// The <see cref="Message"/> property represents the message payload of type <typeparamref name="T"/>.
    /// </remarks>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    public T Message { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    /// <value>
    /// The correlation ID of the object.
    /// </value>
    public string CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the string value indicating the reply address or recipient.
    /// </summary>
    /// <value>
    /// The string value representing the reply address or recipient.
    /// </value>
    public string ReplyTo { get; set; }
  }

  /// <summary>
  /// Represents a reply from Kafka.
  /// </summary>
  public class KafkaReply
  {
    /// <summary>
    /// Gets or sets the Reply property.
    /// </summary>
    /// <value>
    /// The Reply property.
    /// </value>
    public object Reply { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    /// <value>
    /// The correlation ID.
    /// </value>
    /// <remarks>
    /// The correlation ID is used for tracing and tracking purposes. It is a unique identifier that
    /// is often used to correlate related events in a distributed system. It can be used to group
    /// together log entries or to track the progress of a specific operation across multiple services.
    /// </remarks>
    public string CorrelationId { get; set; }
  }

  /// <summary>
  /// Represents a Kafka reply message.
  /// </summary>
  /// <typeparam name="T">The type of the reply message.</typeparam>
  public class KafkaReply<T>
  {
    /// <summary>
    /// Gets or sets the Reply property.
    /// </summary>
    /// <typeparam name="T">The type of the Reply property.</typeparam>
    /// <value>The Reply property.</value>
    public T Reply { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID.
    /// </summary>
    public string CorrelationId { get; set; }
  }
}
