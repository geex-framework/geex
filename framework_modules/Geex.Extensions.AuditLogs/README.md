# 审计日志

服务启动时从 `AuditLogsModuleOptions` 配置节读取默认审计级别:

```json
{
  "AuditLogsModuleOptions": {
    "DefaultAuditLevel": "None",
    "RetentionDays": 365
  }
}
```

`None` 仅记录单独开启的操作, `MutationOnly` 默认记录 Mutation, `All` 默认记录 Query/Mutation/Subscription. 未配置时使用 `None`. 修改配置后重启服务生效.

配置优先级为字段 > 所属操作扩展类型 > 全局默认. 类型和字段配置的声明先后顺序不影响优先级.

```csharp
protected override void Configure(IObjectTypeDescriptor<OrderMutation> descriptor)
{
    descriptor.AuditFieldsImplicitly(AuditLevel.MutationOnly);
    descriptor.Field(x => x.SaveOrder(default!)).Audit();
    descriptor.Field(x => x.PreviewOrder(default!)).Audit(false);
    base.Configure(descriptor);
}
```

无参数 `AuditFieldsImplicitly()` 表示该类型采用 `All`, `.Audit()` 表示单个字段开启. 自动生成的审批/删除字段采用其实现类型的配置, 也可按生成后的字段名使用 `.Audit(false)` 或 `.Audit()` 单独配置. `authenticate`/`federateAuthenticate`/`cancelAuthentication` 具有内置的字段级开启配置, 显式字段配置可以覆盖.

`auditLogs` 使用已有审计查询权限和租户过滤. `filter` 支持 ID/操作名/操作类型/操作人/成功状态/客户端 IP/租户/创建时间, `sort` 支持排序, `skip`/`take` 控制分页. 详情通过 `filter: { id: { eq: id } }` 查询, 按需选择 `operation`/`variables`/`result` 等字段.
