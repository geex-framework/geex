using System;
using System.Linq.Expressions;

using FastExpressionCompiler;

using Geex.Validation;

using HotChocolate.Types.Descriptors;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types
{
    public static class InputObjectTypeDescriptorExtensions
    {
        // 增强版 Field 方法
        public static InputFieldDescriptor<T, TValue> TypedField<T, TValue>(
            this IInputObjectTypeDescriptor<T> descriptor,
            Expression<Func<T, TValue>> property)
        {
            var field = descriptor.Field(property);
            return new InputFieldDescriptor<T, TValue>(field);
        }

        // Input Object Field Descriptor extensions (for input object fields)
        public static IInputFieldDescriptor<T, TValue> Validate<T, TValue>(this IInputFieldDescriptor<T, TValue> descriptor,
            ValidateRule<TValue> rule, string message = null)
        {
            return new InputFieldDescriptor<T, TValue>(descriptor.Directive(new ValidateDirective(rule, message)));
        }

        public static IInputFieldDescriptor<T, TValue> Validate<T, TValue>(this IInputFieldDescriptor<T, TValue> descriptor,
            Expression<Func<TValue, bool>> predicate, string message = null)
        {
            var compiledPredicate = predicate.CompileFast();
            var validatorName = typeof(T).Name + ":" + predicate;
            var rule = ValidateRule<TValue>.Create(compiledPredicate, validatorName);
            return new InputFieldDescriptor<T, TValue>(descriptor.Directive(new ValidateDirective(rule, message)));
        }

        // 新增：直接使用RuleKey的重载
        public static IInputFieldDescriptor<T, TValue> Validate<T, TValue>(this IInputFieldDescriptor<T, TValue> descriptor,
            string ruleKey, string message = null)
        {
            return new InputFieldDescriptor<T, TValue>(descriptor.Directive(new ValidateDirective(ruleKey, message)));
        }

        // Argument Descriptor extensions (for mutation/query arguments)
        public static IArgumentDescriptor Validate<T>(this IArgumentDescriptor descriptor,
            ValidateRule<T> rule, string message = null)
        {
            return descriptor.Directive(new ValidateDirective(rule.RuleKey, message));
        }

        public static IArgumentDescriptor Validate<TObject>(this IArgumentDescriptor descriptor,
            Expression<Func<TObject, bool>> predicate, string message = null)
        {

            var compiledPredicate = predicate.CompileFast();
            var validatorName = typeof(TObject).Name + ":" + predicate;
            var rule = ValidateRule<TObject>.Create(compiledPredicate, validatorName);
            return descriptor.Directive(new ValidateDirective(rule, message));
        }

        // 新增：直接使用RuleKey的重载
        public static IArgumentDescriptor Validate(this IArgumentDescriptor descriptor,
            string ruleKey, string message = null)
        {
            return descriptor.Directive(new ValidateDirective(ruleKey, message));
        }
    }
}
