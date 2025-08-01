using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;

using Microsoft.Extensions.Logging;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Geex.Validation
{
    /// <summary>
    /// Middleware for handling validation directives on arguments and input fields
    /// </summary>
    public class ValidateMiddleware
    {
        private readonly FieldDelegate _next;
        private readonly ILogger<ValidateMiddleware> _logger;

        public ValidateMiddleware(FieldDelegate next, ILogger<ValidateMiddleware> logger = null)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger;
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            try
            {
                // 验证字段参数
                await ValidateFieldArguments(context);

                // 验证输入对象字段
                await ValidateInputObjectFields(context);

                if (context.HasErrors)
                {
                    context.Result = null;
                    return;
                }

                // 如果验证通过，继续执行
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error occurred during validation middleware execution");
                var error = ErrorBuilder.New()
                    .SetMessage($"Validation middleware error: {ex.Message}")
                    .SetPath(context.Path)
                    .SetCode("VALIDATION_MIDDLEWARE_ERROR")
                    .Build();
                context.ReportError(error);
            }
        }

        private async Task ValidateFieldArguments(IMiddlewareContext context)
        {
            foreach (var argument in context.Selection.Field.Arguments)
            {
                var validationDirectives = argument.Directives
                    .Where(d => d.Type.Name == ValidateDirective.DirectiveName)
                    .Select(d => d.AsValue<ValidateDirective>());

                foreach (var validationDirective in validationDirectives)
                {
                    var argumentValue = context.ArgumentValue<object>(argument.Name);
                    var fieldPath = $"argument '{argument.Name}'";

                    await ValidateValue(context, argumentValue, validationDirective, fieldPath);
                }
            }
        }

        private async Task ValidateInputObjectFields(IMiddlewareContext context)
        {
            foreach (var argument in context.Selection.Field.Arguments)
            {
                var argumentValue = context.ArgumentValue<object>(argument.Name);
                if (argumentValue != null)
                {
                    await ValidateInputObjectRecursively(context, argumentValue, argument.Type, $"argument '{argument.Name}'");
                }
            }
        }

        private async Task ValidateInputObjectRecursively(IMiddlewareContext context, object inputObject, IInputType inputType, string parentPath)
        {
            if (inputObject == null) return;

            if (inputType is InputObjectType inputObjectType)
            {
                await ValidateInputObject(context, inputObject, inputObjectType, parentPath);
            }
            else if (inputType is ListType listType && inputObject is System.Collections.IEnumerable enumerable)
            {
                await ValidateList(context, enumerable, listType, parentPath);
            }
            else if (inputType is NonNullType { Type: IInputType realInputType })
            {
                await ValidateInputObjectRecursively(context, inputObject, realInputType, parentPath);
            }
        }

        private async Task ValidateInputObject(IMiddlewareContext context, object inputObject, InputObjectType inputObjectType, string parentPath)
        {
            var inputFields = inputObjectType.Fields;

            foreach (var inputField in inputFields)
            {
                var fieldValue = inputField.Property?.GetValue(inputObject);
                var fieldPath = $"{parentPath}.{inputField.Name}";

                var validateDirectives = inputField.Directives
                    .Where(d => d.Type.Name == ValidateDirective.DirectiveName)
                    .Select(d => d.AsValue<ValidateDirective>())
                    .Where(d => d != null)
                    .ToList();

                if (validateDirectives.Any())
                {
                    foreach (var validationDirective in validateDirectives)
                    {
                        await ValidateValue(context, fieldValue, validationDirective, fieldPath);
                    }

                    // 递归验证嵌套输入对象
                    if (fieldValue != null && inputField.Type is IInputType nestedInputType)
                    {
                        await ValidateInputObjectRecursively(context, fieldValue, nestedInputType, fieldPath);
                    }
                }
            }
        }

        private async Task ValidateList(IMiddlewareContext context, System.Collections.IEnumerable enumerable, ListType listType, string parentPath)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    var itemPath = $"{parentPath}[{index}]";
                    var elementType = listType.ElementType;

                    if (elementType is NonNullType nonNullElementType && nonNullElementType.Type is IInputType realInputType)
                    {
                        await ValidateInputObjectRecursively(context, item, realInputType, itemPath);
                    }
                    else if (elementType is IInputType inputType)
                    {
                        await ValidateInputObjectRecursively(context, item, inputType, itemPath);
                    }
                }
                index++;
            }
        }

        private async Task<bool> ValidateValue(IMiddlewareContext context, object value, ValidateDirective validationDirective, string fieldPath)
        {
            try
            {
                var rule = validationDirective.Rule;
                if (rule == null)
                {
                    _logger?.LogWarning("Validation rule '{RuleKey}' not found at {FieldPath}", validationDirective.RuleKey, fieldPath);
                    var error = ErrorBuilder.New()
                        .SetMessage($"Validation rule '{validationDirective.RuleKey}' not found at {fieldPath}")
                        .SetPath(context.Path)
                        .SetCode("VALIDATION_RULE_NOT_FOUND")
                        .Build();
                    context.ReportError(error);
                    return false;
                }

                var validationResult = rule.Validate(value);
                if (!string.IsNullOrEmpty(validationResult?.ErrorMessage))
                {
                    var message = validationDirective.Message ?? validationResult.ErrorMessage ?? $"Validation failed for rule: {validationDirective.RuleKey}";
                    _logger?.LogDebug("Validation failed for rule '{RuleKey}' at {FieldPath}: {Message}", validationDirective.RuleKey, fieldPath, message);

                    var error = ErrorBuilder.New()
                        .SetMessage($"{message} at {fieldPath}")
                        .SetPath(context.Path)
                        .SetCode("VALIDATION_ERROR")
                        .SetExtension("ruleKey", validationDirective.RuleKey)
                        .SetExtension("fieldPath", fieldPath)
                        .Build();
                    context.ReportError(error);
                    return false;
                }

                _logger?.LogTrace("Validation passed for rule '{RuleKey}' at {FieldPath}", validationDirective.RuleKey, fieldPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Validation error for rule {RuleKey} at {FieldPath}", validationDirective.RuleKey, fieldPath);
                var error = ErrorBuilder.New()
                    .SetMessage($"Validation error for rule {validationDirective.RuleKey} at {fieldPath}: {ex.Message}")
                    .SetPath(context.Path)
                    .SetCode("VALIDATION_EXECUTION_ERROR")
                    .Build();
                context.ReportError(error);
                return false;
            }
            return true;
        }
    }
}
