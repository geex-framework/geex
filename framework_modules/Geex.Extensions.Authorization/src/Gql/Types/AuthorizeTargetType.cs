namespace Geex.Extensions.Authorization.Gql.Types
{
    public class AuthorizeTargetType : Enumeration<AuthorizeTargetType>
    {
        public static AuthorizeTargetType Role { get; } = FromValue(nameof(Role));
        public static AuthorizeTargetType User { get; } = FromValue(nameof(User));
    }
}
