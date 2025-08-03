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

            // 检查是否在查询表达式中（Where, Select, OrderBy等）
            if (!IsInQueryExpression(invocation))
                return;

            // 检查调用的方法是否在不支持列表中
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.ValueText;

                if (UnsupportedMethods.TryGetValue(methodName, out var suggestion))
                {
                    // 检查调用者类型是否实现了IEntityBase或其属性
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess.Expression);
                    if (symbolInfo.Symbol is IPropertySymbol property)
                    {
                        if (IsEntityBaseType(property.ContainingType) || HasEntityBaseInPropertyChain(property, context.SemanticModel))
                        {
                            var diagnostic = Diagnostic.Create(
                                NotSupportedQueryExpressionRule,
                                invocation.GetLocation(),
                                methodName,
                                suggestion);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                    else if (symbolInfo.Symbol is IParameterSymbol parameter)
                    {
                        if (IsEntityBaseType(parameter.Type))
                        {
                            var diagnostic = Diagnostic.Create(
                                NotSupportedQueryExpressionRule,
                                invocation.GetLocation(),
                                methodName,
                                suggestion);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }

                // 特殊检查：字符串方法在Where子句中的限制
                if (IsInWhereClause(invocation))
                {
                    var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
                    if (receiverType?.SpecialType == SpecialType.System_String)
                    {
                        // 检查不支持的字符串方法
                        var unsupportedStringMethods = new[] { "StartsWith", "EndsWith", "Contains" };
                        if (unsupportedStringMethods.Contains(methodName))
                        {
                            // 检查参数是否为常量
                            if (invocation.ArgumentList.Arguments.Count > 0)
                            {
                                var firstArg = invocation.ArgumentList.Arguments[0].Expression;
                                if (firstArg is not LiteralExpressionSyntax)
                                {
                                    var diagnostic = Diagnostic.Create(
                                        NotSupportedQueryExpressionRule,
                                        invocation.GetLocation(),
                                        $"string.{methodName}",
                                        $"在Where子句中，{methodName}只支持常量字符串参数");
                                    context.ReportDiagnostic(diagnostic);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
        {
            var memberAccess = (MemberAccessExpressionSyntax)context.Node;

            // 检查是否在查询表达式中
            if (!IsInQueryExpression(memberAccess))
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
            if (symbolInfo.Symbol is IPropertySymbol property)
            {
                var containingTypeName = property.ContainingType.ToDisplayString();
                var propertyName = property.Name;

                // 特殊处理：DateTime/DateTimeOffset属性在$match阶段（Where子句）的限制
                if (IsInWhereClause(memberAccess) &&
                    (containingTypeName == "System.DateTime" || containingTypeName == "System.DateTimeOffset"))
                {
                    // 在Where子句中，所有DateTime属性都不支持
                    var expressionSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression);
                    if (expressionSymbol.Symbol is IPropertySymbol sourceProperty)
                    {
                        if (IsEntityBaseType(sourceProperty.ContainingType) || HasEntityBaseInPropertyChain(sourceProperty, context.SemanticModel))
                        {
                            var diagnostic = Diagnostic.Create(
                                NotSupportedDateTimePropertyInMatchRule,
                                memberAccess.GetLocation(),
                                $"{containingTypeName}.{propertyName}");
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                    else if (expressionSymbol.Symbol is IParameterSymbol parameter)
                    {
                        if (IsEntityBaseType(parameter.Type))
                        {
                            var diagnostic = Diagnostic.Create(
                                NotSupportedDateTimePropertyInMatchRule,
                                memberAccess.GetLocation(),
                                $"{containingTypeName}.{propertyName}");
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                    return; // 已经报告了诊断，不需要继续检查
                }

                // 检查是否为不支持的属性
                if (UnsupportedProperties.TryGetValue(containingTypeName, out var unsupportedProps) &&
                    unsupportedProps.Contains(propertyName))
                {
                    // 特殊处理：String.Length只在$match阶段不支持
                    if (containingTypeName == "System.String" && propertyName == "Length")
                    {
                        if (!IsInWhereClause(memberAccess))
                            return; // 在Select等其他地方是支持的
                    }

                    // 特殊处理：DateTime属性在Select中的支持情况
                    if (containingTypeName == "System.DateTime")
                    {
                        if (SupportedDateTimeProperties.Contains(propertyName) && !IsInWhereClause(memberAccess))
                            return; // 在Select等地方支持这些属性
                    }

                    // 特殊处理：TimeSpan属性在Select中的支持情况
                    if (containingTypeName == "System.TimeSpan")
                    {
                        if (SupportedTimeSpanProperties.Contains(propertyName) && !IsInWhereClause(memberAccess))
                            return; // 在Select等地方支持这些属性
                    }

                    // 检查是否在EntityBase类型的查询中
                    var expressionSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression);
                    if (expressionSymbol.Symbol is IPropertySymbol sourceProperty)
                    {
                        if (IsEntityBaseType(sourceProperty.ContainingType) || HasEntityBaseInPropertyChain(sourceProperty, context.SemanticModel))
                        {
                            var suggestion = GetPropertySuggestion(containingTypeName, propertyName);
                            var diagnostic = Diagnostic.Create(
                                NotSupportedPropertyRule,
                                memberAccess.GetLocation(),
                                $"{containingTypeName}.{propertyName}",
                                suggestion);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                    else if (expressionSymbol.Symbol is IParameterSymbol parameter)
                    {
                        if (IsEntityBaseType(parameter.Type))
                        {
                            var suggestion = GetPropertySuggestion(containingTypeName, propertyName);
                            var diagnostic = Diagnostic.Create(
                                NotSupportedPropertyRule,
                                memberAccess.GetLocation(),
                                $"{containingTypeName}.{propertyName}",
                                suggestion);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static bool IsInQueryExpression(SyntaxNode node)
        {
            // 向上查找，检查是否在LINQ查询方法的lambda表达式中
            var current = node.Parent;
            while (current != null)
            {
                if (current is LambdaExpressionSyntax lambda)
                {
                    // 检查lambda是否是查询方法的参数
                    if (lambda.Parent is ArgumentSyntax argument &&
                        argument.Parent is ArgumentListSyntax argumentList &&
                        argumentList.Parent is InvocationExpressionSyntax invocation)
                    {
                        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                        {
                            var methodName = memberAccess.Name.Identifier.ValueText;
                            return IsQueryMethod(methodName);
                        }
                    }
                }
                current = current.Parent;
            }
            return false;
        }

        private static bool IsInWhereClause(SyntaxNode node)
        {
            // 检查是否特定在Where子句中
            var current = node.Parent;
            while (current != null)
            {
                if (current is LambdaExpressionSyntax lambda)
                {
                    if (lambda.Parent is ArgumentSyntax argument &&
                        argument.Parent is ArgumentListSyntax argumentList &&
                        argumentList.Parent is InvocationExpressionSyntax invocation)
                    {
                        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                        {
                            var methodName = memberAccess.Name.Identifier.ValueText;
                            return methodName == "Where";
                        }
                    }
                }
                current = current.Parent;
            }
            return false;
        }

        private static bool IsQueryMethod(string methodName)
        {
            var queryMethods = new HashSet<string>
            {
                "Where", "Select", "SelectMany", "OrderBy", "OrderByDescending",
                "ThenBy", "ThenByDescending", "GroupBy", "Join", "Count", "Any",
                "All", "First", "FirstOrDefault", "Single", "SingleOrDefault",
                "Sum", "Min", "Max", "Average", "Take", "Skip", "Distinct"
            };
            return queryMethods.Contains(methodName);
        }

        private static bool IsEntityBaseType(ITypeSymbol type)
        {
            if (type == null) return false;

            // 检查类型是否直接或间接实现IEntityBase接口
            return type.AllInterfaces.Any(i =>
                i.ToDisplayString() == "MongoDB.Entities.IEntityBase" ||
                i.Name == "IEntityBase");
        }

        private static bool HasEntityBaseInPropertyChain(IPropertySymbol property, SemanticModel semanticModel)
        {
            // 检查属性链中是否包含EntityBase类型
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
            if (typeName == "System.String" && propertyName == "Length")
                return "在$match阶段不支持String.Length，考虑使用聚合管道或在Select阶段使用";

            if (typeName.Contains("DateTime") && (propertyName == "Ticks" || propertyName == "TimeOfDay"))
                return "Ticks和TimeOfDay属性在MongoDB查询中不受支持，考虑使用支持的日期属性如Year, Month, Day等";

            if (typeName.Contains("DateTime") && propertyName == "Kind")
                return "在$match阶段不支持DateTime.Kind属性，考虑在Select阶段使用";

            if (typeName == "System.DateTimeOffset" && (propertyName == "Offset" || propertyName == "LocalDateTime" || propertyName == "UtcDateTime"))
                return "在$match阶段不支持此DateTimeOffset属性，考虑使用聚合管道";

            if (typeName == "System.TimeSpan")
                return "部分TimeSpan属性在某些上下文中不受支持，建议使用Ticks或Total*属性";

            return "此属性在MongoDB查询中不受支持";
        }
    }
}
