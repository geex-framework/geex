# 通用 Entity 收藏

收藏属于当前登录用户, 按当前租户隔离. `ApprovalFlowModule` 默认依赖 `FavoritesModule`, 并注册审批流的收藏类型. Angular 的 `provideGeexApprovalFlows()` 同时注册收藏服务.

## 数据与配置

`Favorite` 保存 `TenantCode` / `UserId` / `EntityType` / `EntityId` / `Title` / `CreatedOn`. 前四个字段构成唯一索引, 宿主租户存储为空字符串. 首次添加时保存标题和时间. 重复添加保留首次快照, 取消后重新收藏会重新取值. 升级前没有标题快照的收藏显示实体 ID.

Setting 的持久化键和 GraphQL 枚举值均为 `Favorites_EntityTypes`. 支持 Global / Tenant, 租户配置整项覆盖全局配置. 个人中心按 JSON 数组顺序展示 tab.

```json
[
  {
    "EntityType": "ApprovalFlow",
    "NameField": "Name",
    "DetailRoute": "/approval-flows/inbox/{id}"
  }
]
```

`NameField` 使用公开的直接映射字符串属性名. 涉及自定义展示或字段权限时, 由业务注册相同的授权取值逻辑. 读取目标或标题被拒绝时, 后端记录一条错误日志并保存 ID. 可读取的空标题也使用 ID. `DetailRoute` 可省略或设为 null, 此时标题显示为普通文本. 路径以单个 `/` 开始, 支持 `{id}` 占位符.

配置为 `[]` 时, 公共按钮和个人中心的"我的收藏"随之隐藏. 禁用类型保留原记录. 保存设置或恢复继承后, 下次进入审批页或个人中心时重新读取配置. 配置中失效的单个条目会被跳过并记录日志, 修正后恢复原收藏. 租户设置编辑页的"恢复继承全局"会删除该租户的覆盖值.

## GraphQL

全部接口要求登录, 用户 ID 和租户由服务端上下文决定.

| 接口 | 用途 |
| --- | --- |
| `favoriteEntityTypes` | 返回当前有效的类型/显示字段/详情路径配置 |
| `favoriteStatuses(entityType, entityIds)` | 批量查询收藏状态及原收藏时间, 最多 500 个 ID |
| `favorites(entityType, filter, sort, skip, take)` | 当前用户在该类型下的收藏, 返回 `items` / `totalCount` / `pageInfo` |
| `addFavorite(request)` | 添加收藏并返回 `FavoriteStatus`. 已收藏时返回首次收藏时间 |
| `removeFavorite(request)` | 幂等取消, 返回 Boolean |

并发请求同时首次添加同一条收藏时, 联合唯一索引保证只保存一条记录. 遇到唯一键冲突的调用方可重新发起 `addFavorite`, 读取已提交的收藏状态和时间.

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

个人中心直接按当前用户/租户/类型查询收藏记录, 按收藏时间筛选和排序后分页. 目标改名/删除或业务权限收回时, 原标题和数量保持可见, 用户可继续取消收藏. 点击标题时, 详情页执行当前业务权限和存在性检查.

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

在应用已加载的模块程序集内注册 Enumeration 成员.

```csharp
public sealed class ApplicationFavoriteEntityType(string value, Type entityType)
    : FavoriteEntityType(value, entityType)
{
    public static FavoriteEntityType Document { get; } = new ApplicationFavoriteEntityType("Document", typeof(Document));
}
```

自定义标题字段可在枚举构造函数中调用 `RegisterTitleField(name, readAuthorizedTitle)`. 委托接收工作单元/目标实体/取消令牌, 复用详情的业务取值方法, 返回 `null` 表示该字段不可读.

随后在 Setting 中添加 `Document` 的字段和路由, 并添加 `Favorites.entityTypes.Document` 翻译. 类型值应为稳定的 GraphQL 枚举标识符. 业务读权限若没有实现为实体数据过滤器, 应在该注册类型重写 `Query(IUnitOfWork, string[])` 并追加访问约束.

## 验证

后端行为用例位于 `Geex.Tests/FeatureTests/Favorites.Tests.cs`, 使用本机独立 MongoDB 副本集和 Redis. 构建 `Geex.Tests.csproj` 成功后, 设置 `GEEX_APPROVAL_TEST_MONGO` / `GEEX_APPROVAL_TEST_REDIS_PORT`, 运行 `FullyQualifiedName~FavoritesTests` 筛选. 数据库名必须以 `geex_approval_` 开始. `GEEX_FAVORITES_TEST_EVIDENCE` 可指定 schema 导出目录.

前端先构建 `geex-angular` 和 `geex-extensions-favorites`, 再运行收藏包的 `pnpm test`. 设置 `GEEX_FAVORITES_SCHEMA` 可校验所有收藏操作与导出的真实 schema. Schematics 直接运行 JavaScript, 使用其 `pnpm test`.
