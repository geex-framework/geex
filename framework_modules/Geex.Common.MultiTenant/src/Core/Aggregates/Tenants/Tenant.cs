using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Geex.Common.Abstraction.MultiTenant;
using Geex.Common.Abstraction.Storage;
using Geex.Common.MultiTenant.Api.Aggregates.Tenants;


namespace Geex.Common.MultiTenant.Core.Aggregates.Tenants
{
    public class Tenant : Entity<Tenant>, ITenant
    {
        public string Code { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }
        public bool IsEnabled { get; set; }

        internal static Tenant Create(string code, string name, JsonNode externalInfo = default)
        {
            return new Tenant()
            {
                Code = code,
                Name = name,
                IsEnabled = true,
                ExternalInfo = externalInfo ?? JsonNode.Parse("{}")
            };
        }

        public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
        {
            return ValidationResult.Success;
        }

        /// <inheritdoc />
        public JsonNode? ExternalInfo { get; set; }
    }
}
