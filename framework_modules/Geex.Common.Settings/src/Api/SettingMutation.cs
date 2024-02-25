using System.Threading.Tasks;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Settings.Api.Aggregates.Settings;
using Geex.Common.Requests.Settings;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Settings.Api
{
    public class SettingMutation : MutationExtension<SettingMutation>
    {
        private readonly IMediator _mediator;

        public SettingMutation(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<SettingMutation> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            base.Configure(descriptor);
        }

        /// <summary>
        /// 更新设置
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ISetting> EditSetting(EditSettingRequest request)
        {
            return await _mediator.Send(request);
        }
    }
}
