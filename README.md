# [Geex Framework](https://docs.geexcode.com/)
![star](https://gitcode.com/geexcode/geex/star/badge.svg)
[![publish](https://github.com/geex-framework/geex/actions/workflows/publish.yml/badge.svg)](https://github.com/geex-framework/geex/actions/workflows/publish.yml)

Geex 是一个模块化、业务友好、以绝佳开发体验为终极目标的全栈应用框架，采用 DDD（领域驱动设计）和 ActiveEntity 模式，专为构建高性能、可扩展、全功能的企业级 SaaS 应用而设计。

## 🏗️ 核心特性

- **🔧 ActiveEntity 模式**&nbsp;: &nbsp;&nbsp;&nbsp;业务逻辑内聚在实体中, 减少样板代码, 加速开发
- **📦 模块化架构**&nbsp;: &nbsp;&nbsp;&nbsp;清晰的模块边界和依赖管理, 模块可零改动升格为微服务
- **🔗 GraphQL API**&nbsp;: &nbsp;&nbsp;&nbsp;基于 Hot Chocolate 的现代化 API, 减少前后端沟通成本
- **📊 MongoDB 集成**&nbsp;: &nbsp;&nbsp;&nbsp;灵活且高性能的 NoSQL 数据存储, 支持事务、读写分离
- **🔐 身份认证授权**&nbsp;: &nbsp;&nbsp;&nbsp;基于枚举的 RBAC 权限管理系统, 支持字段级的权限控制
- **🌐 多租户支持**&nbsp;: &nbsp;&nbsp;&nbsp;租户级的数据隔离, 自动、无感的租户数据过滤
- **🚀 代码生成器**&nbsp;: &nbsp;&nbsp;&nbsp;一键生成全栈、前端、后端、模块代码， 无需繁琐的项目初始化流程
- **🐳 容器化部署**&nbsp;: &nbsp;&nbsp;&nbsp;Traefik + ELK 完整基础设施一键部署, 通过域名快速挂载, 初级开发也能快速上手
- **🔑 开发时HTTPS支持**&nbsp;: &nbsp;&nbsp;&nbsp;基于域名的本地 HTTPS 开发调试, 抹平各环境间的鸿沟

## 🌍 技术栈

| 前端技术栈         | 后端技术栈            |
| :----------------- | :-------------------- |
| Angular 18+        | .NET 9.0+             |
| NG-ALAIN 脚手架    | Hot Chocolate GraphQL |
| NG-ZORRO UI 组件库 | MongoDB + Redis       |

## 入门指南

推荐直接使用Geex的[🔗vscode插件](https://marketplace.visualstudio.com/items?itemName=Lulus.geex-schematics)进行项目创建和模块安装。

详细教程请参考我们的[🔗官方文档](https://docs.geexcode.com/), 或者观看我们的[🔗视频教程](https://www.bilibili.com/video/BV1QF4m1u7iB/)

## 贡献指南

欢迎对Geex框架做出贡献！请遵循以下步骤：

1. Fork本仓库
2. 创建特性分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add some amazing feature'`)
4. 推送到分支 (`git push origin feature/amazing-feature`)
5. 开启Pull Request

## 许可证

本项目采用MIT许可证 - 详情请参阅LICENSE文件
