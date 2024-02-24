using System;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

using RestSharp;

namespace Geex.Common.Abstraction
{
    public class LoggedRestClient : RestClient
    {
        private readonly ILogger _logger;

        public LoggedRestClient(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public override RestRequestAsyncHandle ExecuteAsync(
      IRestRequest request,
      Action<IRestResponse, RestRequestAsyncHandle> callback,
      Method httpMethod)
        {
            var requestToLog = BuildRequestLogEntry(request);
            Action<IRestResponse, RestRequestAsyncHandle> newCallback = (restResponse, handle) =>
            {
                callback.Invoke(restResponse, handle);
                var responseToLog = BuildResponseLogEntry(restResponse);
                // todo, 优化日志形式
                _logger.LogInformation(string.Format($"Request completed. {Environment.NewLine}Request: {{0}}, {Environment.NewLine}Response: {{1}}",
                        JsonSerializer.Serialize(requestToLog),
                        JsonSerializer.Serialize(responseToLog)));
            };
            return base.ExecuteAsync(request, newCallback, httpMethod);
        }

        /// <inheritdoc />
        public override IRestResponse Execute(IRestRequest request)
        {
            var requestToLog = BuildRequestLogEntry(request);


            var response = base.Execute(request);
            var responseToLog = BuildResponseLogEntry(response);
            // todo, 优化日志形式
            _logger.LogInformation(string.Format($"Request completed. {Environment.NewLine}Request: {{0}}, {Environment.NewLine}Response: {{1}}",
                    JsonSerializer.Serialize(requestToLog),
                    JsonSerializer.Serialize(responseToLog)));
            return response;
        }

        private static object BuildResponseLogEntry(IRestResponse response)
        {
            return new
            {
                statusCode = response.StatusCode,
                content = response.Content,
                headers = response.Headers,
                // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
                responseUri = response.ResponseUri,
                errorMessage = response.ErrorMessage,
            };
        }

        private object BuildRequestLogEntry(IRestRequest request)
        {
            return new
            {
                resource = request.Resource,
                // Parameters are custom anonymous objects in order to have the parameter type as a nice string
                // otherwise it will just show the enum value
                parameters = request.Parameters.Select(parameter => new
                {
                    name = parameter.Name,
                    value = parameter.Value,
                    type = parameter.Type.ToString()
                }),
                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                method = request.Method.ToString(),
                // This will generate the actual Uri used in the request
                uri = this.BuildUri(request),
            };
        }
    }
}
