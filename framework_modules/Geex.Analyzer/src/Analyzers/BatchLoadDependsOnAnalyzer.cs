using System;
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
    public class BatchLoadDependsOnAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "GEEX007";

        public static readonly DiagnosticDescriptor MissingBatchLoadDependsOnRule = new DiagnosticDescriptor(
            DiagnosticId,
            "缺少 BatchLoadDependsOn",
            "计算属性 '{0}' 在 getter 中访问了 Lazy 导航 '{1}'，但未声明 [BatchLoadDependsOn]。",
            "GraphQL",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "当计算属性间接访问已注册的 Lazy 导航时，应声明 BatchLoadDependsOn 以供 AutoBatchLoad 扩展 batch 路径。");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MissingBatchLoadDependsOnRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
            var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration) as IPropertySymbol;
            if (propertySymbol == null ||
                propertySymbol.DeclaredAccessibility != Accessibility.Public ||
                propertySymbol.IsStatic)
            {
                return;
            }

            var containingType = propertySymbol.ContainingType;
            if (!IsEntityType(containingType))
            {
                return;
            }

            var containingTypeDeclaration = propertyDeclaration.Parent as TypeDeclarationSyntax;
            var lazyNavigationProperties = CollectLazyNavigationPropertyNames(
                containingType,
                containingTypeDeclaration,
                context.SemanticModel);
            if (lazyNavigationProperties.Count == 0)
            {
                return;
            }

            if (IsLazyNavigationProperty(propertySymbol) ||
                (lazyNavigationProperties.Contains(propertySymbol.Name) &&
                 ContainsLazyQueryInvocation(propertyDeclaration)))
            {
                return;
            }

            var getterRoot = GetGetterRoot(propertyDeclaration);
            if (getterRoot == null)
            {
                return;
            }

            var accessedLazyNavigations = CollectAccessedLazyNavigations(
                getterRoot,
                context.SemanticModel,
                containingType,
                containingTypeDeclaration,
                lazyNavigationProperties);
            if (accessedLazyNavigations.Count == 0)
            {
                return;
            }

            var declaredDependencies = GetDeclaredDependsOnNavigationNames(propertySymbol);
            foreach (var navigationName in accessedLazyNavigations)
            {
                if (declaredDependencies.Contains(navigationName))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    MissingBatchLoadDependsOnRule,
                    propertyDeclaration.Identifier.GetLocation(),
                    CreateProperties(navigationName),
                    propertySymbol.Name,
                    navigationName));
            }
        }

        private static bool IsEntityType(INamedTypeSymbol typeSymbol)
        {
            for (var current = typeSymbol; current != null; current = current.BaseType)
            {
                if (current.Name == "EntityBase")
                {
                    return true;
                }
            }

            return typeSymbol.AllInterfaces.Any(iface => iface.Name == "IEntityBase");
        }

        private static bool IsLazyNavigationProperty(IPropertySymbol propertySymbol) =>
            IsLazyNavigationType(propertySymbol.Type);

        private static bool IsLazyNavigationType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            {
                return false;
            }

            var definitionName = namedType.OriginalDefinition.Name;
            if (definitionName is not ("IQueryable`1" or "Lazy`1" or "ResettableLazy`1"))
            {
                return false;
            }

            return IsEntityTypeArgument(namedType.TypeArguments[0]);
        }

        private static bool IsEntityTypeArgument(ITypeSymbol typeArgument)
        {
            if (typeArgument is INamedTypeSymbol namedTypeArgument)
            {
                if (IsEntityType(namedTypeArgument))
                {
                    return true;
                }
            }

            return typeArgument.AllInterfaces.Any(iface => iface.Name == "IEntityBase");
        }

        private static ImmutableHashSet<string> CollectLazyNavigationPropertyNames(
            INamedTypeSymbol containingType,
            TypeDeclarationSyntax? typeDeclaration,
            SemanticModel semanticModel)
        {
            var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

            for (var type = containingType; type != null; type = type.BaseType)
            {
                CollectFromTypeSyntax(type, typeDeclaration, semanticModel, builder);

                foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.IsStatic)
                    {
                        continue;
                    }

                    if (IsLazyNavigationProperty(member))
                    {
                        builder.Add(member.Name);
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static void CollectFromTypeSyntax(
            INamedTypeSymbol typeSymbol,
            TypeDeclarationSyntax? currentTypeDeclaration,
            SemanticModel semanticModel,
            ImmutableHashSet<string>.Builder builder)
        {
            if (currentTypeDeclaration != null &&
                SymbolEqualityComparer.Default.Equals(
                    semanticModel.GetDeclaredSymbol(currentTypeDeclaration),
                    typeSymbol))
            {
                CollectFromConfigLazyQuery(currentTypeDeclaration, builder);
                CollectFromLazyQueryProperties(currentTypeDeclaration, semanticModel, builder);
                return;
            }

            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is not TypeDeclarationSyntax typeDeclaration)
                {
                    continue;
                }

                CollectFromConfigLazyQuery(typeDeclaration, builder);
                CollectFromLazyQueryProperties(typeDeclaration, semanticModel, builder);
            }
        }

        private static void CollectFromConfigLazyQuery(
            TypeDeclarationSyntax typeDeclaration,
            ImmutableHashSet<string>.Builder builder)
        {
            foreach (var constructor in typeDeclaration.Members.OfType<ConstructorDeclarationSyntax>())
            {
                foreach (var invocation in constructor.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (!IsConfigLazyQueryInvocation(invocation))
                    {
                        continue;
                    }

                    if (invocation.ArgumentList.Arguments.Count == 0)
                    {
                        continue;
                    }

                    var navigationName = ExtractLambdaMemberName(invocation.ArgumentList.Arguments[0].Expression);
                    if (!string.IsNullOrEmpty(navigationName))
                    {
                        builder.Add(navigationName);
                    }
                }
            }
        }

        private static bool IsConfigLazyQueryInvocation(InvocationExpressionSyntax invocation) =>
            invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "ConfigLazyQuery",
                MemberAccessExpressionSyntax memberAccess =>
                    memberAccess.Name.Identifier.ValueText == "ConfigLazyQuery",
                _ => false
            };

        private static void CollectFromLazyQueryProperties(
            TypeDeclarationSyntax typeDeclaration,
            SemanticModel semanticModel,
            ImmutableHashSet<string>.Builder builder)
        {
            foreach (var property in typeDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                var propertySymbol = semanticModel.GetDeclaredSymbol(property) as IPropertySymbol;
                if (propertySymbol == null || !IsLazyNavigationProperty(propertySymbol))
                {
                    continue;
                }

                if (ContainsLazyQueryInvocation(property))
                {
                    builder.Add(propertySymbol.Name);
                }
            }
        }

        private static string? ExtractLambdaMemberName(ExpressionSyntax expression) =>
            expression switch
            {
                SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax memberAccess } =>
                    memberAccess.Name.Identifier.ValueText,
                ParenthesizedLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax memberAccess } =>
                    memberAccess.Name.Identifier.ValueText,
                SimpleLambdaExpressionSyntax { Body: IdentifierNameSyntax identifier } =>
                    identifier.Identifier.ValueText,
                ParenthesizedLambdaExpressionSyntax { Body: IdentifierNameSyntax identifier } =>
                    identifier.Identifier.ValueText,
                _ => null
            };

        private static bool ContainsLazyQueryInvocation(PropertyDeclarationSyntax property)
        {
            var expression = GetPropertyExpression(property);
            return expression != null && IsLazyQueryInvocation(expression);
        }

        private static bool IsLazyQueryInvocation(ExpressionSyntax expression)
        {
            if (expression is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            return invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "LazyQuery",
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText == "LazyQuery",
                _ => false
            };
        }

        private static SyntaxNode? GetGetterRoot(PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null)
            {
                return GetBodyRoot(property.ExpressionBody, null);
            }

            var getter = property.AccessorList?.Accessors
                .FirstOrDefault(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            return getter == null ? null : GetBodyRoot(getter.ExpressionBody, getter.Body);
        }

        private static SyntaxNode? GetBodyRoot(ArrowExpressionClauseSyntax? expressionBody, BlockSyntax? body) =>
            expressionBody != null ? expressionBody.Expression : body;

        private static ExpressionSyntax? GetPropertyExpression(PropertyDeclarationSyntax property)
        {
            if (property.ExpressionBody != null)
            {
                return property.ExpressionBody.Expression;
            }

            var getter = property.AccessorList?.Accessors
                .FirstOrDefault(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            if (getter?.ExpressionBody != null)
            {
                return getter.ExpressionBody.Expression;
            }

            return getter?.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .Select(statement => statement.Expression)
                .FirstOrDefault(expression => expression != null);
        }

        private static ImmutableHashSet<string> CollectAccessedLazyNavigations(
            SyntaxNode getterRoot,
            SemanticModel semanticModel,
            INamedTypeSymbol containingType,
            TypeDeclarationSyntax? containingTypeDeclaration,
            ImmutableHashSet<string> lazyNavigationProperties)
        {
            var accessed = new HashSet<string>(StringComparer.Ordinal);
            CollectAccessedLazyNavigationsFromSyntax(
                getterRoot,
                semanticModel,
                containingType,
                lazyNavigationProperties,
                accessed);
            CollectAccessedLazyNavigationsFromPrivateMethods(
                getterRoot,
                semanticModel,
                containingType,
                containingTypeDeclaration,
                lazyNavigationProperties,
                accessed);
            return accessed.ToImmutableHashSet(StringComparer.Ordinal);
        }

        private static void CollectAccessedLazyNavigationsFromSyntax(
            SyntaxNode root,
            SemanticModel semanticModel,
            INamedTypeSymbol containingType,
            ImmutableHashSet<string> lazyNavigationProperties,
            HashSet<string> accessed)
        {
            foreach (var node in root.DescendantNodesAndSelf())
            {
                ISymbol? symbol = node switch
                {
                    IdentifierNameSyntax identifier => semanticModel.GetSymbolInfo(identifier).Symbol,
                    MemberAccessExpressionSyntax memberAccess when memberAccess.Expression is ThisExpressionSyntax or BaseExpressionSyntax or null =>
                        semanticModel.GetSymbolInfo(memberAccess).Symbol,
                    MemberAccessExpressionSyntax memberAccess when memberAccess.Expression is IdentifierNameSyntax =>
                        semanticModel.GetSymbolInfo(memberAccess).Symbol,
                    MemberBindingExpressionSyntax memberBinding =>
                        semanticModel.GetSymbolInfo(memberBinding).Symbol,
                    _ => null
                };

                if (symbol is not IPropertySymbol propertySymbol ||
                    !IsPropertyOnTypeHierarchy(propertySymbol, containingType))
                {
                    continue;
                }

                if (lazyNavigationProperties.Contains(propertySymbol.Name))
                {
                    accessed.Add(propertySymbol.Name);
                }
            }
        }

        private static void CollectAccessedLazyNavigationsFromPrivateMethods(
            SyntaxNode getterRoot,
            SemanticModel semanticModel,
            INamedTypeSymbol containingType,
            TypeDeclarationSyntax? containingTypeDeclaration,
            ImmutableHashSet<string> lazyNavigationProperties,
            HashSet<string> accessed)
        {
            foreach (var invocation in getterRoot.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (!TryGetPrivateMethodDeclaration(
                        invocation,
                        semanticModel,
                        containingType,
                        containingTypeDeclaration,
                        out var methodDeclaration))
                {
                    continue;
                }

                var methodRoot = GetBodyRoot(methodDeclaration.ExpressionBody, methodDeclaration.Body);
                if (methodRoot == null)
                {
                    continue;
                }

                CollectAccessedLazyNavigationsFromSyntax(
                    methodRoot,
                    semanticModel,
                    containingType,
                    lazyNavigationProperties,
                    accessed);
            }
        }

        private static bool TryGetPrivateMethodDeclaration(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            INamedTypeSymbol containingType,
            TypeDeclarationSyntax? containingTypeDeclaration,
            out MethodDeclarationSyntax methodDeclaration)
        {
            methodDeclaration = null!;

            if (invocation.Expression is not (IdentifierNameSyntax or MemberAccessExpressionSyntax
                {
                    Expression: ThisExpressionSyntax or null,
                    Name: SimpleNameSyntax
                }))
            {
                return false;
            }

            if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol ||
                !IsExpandablePrivateMethod(methodSymbol, containingType))
            {
                return false;
            }

            if (methodSymbol.DeclaringSyntaxReferences is { Length: > 0 } &&
                methodSymbol.DeclaringSyntaxReferences[0].GetSyntax() is MethodDeclarationSyntax declarationFromSymbol)
            {
                methodDeclaration = declarationFromSymbol;
                return true;
            }

            if (containingTypeDeclaration == null)
            {
                return false;
            }

            var methodName = invocation.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                _ => null
            };

            if (string.IsNullOrEmpty(methodName))
            {
                return false;
            }

            foreach (var candidate in containingTypeDeclaration.Members.OfType<MethodDeclarationSyntax>())
            {
                if (candidate.Identifier.ValueText != methodName)
                {
                    continue;
                }

                if (semanticModel.GetDeclaredSymbol(candidate) is not IMethodSymbol candidateSymbol ||
                    !IsExpandablePrivateMethod(candidateSymbol, containingType))
                {
                    continue;
                }

                methodDeclaration = candidate;
                return true;
            }

            return false;
        }

        private static bool IsExpandablePrivateMethod(IMethodSymbol method, INamedTypeSymbol containingType) =>
            !method.IsStatic &&
            method.MethodKind == MethodKind.Ordinary &&
            !method.IsAbstract &&
            SymbolEqualityComparer.Default.Equals(method.ContainingType, containingType) &&
            method.DeclaredAccessibility is Accessibility.Private or Accessibility.Internal;

        private static bool IsPropertyOnTypeHierarchy(IPropertySymbol propertySymbol, INamedTypeSymbol containingType)
        {
            for (var current = containingType; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingType, current))
                {
                    return true;
                }
            }

            return false;
        }

        private static ImmutableHashSet<string> GetDeclaredDependsOnNavigationNames(IPropertySymbol propertySymbol)
        {
            var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

            foreach (var attribute in propertySymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name is not "BatchLoadDependsOnAttribute")
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Length > 0 &&
                    attribute.ConstructorArguments[0].Value is string navigationName &&
                    !string.IsNullOrEmpty(navigationName))
                {
                    builder.Add(navigationName);
                }
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<string, string?> CreateProperties(string navigationPropertyName) =>
            ImmutableDictionary<string, string?>.Empty.Add(
                "NavigationPropertyName",
                navigationPropertyName);
    }
}
