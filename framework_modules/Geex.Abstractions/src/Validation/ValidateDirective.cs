using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Resolvers;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Geex;

namespace Geex.Validation
{
    public class ValidateDirective
    {
        public string RuleKey { get; set; }

        [GraphQLIgnore]
        public ValidateRule Rule
        {
            get => _rule ??= ValidateRule.FromRuleKey(this.RuleKey);
            set => _rule = value;
        }

        public string Message { get; set; }
        public static string DirectiveName = "validate";
        private ValidateRule? _rule;

        public ValidateDirective()
        {

        }
        public ValidateDirective(ValidateRule rule, string message = null)
        {
            Rule = rule;
            RuleKey = rule?.RuleKey;
            Message = message;
        }

        public ValidateDirective(string ruleKey, string message = null)
        {
            RuleKey = ruleKey;
            Rule = ValidateRule.FromRuleKey(ruleKey);
            Message = message;
        }

        public class ValidateDirectiveType : GqlConfig.Directive<ValidateDirective>
        {
            protected override void Configure(IDirectiveTypeDescriptor<ValidateDirective> descriptor)
            {
                descriptor.Name(ValidateDirective.DirectiveName);
                descriptor.Location(DirectiveLocation.ArgumentDefinition | DirectiveLocation.InputFieldDefinition | DirectiveLocation.FieldDefinition);
                descriptor.Repeatable();

                descriptor.Argument(t => t.RuleKey).Type<StringType>();
                descriptor.Argument(t => t.Message).Ignore();
            }
        }
    }
}
