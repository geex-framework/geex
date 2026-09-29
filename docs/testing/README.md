# 测试入口

规则以 [Geex 项目 AI 测试硬性规范](../Geex项目AI测试硬性规范.md) 为准. 当前入口覆盖 [Enumeration 动态枚举专项](scopes/enumeration-dynamic.md), 不代表项目完整测试范围已登记或全量验收已完成.

- 公共契约: [Enumeration API](../enumeration.md).
- 范围/用例来源/边界: [Enumeration 专项](scopes/enumeration-dynamic.md).
- 执行工具: [test-enumeration-dynamic.ps1](../../scripts/testing/test-enumeration-dynamic.ps1).
- 机器格式: [JSON Schema](../../scripts/testing/schemas/enumeration-dynamic-evidence.schema.json).
- 本次缺陷与阻塞台账: 每个证据包中的 `defects/issues.json`, 不跨运行推断问题已关闭.

在仓库根目录使用 PowerShell 7 执行:

```powershell
pwsh -File scripts/testing/test-enumeration-dynamic.ps1 -Stage DEVELOPMENT -Executor '<执行者标识>'
```

依赖已恢复且本次明确选择跳过恢复时, 添加 `-NoRestore`. 独立执行者复用时可使用 `-Stage ACCEPTANCE -Executor '<独立执行者标识>'`; 阶段参数只记录本次角色, 不证明独立性, 不替代独立审查或全项目验收.

Runner 先构建 `Geex.Tests` 及项目依赖, 构建成功且输入身份稳定后, 执行 `EnumerationDynamicTests` 专项. 构建失败时不启动依赖测试. 退出码 `0` 表示本批次已实现的适用检查通过, `1` 表示专项条件未满足, `2` 表示工具/输入/配置故障.

每次创建独立 `.test-evidence/<runId>/`, 命令与完整输出保存在 `runner/<invocationId>/`. 从该目录的 `report.md` 查看结论, `manifest.json`/`cases.jsonl`/`artifacts.jsonl`/`gate-result.json` 查看机器记录. Schema 随包保存, 可在仓库副本中访问原始工具. Git 外证据须连同整个目录交付, 不清理历史包或复用旧报告.
