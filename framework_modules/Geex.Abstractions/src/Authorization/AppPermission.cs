namespace Geex
{
    public class AppPermission : Enumeration<AppPermission>
    {
        public AppPermission(string value) : base(value)
        {
            var split = value.Split('_');
            this.Mod = split[0];
            this.Obj = split[1];
            this.Field = split[2];
        }

        public string Field { get; set; }

        public string Obj { get; set; }

        public string Mod { get; set; }
    }

    public abstract class AppPermission<TImplementation> : AppPermission
    {
        protected AppPermission(string value) : base((PermissionString)value)
        {

        }
    }
}
