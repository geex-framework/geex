using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Geex.Common.Abstraction.Entities;
using Geex.Common.Abstraction.Enumerations;
using Geex.Common.Notifications;
using Geex.Common.Requests;
using Geex.Common.Abstractions;
using Geex.Common.Identity.Api.Aggregates.Orgs.Events;
using Geex.Common.Identity.Core.Aggregates.Orgs;
using Geex.Common.Identity.Core.Aggregates.Users;
using Geex.Common.Requests.Identity;
using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;
using Geex.Common.Messaging.Api.GqlSchemas.Messages;
using Geex.Common.Messaging.Core.Aggregates.FrontendCalls;

using HotChocolate.Subscriptions;

using MediatR;

using MongoDB.Entities;


namespace Geex.Common.Identity.Core.Handlers
{
    public class OrgHandler :
        IRequestHandler<QueryRequest<IOrg>, IQueryable<IOrg>>,
        IRequestHandler<CreateOrgRequest, IOrg>,
        IRequestHandler<FixUserOrgRequest, bool>,
        INotificationHandler<OrgCodeChangedEvent>,
        INotificationHandler<EntityCreatedNotification<IOrg>>,
        INotificationHandler<EntityDeletedNotification<IOrg>>
    {
        private readonly ITopicEventSender _topicEventSender;
        public IUnitOfWork DbContext { get; }

        public OrgHandler(IUnitOfWork dbContext, ITopicEventSender topicEventSender)
        {
            _topicEventSender = topicEventSender;
            DbContext = dbContext;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IQueryable<IOrg>> Handle(QueryRequest<IOrg> request, CancellationToken cancellationToken)
        {
            return DbContext.Query<IOrg>().WhereIf(request.Filter != default, request.Filter);
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IOrg> Handle(CreateOrgRequest request, CancellationToken cancellationToken)
        {
            var entity = new Org(request.Code, request.Name, request.OrgType);
            DbContext.Attach(entity);
            var userId = request.CreateUserId;
            // 区域创建者自动拥有Org权限
            if (!userId.IsNullOrEmpty())
            {
                var user = await DbContext.Query<User>().OneAsync(userId, cancellationToken: cancellationToken);
                await user.AddOrg(entity);
            }

            // 拥有上级Org权限的用户自动获得新增子Org的权限
            var upperUsers = DbContext.Query<User>().Where(x => x.OrgCodes.Contains(entity.ParentOrgCode)).ToList();
            foreach (var upperUser in upperUsers)
            {
                await upperUser.AddOrg(entity);
            }

            return entity;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<bool> Handle(FixUserOrgRequest request, CancellationToken cancellationToken)
        {
            try
            {
                using var _ = (DbContext as DbContext).DisableAllDataFilters();
                var userList = DbContext.Query<User>().ToList();
                foreach (var user in userList.Where(x => !x.OrgCodes.Any()))
                {
                    if (user.TenantCode.IsNullOrWhiteSpace())
                    {
                        continue;
                    }

                    var entity = DbContext.Query<IOrg>()
                        .FirstOrDefault(x => x.TenantCode == user.TenantCode && x.Name == "集团总部");
                    if (entity is null)
                    {
                        entity = new Org(user.TenantCode, "公司总部", OrgTypeEnum.Default)
                        {
                            TenantCode = user.TenantCode
                        };
                        DbContext.Attach(entity);
                    }
                    await user.AddOrg(entity);
                    // 拥有上级Org权限的用户自动获得新增子Org的权限
                    var upperUsers = DbContext.Query<User>().Where(x => x.OrgCodes.Contains(entity.ParentOrgCode)).ToList();
                    foreach (var upperUser in upperUsers)
                    {
                        await upperUser.AddOrg(entity);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return true;
        }

        /// <inheritdoc />
        public virtual async Task Handle(OrgCodeChangedEvent notification, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(ChangeDetectDataType.Org);
        }

        /// <inheritdoc />
        public virtual async Task Handle(EntityCreatedNotification<IOrg> notification, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(ChangeDetectDataType.Org);
        }

        /// <inheritdoc />
        public virtual async Task Handle(EntityDeletedNotification<IOrg> notification, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(ChangeDetectDataType.Org);
        }

        private async Task NotifyCacheDataChange(ChangeDetectDataType type)
        {
            // bug:这里的type无法正常序列化为枚举, 暂时toString
            await this._topicEventSender.SendAsync<IFrontendCall>(nameof(MessageSubscription.OnBroadcast), new FrontendCall(FrontendCallType.DataChange, JsonSerializer.SerializeToNode(new { Type = type.ToString() })));
        }
    }
}
