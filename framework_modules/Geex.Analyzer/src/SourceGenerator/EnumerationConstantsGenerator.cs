using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Geex.Analyzer.SourceGenerator
{
    [Generator]
    public class EnumerationConstantsGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 注册语法提供器收集所有类声明
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax,
                    transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node
                );

            // 将类声明与编译合并
            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
                context.CompilationProvider.Combine(classDeclarations.Collect());

            // 注册源输出
            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                return;
            }

            // 按命名空间分组的枚举类型
            var enumerationsByNamespace = new Dictionary<string, List<EnumerationInfo>>();

            foreach (var classDeclaration in classes)
            {
                var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

                if (symbol == null)
                    continue;

                // 检查类是否继承自 Enumeration<T>
                if (IsEnumerationType(symbol))
                {
                    var enumerationInfo = ExtractEnumerationInfo(symbol, classDeclaration);
                    if (enumerationInfo != null)
                    {
                        var namespaceName = symbol.ContainingNamespace.ToDisplayString();
                        if (!enumerationsByNamespace.ContainsKey(namespaceName))
                        {
                            enumerationsByNamespace[namespaceName] = new List<EnumerationInfo>();
                        }
                        enumerationsByNamespace[namespaceName].Add(enumerationInfo);
                    }
                }
            }

            // 为每个命名空间生成 Enumerations 类
            foreach (var kvp in enumerationsByNamespace)
            {
                var namespaceName = kvp.Key;
                var enumerations = kvp.Value;

                var source = GenerateEnumerationsClass(namespaceName, enumerations);
                if (!string.IsNullOrEmpty(source))
                {
                    var fileName = namespaceName.Replace(".", "_") + "_Enumerations.g.cs";
                    context.AddSource(fileName, source);
                }
            }
        }

        private static bool IsEnumerationType(INamedTypeSymbol symbol)
        {
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.Name == "Enumeration")
                {
                    return true;
                }
                if (baseType.Name == "Enumeration")
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        private static EnumerationInfo ExtractEnumerationInfo(INamedTypeSymbol symbol, ClassDeclarationSyntax classDeclaration)
        {
            var properties = new List<PropertyInfo>();
            
            // 首先提取所有常量字段的值
            var constantFields = new Dictionary<string, string>();
            foreach (var member in symbol.GetMembers())
            {
                if (member is IFieldSymbol field &&
                    field.IsConst &&
                    field.Type.SpecialType == SpecialType.System_String)
                {
                    var constantValue = field.ConstantValue?.ToString();
                    if (!string.IsNullOrEmpty(constantValue))
                    {
                        constantFields[field.Name] = constantValue;
                    }
                }
            }

            // 提取所有公共静态属性
            foreach (var member in symbol.GetMembers())
            {
                if (member is IPropertySymbol property &&
                    property.IsStatic &&
                    property.DeclaredAccessibility == Accessibility.Public &&
                    property.Type.Name == symbol.Name) // 确保属性类型是枚举类型本身
                {
                    // 尝试获取属性的值
                    var valueConstant = GetPropertyValue(property, classDeclaration, constantFields);
                    if (!string.IsNullOrEmpty(valueConstant))
                    {
                        properties.Add(new PropertyInfo
                        {
                            Name = property.Name,
                            Value = valueConstant
                        });
                    }
                }
            }

            if (properties.Count > 0)
            {
                return new EnumerationInfo
                {
                    ClassName = symbol.Name,
                    Properties = properties
                };
            }

            return null;
        }

        private static string GetPropertyValue(IPropertySymbol property, ClassDeclarationSyntax classDeclaration, Dictionary<string, string> constantFields)
        {
            // 查找对应的属性声明
            foreach (var member in classDeclaration.Members)
            {
                if (member is PropertyDeclarationSyntax propDecl &&
                    propDecl.Identifier.ValueText == property.Name &&
                    propDecl.Initializer?.Value != null)
                {
                    return ExtractValueFromExpression(propDecl.Initializer.Value, constantFields);
                }
            }

            return null;
        }

        private static string ExtractValueFromExpression(ExpressionSyntax expression, Dictionary<string, string> constantFields)
        {
            // 处理 new 表达式，如 new("value")
            if (expression is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.ArgumentList?.Arguments.Count > 0)
            {
                var firstArg = objectCreation.ArgumentList.Arguments[0];
                return ExtractStringLiteral(firstArg.Expression, constantFields);
            }

            // 处理静态方法调用，如 FromNameAndValue(nameof(Sub), _Sub)
            if (expression is InvocationExpressionSyntax invocation &&
                invocation.ArgumentList?.Arguments.Count >= 2)
            {
                var secondArg = invocation.ArgumentList.Arguments[1];
                return ExtractStringLiteral(secondArg.Expression, constantFields);
            }

            return ExtractStringLiteral(expression, constantFields);
        }

        private static string ExtractStringLiteral(ExpressionSyntax expression, Dictionary<string, string> constantFields)
        {
            // 处理字符串字面量
            if (expression is LiteralExpressionSyntax literal &&
                literal.Kind() == SyntaxKind.StringLiteralExpression)
            {
                return literal.Token.ValueText;
            }

            // 处理标识符（常量引用）
            if (expression is IdentifierNameSyntax identifier)
            {
                // 尝试从常量字段中查找值
                if (constantFields.TryGetValue(identifier.Identifier.ValueText, out var constantValue))
                {
                    return constantValue;
                }
            }

            return null;
        }

        private static string GenerateEnumerationsClass(string namespaceName, List<EnumerationInfo> enumerations)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated />");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 自动生成的枚举常量字符串类");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class Enumerations");
            sb.AppendLine("    {");

            foreach (var enumeration in enumerations.OrderBy(e => e.ClassName))
            {
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// {enumeration.ClassName} 枚举的常量字符串");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static class {enumeration.ClassName}");
                sb.AppendLine("        {");

                foreach (var property in enumeration.Properties.OrderBy(p => p.Name))
                {
                    sb.AppendLine($"            /// <summary>");
                    sb.AppendLine($"            /// {enumeration.ClassName}.{property.Name} 的值");
                    sb.AppendLine($"            /// </summary>");
                    sb.AppendLine($"            public const string {property.Name} = \"{property.Value}\";");
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private class EnumerationInfo
        {
            public string ClassName { get; set; }
            public List<PropertyInfo> Properties { get; set; }
        }

        private class PropertyInfo
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}
