using System;
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

        // 不支持的方法名称映射到建议信息
        private static readonly ImmutableDictionary<string, string> UnsupportedMethods =
            ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string>("GetHashCode", "GetHashCode方法在MongoDB查询中不受支持"),
                new KeyValuePair<string, string>("ToString", "在查询中避免使用ToString，建议在查询结果上调用"),
                new KeyValuePair<string, string>("GetType", "GetType方法在MongoDB查询中不受支持"),
                new KeyValuePair<string, string>("Equals", "对于字段比较，请使用==运算符代替Equals方法"),
                new KeyValuePair<string, string>("ReferenceEquals", "ReferenceEquals在MongoDB查询中不受支持")
            });

        // 在特定类型上不支持的属性
        private static readonly ImmutableDictionary<string, ImmutableArray<string>> UnsupportedProperties =
            ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, ImmutableArray<string>>("System.String",
                    ImmutableArray.Create("Length")),
                new KeyValuePair<string, ImmutableArray<string>>("System.DateTime",
                    ImmutableArray.Create("Ticks", "TimeOfDay")),
                new KeyValuePair<string, ImmutableArray<string>>("System.DateTimeOffset",
                    ImmutableArray.Create("Ticks", "TimeOfDay", "Offset"))
            });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(NotSupportedQueryExpressionRule, NotSupportedPropertyRule);

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

                // 检查是否为不支持的属性
                if (UnsupportedProperties.TryGetValue(containingTypeName, out var unsupportedProps) &&
                    unsupportedProps.Contains(propertyName))
                {
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

        private static bool IsQueryMethod(string methodName)
        {
            var queryMethods = new HashSet<string>
            {
                "Where", "Select", "SelectMany", "OrderBy", "OrderByDescending",
                "ThenBy", "ThenByDescending", "GroupBy", "Join", "Count", "Any",
                "All", "First", "FirstOrDefault", "Single", "SingleOrDefault",
                "Sum", "Min", "Max", "Average"
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
                return "在$match阶段不支持String.Length，考虑使用聚合管道";

            if (typeName.Contains("DateTime") && (propertyName == "Ticks" || propertyName == "TimeOfDay"))
                return "在$match阶段不支持此DateTime属性，考虑使用支持的日期操作";

            return "此属性在MongoDB查询中不受支持";
        }
    }
}
