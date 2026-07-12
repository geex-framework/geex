namespace Geex;

public class LoginProviderEnum : Enumeration<LoginProviderEnum>
{
    public static LoginProviderEnum Local { get; } = FromValue(nameof(Local));
    public static LoginProviderEnum PersonalAccessToken { get; } = FromValue(nameof(PersonalAccessToken));
}
