using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Validation;

namespace Geex.Abstractions.Gql
{
    public class ExtraArgsTolerantValidationVisitor : DocumentValidatorVisitor
    {

        protected override ISyntaxVisitorAction Enter(ISyntaxNode node, IDocumentValidatorContext context)
        {
            var result = base.Enter(node, context);
            //if (result.IsBreak() && context.Errors.All(x => x.Code == "HC0016"))
            //{
            //    return Continue;
            //}
            return result;
        }
    }
}
