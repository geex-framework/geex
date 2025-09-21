using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Geex.Extensions.Messaging;
using Geex.Extensions.Messaging.ClientNotification;
using Geex.Notifications;
using Geex.Requests;
using HotChocolate.Subscriptions;

using MediatX;

using MongoDB.Entities;


namespace Geex.Extensions.Identity.Core.Handlers
{
    public class OrgHandler :
        IRequestHandler<QueryRequest<IOrg>, IQueryable<IOrg>>,
        IRequestHandler<CreateOrgRequest, IOrg>,
        IRequestHandler<UpdateOrgRequest, IOrg>,
        IRequestHandler<MoveOrgRequest, bool>,
        IRequestHandler<ImportOrgRequest, IEnumerable<IOrg>>,
        IRequestHandler<FixUserOrgRequest, bool>,
        IEventHandler<OrgCodeChangedEvent>,
        IEventHandler<EntityCreatedEvent<IOrg>>,
        IEventHandler<EntityDeletedEvent<IOrg>>
    {
        private readonly ITopicEventSender _topicEventSender;
        public IUnitOfWork Uow { get; }

        public OrgHandler(IUnitOfWork uow, ITopicEventSender topicEventSender)
        {
            _topicEventSender = topicEventSender;
            Uow = uow;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IQueryable<IOrg>> Handle(QueryRequest<IOrg> request, CancellationToken cancellationToken)
        {
            return Uow.Query<IOrg>().WhereIf(request.Filter != default, request.Filter);
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IOrg> Handle(CreateOrgRequest request, CancellationToken cancellationToken)
        {
            var entity = new Org(request.Code, request.Name, request.OrgType);
            Uow.Attach(entity);
            var userId = request.CreateUserId;
            // 区域创建者自动拥有Org权限
            if (!userId.IsNullOrEmpty())
            {
                var user = await Uow.Query<User>().OneAsync(userId, cancellationToken: cancellationToken);
                await user.AddOrg(entity);
            }

            // 拥有上级Org权限的用户自动获得新增子Org的权限
            var upperUsers = Uow.Query<User>().Where(x => x.OrgCodes.Contains(entity.ParentOrgCode)).ToList();
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
                using var _ = (Uow as DbContext).DisableAllDataFilters();
                var userList = Uow.Query<User>().ToList();
                foreach (var user in userList.Where(x => !x.OrgCodes.Any()))
                {
                    if (user.TenantCode.IsNullOrWhiteSpace())
                    {
                        continue;
                    }

                    var entity = Uow.Query<IOrg>()
                        .FirstOrDefault(x => x.TenantCode == user.TenantCode && x.Name == "集团总部");
                    if (entity is null)
                    {
                        entity = new Org(user.TenantCode, "公司总部", OrgTypeEnum.Default)
                        {
                            TenantCode = user.TenantCode
                        };
                        Uow.Attach(entity);
                    }
                    await user.AddOrg(entity);
                    // 拥有上级Org权限的用户自动获得新增子Org的权限
                    var upperUsers = Uow.Query<User>().Where(x => x.OrgCodes.Contains(entity.ParentOrgCode)).ToList();
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

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IOrg> Handle(UpdateOrgRequest request, CancellationToken cancellationToken)
        {
            var org = await Uow.Query<Org>().OneAsync(request.Id, cancellationToken: cancellationToken);
            org.UpdateOrg(request.Name, request.Code, request.OrgType);
            return org;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<bool> Handle(MoveOrgRequest request, CancellationToken cancellationToken)
        {
            var org = await Uow.Query<Org>().OneAsync(request.Id, cancellationToken: cancellationToken);
            org.MoveToParent(request.NewParentOrgCode);
            return true;
        }

        /// <summary>Handles a request</summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response from the request</returns>
        public virtual async Task<IEnumerable<IOrg>> Handle(ImportOrgRequest request, CancellationToken cancellationToken)
        {
            var createdOrgs = new List<IOrg>();
            
            foreach (var orgItem in request.OrgItems)
            {
                // 检查组织是否已存在
                var existingOrg = Uow.Query<Org>().FirstOrDefault(x => x.Code == orgItem.Code);
                if (existingOrg != null)
                {
                    // 更新现有组织
                    existingOrg.UpdateOrg(orgItem.Name, orgItem.Code, orgItem.OrgType);
                    createdOrgs.Add(existingOrg);
                }
                else
                {
                    // 创建新组织
                    var newOrg = new Org(orgItem.Code, orgItem.Name, orgItem.OrgType);
                    Uow.Attach(newOrg);
                    
                    // 如果指定了父组织，则调整编码
                    if (!orgItem.ParentOrgCode.IsNullOrEmpty())
                    {
                        newOrg.MoveToParent(orgItem.ParentOrgCode);
                    }
                    
                    createdOrgs.Add(newOrg);
                }
            }
            
            return createdOrgs;
        }

        /// <inheritdoc />
        public virtual async Task Handle(OrgCodeChangedEvent notification, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(DataChangeType.Org);
        }

        /// <inheritdoc />
        public virtual async Task Handle(EntityCreatedEvent<IOrg> @event, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(DataChangeType.Org);
        }

        /// <inheritdoc />
        public virtual async Task Handle(EntityDeletedEvent<IOrg> @event, CancellationToken cancellationToken)
        {
            await this.NotifyCacheDataChange(DataChangeType.Org);
        }

        private async Task NotifyCacheDataChange(DataChangeType type)
        {
            await Uow.ClientNotify(new DataChangeClientNotify(type));
        }
    }
}
