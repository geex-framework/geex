# Geex Framework

[![publish](https://github.com/geex-framework/geex/actions/workflows/publish.yml/badge.svg)](https://github.com/geex-framework/geex/actions/workflows/publish.yml)

Geex是一个基于.NET的现代化模块化应用框架，专为构建高性能、可扩展的企业应用而设计。它集成了多种常用功能模块和扩展，为开发者提供了完整的应用开发解决方案。

## 核心特性

- **模块化架构**：基于模块化设计，可按需组合各种功能模块
- **GraphQL支持**：内置HotChocolate集成，轻松构建GraphQL API
- **MongoDB集成**：使用改进的MongoDB.Entities提供强大的数据访问层
- **CQRS模式**：实现跨进程通信的命令查询职责分离
- **认证与授权**：集成完整的身份验证和基于Casbin的授权系统
- **微服务Ready**：支持零改动将业务模块升格为独立的微服务

## 项目结构

```
framework_modules/         # 核心框架模块
├── Geex.Casbin/           # 基于Casbin的授权系统
├── Geex.Common/           # 框架通用组件
├── Geex.Abstractions/ # 抽象层和接口定义
├── Geex.Common.Accounting/ # 账户功能
├── Geex.Common.Authentication/ # 认证模块
├── Geex.Common.Authorization/ # 授权模块
├── Geex.Common.BackgroundJob/ # 后台任务处理
├── Geex.Common.BlobStorage/ # Blob存储功能
├── Geex.Common.Identity/   # 身份管理
├── Geex.Common.Logging/    # 日志系统
├── Geex.Common.Messaging/  # 消息系统
├── Geex.Common.MultiTenant/ # 多租户支持
├── Geex.Common.Settings/   # 应用设置管理
├── Geex.MediatX/           # MediatR扩展，支持分布式任务
└── Geex.MongoDB.Entities/  # MongoDB数据访问增强库

extensions/                # VS Code扩展
├── vscode-npm-quick-install/ # NPM包快速安装工具

scripts/                   # 实用脚本
└── enable_nested_vitualization.ps1 # 启用嵌套虚拟化脚本

nupkg/                     # NuGet包输出目录
```

## 入门指南

推荐直接使用Geex的[vscode插件](https://marketplace.visualstudio.com/items?itemName=Lulus.geex-schematics)进行项目创建和模块安装。

### 安装

Geex框架的各个模块作为NuGet包发布，可以使用NuGet包管理器安装：

```bash
# 安装核心抽象层
dotnet add package Geex.Abstractions

# 安装通用模块
dotnet add package Geex.Common

# 根据需要添加其他模块
dotnet add package Geex.Common.Authentication
dotnet add package Geex.Common.Authorization
# ...其他模块
```

### 基本用法

1. 安装Geex的VSCode插件
2. 生成样板项目
3. 创建应用模块：

```csharp
[DependsOn(
    typeof(GeexCommonModule),
    typeof(GeexAuthenticationModule),
    typeof(GeexAuthorizationModule)
)]
public class YourAppModule : GeexModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // 配置应用服务
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // 应用初始化逻辑
    }
}
```

## 主要模块说明

### Geex.MediatX

MediatX是对MediatR的扩展，允许将in-process的Mediator模式扩展为支持跨进程通信，便于构建微服务架构。它支持多种传输方式，包括RabbitMQ、Kafka和GRPC。

### Geex.MongoDB.Entities

基于官方MongoDB.Entities的增强版本，提供了更友好的API来操作MongoDB数据库，简化了数据访问层的开发。

### Geex.Common.Authorization

基于Casbin的授权系统，提供灵活的基于RBAC和ABAC的权限控制。

### Geex.Common.Authentication

提供JWT认证和其他身份验证机制的集成。

### Geex.Common.MultiTenant

多租户支持模块，为SaaS应用提供租户隔离和管理功能。

## 贡献指南

欢迎对Geex框架做出贡献！请遵循以下步骤：

1. Fork本仓库
2. 创建特性分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add some amazing feature'`)
4. 推送到分支 (`git push origin feature/amazing-feature`)
5. 开启Pull Request

## 许可证

本项目采用MIT许可证 - 详情请参阅LICENSE文件
