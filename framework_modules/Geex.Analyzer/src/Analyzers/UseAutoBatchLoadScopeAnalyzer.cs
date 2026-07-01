using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Geex.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseAutoBatchLoadScopeAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor NonRootFieldUseRule = new DiagnosticDescriptor(
            "GEEX006",
            "UseAutoBatchLoad 作用域错误",
            "UseAutoBatchLoad 仅允许在 Query、Mutation 或 Subscription 的根字段上配置，不能在实体类型字段（如 user.orgs）上调用。",
            "GraphQL",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "UseAutoBatchLoad 仅支持 Operation Extension 根字段级或 Operation 类型级配置。");

        private static readonly ImmutableHashSet<string> OperationExtensionNames = ImmutableHashSet.Create(
            "QueryExtension",
            "MutationExtension",
            "SubscriptionExtension");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(NonRootFieldUseRule);

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

            if (!IsFieldDescriptorType(receiverType) && !IsObjectTypeDescriptorType(receiverType))
            {
                return;
            }

            if (IsInsideOperationExtension(invocation, context.SemanticModel))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                NonRootFieldUseRule,
                invocation.GetLocation()));
        }

        private static bool IsFieldDescriptorType(ITypeSymbol receiverType) =>
            MatchesTypeDefinition(receiverType, "IObjectFieldDescriptor") ||
            MatchesTypeDefinition(receiverType, "ObjectFieldDescriptor");

        private static bool IsObjectTypeDescriptorType(ITypeSymbol receiverType) =>
            MatchesTypeDefinition(receiverType, "IObjectTypeDescriptor") ||
            MatchesTypeDefinition(receiverType, "ObjectTypeDescriptor");

        private static bool MatchesTypeDefinition(ITypeSymbol type, string definitionName)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (GetDefinitionName(current) == definitionName)
                {
                    return true;
                }
            }

            foreach (var iface in type.AllInterfaces)
            {
                if (GetDefinitionName(iface) == definitionName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetDefinitionName(ITypeSymbol type) =>
            type is INamedTypeSymbol namedType && namedType.IsGenericType
                ? namedType.OriginalDefinition.Name
                : type.Name;

        private static bool IsInsideOperationExtension(SyntaxNode node, SemanticModel semanticModel)
        {
            var containingClass = node.Ancestors()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault();

            if (containingClass == null)
            {
                return false;
            }

            var classSymbol = semanticModel.GetDeclaredSymbol(containingClass) as INamedTypeSymbol;
            return classSymbol != null && IsOperationExtensionClass(classSymbol);
        }

        private static bool IsOperationExtensionClass(INamedTypeSymbol classSymbol)
        {
            for (var current = classSymbol; current != null; current = current.BaseType)
            {
                var definition = current.IsGenericType
                    ? current.OriginalDefinition
                    : current;

                if (OperationExtensionNames.Contains(definition.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
