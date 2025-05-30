﻿using JetBrains.Annotations;

namespace Geex;

public class LoginProviderEnum : Enumeration<LoginProviderEnum>
{
    public static LoginProviderEnum Local { get; } = new LoginProviderEnum(LoginProviderEnum._Local);
    public const string _Local = nameof(Local);
    public LoginProviderEnum([NotNull] string name, string value) : base(name, value)
    {
    }

    public LoginProviderEnum(string value) : base(value)
    {
    }
}
