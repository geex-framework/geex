using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Geex.Extensions.Logging.Elasticsearch
{
    public class EsLoggerOptionsSetup : ConfigureFromConfigurationOptions<EsLoggerOptions>
    {
        public EsLoggerOptionsSetup(ILoggerProviderConfiguration<EsLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        {

        }
    }
}
