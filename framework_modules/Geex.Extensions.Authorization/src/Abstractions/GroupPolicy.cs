using System.Collections.Generic;

namespace Geex.Authorization
{
    public class GroupPolicy
    {
        public GroupPolicy(List<string> x)
        {
            this.Sub = x[0];
            this.Group = x[1];
        }

        public GroupPolicy(string sub, string group)
        {
            Sub = sub;
            Group = group;
        }

        public string Sub { get; }
        public string Group { get; }
    }
}
