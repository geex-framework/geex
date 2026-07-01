using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Geex.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ValidateAttributeAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor InvalidValidateRuleRule = new DiagnosticDescriptor(
            "GEEX003",
            "Invalid ValidateRule usage",
            "无效的 ValidateRule 规则 '{0}'。请使用有效的 ValidateRule 方法名",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "ValidateAttribute must use valid ValidateRule method names.");

        // ValidateRule 中所有可用的静态方法名称
        private static readonly ImmutableHashSet<string> ValidRuleNames = ImmutableHashSet.Create(
            "Regex", "LengthMin", "LengthMax", "LengthRange", "Min", "Max", "Range",
            "Email", "EmailNotDisposable", "ChinesePhone", "Url", "CreditCard", "Json",
            "IPv4", "IPv6", "IP", "MacAddress", "Guid", "DateMin", "DateMax", "DateRange",
            "DateFuture", "DatePast", "BirthDateMinAge", "ListNotEmpty", "ListSizeMin",
            "ListSizeMax", "ListSizeRange", "AlphaNumeric", "Alpha", "Numeric", "NoWhitespace",
            "StrongPassword", "Price", "ChineseIdCard", "FileExtension"
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(InvalidValidateRuleRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
        }

        private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
        {
            var attributeSyntax = (AttributeSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            // 检查是否是 ValidateAttribute
            var symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol ||
                methodSymbol.ContainingType.Name != "ValidateAttribute")
            {
                return;
            }

            // 检查参数列表
            if (attributeSyntax.ArgumentList?.Arguments.Count == 0)
                return;

            var firstArgument = attributeSyntax.ArgumentList.Arguments[0];
            
            // 检查第一个参数是否是 nameof 表达式
            if (firstArgument.Expression is InvocationExpressionSyntax invocation &&
                invocation.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText == "nameof")
            {
                // 检查 nameof 的参数
                if (invocation.ArgumentList.Arguments.Count > 0)
                {
                    var nameofArgument = invocation.ArgumentList.Arguments[0];
                    if (nameofArgument.Expression is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Expression is IdentifierNameSyntax classIdentifier &&
                        classIdentifier.Identifier.ValueText == "ValidateRule")
                    {
                        var ruleName = memberAccess.Name.Identifier.ValueText;
                        
                        // 检查规则名称是否有效
                        if (!ValidRuleNames.Contains(ruleName))
                        {
                            var diagnostic = Diagnostic.Create(
                                InvalidValidateRuleRule,
                                memberAccess.Name.GetLocation(),
                                ruleName);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
            // 检查是否是字符串字面量
            else if (firstArgument.Expression is LiteralExpressionSyntax literal &&
                     literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                var ruleValue = literal.Token.ValueText;
                
                // 如果字符串包含参数（包含?），提取规则名称部分
                var ruleName = ruleValue.Contains('?') ? ruleValue.Split('?')[0] : ruleValue;
                
                // 检查规则名称是否有效
                if (!ValidRuleNames.Contains(ruleName))
                {
                    var diagnostic = Diagnostic.Create(
                        InvalidValidateRuleRule,
                        literal.GetLocation(),
                        ruleName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
