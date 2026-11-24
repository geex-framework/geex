# 通用 Entity 收藏

收藏属于当前登录用户, 按当前租户隔离. `ApprovalFlowModule` 默认依赖 `FavoritesModule`, 并注册审批流的收藏类型. Angular 的 `provideGeexApprovalFlows()` 同时注册收藏服务.

## 数据与配置

`Favorite` 保存 `TenantCode` / `UserId` / `EntityType` / `EntityId` / `CreatedOn`. 唯一索引为前四个字段的组合, 宿主租户统一存储为空字符串. 标题不入库, 列表读取目标实体当前的显示字段. 重复添加保留原收藏时间, 取消后重新添加生成新的收藏时间.

Setting 的持久化键和 GraphQL 枚举值均为 `Favorites_EntityTypes`. 支持 Global / Tenant, 租户配置整项覆盖全局配置. JSON 数组顺序决定个人中心的 tab 顺序:

```json
[
  {
    "EntityType": "ApprovalFlow",
    "NameField": "Name",
    "DetailRoute": "/approval-flows/inbox/{id}"
  }
]
```

`NameField` 使用 C# 属性名, 必须是 GraphQL 公开可读/没有字段授权指令的顶层字符串属性. 字段为空时展示实体 ID. `DetailRoute` 可省略或设为 null, 此时名称展示为普通文本. 路径必须以单个 `/` 开始, 支持 `{id}` 占位符.

有效配置为 `[]` 时, 公共按钮和个人中心 section 自动隐藏. 禁用类型保留原收藏记录, 重新启用后恢复展示. 无访问权限的目标暂时隐藏, 真实删除目标通过 `EntityDeletedEvent<T>` 清理收藏引用. 应用应使用工作单元删除 API 触发实体生命周期事件; 直接操作数据库需要应用自行处理引用清理.

## GraphQL

全部接口要求登录, 用户 ID 和租户由服务端上下文决定.

| 接口 | 用途 |
| --- | --- |
| `favoriteEntityTypes` | 返回当前有效的类型/显示字段/详情路径配置 |
| `favoriteStatuses(entityType, entityIds)` | 批量查询收藏状态及原收藏时间, 最多 500 个 ID |
| `favorites(entityType, filter, sort, skip, take)` | 当前用户的可见收藏, 返回 `items` / `totalCount` / `pageInfo` |
| `addFavorite(request)` | 幂等添加, 返回 `FavoriteStatus` |
| `removeFavorite(request)` | 幂等取消, 返回 Boolean |

```graphql
mutation {
  addFavorite(request: { entityType: ApprovalFlow, entityId: "ENTITY_ID" }) {
    entityType entityId isFavorite createdOn
  }
}

query {
  favorites(
    entityType: ApprovalFlow
    filter: { createdOn: { gte: "2026-11-01T00:00:00Z", lt: "2026-12-01T00:00:00Z" } }
    sort: [{ createdOn: descend }, { id: descend }]
    skip: 0
    take: 10
  ) {
    totalCount
    items { id entityType entityId createdOn displayValue detailUrl }
    pageInfo { hasPreviousPage hasNextPage }
  }
}
```

目标实体查询保留工作单元的数据过滤器, 收藏服务额外校验目标的租户. 当前列表实现批量读取当前用户/类型的收藏目标, 在可见集合上应用过滤/排序后分页, 因此 `totalCount` 不包含无权访问的目标. 收藏量很大的应用应评估这一批量读取成本.

## Angular 与 Schematics

安装 `@geexcode/geex-extensions-favorites`. 未使用审批流模块的应用可单独调用 `provideGeexFavorites()`. 在列表/详情中导入同一个 standalone 组件:

```html
<geex-favorite-button entityType="ApprovalFlow" [entityId]="entity.id" appearance="link" />
```

个人中心使用 `FavoritesSectionComponent` / `<geex-favorites-section />`. 类型为空时该组件不渲染 section. 列表使用原生 ST 日期范围过滤和排序, 默认按收藏时间倒序, 各 tab 独立保留筛选/排序/分页状态. 日期范围按浏览器本地日历日转换为 UTC 起始时刻和结束日期的次日零点.

`FavoritesService` 负责同一轮渲染的状态请求合并/共享状态/防止重复点击/会话失效时清理缓存. Settings 保存后刷新有效配置. 应用直接修改配置时可调用 `geex.favorites.refresh()`.

```powershell
pnpm geex add favorites
pnpm geex sync identity
pnpm geex sync approval-flows
pnpm geex sync settings
pnpm install
```

`add favorites` 安装 I18n 配置并注册到模块字典. `sync` 默认保留已有文件, 更新已安装模板时应按项目自己的改动合并对应文件. `identity` / `approval-flows` 模板声明公共组件, 安装时补充收藏包依赖.

类型 tab 标签取前端 `Favorites.entityTypes[EntityType]`. Schematics 的字典默认定义 `ApprovalFlow`, 应用添加新类型时扩展 zh-CN / en-US 字典和 Setting 即可.

## 应用扩展类型

在应用已加载的模块程序集内注册 Enumeration 成员:

```csharp
public sealed class ApplicationFavoriteEntityType(string value, Type entityType)
    : FavoriteEntityType(value, entityType)
{
    public static FavoriteEntityType Document { get; } = new ApplicationFavoriteEntityType("Document", typeof(Document));
}
```

随后在 Setting 中添加 `Document` 的字段和路由, 并添加 `Favorites.entityTypes.Document` 翻译. 类型值应为稳定的 GraphQL 枚举标识符. 业务读权限若没有实现为实体数据过滤器, 应在该注册类型重写 `Query(IUnitOfWork, string[])` 并追加访问约束.

## 验证

后端行为用例位于 `Geex.Tests/FeatureTests/Favorites.Tests.cs`, 使用本机独立 MongoDB 副本集和 Redis. 构建 `Geex.Tests.csproj` 成功后, 设置 `GEEX_APPROVAL_TEST_MONGO` / `GEEX_APPROVAL_TEST_REDIS_PORT`, 运行 `FullyQualifiedName~FavoritesTests` 筛选. 数据库名必须以 `geex_approval_` 开始. `GEEX_FAVORITES_TEST_EVIDENCE` 可指定 schema 导出目录.

前端先构建 `geex-angular` 和 `geex-extensions-favorites`, 再运行收藏包的 `pnpm test`. 设置 `GEEX_FAVORITES_SCHEMA` 可校验所有收藏操作与导出的真实 schema. Schematics 直接运行 JavaScript, 使用其 `pnpm test`.
