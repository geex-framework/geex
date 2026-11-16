using Xunit;

namespace Geex.Tests.UnitTests;

[Collection(EnumerationDynamicTestCollection.Name)]
public sealed class EnumerationFactoryTests
{
    [Fact]
    public void ExplicitFactory_TakesPriorityAndReusesTheFamilyCache()
    {
        var value = Guid.NewGuid().ToString("N");
        var instance = PreferredFactoryEnum.FromNameAndValue("display-" + value, value);

        Assert.Equal("factory", instance.Source);
        Assert.Equal("display-" + value, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Same(instance, PreferredFactoryEnum.FromValue(value));
        Assert.Same(instance, PreferredFactoryEnum.FromName(instance.Name));
        Assert.Equal(1, PreferredFactoryEnum.FactoryCalls);
    }

    [Fact]
    public void FactoryReturningCompatibleChild_PreservesTypedLookupIdentity()
    {
        var value = Guid.NewGuid().ToString("N");
        var instance = ChildFactoryRoot.FromValue(value);

        Assert.IsType<FactoryChild>(instance);
        Assert.Equal(value, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Same(instance, ChildFactoryRoot.FromValue<FactoryChild>(value));
        Assert.Same(instance, ChildFactoryRoot.FromName<FactoryChild>(value));
    }

    [Fact]
    public void FactoryFailure_DoesNotFallBackOrCacheAndCanBeRetried()
    {
        var value = Guid.NewGuid().ToString("N");
        var failure = Assert.Throws<InvalidOperationException>(() => RetriableFactoryEnum.FromValue(value));

        Assert.IsType<FactoryFailure>(failure.InnerException);
        Assert.Equal(0, RetriableFactoryEnum.ConstructorCalls);
        Assert.Null(RetriableFactoryEnum.FromExistedValue(value, true));
        var recovered = RetriableFactoryEnum.FromValue(value);
        Assert.Equal(value, recovered.Value);
        Assert.Equal(1, RetriableFactoryEnum.ConstructorCalls);
        Assert.Equal(2, RetriableFactoryEnum.FactoryCalls);
        Assert.Same(recovered, RetriableFactoryEnum.FromValue(value));
    }

    [Fact]
    public void FactoryChangingName_RejectsTheInstanceWithoutCaching() => AssertInvalidIdentity<WrongName>();

    [Fact]
    public void FactoryChangingValue_RejectsTheInstanceWithoutCaching() => AssertInvalidIdentity<WrongValue>();

    private static void AssertInvalidIdentity<TMarker>()
    {
        var value = Guid.NewGuid().ToString("N");
        Assert.Throws<InvalidOperationException>(() => InvalidIdentityFactory<TMarker>.FromValue(value));
        Assert.Null(InvalidIdentityFactory<TMarker>.FromExistedValue(value, true));
        Assert.Null(InvalidIdentityFactory<TMarker>.FromExistedValue(value + "-invalid", true));
        Assert.Null(InvalidIdentityFactory<TMarker>.FromExistedName(value, noException: true));
        Assert.Null(InvalidIdentityFactory<TMarker>.FromExistedName(value + "-invalid", noException: true));
    }

    private sealed class PreferredFactoryEnum : Enumeration<PreferredFactoryEnum>, IEnumerationFactory<PreferredFactoryEnum>
    {
        public static int FactoryCalls;
        public string Source { get; }
        private PreferredFactoryEnum(string name, string value) : this(name, value, "constructor") { }
        private PreferredFactoryEnum(string name, string value, string source) : base(name, value) => Source = source;
        static PreferredFactoryEnum IEnumerationFactory<PreferredFactoryEnum>.CreateEnumeration(string name, string value)
        {
            FactoryCalls++;
            return new(name, value, "factory");
        }
    }

    private abstract class ChildFactoryRoot : Enumeration<ChildFactoryRoot>, IEnumerationFactory<ChildFactoryRoot>
    {
        protected ChildFactoryRoot(string name, string value) : base(name, value) { }
        static ChildFactoryRoot IEnumerationFactory<ChildFactoryRoot>.CreateEnumeration(string name, string value)
            => new FactoryChild(name, value);
    }

    private sealed class FactoryChild(string name, string value) : ChildFactoryRoot(name, value) { }

    private sealed class RetriableFactoryEnum : Enumeration<RetriableFactoryEnum>, IEnumerationFactory<RetriableFactoryEnum>
    {
        public static int FactoryCalls;
        public static int ConstructorCalls;
        private RetriableFactoryEnum(string name, string value) : base(name, value) => ConstructorCalls++;
        static RetriableFactoryEnum IEnumerationFactory<RetriableFactoryEnum>.CreateEnumeration(string name, string value)
        {
            if (++FactoryCalls == 1) throw new FactoryFailure();
            return new(name, value);
        }
    }

    private sealed class InvalidIdentityFactory<TMarker> : Enumeration<InvalidIdentityFactory<TMarker>>,
        IEnumerationFactory<InvalidIdentityFactory<TMarker>>
    {
        private InvalidIdentityFactory(string name, string value, bool invalid)
            : base(typeof(TMarker) == typeof(WrongName) ? name + "-invalid" : name,
                typeof(TMarker) == typeof(WrongValue) ? value + "-invalid" : value) { }
        static InvalidIdentityFactory<TMarker> IEnumerationFactory<InvalidIdentityFactory<TMarker>>.CreateEnumeration(string name, string value)
            => new(name, value, true);
    }

    private sealed class FactoryFailure : Exception { }
    private sealed class WrongName { }
    private sealed class WrongValue { }
}
