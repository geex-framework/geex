using HotChocolate.Types;
using System;
using System.Linq.Expressions;
using FastExpressionCompiler;

namespace Geex.Validation
{
    public static class ValidateExtensions
    {
        // Input Object Field Descriptor extensions (for input object fields)
        public static IInputFieldDescriptor ApplyValidate<T>(this IInputFieldDescriptor descriptor,
            ValidateRule<T> rule, string message = null)
        {
            return descriptor.Directive(new ValidateDirective(rule, message));
        }

        public static IInputFieldDescriptor ApplyValidate<TObject>(this IInputFieldDescriptor descriptor,
            Expression<Func<TObject, bool>> predicate, string message = null)
        {
            var compiledPredicate = predicate.CompileFast();
            var validatorName = typeof(TObject).Name + ":" + predicate;
            var rule = ValidateRule<TObject>.Create(compiledPredicate, validatorName);
            return descriptor.Directive(new ValidateDirective(rule, message));
        }

        // 新增：直接使用RuleKey的重载
        public static IInputFieldDescriptor ApplyValidate(this IInputFieldDescriptor descriptor,
            string ruleKey, string message = null)
        {
            return descriptor.Directive(new ValidateDirective(ruleKey, message));
        }

        // Argument Descriptor extensions (for mutation/query arguments)
        public static IArgumentDescriptor ApplyValidate<T>(this IArgumentDescriptor descriptor,
            ValidateRule<T> rule, string message = null)
        {
            return descriptor.Directive(new ValidateDirective(rule.RuleKey, message));
        }

        public static IArgumentDescriptor ApplyValidate<TObject>(this IArgumentDescriptor descriptor,
            Expression<Func<TObject, bool>> predicate, string message = null)
        {

            var compiledPredicate = predicate.CompileFast();
            var validatorName = typeof(TObject).Name + ":" + predicate;
            var rule = ValidateRule<TObject>.Create(compiledPredicate, validatorName);
            return descriptor.Directive(new ValidateDirective(rule, message));
        }

        // 新增：直接使用RuleKey的重载
        public static IArgumentDescriptor ApplyValidate(this IArgumentDescriptor descriptor,
            string ruleKey, string message = null)
        {
            return descriptor.Directive(new ValidateDirective(ruleKey, message));
        }
    }
}
