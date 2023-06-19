using Geex.Common.Abstractions;
using JetBrains.Annotations;

namespace Geex.Common.Abstraction;

public class LoginProviderEnum : Enumeration<LoginProviderEnum>
{
    public static LoginProviderEnum Local { get; } = new LoginProviderEnum(LoginProviderEnum._Local);
    public const string _Local = nameof(Local);
    public static LoginProviderEnum Trusted { get; } = new LoginProviderEnum(LoginProviderEnum._Trusted);
    public const string _Trusted = nameof(Trusted);
    public LoginProviderEnum([NotNull] string name, string value) : base(name, value)
    {
    }

    public LoginProviderEnum(string value) : base(value)
    {
    }
}