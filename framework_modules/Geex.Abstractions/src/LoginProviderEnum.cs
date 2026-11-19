namespace Geex;

public class LoginProviderEnum : Enumeration<LoginProviderEnum>
{
    /// <summary>Supports the default dynamic enumeration creation path.</summary>
    public LoginProviderEnum() { }

    /// <summary>Allows a subtype factory to initialize its final name and value.</summary>
    protected LoginProviderEnum(string name, string value) : base(name, value) { }

    public static LoginProviderEnum Local { get; } = FromValue(nameof(Local));
    public static LoginProviderEnum PersonalAccessToken { get; } = FromValue(nameof(PersonalAccessToken));
}
