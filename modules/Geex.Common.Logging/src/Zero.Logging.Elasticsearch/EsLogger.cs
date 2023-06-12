using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text.Json;
using Geex.Common.Abstraction.Json;
using GeexBox.ElasticSearch.Zero.Logging.Commom;
using Geexbox.Logging.ElasticSearch.ZeroLoggingCommom;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GeexBox.ElasticSearch.Zero.Logging.Elasticsearch
{
    public class EsLogger : ILogger
    {
        private readonly BatchingLoggerProvider _provider;
        private readonly string _category;
        private readonly string _serviceName;
        private readonly string _serverIp;
        private readonly string _env;
        private readonly JsonSerializerOptions _serializerSettings = new JsonSerializerOptions(Json.DefaultSerializeSettings)
        {
            IgnoreNullValues = true,
            WriteIndented = false
        };
        //    new JsonSerializerOptions()
        //{
        //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        //    Error = (sender, args) => args.ErrorContext.Handled = true,
        //    Formatting = Formatting.None,
        //    MissingMemberHandling = MissingMemberHandling.Ignore,
        //    ContractResolver = new DefaultContractResolver
        //    {
        //        NamingStrategy = new CamelCaseNamingStrategy(),
        //    },
        //    NullValueHandling = NullValueHandling.Ignore
        //};


        public EsLogger(BatchingLoggerProvider loggerProvider, string categoryName, string env, string serviceName,
            string serverIp)
        {
            _provider = loggerProvider;
            _category = categoryName;
            _serviceName = serviceName;
            _serverIp = serverIp;
            _env = env;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _provider.IsEnabled;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatterOrDefaultMessage)
        {
            Log(DateTimeOffset.Now, logLevel, eventId, state as object[], exception,
                formatterOrDefaultMessage(state, exception));
        }

        public void Log(DateTimeOffset timestamp, LogLevel logLevel, EventId eventId, object[] state,
            Exception exception, string formatterOrDefaultMessage)
        {
            var jsonData = new Dictionary<string, object>()
            {
                {"timestamp", timestamp},
                {"level", logLevel.ToString()},
                {"env", _env},
                {"serviceName", _serviceName},
                {"serverIp", _serverIp},
                {"category", _category},
                {"eventId", eventId},
                {"message", formatterOrDefaultMessage},
            };
            if (exception != null)
            {
                jsonData["exceptions"] = new List<ExceptionModel>();
                WriteException(jsonData["exceptions"] as List<ExceptionModel>, exception, 0);
            }

            if (state != default)
            {
                jsonData["data"] = state;
            }

            _provider.AddMessage(timestamp, JsonSerializer.Serialize(jsonData, _serializerSettings));
        }

        private void WriteException(List<ExceptionModel> exceptionList, Exception exception, int depth)
        {
            WriteSingleException(exceptionList, exception, depth);
            if (exception.InnerException != null && depth < 20)
                WriteException(exceptionList, exception.InnerException, ++depth);
        }

        private void WriteSingleException(List<ExceptionModel> exceptionList, Exception exception, int depth)
        {
            exceptionList.Add(new ExceptionModel
            {
                depth = depth,
                message = exception.Message,
                source = exception.Source,
                stackTrace = exception.StackTrace,
                hResult = exception.HResult,
            });
        }

        internal class ExceptionModel
        {
            public int depth { get; set; }
            public string message { get; set; }
            public string source { get; set; }
            public string stackTrace { get; set; }
            public int hResult { get; set; }
        }
    }
}

