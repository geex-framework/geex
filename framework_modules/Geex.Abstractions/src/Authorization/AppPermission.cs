namespace Geex
{
    public class AppPermission : Enumeration<AppPermission>, IEnumerationFactory<AppPermission>
    {
        public AppPermission(string value) : this(value, value)
        {
        }

        internal AppPermission(string name, string value) : base(name, value)
        {
            var split = value.Split('_');
            this.Mod = split[0];
            this.Obj = split[1];
            this.Field = split[2];
        }

        static AppPermission IEnumerationFactory<AppPermission>.CreateEnumeration(string name, string value)
            => new(name, value);

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
