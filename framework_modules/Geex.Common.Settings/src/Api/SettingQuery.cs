using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geex.Common.Abstraction;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstraction.Settings;
using Geex.Common.Settings.Core;
using Geex.Common.Requests.Settings;
using Geex.Common.Settings.Abstraction;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

using MediatR;

namespace Geex.Common.Settings.Api
{
    public sealed class SettingQuery : QueryExtension<SettingQuery>
    {
        /// <inheritdoc />
        protected override void Configure(IObjectTypeDescriptor<SettingQuery> descriptor)
        {
            descriptor.Field(x => x.Settings(default))
                .UseOffsetPaging<ObjectType<Setting>>()
                .UseFiltering<ISetting>(x =>
                {
                    x.BindFieldsExplicitly();
                    x.Field(y => y.Id);
                    x.Field(y => y.Name).Type<EnumOperationFilterInputType<SettingDefinition>>();
                    x.Field(y => y.Scope).Type<EnumOperationFilterInputType<SettingScopeEnumeration>>();
                    x.Field(y => y.ScopedKey);
                });
            base.Configure(descriptor);
        }

        private readonly IUnitOfWork _uow;

        public SettingQuery(IUnitOfWork uow)
        {
            this._uow = uow;
        }

        /// <summary>
        /// 根据provider获取全量设置
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<ISetting>> Settings(GetSettingsRequest request)
        {
            return await _uow.Request(request);
        }

        /// <summary>
        /// 获取初始化应用所需的settings
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<List<ISetting>> InitSettings()
        {
            return await _uow.Request(new GetInitSettingsRequest());
        }
    }
}
