using System.Threading.Tasks;
using Geex.Extensions.Settings.Requests;
using System.Threading;
using Geex.Gql.Types;
using HotChocolate.Types;

namespace Geex.Extensions.Settings.Gql
{
    public sealed class SettingMutation : MutationExtension<SettingMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<SettingMutation> descriptor)
        {
            descriptor.Field(mutation => mutation.EditSetting(default!)).Authorize(SettingsPermission.Edit);
            descriptor.Field(mutation => mutation.RestoreTenantSettingInheritance(default!, default, default)).Authorize(SettingsPermission.Edit);
            base.Configure(descriptor);
        }
        private readonly IUnitOfWork _uow;

        public SettingMutation(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        /// <summary>
        /// 更新设置
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ISetting> EditSetting(EditSettingRequest request) => await _uow.Request(request);

        public Task<bool> RestoreTenantSettingInheritance(SettingDefinition name, string? tenantCode, CancellationToken cancellationToken)
            => _uow.GetSettingService()!.RestoreTenantInheritanceAsync(name, tenantCode, cancellationToken);
    }
}
