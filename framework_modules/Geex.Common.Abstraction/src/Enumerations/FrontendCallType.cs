using Geex.Common.Abstractions;

namespace Geex.Common.Abstraction.Enumerations;

public class FrontendCallType : Enumeration<FrontendCallType>
{
    protected FrontendCallType(string name, string value) : base(name, value)
    {
    }

    public static FrontendCallType NewMessage { get; } = new(nameof(NewMessage), nameof(NewMessage));
    public static FrontendCallType DataChange { get; } = new(nameof(DataChange), nameof(DataChange));
}