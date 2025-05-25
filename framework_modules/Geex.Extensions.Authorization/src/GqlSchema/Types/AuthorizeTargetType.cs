using Geex.Abstractions;

namespace Geex.Extensions.Authorization.GqlSchema.Types
{
    public class AuthorizeTargetType : Enumeration<AuthorizeTargetType>
    {
        public static AuthorizeTargetType Role { get; } = new AuthorizeTargetType(nameof(Role));
        public static AuthorizeTargetType User { get; } = new AuthorizeTargetType(nameof(User));

        public AuthorizeTargetType(string value) : base(value)
        {
        }
    }
}
