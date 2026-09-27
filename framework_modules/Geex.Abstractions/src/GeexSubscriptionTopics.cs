using System;
using MongoDB.Driver;

namespace Geex;

public static class GeexSubscriptionTopics
{
    public static string Prefix(GeexCoreModuleOptions options, string environment) =>
        "geex:" + string.Join(":", Array.ConvertAll(new[] { options.AppName, environment,
            new MongoUrl(options.ConnectionString).DatabaseName, options.Redis?.Database.ToString() ?? "" },
            value => Uri.EscapeDataString(value ?? ""))) + ":subscriptions:";
}
