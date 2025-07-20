using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Geex.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NullableAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GEEX001";
        private static readonly LocalizableString Title = "Should add NullableAttribute";
        private static readonly LocalizableString MessageFormat = "Field or parameter '{0}' has a default value and should be marked with [Nullable]";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, "Usage", DiagnosticSeverity.Info, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        }

        private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            var property = (PropertyDeclarationSyntax)context.Node;
            // 只处理public属性
            if (!property.Modifiers.Any(SyntaxKind.PublicKeyword))
                return;
            // 有默认值
            if (property.Initializer == null)
                return;
            // 已有NullableAttribute
            if (property.AttributeLists.SelectMany(x => x.Attributes)
                .Any(attr => attr.Name.ToString().Contains("Nullable")))
                return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, property.Identifier.GetLocation(), property.Identifier.Text));
        }

        private void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            var parameter = (ParameterSyntax)context.Node;
            // 有默认值
            if (parameter.Default == null)
                return;
            // 已有NullableAttribute
            if (parameter.AttributeLists.SelectMany(x => x.Attributes)
                .Any(attr => attr.Name.ToString().Contains("Nullable")))
                return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, parameter.Identifier.GetLocation(), parameter.Identifier.Text));
        }
    }
}
