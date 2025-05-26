using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using Geex.Abstractions;
using Geex.MultiTenant;
using Geex.Storage;
using HotChocolate.Types;
using MongoDB.Bson.Serialization;

namespace Geex.Extensions.MultiTenant.Core.Aggregates.Tenants
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
                ExternalInfo = externalInfo
            };
        }

        public override async Task<ValidationResult> Validate(IServiceProvider sp, CancellationToken cancellation = default)
        {
            return ValidationResult.Success;
        }

        /// <inheritdoc />
        public JsonNode? ExternalInfo { get; set; }

        public class TenantBsonConfig : BsonConfig<Tenant>
        {
            protected override void Map(BsonClassMap<Tenant> map, BsonIndexConfig<Tenant> indexConfig)
            {
                map.Inherit<ITenant>();
                map.AutoMap();
                indexConfig.MapEntityDefaultIndex();
                indexConfig.MapIndex(builder => builder.Ascending(x => x.Code), options =>
                {
                    options.Background = true;
                    options.Sparse = true;
                    options.Unique = true;

                });
            }
        }
        public class TenantGqlConfig : GqlConfig.Object<Tenant>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<Tenant> descriptor)
            {
                descriptor.BindFieldsImplicitly();
                descriptor.Implements<InterfaceType<ITenant>>();
            }
        }
    }
}
