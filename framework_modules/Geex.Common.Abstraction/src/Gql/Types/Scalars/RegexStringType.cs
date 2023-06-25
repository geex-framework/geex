using System.Text.RegularExpressions;


using HotChocolate;
using HotChocolate.Types;

namespace Geex.Common.Identity.Api.GqlSchemas.Roles.Types
{
    public abstract class RegexStringType : ScalarType
    {
        protected RegexStringType(string name, string pattern) : base(name)
        {
            this.Regex = new Regex(pattern, RegexOptions.Compiled);
        }

        public Regex Regex { get; set; }
    }
}
