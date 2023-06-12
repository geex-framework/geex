﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using ElasticSearch;
using GeexBox.ElasticSearch.Zero.Logging.Commom;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeexBox.ElasticSearch.Zero.Logging.Elasticsearch
{
    [ProviderAlias("Elasticsearch")]
    public class EsLoggerProvider : BatchingLoggerProvider
    {
        private readonly IHostEnvironment _env;
        private static string _serverIp;
        private readonly ElasticsearchHelper _esHelper;

        public EsLoggerProvider(IOptionsMonitor<EsLoggerOptions> options, IHostEnvironment env) : base(options)
        {
            _env = env;
            if (_serverIp == null)
            {
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                _serverIp = ipHost.AddressList.GetLocalIPv4().MapToIPv4().ToString();
            }
            var loggerOptions = options.CurrentValue;
            if (!string.IsNullOrWhiteSpace(loggerOptions.TemplateName))
            {
                loggerOptions.AutoRegisterTemplate = true;
            }
            _esHelper = ElasticsearchHelper.Create(loggerOptions);
            _esHelper.RegisterTemplateIfNeeded();
        }

        public EsLoggerProvider(EsLoggerOptions options, IHostEnvironment env) : base(options)
        {
            _env = env;
            if (_serverIp == null)
            {
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                _serverIp = ipHost.AddressList.GetLocalIPv4().MapToIPv4().ToString();
            }
            var loggerOptions = options;
            if (!string.IsNullOrWhiteSpace(loggerOptions.TemplateName))
            {
                loggerOptions.AutoRegisterTemplate = true;
            }
            _esHelper = ElasticsearchHelper.Create(loggerOptions);
            _esHelper.RegisterTemplateIfNeeded();
        }

        public override ILogger CreateLogger(string categoryName)
        {
            return new EsLogger(this, categoryName, _env.EnvironmentName,_env.ApplicationName, _serverIp);
        }

        protected override async Task WriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken)
        {
            DynamicResponse result;
            try
            {
                result = await PostToEsAsync<DynamicResponse>(messages);
            }
            catch (Exception ex)
            {
                HandleException(ex, messages);
                return;
            }

            // Handle the results from ES, check if there are any errors.
            if (result.Success && result.Body?["errors"] == true)
            {
                var indexer = 0;
                var items = result.Body["items"];
                foreach (var obj in items)
                {
                    var item = (dynamic)obj;
                    if (item["index"] != null && item["index"]["error"] != null)
                    {
                        var e = messages.ElementAt(indexer);

                        if (_esHelper.Options.FailureCallback != null)
                        {
                            // Send to a failure callback
                            try
                            {
                                _esHelper.Options.FailureCallback(e);
                            }
                            catch (Exception ex)
                            {
                                // We do not let this fail too
                                Console.WriteLine("Caught exception while emitting to callback {1}: {0}", ex, _esHelper.Options.FailureCallback);
                            }
                        }

                    }
                    indexer++;
                }
            }
            else if (result.Success == false && result.OriginalException != null)
            {
                HandleException(result.OriginalException, messages);
            }
        }

        protected virtual async Task<T> PostToEsAsync<T>(IEnumerable<LogMessage> messages) where T : class, IElasticsearchResponse, new()
        {
            if (messages == null || !messages.Any())
                return null;

            var payload = new List<string>();
            foreach (var e in messages)
            {
                var indexName = _esHelper.GetIndexForEvent(e, e.Timestamp.ToUniversalTime());
                var action = default(object);

                var pipelineName = _esHelper.Options.PipelineNameDecider?.Invoke(e) ?? _esHelper.Options.PipelineName;
                if (string.IsNullOrWhiteSpace(pipelineName))
                {
                    action = new { index = new { _index = indexName } };
                }
                else
                {
                    action = new { index = new { _index = indexName , pipeline = pipelineName } };
                }
                var actionJson = _esHelper.Serialize(action);
                payload.Add(actionJson);
                payload.Add(e.Message);
            }
            return await _esHelper.Client.BulkAsync<T>(PostData.MultiJson(payload));
        }

        protected virtual void HandleException(Exception ex, IEnumerable<LogMessage> messages)
        {
            Console.WriteLine("Caught exception while preforming bulk operation to Elasticsearch: {0}", ex);
            if (_esHelper.Options.FailureCallback != null)
            {
                // Send to a failure callback
                try
                {
                    foreach (var e in messages)
                    {
                        _esHelper.Options.FailureCallback(e);
                    }
                }
                catch (Exception exCallback)
                {
                    // We do not let this fail too
                    Console.WriteLine("Caught exception while emitting to callback {1}: {0}", exCallback, _esHelper.Options.FailureCallback);
                }
            }
        }
    }
}
