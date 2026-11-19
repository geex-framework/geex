# BlobStorage

BlobStorage 提供 Db/FileSystem/Cache 内容存储及 Blob 资源创建/查询/删除. 前端附件流程通过 `@geexcode/geex-extensions-blob-storage` 的 `attachBlob(storage, content, fileName, storageType?)` 查找可复用资源.

## 附加附件

表单上传 widget 和独立上传组件的默认附件流程使用 `attachBlob`. 对 Db/FileSystem, 以下五项全部相同且未过期时直接返回已有 Blob 的 ID/URL, 不发送上传请求:

1. 附件文件名.
2. 实际待上传内容的 MD5.
3. 实际待上传内容的大小.
4. MIME, 去除首尾空格并统一小写.
5. 存储类型.

查询按文件名/MD5/大小/存储类型筛选, 分页检查候选记录的 MIME 和过期时间. 复用保留已有的未来 `ExpireAt`; 无过期时间的记录也可复用, 已过期或过期时间无效的记录不参与复用. 查询未命中时调用 `createBlobObject` 上传. 查询失败或结果格式不完整时向上传控件报告错误, 不自动创建替代资源.

MD5 和文件大小来自上传控件处理后的 `postFile`, 上传请求也提交这份内容, 保留原附件名称. 浏览器未提供 MIME 时使用 `application/octet-stream`, 匹配和上传使用相同值. `Cache` 每次创建独立 Blob, 保留五分钟生命周期. 调用方自定义的 widget `customRequest` 使用自身流程.

前端模块的列表查询显式使用 `no-cache` 和 `errorPolicy: none`, 创建请求也使用 `errorPolicy: none`, 避免缓存或部分错误结果被当作可用附件. 自定义 `BlobStorageModule` 工厂需要提供 list/create 能力.

附件检查不提供跨客户端的原子去重保证: 同时未命中的上传可能各自创建记录. 已有重复 Blob 不自动合并, 已存储内容损坏也不由附件查找流程自动修复.

## 创建与查询

后端 `CreateBlobObjectRequest` 每次创建新的 Blob 对象, 在调用方工作单元中处理, 不提供复用工厂或上传锁. Blob 资源管理页执行普通创建.

新上传在内容写入和工作单元保存完成前保留内部 `UploadPending` 标记. 公共 Blob 查询排除写入中或写入失败的记录, 该标记不暴露到 GraphQL. 没有标记的历史记录保持可查询. 内部实体查询仍可定位未完成记录, 供 Backups 等流程清理. 已取消的创建请求在打开输入流前终止.

## 附件移除与资源删除

公共上传 widget 和独立组件的普通移除默认只更新当前字段值, 解除当前业务引用. 只有 `deleteRemoteFile` 明确返回 `true` 时才调用远程删除.

`DeleteBlobObjectRequest` 执行资源级删除. 删除被多个业务引用复用的同一 Blob 后, 这些引用都会失效. 模块不维护业务引用登记或计数, 无引用附件的清理由调用方显式执行.

不同 Blob 共享物理内容时, 删除只统计相同存储类型和 MD5 的 Blob. Db 的最后一份引用删除后, 通过 `DbFile.DeleteAsync` 清理文件记录及二进制块, 同时清理同 MD5 的历史未完成文件记录. Db 上传和读取排除未完成或大小不符的内容记录, FileSystem 按已有大小检查物理内容; 异步处理使用各次调用独立租用的缓冲区.

## 接入与验证

业务项目中的上传 widget/独立组件需要与公共生成模板保持一致, 并使用包含 `attachBlob` 的前端扩展包. 模板同步方式见 [Geex 模块生成工具](../../frontend_libs/geex-module-schematics/README.md).

验证入口见 [执行脚本](../../scripts/testing/test-patch-extraction.ps1). 测试实现位于 [后端测试目录](tests) 和 [前端扩展源码目录](../../frontend_libs/geex-extensions-blob-storage/src).
