# 审批流

流程模板配置流程字段/节点字段/默认审批人/抄送人及流程分类. 创建实例时复制模板快照, 同时确定关联实体. 流程分类配置依赖 Settings 模块, 通过 `ApprovalFlowSettings.Tags` 保存分类列表.

## 表单与办理

- 字段类型: Text/MultilineText/Select/MultiSelect/Date/User/File. 文件值为 BlobStorage 文件 ID, 日期值为 yyyy-MM-dd, 多选及文件保存多个值.
- 流程字段的 `AllowEdit` 控制办理期间是否可修改. 创建时填写完整流程表单; 办理期间仅发起人可以更新允许修改的字段.
- 节点表单由该节点审批人填写, 支持未来节点提前填写, 节点通过后只读. 节点通过时校验必填字段.
- 流程结束后表单只读. 多人征询保存每个人的请求和回复, 全部回复后继续办理. 转交和驳回复制节点表单快照及已有值.
- 流程状态为 `Processing`/`Finished`/`Closed`, 分别表示审批中/正常完成/主动关闭. `ArchivedAt` 独立记录归档时间, 归档保留流程结果.
- 当前激活且已开始节点的实际审批人可调用 `closeApprovalFlow(nodeId)` 关闭流程, 征询期间也可关闭. 关闭权限由节点审批人身份确定. 关闭后状态为 `Closed` 并立即归档, 保留表单/征询/日志快照, 通知显示已关闭并归档.
- 详情集中展示节点表单/抄送/征询和流程日志. 当前节点默认展开.

## 业务集成

通过 `AssociatedEntityType` 注册业务实体及回调. 实例创建时传入实体类型及 ID, `ResolveOrgCode` 从业务对象确定组织范围; 开始时调用实体的上报方法. 终审在同一数据库事务中调用 `OnApprove`, 默认执行实体审批, 业务扩展可在此同步执行必须成功的关联处理. 回调失败时返回错误并回滚.

关闭调用 `OnClose`, 默认取消关联实体上报并发布对应业务类型的取消上报事件, 流程通过 `ApprovalFlowClosedEvent` 通知订阅方. 关联实体必须处于已上报状态. 关闭需要数据库事务支持, 回调在事务开启后执行; 保存时领域事件必须成功, 事件或保存失败时回滚, 该工作单元再次保存会被拒绝, 重试须使用新的工作单元.

关联对象只保留一个尚未被替代的流程. 状态为 `Closed` 且尚未关联替代流程时可重新发起, 并允许选择新模板. 新流程创建与旧流程关联在同一事务中完成, 旧流程保留 `Closed` 状态, 保留已有归档时间或在衔接时填写归档时间, 并记录替代流程. `ApprovalFlowAssociation` 使用对象键及前序流程 ID 的唯一索引约束首次创建和后续重报的竞争. 流程/节点保存时同时校验版本, 数据库需要支持事务.

参与人须为启用用户, 普通用户按当前租户及流程组织校验. 内置 host 管理员按固定 ID 查询, 查询期间临时关闭租户过滤, 完成后恢复原过滤范围.

前端 `StartApprovalFlowService.open(context)` 接受标题/提示/实体上下文/预选模板及创建回调. 弹窗内完成模板筛选/表单填写/创建结果, 按模板保留返回上一步的草稿. 业务页面通过 `approvalFlowByEntity` 查询 `ArchivedAt` 为空的流程, 然后进入审批事项详情. 审批事项列表通过 `QueryApprovalFlowRequest.Status` 和 `Archived` 分别筛选流程结果与归档情况.

数据库迁移 `639288672498274456` 统一已有终止记录的状态存储, 保留归档时间/替代关系/操作人及日志. 启用 `GeexCoreModuleOptions.AutoMigration` 的宿主在初始化时执行; 手动管理迁移的环境需在启用本版接口前执行该迁移.

开发验证使用 `Geex.Tests` 中的 `ApprovalFlowLifecycleTests`. 运行前构建测试项目, 将 `GEEX_APPROVAL_TEST_MONGO` 指向独立的本地 MongoDB 副本集数据库 (库名以 `geex_approval_` 开头), 将 `GEEX_APPROVAL_TEST_REDIS_PORT` 指向独立 Redis, 并将 `ASPNETCORE_TEST_CONTENTROOT_GEEX_TESTS` 设为测试构建输出目录. `GEEX_APPROVAL_TEST_EVIDENCE` 可指定接口请求/响应记录目录. 测试数据由该独立环境的创建方统一回收, 单次验证结果保存在 `.test-evidence/<runId>/`.

PIMS 合同集成及验收边界见 PIMS 的 `docs/requirements/baselines/审批流扩展与合同接入.md` 和 `M-DEV-AF-001` 记录.
