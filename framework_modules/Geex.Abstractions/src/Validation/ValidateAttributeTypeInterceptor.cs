using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Geex.Validation
{
    /// <summary>
    /// Type interceptor for converting ValidateAttribute to ValidateDirective
    /// </summary>
    public class ValidateAttributeTypeInterceptor : TypeInterceptor
    {
        public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
        {
            if (definition is ObjectTypeDefinition objectTypeDefinition)
            {
                ProcessObjectTypeFields(objectTypeDefinition, completionContext);
            }
            else if (definition is InputObjectTypeDefinition inputObjectTypeDefinition)
            {
                ProcessInputObjectTypeFields(inputObjectTypeDefinition, completionContext);
            }

            base.OnBeforeCompleteType(completionContext, definition);
        }

        private void ProcessObjectTypeFields(ObjectTypeDefinition objectTypeDefinition, ITypeCompletionContext completionContext)
        {
            var logger = completionContext.Services.GetService<ILogger<ValidateAttributeTypeInterceptor>>();

            foreach (var fieldDefinition in objectTypeDefinition.Fields)
            {
                // Process field arguments
                foreach (var argumentDefinition in fieldDefinition.Arguments)
                {
                    ProcessArgumentAttributes(argumentDefinition, completionContext, logger);
                }
            }
        }

        private void ProcessInputObjectTypeFields(InputObjectTypeDefinition inputObjectTypeDefinition, ITypeCompletionContext completionContext)
        {
            var logger = completionContext.Services.GetService<ILogger<ValidateAttributeTypeInterceptor>>();

            foreach (var fieldDefinition in inputObjectTypeDefinition.Fields)
            {
                ProcessInputFieldAttributes(fieldDefinition, completionContext, logger);
            }
        }

        private void ProcessArgumentAttributes(ArgumentDefinition argumentDefinition, ITypeCompletionContext completionContext, ILogger logger)
        {
            if (argumentDefinition.Parameter != null)
            {
                var validateAttributes = argumentDefinition.Parameter.GetCustomAttributes<ValidateAttribute>();
                foreach (var validateAttribute in validateAttributes)
                {
                    var directive = validateAttribute.ToDirective();
                    argumentDefinition.AddDirective(directive, completionContext.TypeInspector);
                    logger?.LogInformation($"Applied ValidateAttribute to argument: {argumentDefinition.Name}, RuleKey: {validateAttribute.RuleKey}");
                }
            }
        }

        private void ProcessInputFieldAttributes(InputFieldDefinition fieldDefinition, ITypeCompletionContext completionContext, ILogger logger)
        {
            if (fieldDefinition.Property != null)
            {
                var validateAttributes = fieldDefinition.Property.GetCustomAttributes<ValidateAttribute>();
                foreach (var validateAttribute in validateAttributes)
                {
                    var directive = validateAttribute.ToDirective();
                    fieldDefinition.AddDirective(directive, completionContext.TypeInspector);
                    logger?.LogInformation($"Applied ValidateAttribute to input field: {fieldDefinition.Name}, RuleKey: {validateAttribute.RuleKey}");
                }
            }
            else
            {
                logger?.LogWarning("Validator not applied to input field: " + fieldDefinition.Name + " (no property found)");
            }
        }
    }
}
