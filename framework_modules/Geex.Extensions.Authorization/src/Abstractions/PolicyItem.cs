using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Geex.Extensions.Authorization
{
    public class PolicyItem : IAuthorizationRequirement
    {
        public PolicyItem(List<string> x)
        {
            this.Sub = x[0];
            this.Obj = x[1];
            this.Act = x[2];
            this.Fields = x.ElementAtOrDefault(3) ?? "*";
        }

        public string Sub { get; }
        public string Obj { get; }
        public string Act { get; }
        public string Fields { get; }
    }
}
