namespace Geex.Extensions.Mocking;

public class MockOutcomeEnum : Enumeration<MockOutcomeEnum>
{
    public static MockOutcomeEnum Return { get; } = FromValue(nameof(Return));
    public static MockOutcomeEnum Throw { get; } = FromValue(nameof(Throw));
}
