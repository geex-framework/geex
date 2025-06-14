using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Geex.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BannedMediatRAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor MediatRUsageRule = new DiagnosticDescriptor(
            "GEEX001",
            "MediatR usage is not allowed",
            "不推荐使用 MediatR 命名空间 '{0}'。请使用 MediatX",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "MediatR has been deprecated in favor of our custom MediatX.");

        public static readonly DiagnosticDescriptor MediatRTypeUsageRule = new DiagnosticDescriptor(
            "GEEX002",
            "MediatR type usage is not allowed",
            "不推荐使用使用 MediatR 类型 '{0}'。请使用 '{1}' 替代",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "MediatR types have been deprecated in favor of our custom interfaces.");

        private static readonly ImmutableArray<string> BannedNamespaces =
            ImmutableArray.Create("MediatR");

        private static readonly ImmutableDictionary<string, string> BannedTypes =
            ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string>("MediatR.IMediator", "MediatX.IMediator"),
                new KeyValuePair<string, string>("MediatR.IRequest", "MediatX.IRequest"),
                new KeyValuePair<string, string>("MediatR.IRequestHandler", "MediatX.IRequestHandler"),
                new KeyValuePair<string, string>("MediatR.INotification", "MediatX.IEvent"),
                new KeyValuePair<string, string>("MediatR.INotificationHandler", "MediatX.IEventHandlerr")
            });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MediatRUsageRule, MediatRTypeUsageRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
            context.RegisterSyntaxNodeAction(AnalyzeQualifiedName, SyntaxKind.QualifiedName);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
        }

        private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
        {
            var usingDirective = (UsingDirectiveSyntax)context.Node;
            var namespaceName = usingDirective.Name?.ToString();

            if (namespaceName != null && BannedNamespaces.Any(banned => namespaceName.StartsWith(banned)))
            {
                var diagnostic = Diagnostic.Create(
                    MediatRUsageRule,
                    usingDirective.GetLocation(),
                    namespaceName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeQualifiedName(SyntaxNodeAnalysisContext context)
        {
            var qualifiedName = (QualifiedNameSyntax)context.Node;
            var fullName = qualifiedName.ToString();

            if (BannedTypes.TryGetValue(fullName, out var replacement))
            {
                var diagnostic = Diagnostic.Create(
                    MediatRTypeUsageRule,
                    qualifiedName.GetLocation(),
                    fullName,
                    replacement);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
        {
            var identifierName = (IdentifierNameSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var symbolInfo = semanticModel.GetSymbolInfo(identifierName);

            if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol)
            {
                var fullName = typeSymbol.ToDisplayString();
                if (BannedTypes.TryGetValue(fullName, out var replacement))
                {
                    var diagnostic = Diagnostic.Create(
                        MediatRTypeUsageRule,
                        identifierName.GetLocation(),
                        fullName,
                        replacement);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
