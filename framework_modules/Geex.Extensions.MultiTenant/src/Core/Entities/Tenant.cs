using System;
using Geex.Validation;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Requests.MultiTenant;
using Geex.MultiTenant;
using Geex.Storage;

using HotChocolate.Types;

using MongoDB.Bson.Serialization;

namespace Geex.Extensions.MultiTenant.Core.Aggregates.Tenants
{
    public class Tenant : Entity<Tenant>, ITenant
    {
        public Tenant(CreateTenantRequest request, IUnitOfWork uow = default)
        {
            Code = request.Code;
            Name = request.Name;
            ExternalInfo = request.ExternalInfo;
            IsEnabled = true;
            uow?.Attach(this);
        }

        public string Code { get; set; }
        /// <inheritdoc />
        public string Name { get; set; }
        public bool IsEnabled { get; set; }

        public override async Task<ValidationResult> Validate(CancellationToken cancellation = default)
        {
            if (this.Code.IsNullOrEmpty())
            {
                return new ValidationResult("Invalid tenant code", [nameof(Code)]);
            }

            if (this.Name.IsNullOrEmpty())
            {
                return new ValidationResult("Invalid tenant name", [nameof(Name)]);
            }

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
