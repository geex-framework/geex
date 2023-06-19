using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Validation;
using JetBrains.Annotations;

namespace Geex.Common.Abstraction.Gql
{
    public class ExtraArgsTolerantValidationVisitor : DocumentValidatorVisitor
    {

        protected override ISyntaxVisitorAction Enter(ISyntaxNode node, IDocumentValidatorContext context)
        {
            var result = base.Enter(node, context);
            //if (result.IsBreak() && context.Errors.All(x=>x.Code == "HC0016"))
            //{
            //    return Continue;
            //}
            return result;
        }
    }
}
