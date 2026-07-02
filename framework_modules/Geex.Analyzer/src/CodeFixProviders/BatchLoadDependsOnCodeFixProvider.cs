using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Geex.Analyzer.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Geex.Analyzer.CodeFixProviders
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BatchLoadDependsOnCodeFixProvider)), Shared]
    public sealed class BatchLoadDependsOnCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(BatchLoadDependsOnAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                var navigationPropertyName = ExtractNavigationPropertyName(diagnostic);
                if (string.IsNullOrEmpty(navigationPropertyName))
                {
                    continue;
                }

                var propertyDeclaration = root.FindNode(diagnostic.Location.SourceSpan)
                    .FirstAncestorOrSelf<PropertyDeclarationSyntax>();
                if (propertyDeclaration == null)
                {
                    continue;
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Add BatchLoadDependsOn for '{navigationPropertyName}'",
                        createChangedDocument: cancellationToken =>
                            AddBatchLoadDependsOnAttributeAsync(
                                context.Document,
                                propertyDeclaration,
                                navigationPropertyName,
                                cancellationToken),
                        equivalenceKey: $"{nameof(BatchLoadDependsOnCodeFixProvider)}_{navigationPropertyName}"),
                    diagnostic);
            }
        }

        private static string? ExtractNavigationPropertyName(Diagnostic diagnostic)
        {
            if (diagnostic.Properties.TryGetValue("NavigationPropertyName", out var navigationPropertyName) &&
                !string.IsNullOrEmpty(navigationPropertyName))
            {
                return navigationPropertyName;
            }

            return null;
        }

        private static async Task<Document> AddBatchLoadDependsOnAttributeAsync(
            Document document,
            PropertyDeclarationSyntax propertyDeclaration,
            string navigationPropertyName,
            CancellationToken cancellationToken)
        {
            if (propertyDeclaration.AttributeLists.Any(list =>
                    list.Attributes.Any(existing =>
                        existing.Name.ToString() is "BatchLoadDependsOn" or "BatchLoadDependsOnAttribute" &&
                        existing.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString() ==
                        $"nameof({navigationPropertyName})")))
            {
                return document;
            }

            var attribute = SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("Geex.Gql.Attributes.BatchLoadDependsOn"))
                .WithArgumentList(
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.IdentifierName("nameof"))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.IdentifierName(navigationPropertyName)))))))));

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var newProperty = propertyDeclaration.AddAttributeLists(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(attribute)));
            editor.ReplaceNode(propertyDeclaration, newProperty);
            return editor.GetChangedDocument();
        }
    }
}
