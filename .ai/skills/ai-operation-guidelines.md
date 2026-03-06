# Skill: AI 操作规范 (AI Operation Guidelines)

## 描述
规范 AI 在本项目中执行任务时必须遵守的操作步骤和注意事项，避免常见的操作失误。

---

## 规则 1: 终端管理 — 始终创建新终端执行命令

**绝对禁止**在已有正在运行进程的终端中执行新命令。

- 后端 `dotnet run --watch` 占用的终端不能再执行其他命令
- 前端 `pnpm start` 占用的终端不能再执行其他命令
- 需要执行新命令时，**必须创建一个新的终端**

示例：
```
❌ 在运行 dotnet run 的 server 终端执行 dotnet build → 后端被中断
✅ 新建终端 → 在新终端执行 dotnet build
```

---

## 规则 2: gqlgen 执行策略

执行 `pnpm gqlgen` 需要后端服务正在运行（用于拉取 GraphQL Schema）。

**执行流程**：
1. 检查后端是否正在运行
2. 如果后端未运行 → **新建终端**启动后端：`cd server && dotnet run --project ./Bigworld.Server/Bigworld.Server.csproj`
3. 等待后端启动完成（确认日志中出现监听端口信息）
4. **在另一个新终端**执行 `cd client/admin/ClientApp && pnpm gqlgen`
5. 确认 gqlgen 执行成功，无报错

**禁止行为**：
- ❌ gqlgen 失败后手动编写 `operations.gql.ts` 文件
- ❌ 在后端运行的同一终端执行 gqlgen
- ❌ 后端未完全启动就执行 gqlgen

---

## 规则 3: 修改后必须检查编译错误

**每次**修改或新增代码后，无论前端还是后端，最后一步必须验证编译通过。

**后端检查**（新建终端执行）：
```powershell
cd server && dotnet build ./Bigworld.sln
```

**前端检查**（新建终端执行）：
```powershell
cd client/admin/ClientApp && npx ng build --configuration development 2>&1 | Select-Object -First 50
```

也可以使用 IDE 的错误检查工具（`get_errors`）快速验证。

**必须确认**：0 个编译错误后才能向用户报告任务完成。

---

## 规则 4: 修改后验证业务逻辑正确性

编译通过后，还需验证修改是否符合需求和业务逻辑：

- **检查方式**：
  - 审查生成的代码是否与需求一致
  - 对照现有模块（如 Customer、Companion）的实现模式
  - 如有必要，编写临时验证脚本（如 GraphQL 请求测试脚本）确认 API 可用

- **后端测试示例**（新建终端）：
```powershell
cd server && dotnet test ./Bigworld.Tests/Bigworld.Tests.csproj --filter "FullyQualifiedName~{相关测试类}"
```

- **前端验证**：确认页面组件引用正确、GraphQL 操作类型匹配、路由注册无遗漏

---

## 规则 5: 长任务拆分 — 防止请求超时

当任务涉及大量代码生成或修改时，**必须主动拆分**为多个小步骤执行，避免单次请求内容过长导致网络超时。

**拆分策略**：
- 每次只创建/修改 1-2 个文件
- 使用 `manage_todo_list` 跟踪进度，确保不遗漏
- 大文件内容通过多次小编辑完成，不要一次性生成整个文件
- 如果遇到超时错误，立即将剩余任务拆分为更细粒度的步骤继续执行

**示例拆分**：
```
全栈新功能（原本 1 次完成）→ 拆分为：
  1. 后端接口 + 实体（2-3 个文件一组）
  2. 后端 GQL（2 个文件一组）
  3. 前端 GraphQL 操作
  4. 前端列表页
  5. 前端编辑页
  6. 路由注册 + 编译验证
```
