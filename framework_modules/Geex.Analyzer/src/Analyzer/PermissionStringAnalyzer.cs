using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Geex.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PermissionStringAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor InvalidPermissionFormatRule = new DiagnosticDescriptor(
            "GEEX004",
            "Invalid permission string format",
            "权限字符串格式错误 '{0}'。权限字符串格式应为: <prefix>_<gqlObjectName>_<fieldName>，如: identity_mutation_createUser",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Permission strings must follow the format: <prefix>_<gqlObjectName>_<fieldName>");

        // 权限字符串验证正则表达式（与 PermissionString 中的相同）
        private static readonly Regex PermissionValidator = 
            new Regex(@"^(?<module>\w+)_(?<type>\w+)_((?<field>\w+))+$", RegexOptions.Compiled);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(InvalidPermissionFormatRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeConstructorDeclaration, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
        {
            var constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            
            // 检查构造函数是否属于 AppPermission 派生类
            var constructorSymbol = semanticModel.GetDeclaredSymbol(constructorDeclaration);
            if (constructorSymbol?.ContainingType == null)
                return;

            if (!IsAppPermissionDerivedType(constructorSymbol.ContainingType))
                return;

            // 检查构造函数中的 base() 调用
            if (constructorDeclaration.Initializer?.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword) == true)
            {
                AnalyzeBaseConstructorCall(context, constructorDeclaration.Initializer);
            }

            // 检查构造函数体中的赋值
            if (constructorDeclaration.Body != null)
            {
                foreach (var statement in constructorDeclaration.Body.Statements)
                {
                    if (statement is ExpressionStatementSyntax expressionStatement &&
                        expressionStatement.Expression is AssignmentExpressionSyntax assignment)
                    {
                        AnalyzeAssignmentExpression(context, assignment);
                    }
                }
            }
        }

        private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            // 检查字段是否属于 AppPermission 派生类
            var fieldSymbol = semanticModel.GetDeclaredSymbol(fieldDeclaration.Declaration.Variables[0]);
            if (fieldSymbol?.ContainingType == null)
                return;

            if (!IsAppPermissionDerivedType(fieldSymbol.ContainingType))
                return;

            // 检查字段初始化器中的权限字符串
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                if (variable.Initializer?.Value is ObjectCreationExpressionSyntax objectCreation)
                {
                    AnalyzeObjectCreation(context, objectCreation);
                }
            }
        }

        private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            // 检查属性是否属于 AppPermission 派生类
            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration);
            if (propertySymbol?.ContainingType == null)
                return;

            if (!IsAppPermissionDerivedType(propertySymbol.ContainingType))
                return;

            // 检查属性初始化器中的权限字符串
            if (propertyDeclaration.Initializer?.Value is ObjectCreationExpressionSyntax objectCreation)
            {
                AnalyzeObjectCreation(context, objectCreation);
            }
        }

        private static void AnalyzeBaseConstructorCall(SyntaxNodeAnalysisContext context, ConstructorInitializerSyntax initializer)
        {
            if (initializer.ArgumentList?.Arguments.Count > 0)
            {
                var firstArgument = initializer.ArgumentList.Arguments[0];
                AnalyzePermissionStringExpression(context, firstArgument.Expression);
            }
        }

        private static void AnalyzeAssignmentExpression(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment)
        {
            AnalyzePermissionStringExpression(context, assignment.Right);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation)
        {
            if (objectCreation.ArgumentList?.Arguments.Count > 0)
            {
                var firstArgument = objectCreation.ArgumentList.Arguments[0];
                AnalyzePermissionStringExpression(context, firstArgument.Expression);
            }
        }

        private static void AnalyzePermissionStringExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            // 检查字符串字面量
            if (expression is LiteralExpressionSyntax literal &&
                literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                var permissionString = literal.Token.ValueText;
                ValidatePermissionString(context, literal, permissionString);
            }
            // 检查字符串插值表达式
            else if (expression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                // 尝试提取字符串插值的内容进行基本验证
                var content = interpolatedString.Contents;
                if (content.Count == 1 && content[0] is InterpolatedStringTextSyntax textPart)
                {
                    var permissionString = textPart.TextToken.ValueText;
                    ValidatePermissionString(context, interpolatedString, permissionString);
                }
            }
            // 检查二元表达式（字符串拼接）
            else if (expression is BinaryExpressionSyntax binaryExpression &&
                     binaryExpression.IsKind(SyntaxKind.AddExpression))
            {
                // 对于字符串拼接，我们尽量提取字面量部分进行验证
                var leftLiteral = ExtractStringLiteral(binaryExpression.Left);
                var rightLiteral = ExtractStringLiteral(binaryExpression.Right);
                
                if (!string.IsNullOrEmpty(leftLiteral) && !string.IsNullOrEmpty(rightLiteral))
                {
                    var permissionString = leftLiteral + rightLiteral;
                    ValidatePermissionString(context, binaryExpression, permissionString);
                }
            }
        }

        private static string ExtractStringLiteral(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax literal &&
                literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                return literal.Token.ValueText;
            }
            return null;
        }

        private static void ValidatePermissionString(SyntaxNodeAnalysisContext context, SyntaxNode node, string permissionString)
        {
            if (string.IsNullOrEmpty(permissionString))
                return;

            if (!PermissionValidator.IsMatch(permissionString))
            {
                var diagnostic = Diagnostic.Create(
                    InvalidPermissionFormatRule,
                    node.GetLocation(),
                    permissionString);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsAppPermissionDerivedType(INamedTypeSymbol typeSymbol)
        {
            var baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == "AppPermission" || 
                    (baseType.IsGenericType && baseType.Name == "AppPermission"))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }
    }
}
