﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

[Generator]
public class IUnitOfWorkCreateMethodGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // 注册语法接收器
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // 获取语法接收器
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return;

        // 获取编译器中的类型信息
        var compilation = context.Compilation;

        // 过滤出需要的类型
        var entityTypes = new List<INamedTypeSymbol>();
        var debugSource = new StringBuilder("");
        foreach (var classDeclaration in receiver.CandidateClasses)
        {
            var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (symbol == null)
                continue;
            // 判断是否是 Entity 基类的子类
            if (IsEntity(symbol, debugSource))
            {
                entityTypes.Add(symbol);
            }
        }
        //context.AddSource($"IUnitOfWorkExtensions.g.log", debugSource.ToString());

        entityTypes = entityTypes.Distinct(new Comparer()).ToList();
        // 为每个实体生成扩展方法
        foreach (var entityType in entityTypes)
        {
            var source = GenerateExtensionMethod(entityType);
            if (!string.IsNullOrEmpty(source))
            {
                context.AddSource($"{entityType.Name}_IUnitOfWorkExtensions.g.cs", source);
            }
        }
    }

    class Comparer : EqualityComparer<INamedTypeSymbol>
    {
        /// <inheritdoc />
        public override bool Equals(INamedTypeSymbol x, INamedTypeSymbol y)
        {
            return x.Name == y.Name;
        }

        /// <inheritdoc />
        public override int GetHashCode(INamedTypeSymbol obj)
        {
            return obj.Name.GetHashCode();
        }
    }
    private bool IsEntity(INamedTypeSymbol symbol, StringBuilder debugSource)
    {
        // 判断是否继承自 Entity<T>，根据您的基类修改判断逻辑
        var baseType = symbol.BaseType;
        debugSource.Append(symbol.Name + ":");
        while (baseType != null)
        {
            debugSource.Append(baseType.Name + ":");
            if (baseType.Name == "Entity")
                return true;
            baseType = baseType.BaseType;
        }
        debugSource.AppendLine();
        return false;
    }

    private string GenerateExtensionMethod(INamedTypeSymbol entityType)
{
    var namespaceName = entityType.ContainingNamespace.ToDisplayString();
    var entityName = entityType.Name;

    // Get public instance constructors
    var constructors = entityType.Constructors
        .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic);

    // If no public constructors, try to use any parameterless constructor
    if (!constructors.Any())
    {
        var parameterlessConstructor = entityType.Constructors.FirstOrDefault(c => c.Parameters.Length == 0);
        if (parameterlessConstructor != null)
        {
            constructors = new[] { parameterlessConstructor };
        }
    }

    if (!constructors.Any())
    {
        return default;
    }

    var methods = new StringBuilder();

    foreach (var constructor in constructors)
    {
        var parameters = constructor.Parameters;

        var parameterList = string.Join(", ", parameters.Select(p =>
        {
            var type = p.Type.ToDisplayString();
            var name = p.Name;

            if (p.HasExplicitDefaultValue)
            {
                var defaultValue = p.ExplicitDefaultValue;

                string defaultValueCode;
                if (defaultValue == default)
                {
                    defaultValueCode = "default";
                }
                else if (defaultValue is string)
                {
                    defaultValueCode = $"\"{defaultValue}\"";
                }
                else if (defaultValue is char)
                {
                    defaultValueCode = $"'{defaultValue}'";
                }
                else if (defaultValue is bool)
                {
                    defaultValueCode = defaultValue.ToString().ToLower();
                }
                else if (p.Type.TypeKind == TypeKind.Enum)
                {
                    defaultValueCode = $"{type}.{defaultValue}";
                }
                else
                {
                    defaultValueCode = defaultValue.ToString();
                }

                return $"{type} {name} = {defaultValueCode}";
            }
            else
            {
                return $"{type} {name}";
            }
        }));

        var argumentList = string.Join(", ", parameters.Select(p => p.Name));

        methods.AppendLine($@"
    public static {entityName} Create(this IUnitOfWork uow{(string.IsNullOrEmpty(parameterList) ? "" : ", " + parameterList)})
    {{
        var instance = ActivatorUtilities.CreateInstance<{entityName}>(uow.ServiceProvider{(string.IsNullOrEmpty(argumentList) ? "" : ", " + argumentList)});
        return uow.Attach(instance);
    }}");
    }

    var source = $@"
using System;
using Microsoft.Extensions.DependencyInjection;
using Geex.Common;

namespace {namespaceName} {{
    public static class IUnitOfWorkExtensions_{entityName} {{
        {methods}
    }}
}}
";
    return source;
}

    class SyntaxReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // 查找所有的类声明
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                CandidateClasses.Add(classDeclaration);
            }
        }
    }
}