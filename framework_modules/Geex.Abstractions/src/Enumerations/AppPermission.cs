using System.Linq;
using Geex.Abstractions;

namespace Geex.Common.Authorization
{
    public class AppPermission : Enumeration<AppPermission>
    {
        public AppPermission(string value) : base(value)
        {
            var split = value.Split('_');
            this.Mod = split[0];
            this.Act = split[1];
            this.Obj = split[2];
            this.Field = split.ElementAtOrDefault(3) ?? "";
        }

        public string Field { get; set; }

        public string Obj { get; set; }

        public string Act { get; set; }

        public string Mod { get; set; }
    }

    public abstract class AppPermission<TImplementation> : AppPermission
    {
        protected AppPermission(string value) : base((PermissionString)value)
        {

        }
    }
}
