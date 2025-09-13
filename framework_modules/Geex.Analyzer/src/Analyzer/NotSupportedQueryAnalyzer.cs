using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Geex.Analyzer.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotSupportedQueryAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor NotSupportedQueryExpressionRule = new DiagnosticDescriptor(
            "GEEX003",
            "不支持的查询表达式",
            "在MongoDB查询中不支持使用方法 '{0}'。{1}",
            "Query",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "某些方法在MongoDB查询表达式中不受支持，会导致运行时异常。");

        public static readonly DiagnosticDescriptor NotSupportedPropertyRule = new DiagnosticDescriptor(
            "GEEX004",
            "不支持的查询属性访问",
            "在MongoDB查询中不支持访问属性 '{0}'。{1}",
            "Query",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "某些属性在MongoDB查询表达式中不受支持，会导致运行时异常。");

        public static readonly DiagnosticDescriptor NotSupportedDateTimePropertyInMatchRule = new DiagnosticDescriptor(
            "GEEX005",
            "不支持在$match阶段访问DateTime属性",
            "在MongoDB的$match阶段不支持访问DateTime/DateTimeOffset属性 '{0}'，会导致运行时异常。",
            "Query",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "在MongoDB的$match阶段（Where子句）中不支持访问DateTime属性，会导致运行时异常。");

        // 不支持的方法名称映射到建议信息
        private static readonly ImmutableDictionary<string, string> UnsupportedMethods =
            ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string>("GetHashCode", "GetHashCode方法在MongoDB查询中不受支持"),
                new KeyValuePair<string, string>("ToString", "在查询中避免使用ToString，建议在查询结果上调用"),
                new KeyValuePair<string, string>("GetType", "GetType方法在MongoDB查询中不受支持"),
                new KeyValuePair<string, string>("Equals", "对于字段比较，请使用==运算符代替Equals方法"),
                new KeyValuePair<string, string>("ReferenceEquals", "ReferenceEquals在MongoDB查询中不受支持"),
                // 字符串方法限制（在Where子句中）
                new KeyValuePair<string, string>("ToCharArray", "ToCharArray方法在Where子句中不受支持"),
                new KeyValuePair<string, string>("Insert", "Insert方法在Where子句中不受支持"),
                new KeyValuePair<string, string>("Remove", "Remove方法在Where子句中不受支持"),
                new KeyValuePair<string, string>("PadLeft", "PadLeft方法在Where子句中不受支持"),
                new KeyValuePair<string, string>("PadRight", "PadRight方法在Where子句中不受支持"),
                // DateTime方法限制（在Where子句中）
                new KeyValuePair<string, string>("AddYears", "DateTime.AddYears在Where子句中需要常量参数"),
                new KeyValuePair<string, string>("AddMonths", "DateTime.AddMonths在Where子句中需要常量参数"),
                new KeyValuePair<string, string>("ToShortDateString", "DateTime.ToShortDateString在Where子句中不受支持"),
                new KeyValuePair<string, string>("ToLongDateString", "DateTime.ToLongDateString在Where子句中不受支持"),
                new KeyValuePair<string, string>("ToShortTimeString", "DateTime.ToShortTimeString在Where子句中不受支持"),
                new KeyValuePair<string, string>("ToLongTimeString", "DateTime.ToLongTimeString在Where子句中不受支持"),
                // Math方法在Where子句中的限制
                new KeyValuePair<string, string>("IEEERemainder", "Math.IEEERemainder在Where子句中不受支持"),
                new KeyValuePair<string, string>("BigMul", "Math.BigMul在Where子句中不受支持"),
                new KeyValuePair<string, string>("DivRem", "Math.DivRem在Where子句中不受支持"),
            });

        // 在特定类型上不支持的属性
        private static readonly ImmutableDictionary<string, ImmutableArray<string>> UnsupportedProperties =
            ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, ImmutableArray<string>>("System.String",
                    ImmutableArray.Create("Length")), // 仅在$match阶段不支持
                new KeyValuePair<string, ImmutableArray<string>>("System.DateTime",
                    ImmutableArray.Create("Ticks", "TimeOfDay", "Kind")), // Ticks和TimeOfDay在任何地方都不支持，Kind在$match阶段不支持
                new KeyValuePair<string, ImmutableArray<string>>("System.DateTimeOffset",
                    ImmutableArray.Create("Ticks", "TimeOfDay", "Offset", "LocalDateTime", "UtcDateTime")),
                new KeyValuePair<string, ImmutableArray<string>>("System.TimeSpan",
                    ImmutableArray.Create("Days", "Hours", "Minutes", "Seconds", "Milliseconds")) // 这些在某些上下文中不支持
            });

        // 支持的DateTime属性（在Select中支持）
        private static readonly ImmutableHashSet<string> SupportedDateTimeProperties =
            ImmutableHashSet.Create("Year", "Month", "Day", "Hour", "Minute", "Second", "Millisecond", "DayOfWeek", "DayOfYear", "Date");

        // 支持的TimeSpan属性（在Select中支持）
        private static readonly ImmutableHashSet<string> SupportedTimeSpanProperties =
            ImmutableHashSet.Create("Ticks", "TotalMilliseconds", "TotalSeconds", "TotalMinutes", "TotalHours", "TotalDays");

        // 查询方法集合，避免重复创建
        private static readonly ImmutableHashSet<string> QueryMethods = ImmutableHashSet.Create(
            "Where", "Select", "SelectMany", "OrderBy", "OrderByDescending",
            "ThenBy", "ThenByDescending", "GroupBy", "Join", "Count", "Any",
            "All", "First", "FirstOrDefault", "Single", "SingleOrDefault",
            "Sum", "Min", "Max", "Average", "Take", "Skip", "Distinct"
        );

        // Where子句中有参数限制的字符串方法
        private static readonly ImmutableHashSet<string> StringMethodsWithParameterConstraints =
            ImmutableHashSet.Create("StartsWith", "EndsWith", "Contains");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(NotSupportedQueryExpressionRule, NotSupportedPropertyRule, NotSupportedDateTimePropertyInMatchRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var queryContext = GetQueryContext(invocation, context);

            if (!queryContext.IsInQuery) return;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) return;

            var methodName = memberAccess.Name.Identifier.ValueText;

            // 检查通用不支持的方法
            if (UnsupportedMethods.TryGetValue(methodName, out var suggestion))
            {
                if (IsEntityBaseInvocation(memberAccess.Expression, context.SemanticModel))
                {
                    ReportDiagnostic(context, NotSupportedQueryExpressionRule, invocation.GetLocation(), methodName, suggestion);
                }
            }

            // 检查字符串方法的参数限制（仅在Where子句中）
            if (queryContext.IsInWhere)
            {
                CheckStringMethodParameterConstraints(context, invocation, memberAccess, methodName);
            }
        }

        private static void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;
            var queryContext = GetQueryContext(memberAccess, context);

            if (!queryContext.IsInQuery) return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is not IPropertySymbol property) return;

            var containingTypeName = property.ContainingType.ToDisplayString();
            var propertyName = property.Name;

            // 特殊处理：DateTime/DateTimeOffset属性在$match阶段（Where子句）的限制
            if (queryContext.IsInWhere && IsDateTimeType(containingTypeName))
            {
                if (IsEntityBaseProperty(memberAccess.Expression, context.SemanticModel))
                {
                    ReportDiagnostic(context, NotSupportedDateTimePropertyInMatchRule,
                        memberAccess.GetLocation(), $"{containingTypeName}.{propertyName}");
                    return; // 已经报告了诊断，不需要继续检查
                }
            }

            // 检查不支持的属性
            CheckUnsupportedProperty(context, memberAccess, containingTypeName, propertyName, queryContext.IsInWhere);
        }

        /// <summary>
        /// 获取查询上下文信息（是否在查询中，是否在Where子句中）
        /// </summary>
        private static (bool IsInQuery, bool IsInWhere) GetQueryContext(SyntaxNode node, SyntaxNodeAnalysisContext context)
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is LambdaExpressionSyntax lambda &&
                    lambda.Parent is ArgumentSyntax argument &&
                    argument.Parent is ArgumentListSyntax argumentList &&
                    argumentList.Parent is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var methodName = memberAccess.Name.Identifier.ValueText;

                    if (QueryMethods.Contains(methodName))
                    {
                        var isQueryable = IsQueryableOfEntityBase(memberAccess.Expression, context.SemanticModel);
                        return (isQueryable, isQueryable && methodName == "Where");
                    }
                }
                current = current.Parent;
            }
            return (false, false);
        }

        /// <summary>
        /// 检查字符串方法的参数限制
        /// </summary>
        private static void CheckStringMethodParameterConstraints(SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess, string methodName)
        {
            var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
            if (receiverType?.SpecialType != SpecialType.System_String) return;

            if (!StringMethodsWithParameterConstraints.Contains(methodName)) return;

            // 检查参数是否为常量
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var firstArg = invocation.ArgumentList.Arguments[0].Expression;
                if (firstArg is not LiteralExpressionSyntax)
                {
                    var suggestion = $"在Where子句中，{methodName}只支持常量字符串参数";
                    ReportDiagnostic(context, NotSupportedQueryExpressionRule, invocation.GetLocation(),
                        $"string.{methodName}", suggestion);
                }
            }
        }

        /// <summary>
        /// 检查不支持的属性访问
        /// </summary>
        private static void CheckUnsupportedProperty(SyntaxNodeAnalysisContext context,
            MemberAccessExpressionSyntax memberAccess, string containingTypeName, string propertyName, bool isInWhere)
        {
            if (!UnsupportedProperties.TryGetValue(containingTypeName, out var unsupportedProps) ||
                !unsupportedProps.Contains(propertyName)) return;

            // 检查特殊情况的支持性
            if (IsPropertySupportedInCurrentContext(containingTypeName, propertyName, isInWhere)) return;

            // 检查是否在EntityBase类型的查询中
            if (!IsEntityBaseProperty(memberAccess.Expression, context.SemanticModel)) return;

            var suggestion = GetPropertySuggestion(containingTypeName, propertyName);
            ReportDiagnostic(context, NotSupportedPropertyRule, memberAccess.GetLocation(),
                $"{containingTypeName}.{propertyName}", suggestion);
        }

        /// <summary>
        /// 检查属性在当前上下文中是否被支持
        /// </summary>
        private static bool IsPropertySupportedInCurrentContext(string typeName, string propertyName, bool isInWhere)
        {
            // String.Length只在$match阶段不支持
            if (typeName == "System.String" && propertyName == "Length" && !isInWhere)
                return true;

            // DateTime属性在Select中的支持情况
            if (typeName == "System.DateTime" && !isInWhere && SupportedDateTimeProperties.Contains(propertyName))
                return true;

            // TimeSpan属性在Select中的支持情况
            if (typeName == "System.TimeSpan" && !isInWhere && SupportedTimeSpanProperties.Contains(propertyName))
                return true;

            return false;
        }

        /// <summary>
        /// 统一的诊断报告方法
        /// </summary>
        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule,
            Location location, params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(rule, location, messageArgs);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// 检查表达式是否为EntityBase类型的调用
        /// </summary>
        private static bool IsEntityBaseInvocation(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            return symbolInfo.Symbol switch
            {
                IPropertySymbol property => IsEntityBaseType(property.ContainingType) || HasEntityBaseInPropertyChain(property),
                IParameterSymbol parameter => IsEntityBaseType(parameter.Type),
                _ => false
            };
        }

        /// <summary>
        /// 检查表达式是否为EntityBase类型的属性
        /// </summary>
        private static bool IsEntityBaseProperty(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            return symbolInfo.Symbol switch
            {
                IPropertySymbol property => IsEntityBaseType(property.ContainingType) || HasEntityBaseInPropertyChain(property),
                IParameterSymbol parameter => IsEntityBaseType(parameter.Type),
                _ => false
            };
        }

        private static bool IsEntityBaseType(ITypeSymbol type)
        {
            return type?.AllInterfaces.Any(i =>
                i.ToDisplayString() == "MongoDB.Entities.IEntityBase" || i.Name == "IEntityBase") ?? false;
        }

        private static bool HasEntityBaseInPropertyChain(IPropertySymbol property)
        {
            var currentType = property.ContainingType;
            while (currentType != null)
            {
                if (IsEntityBaseType(currentType))
                    return true;
                currentType = currentType.BaseType;
            }
            return false;
        }

        private static string GetPropertySuggestion(string typeName, string propertyName)
        {
            return (typeName, propertyName) switch
            {
                ("System.String", "Length") => "在$match阶段不支持String.Length，考虑使用聚合管道或在Select阶段使用",
                _ when typeName.Contains("DateTime") && (propertyName == "Ticks" || propertyName == "TimeOfDay") =>
                    "Ticks和TimeOfDay属性在MongoDB查询中不受支持，考虑使用支持的日期属性如Year, Month, Day等",
                _ when typeName.Contains("DateTime") && propertyName == "Kind" =>
                    "在$match阶段不支持DateTime.Kind属性，考虑在Select阶段使用",
                _ when typeName == "System.DateTimeOffset" && (propertyName == "Offset" || propertyName == "LocalDateTime" || propertyName == "UtcDateTime") =>
                    "在$match阶段不支持此DateTimeOffset属性，考虑使用聚合管道",
                ("System.TimeSpan", _) => "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性",
                _ => "此属性在MongoDB查询中不受支持"
            };
        }

        private static bool IsDateTimeType(string typeName) =>
            typeName == "System.DateTime" || typeName == "System.DateTimeOffset";

        /// <summary>
        /// 检查表达式是否为 IQueryable&lt;实体类&gt; 类型
        /// </summary>
        private static bool IsQueryableOfEntityBase(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var typeInfo = semanticModel.GetTypeInfo(expression);
            var type = typeInfo.Type;

            if (type == null) return false;

            // 检查是否为 IQueryable<T>
            var queryableInterface = type.AllInterfaces.FirstOrDefault(i =>
                i.ToDisplayString().StartsWith("System.Linq.IQueryable<"));

            var queryableType = queryableInterface as INamedTypeSymbol ??
                               (type.ToDisplayString().StartsWith("System.Linq.IQueryable<") ? type as INamedTypeSymbol : null);

            // 获取泛型参数 T 并检查是否实现了 IEntityBase
            return queryableType?.TypeArguments.Length > 0 && IsEntityBaseType(queryableType.TypeArguments[0]);
        }
    }
}
