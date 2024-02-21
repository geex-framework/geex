using System;
using System.Security.Cryptography;
using System.Text;
using MediatX.Kafka;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediatX
{
  public static class Extensions
  {
    /// <summary>
    /// Adds the MediatX Kafka Message Dispatcher to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The configuration action to configure the MessageDispatcherOptions.</param>
    /// <returns>The service collection with the MediatX Kafka Message Dispatcher added.</returns>
    public static IServiceCollection AddMediatXKafkaMessageDispatcher(this IServiceCollection services, Action<MessageDispatcherOptions> config)
    {
      services.Configure<MessageDispatcherOptions>(config);
      services.AddSingleton<IExternalMessageDispatcher, MessageDispatcher>();
      return services;
    }

    /// <summary>
    /// Resolves mediatx calls by adding the RequestsManager as a hosted service to the service collection.
    /// </summary>
    /// <param name="services">The service collection to resolve mediatx calls for.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection ResolveMediatXCalls(this IServiceCollection services)
    {
      services.AddHostedService<RequestsManager>();
      return services;
    }

    /// Creates a Kafka topic asynchronously.
    /// @param services The service provider to retrieve additional services from.
    /// @param options The options used for configuring the message dispatcher.
    /// @param topicName The name of the topic to be created.
    /// @param replicationFactor The replication factor for the topic. Defaults to 1.
    /// @remarks
    /// This method creates a Kafka topic using the specified options. The topic name
    /// and replication factor are required parameters. If no replication factor is
    /// specified, it defaults to 1.
    /// @throws CreateTopicsException If an error occurs during topic creation.
    /// @see MessageDispatcherOptions
    /// @see System.IServiceProvider
    /// @see Confluent.Kafka.Admin.AdminClientBuilder
    /// @see Confluent.Kafka.Admin.TopicSpecification
    /// @see Microsoft.Extensions.Logging.ILogger
    /// /
    public static void CreateTopicAsync(this IServiceProvider services, MessageDispatcherOptions options, string topicName, short replicationFactor = 1)
    {
      using (var adminClient = new AdminClientBuilder(options.GetAdminConfig()).Build())
        try
        {
          adminClient.CreateTopicsAsync(new TopicSpecification[]
          {
            new TopicSpecification
            {
              Name = topicName,
              ReplicationFactor = replicationFactor,
              NumPartitions = options.TopicPartition ?? 1
            }
          });
        }
        catch (CreateTopicsException e)
        {
          var logger = services.GetService<ILogger<MessageDispatcher>>();
          logger?.LogError($"An error occurred creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
        }
    }

    /// <summary>
    /// Deletes a topic asynchronously.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="options">The message dispatcher options.</param>
    /// <param name="topicName">The name of the topic to delete.</param>
    public static async void DeleteTopicAsync(this IServiceProvider services, MessageDispatcherOptions options, string topicName)
    {
      using (var adminClient = new AdminClientBuilder(options.GetAdminConfig()).Build())
      {
        await adminClient.DeleteTopicsAsync(new string[] { topicName });
      }
    }
  }
}
