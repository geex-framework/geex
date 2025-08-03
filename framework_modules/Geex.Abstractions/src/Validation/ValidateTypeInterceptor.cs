using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Validation
{
    /// <summary>
    /// Type interceptor for handling validation on fields and arguments
    /// Validation is now handled directly in the ValidateDirective, no middleware needed
    /// </summary>
    public class ValidateTypeInterceptor : TypeInterceptor
    {
        /// <inheritdoc />
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                LogValidationDirectives(objectTypeDefinition, completionContext);
            }
            else if (definition is InputObjectTypeDefinition inputObjectTypeDefinition)
            {
                LogInputObjectValidation(inputObjectTypeDefinition, completionContext);
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        private void LogValidationDirectives(ObjectTypeDefinition objectTypeDefinition, ITypeCompletionContext completionContext)
        {
            var logger = completionContext.Services.GetService<ILogger<ValidateTypeInterceptor>>();

            foreach (var fieldDefinition in objectTypeDefinition.Fields)
            {
                // Check if any arguments have validation directives
                var argumentsWithValidation = fieldDefinition.Arguments
                    .Where(arg => HasValidateDirective(arg.Directives))
                    .ToList();

                if (argumentsWithValidation.Any())
                {
                    logger?.LogInformation($"Found validation directives on field arguments: {objectTypeDefinition.Name}.{fieldDefinition.Name}");
                    foreach (var arg in argumentsWithValidation)
                    {
                        logger?.LogDebug($"  - Argument '{arg.Name}' has validation directives");
                    }
                }
            }
        }

        private void LogInputObjectValidation(InputObjectTypeDefinition inputObjectTypeDefinition, ITypeCompletionContext completionContext)
        {
            var logger = completionContext.Services.GetService<ILogger<ValidateTypeInterceptor>>();

            foreach (var fieldDefinition in inputObjectTypeDefinition.Fields)
            {
                var hasValidationDirectives = HasValidateDirective(fieldDefinition.Directives);

                if (hasValidationDirectives)
                {
                    logger?.LogInformation($"Found validation directives on input field: {inputObjectTypeDefinition.Name}.{fieldDefinition.Name}");
                }
            }
        }

        private bool HasValidateDirective(IEnumerable<DirectiveDefinition> directives)
        {
            return directives.Any(d =>
                (d.Value is ValidateDirective) ||
                (d.Value is DirectiveNode directiveNode && directiveNode.Name.Value == ValidateDirective.DirectiveName));
        }
    }
}
