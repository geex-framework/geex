using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace Geex.Gql
{
    /// <summary>
    /// Type interceptor for handling lazy properties and queryable fields
    /// </summary>
    public class LazyQueryTypeInterceptor : TypeInterceptor
    {
        private static Dictionary<Type, MethodInfo> LazyGetterCache = new Dictionary<Type, MethodInfo>();

        /// <summary>
        /// 用于强制获取Lazy值
        /// </summary>
        private static object? GetLazyValue(object lazy, Type valueType)
        {
            if (LazyGetterCache.TryGetValue(valueType, out var method))
            {
                return method.Invoke(lazy, Array.Empty<object>());
            }

            var lazyType = lazy.GetType();
            method = lazyType.GetProperty(nameof(Lazy<object>.Value))!.GetMethod!;
            LazyGetterCache.Add(valueType, method);
            return method.Invoke(lazy, Array.Empty<object>());
        }

        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                // Handle Lazy and ResettableLazy properties
                var properties = objectTypeDefinition.RuntimeType.GetProperties();
                var lazyGetters = properties.Where(x => x.PropertyType.Name == "ResettableLazy`1" || x.PropertyType.Name == "Lazy`1");
                foreach (var getter in lazyGetters)
                {
                    var lazyFieldName = getter.Name;
                    var field = objectTypeDefinition.Fields.FirstOrDefault(f => f.Name.ToLowerInvariant() == lazyFieldName.ToLowerInvariant());
                    if (field != null)
                    {
                        var valueType = getter.PropertyType.GenericTypeArguments[0];
                        field.ResolverMember = getter;
                        field.ResultType = valueType;

                        // Setting up resolver for lazy field
                        field.Resolver = async context =>
                        {
                            var parent = context.Parent<object>();
                            var lazyValue = getter.GetMethod!.Invoke(parent, Array.Empty<object>());
                            return lazyValue != null ? GetLazyValue(lazyValue, valueType) : null;
                        };
                    }
                }

                // Handle IQueryable properties
                var queryGetters = properties.Where(x => x.PropertyType.Name == "IQueryable`1");
                foreach (var getter in queryGetters)
                {
                    var queryFieldName = getter.Name;
                    var field = objectTypeDefinition.Fields.FirstOrDefault(f => f.Name == queryFieldName);
                    if (field != null)
                    {
                        field.ResolverMember = getter;

                        // Setting up resolver for queryable field
                        field.Resolver = async context =>
                        {
                            var parent = context.Parent<object>();
                            return getter.GetMethod!.Invoke(parent, Array.Empty<object>());
                        };
                    }
                }
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }
    }
}
