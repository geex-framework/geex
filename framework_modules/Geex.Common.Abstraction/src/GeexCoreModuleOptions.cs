﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Common.Abstractions;

using StackExchange.Redis.Extensions.Core.Configuration;

namespace Geex.Common.Abstraction
{
    public class GeexCoreModuleOptions : IGeexModuleOption<GeexCoreModule>
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017/x_proj_x";
        /// <summary>
        /// 后端host基地址
        /// </summary>
        public string Host { get; set; } = "https://x_proj_x.api.dev.x_org_x.com";
        public string AppName { get; set; } = "x_proj_x";
        public RedisConfiguration? Redis { get; set; }
        /// <summary>
        /// 是否在response中抛出异常信息
        /// </summary>
        public bool IncludeExceptionDetails { get; set; } = true;
        /// <summary>
        /// 是否使用migration自动初始化数据
        /// </summary>
        public bool AutoMigration { get; set; } = false;
        /// <summary>
        /// 分页获取数据最多数量
        /// </summary>
        public int? MaxPageSize { get; set; } = 1000;
        /// <summary>
        /// 启用数据库命令语句日志, !极度影响性能
        /// </summary>
        public bool EnableDataLogging { get; set; } = false;
        /// <summary>
        /// 开启后端元数据功能
        /// </summary>
        public bool DisableIntrospection { get; set; } = false;

        public string CorsRegex { get; set; } = ".+";
    }
}
