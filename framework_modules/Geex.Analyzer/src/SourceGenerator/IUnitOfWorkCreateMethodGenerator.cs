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
    public class IUnitOfWorkCreateMethodGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Register the syntax provider to collect all class declarations
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is ClassDeclarationSyntax,
                    transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node
                );

            // Combine the class declarations with the compilation
            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
                context.CompilationProvider.Combine(classDeclarations.Collect());

            // Register the source output
            context.RegisterSourceOutput(compilationAndClasses, 
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                return;
            }

            // Filter and process entity types
            var entityTypes = new List<INamedTypeSymbol>();
            var debugSource = new StringBuilder("");

            foreach (var classDeclaration in classes)
            {
                var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

                if (symbol == null)
                    continue;
                
                // Check if the class inherits from Entity
                if (IsEntity(symbol, debugSource))
                {
                    entityTypes.Add(symbol);
                }
            }

            // Use distinct entities
            entityTypes = entityTypes.Distinct(new SymbolComparer()).ToList();

            // Generate extension methods for each entity type
            foreach (var entityType in entityTypes)
            {
                var source = GenerateExtensionMethod(entityType);
                if (!string.IsNullOrEmpty(source))
                {
                    context.AddSource($"{entityType.Name}_IUnitOfWorkExtensions.g.cs", source);
                }
            }
        }

        private static bool IsEntity(INamedTypeSymbol symbol, StringBuilder debugSource)
        {
            // Check if the class inherits from Entity
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

        private static string GenerateExtensionMethod(INamedTypeSymbol entityType)
        {
            var namespaceName = entityType.ContainingNamespace.ToDisplayString();
            var entityName = entityType.Name;

            // Get public instance constructors that have IUnitOfWork as the last parameter
            var constructors = entityType.Constructors
                .Where(x => x.Parameters.LastOrDefault()?.Type?.Name == "IUnitOfWork")
                .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
                .ToList();

            // If no matching constructors, return empty string
            if (!constructors.Any())
            {
                return string.Empty;
            }

            var methods = new StringBuilder();

            foreach (var constructor in constructors)
            {
                var parameters = constructor.Parameters.Slice(0, constructor.Parameters.Length - 1);

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

                var argumentList = string.Join(", ", parameters.Select(p =>
                {
                    var type = p.Type.ToDisplayString();
                    return $"({type}){p.Name}";
                }));

                methods.AppendLine($@"
    public static {entityName} Create(this IUnitOfWork _{(string.IsNullOrEmpty(parameterList) ? "" : ", " + parameterList)})
    {{
        return ActivatorUtilities.CreateInstance<{entityName}>(_.ServiceProvider{(string.IsNullOrEmpty(argumentList) ? "" : ", " + argumentList)}, _);
    }}");
            }

            var source = $@"
// <auto-generated />
using System;
using Microsoft.Extensions.DependencyInjection;
using Geex;

namespace {namespaceName} {{
    public static class IUnitOfWorkExtensions_{entityName} {{
        {methods}
    }}
}}
";
            return source;
        }

        private class SymbolComparer : EqualityComparer<INamedTypeSymbol>
        {
            public override bool Equals(INamedTypeSymbol x, INamedTypeSymbol y)
            {
                return x.Name == y.Name;
            }

            public override int GetHashCode(INamedTypeSymbol obj)
            {
                return obj.Name.GetHashCode();
            }
        }
    }
}