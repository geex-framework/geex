# 审批流

流程模板配置流程字段/节点字段/默认审批人/抄送人及流程分类. 创建实例时复制模板快照, 同时确定关联实体. 流程分类配置依赖 Settings 模块, 通过 `ApprovalFlowSettings.Categories` 保存分类列表.

## 表单与办理

- 字段类型: Text/MultilineText/Select/MultiSelect/Date/User/File. 文件值为 BlobStorage 文件 ID, 日期值为 yyyy-MM-dd, 多选及文件保存多个值.
- 流程字段的 `AllowEdit` 控制办理期间是否可修改. 创建时填写完整流程表单; 办理期间仅发起人可以更新允许修改的字段.
- 节点表单由该节点审批人填写, 支持未来节点提前填写, 节点通过后只读. 节点通过时校验必填字段.
- 流程结束后表单只读. 多人征询保存每个人的请求和回复, 全部回复后继续办理. 转交和驳回复制节点表单快照及已有值.
- 详情集中展示节点表单/抄送/征询和流程日志. 当前节点默认展开.

## 业务集成

通过 `AssociatedEntityType` 注册业务实体和取消回调. 实例创建时传入实体类型及 ID; 开始和完成时分别调用实体的上报和审批方法. `OnCancel` 实现业务取消上报.

关联对象只保留一个非归档流程. 取消后重新发起时允许选择新模板, 新流程创建与旧流程归档在同一事务中完成. `ApprovalFlowAssociation` 使用对象键及前序流程 ID 的唯一索引约束首次创建和后续重报的竞争. 流程/节点保存时同时校验版本, 数据库需要支持事务.

前端 `StartApprovalFlowService.open(context)` 接受标题/提示/实体上下文/预选模板及创建回调. 弹窗内完成模板筛选/表单填写/创建结果, 按模板保留返回上一步的草稿. 业务页面通过 `approvalFlowByEntity` 查询唯一非归档流程, 然后进入审批事项详情.

PIMS 合同集成及验收边界见 PIMS 的 `docs/requirements/baselines/审批流扩展与合同接入.md` 和 `M-DEV-AF-001` 记录.
