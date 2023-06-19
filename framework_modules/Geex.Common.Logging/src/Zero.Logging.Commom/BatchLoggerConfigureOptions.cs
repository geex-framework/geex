﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GeexBox.ElasticSearch.Zero.Logging.Commom
{
    public class BatchLoggerConfigureOptions : IConfigureOptions<BatchingLoggerOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly string _isEnabledKey;

        public BatchLoggerConfigureOptions(IConfiguration configuration, string isEnabledKey)
        {
            _configuration = configuration;
            _isEnabledKey = isEnabledKey;
        }

        public void Configure(BatchingLoggerOptions options)
        {
            options.IsEnabled = TextToBoolean(_configuration.GetSection(_isEnabledKey)?.Value);
        }

        private static bool TextToBoolean(string text)
        {
            if (string.IsNullOrEmpty(text) ||
                !bool.TryParse(text, out var result))
            {
                result = false;
            }

            return result;
        }
    }
}
