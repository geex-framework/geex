using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Gql.Inputs;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.Abstractions;
using Geex.Common.Gql.Types;
using Geex.Common.Settings.Abstraction;
using Geex.Common.Settings.Api.Aggregates.Settings;
using Geex.Common.Settings.Api.Aggregates.Settings.Inputs;
using Geex.Common.Settings.Api.GqlSchemas.Types;
using Geex.Common.Settings.Core;

using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

using MediatR;

using MongoDB.Entities;

namespace Geex.Common.Settings.Api
{
    public class SettingQuery : QueryExtension<SettingQuery>
    {
        private readonly IMediator _mediator;

        public SettingQuery(IMediator mediator)
        {
            this._mediator = mediator;
        }

        protected override void Configure(IObjectTypeDescriptor<SettingQuery> descriptor)
        {
            descriptor.AuthorizeWithDefaultName();
            descriptor.Field(x => x.Settings(default))
                .Authorize()
            .UseOffsetPaging<SettingGqlType>()
            .UseFiltering<ISetting>(x =>
            {
                x.BindFieldsExplicitly();
                x.Field(y => y.Id);
                x.Field(y => y.Name).Type<EnumOperationFilterInputType<SettingDefinition>>();
                x.Field(y => y.Scope).Type<EnumOperationFilterInputType<SettingScopeEnumeration>>();
                //x.Field(y => y.Name).Type<StringOperationFilterInputType>();
                //x.Field(y => y.Scope).Type<StringOperationFilterInputType>();
                x.Field(y => y.ScopedKey);
            })
            ;
            base.Configure(descriptor);
        }
        /// <summary>
        /// 根据provider获取全量设置
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<IQueryable<ISetting>> Settings(GetSettingsInput input)
        {
            return await _mediator.Send(input);
        }

        /// <summary>
        /// 获取初始化应用所需的settings
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public async Task<List<ISetting>> InitSettings()
        {
            return await _mediator.Send(new GetInitSettingsInput());
        }
    }
}
