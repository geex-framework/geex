using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Geex.Analyzer.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseAutoBatchLoadScopeAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor FieldLevelUseRule = new DiagnosticDescriptor(
            "GEEX006",
            "UseAutoBatchLoad 作用域错误",
            "UseAutoBatchLoad 仅允许在 Query、Mutation 或 Subscription 的 Operation 类型描述符上配置，不能在字段描述符链上调用。",
            "GraphQL",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "UseAutoBatchLoad 仅支持 Operation 级配置。");

        private static readonly ImmutableHashSet<string> FieldDescriptorTypeNames = ImmutableHashSet.Create(
            "IObjectFieldDescriptor",
            "ObjectFieldDescriptor",
            "IObjectFieldDescriptor`1",
            "ObjectFieldDescriptor`1");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(FieldLevelUseRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            if (memberAccess.Name.Identifier.ValueText != "UseAutoBatchLoad")
            {
                return;
            }

            var receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
            if (receiverType == null)
            {
                return;
            }

            var receiverTypeName = receiverType.ToDisplayString();
            if (!FieldDescriptorTypeNames.Any(name => receiverTypeName.StartsWith(name)))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                FieldLevelUseRule,
                invocation.GetLocation()));
        }
    }
}
