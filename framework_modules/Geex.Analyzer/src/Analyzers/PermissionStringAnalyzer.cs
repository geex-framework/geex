using System;
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
            "权限字符串格式错误 '{0}'。base构造函数接收的最终权限字符串格式应为: <prefix>_<gqlObjectName>_<gqlFieldName>，如: identity_mutation_createUser",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Permission strings passing to base constructor must follow the format: <prefix>_<gqlObjectName>_<gqlFieldName>");

        public static readonly DiagnosticDescriptor PermissionFieldNotAllowedRule = new DiagnosticDescriptor(
            "GEEX005",
            "Permission strings must be declared as properties",
            "权限字符串 '{0}' 不允许使用字段声明，必须使用属性声明",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Permission strings must be declared as properties, not fields");

        // 权限字符串验证正则表达式（与 PermissionString 中的相同）
        private static readonly Regex PermissionValidator =
            new Regex(@"^(?<module>[a-zA-Z0-9]+)_(?<type>[a-zA-Z0-9]+)_(?<field>[a-zA-Z0-9]+)$", RegexOptions.Compiled);
        private static readonly Regex CtorParamValidator =
            new Regex(@"\w+\(string\s+(?<param>\w+)\)\s*:\s*base\(.*(?<!\w)\1\}*""*\s*\)", RegexOptions.Compiled);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(InvalidPermissionFormatRule, PermissionFieldNotAllowedRule);

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
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                if (fieldSymbol?.ContainingType == null)
                    continue;

                if (!IsAppPermissionDerivedType(fieldSymbol.ContainingType))
                    continue;

                // 检查字段类型是否为 AppPermission 派生类型
                if (fieldSymbol.Type is INamedTypeSymbol fieldType && IsAppPermissionDerivedType(fieldType))
                {
                    // 如果字段是静态的并且是 AppPermission 派生类型的实例，则报告错误
                    if (fieldSymbol.IsStatic)
                    {
                        var diagnostic = Diagnostic.Create(
                            PermissionFieldNotAllowedRule,
                            variable.GetLocation(),
                            fieldSymbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }

                // 检查字段初始化器中的权限字符串
                if (variable.Initializer?.Value is BaseObjectCreationExpressionSyntax objectCreation)
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
            if (propertyDeclaration.Initializer?.Value is BaseObjectCreationExpressionSyntax objectCreation)
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

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context, BaseObjectCreationExpressionSyntax objectCreation)
        {
            if (objectCreation.ArgumentList?.Arguments.Count > 0)
            {
                var firstArgument = objectCreation.ArgumentList.Arguments[0];

                // 获取对象创建的类型信息
                var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation);
                if (typeInfo.Type is INamedTypeSymbol namedType && IsAppPermissionDerivedType(namedType))
                {
                    // 查找对应的构造函数，看是否有字符串拼接
                    var constructorSymbol = context.SemanticModel.GetSymbolInfo(objectCreation).Symbol as IMethodSymbol;
                    if (constructorSymbol != null)
                    {
                        // 尝试找到构造函数的语法节点
                        var constructorSyntax = FindConstructorSyntax(context, constructorSymbol);
                        if (constructorSyntax != null)
                        {
                            // 检查是否有字符串拼接或插值
                            var concatenationPrefix = ExtractConstructorPrefix(constructorSyntax);
                            if (!string.IsNullOrEmpty(concatenationPrefix))
                            {
                                AnalyzePermissionStringWithPrefix(context, firstArgument.Expression, concatenationPrefix);
                                return;
                            }

                            // 检查是否调用了基类构造函数
                            var basePrefix = ExtractBasePrefixThroughInheritance(context, constructorSyntax);
                            if (!string.IsNullOrEmpty(basePrefix))
                            {
                                AnalyzePermissionStringWithPrefix(context, firstArgument.Expression, basePrefix);
                                return;
                            }
                        }
                    }
                }

                AnalyzePermissionStringExpression(context, firstArgument.Expression);
            }
        }

        private static ConstructorDeclarationSyntax FindConstructorSyntax(SyntaxNodeAnalysisContext context, IMethodSymbol constructorSymbol)
        {
            // 尝试从符号中获取语法引用
            var syntaxReferences = constructorSymbol.DeclaringSyntaxReferences;
            foreach (var syntaxRef in syntaxReferences)
            {
                if (syntaxRef.GetSyntax() is ConstructorDeclarationSyntax constructorSyntax)
                {
                    return constructorSyntax;
                }
            }
            return null;
        }

        private static string ExtractConcatenationPrefix(ConstructorDeclarationSyntax constructorSyntax)
        {
            if (constructorSyntax.Initializer?.ArgumentList?.Arguments.Count > 0)
            {
                var firstArg = constructorSyntax.Initializer.ArgumentList.Arguments[0].Expression;

                // 处理二元表达式（字符串拼接）
                if (firstArg is BinaryExpressionSyntax binaryExpr && binaryExpr.IsKind(SyntaxKind.AddExpression))
                {
                    // 提取左侧的字符串字面量作为前缀
                    if (binaryExpr.Left is LiteralExpressionSyntax literal &&
                        literal.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        return literal.Token.ValueText;
                    }
                }

                // 处理插值字符串
                if (firstArg is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    return ExtractInterpolatedStringPrefix(interpolatedString);
                }
            }
            return string.Empty;
        }

        private static string ExtractConstructorPrefix(ConstructorDeclarationSyntax constructorSyntax)
        {
            // 首先检查当前构造函数的 base 调用
            var prefix = ExtractConcatenationPrefix(constructorSyntax);
            if (!string.IsNullOrEmpty(prefix))
            {
                return prefix;
            }

            return string.Empty;
        }

        private static string ExtractBasePrefixThroughInheritance(SyntaxNodeAnalysisContext context, ConstructorDeclarationSyntax constructorSyntax)
        {
            // 如果当前构造函数调用了基类构造函数，且基类构造函数有字符串拼接
            if (constructorSyntax.Initializer?.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword) == true)
            {
                // 获取当前类型的基类
                var currentType = context.SemanticModel.GetDeclaredSymbol(constructorSyntax)?.ContainingType;
                if (currentType?.BaseType != null && IsAppPermissionDerivedType(currentType.BaseType))
                {
                    // 查找基类的构造函数
                    var baseConstructors = currentType.BaseType.Constructors;
                    foreach (var baseConstructor in baseConstructors)
                    {
                        if (baseConstructor.Parameters.Length == 1 &&
                            baseConstructor.Parameters[0].Type.SpecialType == SpecialType.System_String)
                        {
                            var baseConstructorSyntax = FindConstructorSyntax(context, baseConstructor);
                            if (baseConstructorSyntax != null)
                            {
                                var basePrefix = ExtractConcatenationPrefix(baseConstructorSyntax);
                                if (!string.IsNullOrEmpty(basePrefix))
                                {
                                    return basePrefix;
                                }
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static string ExtractInterpolatedStringPrefix(InterpolatedStringExpressionSyntax interpolatedString)
        {
            var textParts = new System.Collections.Generic.List<string>();

            foreach (var content in interpolatedString.Contents)
            {
                if (content is InterpolatedStringTextSyntax textPart)
                {
                    textParts.Add(textPart.TextToken.ValueText);
                }
                else if (content is InterpolationSyntax interpolation)
                {
                    // 对于插值部分，如果是 Prefix 这样的常量，尝试解析
                    if (interpolation.Expression is IdentifierNameSyntax identifier || interpolation.Expression is LiteralExpressionSyntax literal)
                    {
                        textParts.Add("PREFIX_PLACEHOLDER"); // 这里需要实际解析常量值
                    }
                }
            }

            var result = string.Join("", textParts);
            // 如果包含 PREFIX_PLACEHOLDER，尝试从类中查找 Prefix 常量
            if (result.Contains("PREFIX_PLACEHOLDER"))
            {
                // 这里需要更复杂的逻辑来解析实际的前缀值
                // 简化处理：假设格式是 "{Prefix}_{value}"，返回一个占位符
                return "PREFIX_"; // 返回一个指示需要前缀的标记
            }

            return result;
        }

        private static void AnalyzePermissionStringWithPrefix(SyntaxNodeAnalysisContext context, ExpressionSyntax expression, string prefix)
        {
            // 检查字符串字面量
            if (expression is LiteralExpressionSyntax literal &&
                literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                var permissionString = prefix + literal.Token.ValueText;
                ValidatePermissionString(context, literal, permissionString);
            }
            // 对于其他复杂表达式，回退到普通分析
            else
            {
                AnalyzePermissionStringExpression(context, expression, prefix);
            }
        }

        private static void AnalyzePermissionStringExpression(SyntaxNodeAnalysisContext context,
            ExpressionSyntax expression, string prefix = default)
        {
            prefix ??= string.Empty;
            // 检查字符串字面量
            if (expression is LiteralExpressionSyntax literal &&
                literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                var permissionString = prefix + literal.Token.ValueText;
                ValidatePermissionString(context, literal, permissionString);
            }
            // 检查字符串插值表达式
            else if (expression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                // 尝试解析插值字符串中的内容
                var permissionString = prefix + TryEvaluateInterpolatedString(context, interpolatedString);
                ValidatePermissionString(context, interpolatedString, permissionString);
            }
            // 检查二元表达式（字符串拼接）
            else if (expression is BinaryExpressionSyntax binaryExpression &&
                     binaryExpression.IsKind(SyntaxKind.AddExpression))
            {
                var permissionString = prefix + TryEvaluateStringConcatenation(context, binaryExpression);
                ValidatePermissionString(context, binaryExpression, permissionString);
            }
            // 检查标识符引用（变量或属性）
            else if (expression is IdentifierNameSyntax identifier)
            {
                TryEvaluateIdentifier(context, identifier);
            }
            // 检查成员访问表达式（如 Prefix.Value）
            else if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                TryEvaluateMemberAccess(context, memberAccess);
            }
        }

        private static string TryEvaluateInterpolatedString(SyntaxNodeAnalysisContext context, InterpolatedStringExpressionSyntax interpolatedString)
        {
            // 简单处理：如果只有文本和简单表达式，尝试提取
            var textParts = new System.Collections.Generic.List<string>();

            foreach (var content in interpolatedString.Contents)
            {
                if (content is InterpolatedStringTextSyntax textPart)
                {
                    textParts.Add(textPart.TextToken.ValueText);
                }
                else if (content is InterpolationSyntax interpolation)
                {
                    // 尝试解析插值表达式中的标识符
                    if (interpolation.Expression is IdentifierNameSyntax identifier)
                    {
                        var identifierSymbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                        if (identifierSymbol != null && identifierSymbol.Kind == SymbolKind.Field)
                        {
                            // 尝试获取字段的常量值
                            var constantValue = context.SemanticModel.GetConstantValue(identifier);
                            if (constantValue.HasValue && constantValue.Value is string strValue)
                            {
                                textParts.Add(strValue);
                            }
                        }
                    }
                    else if (interpolation.Expression is LiteralExpressionSyntax literalExpressionSyntax)
                    {
                        var constantValue = context.SemanticModel.GetConstantValue(literalExpressionSyntax);
                        if (constantValue is { HasValue: true, Value: string strValue })
                        {
                            textParts.Add(strValue);
                        }
                    }
                }
            }

            if (textParts.Count > 0)
            {
                var permissionString = string.Join("", textParts);
                return permissionString;
            }
            return String.Empty;
        }

        private static string TryEvaluateStringConcatenation(SyntaxNodeAnalysisContext context, BinaryExpressionSyntax binaryExpression)
        {
            // 递归尝试提取字符串拼接的各部分
            var leftParts = ExtractStringParts(context, binaryExpression.Left);
            var rightParts = ExtractStringParts(context, binaryExpression.Right);

            if (leftParts.Length > 0 && rightParts.Length > 0)
            {
                var permissionString = leftParts + rightParts;
                return permissionString;
            }
            return string.Empty;
        }

        private static void TryEvaluateIdentifier(SyntaxNodeAnalysisContext context, IdentifierNameSyntax identifier)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol != null)
            {
                // 如果是常量或只读字段，获取其值
                if (symbol is IFieldSymbol fieldSymbol &&
                   (fieldSymbol.IsConst || (fieldSymbol.IsReadOnly && fieldSymbol.IsStatic)))
                {
                    var constantValue = context.SemanticModel.GetConstantValue(identifier);
                    if (constantValue.HasValue && constantValue.Value is string strValue)
                    {
                        ValidatePermissionString(context, identifier, strValue);
                    }
                }
                // 如果是属性，尝试获取其值
                else if (symbol is IPropertySymbol propertySymbol && propertySymbol.IsStatic)
                {
                    // 这里只能做简单的格式验证
                }
            }
        }

        private static void TryEvaluateMemberAccess(SyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax memberAccess)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (symbol != null && symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst)
            {
                var constantValue = context.SemanticModel.GetConstantValue(memberAccess);
                if (constantValue.HasValue && constantValue.Value is string strValue)
                {
                    ValidatePermissionString(context, memberAccess, strValue);
                }
            }
        }

        private static string ExtractStringParts(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax literal &&
                literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                return literal.Token.ValueText;
            }
            else if (expression is IdentifierNameSyntax identifier)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
                if (symbol != null && symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst)
                {
                    var constantValue = context.SemanticModel.GetConstantValue(identifier);
                    if (constantValue.HasValue && constantValue.Value is string strValue)
                    {
                        return strValue;
                    }
                }
            }
            else if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
                if (symbol != null && symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst)
                {
                    var constantValue = context.SemanticModel.GetConstantValue(memberAccess);
                    if (constantValue.HasValue && constantValue.Value is string strValue)
                    {
                        return strValue;
                    }
                }
            }
            else if (expression is BinaryExpressionSyntax binaryExpression &&
                     binaryExpression.IsKind(SyntaxKind.AddExpression))
            {
                var leftPart = ExtractStringParts(context, binaryExpression.Left);
                var rightPart = ExtractStringParts(context, binaryExpression.Right);
                return leftPart + rightPart;
            }
            else if (expression is InterpolatedStringExpressionSyntax interpolatedString)
            {
                var textParts = new System.Collections.Generic.List<string>();
                foreach (var content in interpolatedString.Contents)
                {
                    if (content is InterpolatedStringTextSyntax textPart)
                    {
                        textParts.Add(textPart.TextToken.ValueText);
                    }
                    else if (content is InterpolationSyntax interpolation)
                    {
                        var interpolationValue = ExtractStringParts(context, interpolation.Expression);
                        if (!string.IsNullOrEmpty(interpolationValue))
                        {
                            textParts.Add(interpolationValue);
                        }
                    }
                }
                return string.Join("", textParts);
            }
            return string.Empty;
        }

        private static void ValidatePermissionString(SyntaxNodeAnalysisContext context, SyntaxNode node, string permissionString)
        {
            if (context.Node is ConstructorDeclarationSyntax)
            {
                if (!CtorParamValidator.IsMatch(context.Node.ToFullString()))
                {
                    var diagnostic = Diagnostic.Create(
                    InvalidPermissionFormatRule,
                    node.GetLocation(),
                    permissionString);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (string.IsNullOrEmpty(permissionString) || !PermissionValidator.IsMatch(permissionString))
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
