//using System.Collections.Immutable;
//using System.Composition;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CodeFixes;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Editing;
//using Microsoft.CodeAnalysis.CodeActions;

//namespace Geex.Analyzer
//{
//    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableAttributeCodeFixProvider)), Shared]
//    public class NullableAttributeCodeFixProvider : CodeFixProvider
//    {
//        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NullableAttributeAnalyzer.DiagnosticId);

//        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

//        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
//        {
//            // 调试输出
//            System.Diagnostics.Debug.WriteLine("RegisterCodeFixesAsync called");

//            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
//            var diagnostic = context.Diagnostics[0];
//            var node = root.FindNode(diagnostic.Location.SourceSpan);

//            // 修正：确保定位到属性或参数节点
//            var property = node.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
//            var parameter = node.FirstAncestorOrSelf<ParameterSyntax>();

//            SyntaxNode targetNode = property ?? (SyntaxNode)parameter;
//            if (targetNode == null)
//                return;

//            context.RegisterCodeFix(
//                CodeAction.Create(
//                    title: "Add [Nullable] attribute",
//                    createChangedDocument: c => AddNullableAttributeAsync(context.Document, targetNode, c),
//                    equivalenceKey: "AddNullableAttribute",
//                    priority: CodeActionPriority.High
//                    ),
//                diagnostic);
//        }

//        private async Task<Document> AddNullableAttributeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
//        {
//            // 调试输出
//            System.Diagnostics.Debug.WriteLine("AddNullableAttributeAsync called");

//            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

//            if (node is PropertyDeclarationSyntax property)
//            {
//                var newProperty = property.AddAttributeLists(
//                    SyntaxFactory.AttributeList(
//                        SyntaxFactory.SingletonSeparatedList(
//                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("Nullable")))));
//                editor.ReplaceNode(property, newProperty);
//            }
//            else if (node is ParameterSyntax parameter)
//            {
//                var newParameter = parameter.AddAttributeLists(
//                    SyntaxFactory.AttributeList(
//                        SyntaxFactory.SingletonSeparatedList(
//                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("Nullable")))));
//                editor.ReplaceNode(parameter, newParameter);
//            }
//            return editor.GetChangedDocument();
//        }
//    }
//}
