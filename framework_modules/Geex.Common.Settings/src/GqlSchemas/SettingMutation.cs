using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Requests.Settings;
using HotChocolate.Types;
using Geex.Common.Settings.Aggregates;

namespace Geex.Common.Settings.GqlSchemas
{
    public sealed class SettingMutation : MutationExtension<SettingMutation>
    {
        protected override void Configure(IObjectTypeDescriptor<SettingMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
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
    }
}
