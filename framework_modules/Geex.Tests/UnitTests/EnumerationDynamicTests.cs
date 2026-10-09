using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Collections.Concurrent;
using Geex.Bson;
using Geex.Utilities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Xunit;

namespace Geex.Tests.UnitTests;

[CollectionDefinition(EnumerationDynamicTestCollection.Name, DisableParallelization = true)]
public sealed class EnumerationDynamicTestCollection
{
    public const string Name = "Enumeration dynamic tests";
}

[Collection(EnumerationDynamicTestCollection.Name)]
public sealed class EnumerationDynamicTests
{
    [Fact]
    public void ParameterlessEnumeration_PreservesDynamicCreationAndCachedIdentity()
    {
        var value = UniqueValue();

        var instance = ParameterlessEnum.FromValue(value);

        Assert.Equal(value, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Same(instance, ParameterlessEnum.FromName(value));
        Assert.Same(instance, ParameterlessEnum.FromNameAndValue(value, value));
    }

    [Fact]
    public void ParameterlessChild_PreservesTypedDynamicCreation()
    {
        var name = UniqueValue();
        var value = UniqueValue();

        var instance = ParameterlessRoot.FromNameAndValue<ParameterlessChild>(name, value);

        Assert.IsType<ParameterlessChild>(instance);
        Assert.Equal(name, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Same(instance, ParameterlessRoot.FromValue<ParameterlessChild>(value));
        Assert.Same(instance, ParameterlessRoot.FromName<ParameterlessChild>(name));
        Assert.Same(instance, ParameterlessRoot.FromValue(value));
    }

    [Fact]
    public void ExactPrivateConstructor_TakesPriorityAndPreservesDerivedState()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        var instance = PreferredConstructorEnum.FromNameAndValue(name, value);

        Assert.Equal($"{name}:{value}", instance.Description);
        Assert.Equal(name, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Equal(0, PreferredConstructorEnum.ParameterlessCalls);
        Assert.Equal(1, PreferredConstructorEnum.ConstructorCalls);
        Assert.Same(instance, PreferredConstructorEnum.FromName(name));
        Assert.Same(instance, PreferredConstructorEnum.FromValue(value));
    }

    [Fact]
    public void ExactPublicAndProtectedConstructors_AreSupported()
    {
        var publicValue = UniqueValue();
        var protectedValue = UniqueValue();

        Assert.Equal(publicValue, PublicConstructorEnum.FromValue(publicValue).Value);
        Assert.Equal(protectedValue, ProtectedConstructorEnum.FromValue(protectedValue).Value);
    }

    [Fact]
    public void ParentConstructor_IsNotInheritedAsChildConstructionStrategy()
    {
        var value = UniqueValue();

        Assert.Throws<InvalidOperationException>(() =>
            ConstructorRoot.FromValue<ChildWithoutUsableConstructor>(value));

        Assert.Equal(0, ConstructorRoot.ConstructorCalls);
        AssertNotCached<ConstructorRoot>(value, value, typeof(ChildWithoutUsableConstructor));
    }

    [Fact]
    public void ChildWithoutExactConstructor_UsesItsPublicParameterlessConstructor()
    {
        var value = UniqueValue();
        var instance = FallbackConstructorRoot.FromValue<FallbackConstructorChild>(value);

        Assert.IsType<FallbackConstructorChild>(instance);
        Assert.Equal(value, instance.Name);
        Assert.Equal(1, FallbackConstructorChild.ParameterlessCalls);
        Assert.Equal(0, FallbackConstructorRoot.ConstructorCalls);
    }

    [Fact]
    public void ChildExactConstructor_PreservesChildStateAndRequestedIdentity()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        var instance = ConstructorRoot.FromNameAndValue<ChildWithConstructor>(name, value);

        Assert.IsType<ChildWithConstructor>(instance);
        Assert.Equal("child constructor", instance.Source);
        Assert.Equal(name, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Same(instance, ConstructorRoot.FromValue<ChildWithConstructor>(value));
    }

    [Fact]
    public void ExactConstructor_SupportsIndependentIEnumeration()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        var instance = Enumeration<IndependentConstructorEnum>.FromNameAndValue(name, value);

        Assert.Equal(name, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Equal($"{name}:{value}", instance.Description);
        Assert.Same(instance, Enumeration<IndependentConstructorEnum>.FromValue(value));
    }

    [Fact]
    public void ThrowingExactConstructor_PreservesCauseWithoutFallbackOrCaching()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        var expected = ThrowingConstructorEnum.Failure;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                ThrowingConstructorEnum.FromNameAndValue(name, value));
            Assert.Same(expected, exception.InnerException);
            AssertNotCached<ThrowingConstructorEnum>(name, value);
        }

        Assert.Equal(0, ThrowingConstructorEnum.ParameterlessCalls);
        Assert.Equal(2, ThrowingConstructorEnum.ConstructorCalls);
    }

    [Fact]
    public void ConstructorChangingName_IsRejectedWithoutFallbackOrCaching()
    {
        AssertInvalidConstructor<WrongName>();
    }

    [Fact]
    public void ConstructorChangingValue_IsRejectedWithoutFallbackOrCaching()
    {
        AssertInvalidConstructor<WrongValue>();
    }

    [Fact]
    public void UnsupportedOptionalConstructor_IsNotInvokedByGuessingArguments()
    {
        var value = UniqueValue();

        Assert.Throws<InvalidOperationException>(() => UnsupportedConstructorEnum.FromValue(value));

        Assert.Equal(0, UnsupportedConstructorEnum.ConstructorCalls);
        AssertNotCached<UnsupportedConstructorEnum>(value, value);
    }

    [Fact]
    public void OrdinaryNameValueEnumeration_DoesNotWarnForDynamicCreation()
    {
        using var logging = new LoggerScope<ParameterlessEnum>();
        _ = ParameterlessEnum.FromValue(UniqueValue());
        Assert.Empty(logging.Logger.Levels);
    }

    [Fact]
    public void MissingDetails_UseDeclaredDefaultsAndWarnOnlyOnce()
    {
        using var logging = new LoggerScope<DetailsEnum<UnknownDetailsCase>>();
        var value = UniqueValue();
        var instance = DetailsEnum<UnknownDetailsCase>.FromValue(value);

        Assert.Equal(value, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.False(instance.Definition.IsComplete);
        Assert.Equal(UnknownDetails, instance.Definition.Details);
        Assert.Same(instance, DetailsEnum<UnknownDetailsCase>.FromValue(value));
        Assert.Same(instance, DetailsEnum<UnknownDetailsCase>.FromName(value));
        Assert.Single(logging.Logger.Levels, level => level == LogLevel.Warning);
    }

    [Fact]
    public void DetailsParameterlessFallback_PreservesTypeDeclaredDefaults()
    {
        var instance = ParameterlessDetailsEnum.FromValue(UniqueValue());

        Assert.False(instance.Definition.IsComplete);
        Assert.Equal(UnknownDetails, instance.Definition.Details);
    }

    [Fact]
    public void ConstructorCanDeriveCompleteDetails_WithoutWarning()
    {
        using var logging = new LoggerScope<DerivedDetailsEnum>();
        var value = UniqueValue();
        var instance = DerivedDetailsEnum.FromValue(value);

        Assert.True(instance.Definition.IsComplete);
        Assert.Equal(new Details(value, value.Length), instance.Definition.Details);
        Assert.Empty(logging.Logger.Levels);
    }

    [Fact]
    public void DefineFirst_PublishesCompleteDetailsWithoutUnknownWarning()
    {
        using var logging = new LoggerScope<DetailsEnum<DefineFirstCase>>();
        var name = UniqueValue();
        var value = UniqueValue();
        var details = new Details("complete", 7);

        var instance = DetailsEnum<DefineFirstCase>.Define(name, value, details);

        Assert.True(instance.Definition.IsComplete);
        Assert.Equal(details, instance.Definition.Details);
        Assert.Same(instance, DetailsEnum<DefineFirstCase>.FromValue(value));
        Assert.Same(instance, DetailsEnum<DefineFirstCase>.FromName(name));
        Assert.Single(DetailsEnum<DefineFirstCase>.List);
        Assert.Empty(logging.Logger.Levels);
    }

    [Fact]
    public void DefinePlaceholder_CompletesSameInstanceAndPreservesIdentity()
    {
        var value = UniqueValue();
        var instance = DetailsEnum<CompleteLaterCase>.FromValue(value);
        var before = instance.Definition;
        var details = new Details("complete", 9);

        var completed = DetailsEnum<CompleteLaterCase>.Define(UniqueValue(), value, details);

        Assert.Same(instance, completed);
        Assert.Equal(value, completed.Name);
        Assert.Equal(value, completed.Value);
        Assert.False(before.IsComplete);
        Assert.Equal(UnknownDetails, before.Details);
        Assert.True(completed.Definition.IsComplete);
        Assert.Equal(details, completed.Definition.Details);
        Assert.Same(completed, DetailsEnum<CompleteLaterCase>.FromName(value));
        Assert.Same(completed, DetailsEnum<CompleteLaterCase>.ValueCacheDictionary[value]);
    }

    [Fact]
    public void RepeatedDefine_UsesDetailsEqualityAndRejectsConflicts()
    {
        var value = UniqueValue();
        var instance = DetailsEnum<RepeatedDefinitionCase>.Define(value, value, new Details("same", 5));
        var original = instance.Definition;

        Assert.Same(instance, DetailsEnum<RepeatedDefinitionCase>.Define(value, value, new Details("same", 5)));
        Assert.Throws<InvalidOperationException>(() =>
            DetailsEnum<RepeatedDefinitionCase>.Define(value, value, new Details("changed", 6)));

        Assert.Same(original, instance.Definition);
        Assert.Equal(new Details("same", 5), instance.Definition.Details);
    }

    [Fact]
    public void NullDefinition_IsRejectedWithoutCreatingOrCompletingAnInstance()
    {
        var newValue = UniqueValue();
        Assert.Throws<ArgumentNullException>(() => DetailsEnum<NullDetailsCase>.Define(newValue, newValue, null!));
        AssertNotCached<DetailsEnum<NullDetailsCase>>(newValue, newValue);

        var existingValue = UniqueValue();
        var instance = DetailsEnum<NullDetailsCase>.FromValue(existingValue);
        var original = instance.Definition;
        Assert.Throws<ArgumentNullException>(() =>
            DetailsEnum<NullDetailsCase>.Define(existingValue, existingValue, null!));
        Assert.Same(original, instance.Definition);
    }

    [Fact]
    public void DefineNameConflict_DoesNotCompletePlaceholder()
    {
        var name = UniqueValue();
        var firstValue = UniqueValue();
        var secondValue = UniqueValue();
        var named = DetailsEnum<DefinitionNameConflictCase>.FromNameAndValue(name, firstValue);
        var placeholder = DetailsEnum<DefinitionNameConflictCase>.FromValue(secondValue);

        Assert.Throws<InvalidOperationException>(() =>
            DetailsEnum<DefinitionNameConflictCase>.Define(name, secondValue, new Details("complete", 1)));

        Assert.False(placeholder.Definition.IsComplete);
        Assert.Same(named, DetailsEnum<DefinitionNameConflictCase>.FromName(name));
    }

    [Fact]
    public void TypedDefine_ReusesChildButRejectsIncompatibleParent()
    {
        var childValue = UniqueValue();
        var child = DetailsRoot.FromValue<DetailsChild>(childValue);
        var completed = DetailsRoot.Define<DetailsChild>(childValue, childValue, new Details("child", 2));

        Assert.Same(child, completed);
        Assert.True(completed.Definition.IsComplete);
        Assert.Same(completed, DetailsRoot.FromValue(childValue));

        var parentValue = UniqueValue();
        var parent = DetailsRoot.FromValue(parentValue);
        Assert.Throws<InvalidOperationException>(() =>
            DetailsRoot.Define<DetailsChild>(parentValue, parentValue, new Details("child", 2)));
        Assert.False(parent.Definition.IsComplete);
    }

    [Fact]
    public void ValueTypeDetails_CanCompleteWithoutAdditionalConfiguration()
    {
        var value = UniqueValue();
        var instance = ValueDetailsEnum.FromValue(value);

        Assert.False(instance.Definition.IsComplete);
        Assert.Equal(-1, instance.Definition.Details);
        Assert.Same(instance, ValueDetailsEnum.Define(value, value, 0));
        Assert.True(instance.Definition.IsComplete);
        Assert.Equal(0, instance.Definition.Details);
    }

    [Fact]
    public void FromValue_InitializesStaticMembersBeforeResolvingColdType()
    {
        var instance = ColdEnum<ColdValue>.FromValue(KnownValue<ColdValue>());

        Assert.Same(ColdEnum<ColdValue>.Known, instance);
        Assert.Equal(KnownName<ColdValue>(), instance.Name);
        Assert.Equal(1, ColdEnum<ColdValue>.ConstructorCalls);
    }

    [Fact]
    public void FromName_InitializesStaticMembersBeforeResolvingColdType()
    {
        var instance = ColdEnum<ColdName>.FromName(KnownName<ColdName>());

        Assert.Same(ColdEnum<ColdName>.Known, instance);
        Assert.Equal(KnownValue<ColdName>(), instance.Value);
        Assert.Equal(1, ColdEnum<ColdName>.ConstructorCalls);
    }

    [Fact]
    public void FromNameAndValue_InitializesStaticMembersBeforeResolvingColdType()
    {
        var instance = ColdEnum<ColdPair>.FromNameAndValue(
            KnownName<ColdPair>(), KnownValue<ColdPair>());

        Assert.Same(ColdEnum<ColdPair>.Known, instance);
        Assert.Equal(1, ColdEnum<ColdPair>.ConstructorCalls);
    }

    [Fact]
    public void TypedFromValue_InitializesChildStaticMembersBeforeResolvingColdType()
    {
        var instance = ColdRoot<ColdTypedValue>.FromValue<ColdChild<ColdTypedValue>>(
            KnownValue<ColdTypedValue>());

        Assert.Same(ColdChild<ColdTypedValue>.Known, instance);
        Assert.Equal(KnownName<ColdTypedValue>(), instance.Name);
        Assert.Equal(1, ColdChild<ColdTypedValue>.ConstructorCalls);
    }

    [Fact]
    public void TypedFromName_InitializesChildStaticMembersBeforeResolvingColdType()
    {
        var instance = ColdRoot<ColdTypedName>.FromName<ColdChild<ColdTypedName>>(
            KnownName<ColdTypedName>());

        Assert.Same(ColdChild<ColdTypedName>.Known, instance);
        Assert.Equal(KnownValue<ColdTypedName>(), instance.Value);
        Assert.Equal(1, ColdChild<ColdTypedName>.ConstructorCalls);
    }

    [Fact]
    public void TypedFromNameAndValue_InitializesChildStaticMembersBeforeResolvingColdType()
    {
        var instance = ColdRoot<ColdTypedPair>.FromNameAndValue<ColdChild<ColdTypedPair>>(
            KnownName<ColdTypedPair>(), KnownValue<ColdTypedPair>());

        Assert.Same(ColdChild<ColdTypedPair>.Known, instance);
        Assert.Equal(1, ColdChild<ColdTypedPair>.ConstructorCalls);
    }

    [Fact]
    public void RootStaticInitialization_CanResolveColdChildWithoutRecursiveCreation()
    {
        var instance = NestedInitializationRoot<NestedRootCase>.FromValue(KnownValue<NestedRootCase>());

        Assert.Same(NestedInitializationRoot<NestedRootCase>.ReferencedByRoot, instance);
        Assert.Same(NestedInitializationChildB<NestedRootCase>.Known, instance);
        Assert.Equal(1, NestedInitializationChildB<NestedRootCase>.ConstructorCalls);
    }

    [Fact]
    public void ChildStaticInitialization_CanResolveColdSiblingWithoutRecursiveCreation()
    {
        var value = UniqueValue();

        var instance = NestedInitializationRoot<NestedChildCase>
            .FromValue<NestedInitializationChildA<NestedChildCase>>(value);

        Assert.Equal(value, instance.Value);
        Assert.Same(NestedInitializationChildB<NestedChildCase>.Known,
            NestedInitializationChildA<NestedChildCase>.ReferencedSibling);
        Assert.Same(NestedInitializationChildB<NestedChildCase>.Known,
            NestedInitializationRoot<NestedChildCase>.FromValue(KnownValue<NestedChildCase>()));
        Assert.Equal(1, NestedInitializationChildB<NestedChildCase>.ConstructorCalls);
    }

    [Fact]
    public void AbstractEnumerationWithoutConstructor_IsRejectedWithoutCaching()
    {
        var value = UniqueValue();

        Assert.Throws<InvalidOperationException>(() => AbstractEnum.FromValue(value));

        AssertNotCached<AbstractEnum>(value, value);
    }

    [Fact]
    public void AppPermissionConstructor_PreservesDifferentDisplayNameAndParsedValueParts()
    {
        var name = UniqueValue();
        var field = Guid.NewGuid().ToString("N");
        var value = $"ConstructorModule_query_{field}";

        var instance = AppPermission.FromNameAndValue(name, value);

        Assert.Equal(name, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Equal("ConstructorModule", instance.Mod);
        Assert.Equal("query", instance.Obj);
        Assert.Equal(field, instance.Field);
        Assert.Same(instance, AppPermission.FromValue(value));
        Assert.Same(instance, AppPermission.FromName(name));
    }

    [Fact]
    public void JsonStringDeserialization_UsesExplicitConstructorAndCachedInstance()
    {
        var value = UniqueValue();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new Geex.Json.EnumerationConverter());
        var json = JsonSerializer.Serialize(value);

        var instance = JsonSerializer.Deserialize<SerializedConstructorEnum<JsonCase>>(json, options);
        var repeated = JsonSerializer.Deserialize<SerializedConstructorEnum<JsonCase>>(json, options);

        Assert.NotNull(instance);
        Assert.Equal(value, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Equal($"{value}:{value}", instance.Description);
        Assert.Same(instance, repeated);
        Assert.Equal(1, SerializedConstructorEnum<JsonCase>.ConstructorCalls);
    }

    [Fact]
    public void BsonStringDeserialization_UsesExplicitConstructorAndCachedInstance()
    {
        var value = UniqueValue();

        var instance = DeserializeBsonString<SerializedConstructorEnum<BsonCase>>(value);
        var repeated = DeserializeBsonString<SerializedConstructorEnum<BsonCase>>(value);

        Assert.NotNull(instance);
        Assert.Equal(value, instance.Name);
        Assert.Equal(value, instance.Value);
        Assert.Equal($"{value}:{value}", instance.Description);
        Assert.Same(instance, repeated);
        Assert.Equal(1, SerializedConstructorEnum<BsonCase>.ConstructorCalls);
    }

    [Fact]
    public void RegisteredLoginProviders_ParentLookupDiscoversChildrenAndDeserializersReuseThem()
    {
        using var registration = new RegisteredAssemblyScope(typeof(EnumerationDynamicTests).Assembly);

        var instance = LoginProviderEnum.FromValue(RegisteredLoginProviderValue);

        Assert.IsType<RegisteredLoginProviders>(instance);
        Assert.Same(RegisteredLoginProviders.Extra, instance);
        Assert.Same(instance, LoginProviderEnum.FromValue<RegisteredLoginProviders>(RegisteredLoginProviderValue));
        Assert.Same(instance, LoginProviderEnum.FromName(RegisteredLoginProviderValue));

        var options = new JsonSerializerOptions();
        options.Converters.Add(new Geex.Json.EnumerationConverter());
        Assert.Same(instance, JsonSerializer.Deserialize<LoginProviderEnum>(
            JsonSerializer.Serialize(RegisteredLoginProviderValue), options));
        Assert.Same(instance, DeserializeBsonString<LoginProviderEnum>(RegisteredLoginProviderValue));

        var constructorInstance = LoginProviderEnum.FromValue(RegisteredConstructorLoginProviderValue);
        var constructorChild = Assert.IsType<RegisteredConstructorLoginProviders>(constructorInstance);
        Assert.Same(RegisteredConstructorLoginProviders.Extra, constructorChild);
        Assert.Equal(RegisteredConstructorLoginProviderName, constructorChild.Name);
        Assert.Equal(RegisteredConstructorLoginProviderValue, constructorChild.Value);
        Assert.Equal($"{RegisteredConstructorLoginProviderName}:{RegisteredConstructorLoginProviderValue}",
            constructorChild.Description);
        Assert.Equal(1, RegisteredConstructorLoginProviders.ConstructorCalls);
        Assert.Same(constructorChild, LoginProviderEnum.FromNameAndValue<RegisteredConstructorLoginProviders>(
            RegisteredConstructorLoginProviderName, RegisteredConstructorLoginProviderValue));
    }

    [Fact]
    public void RegisteredChildConstructor_ParentLookupDiscoversAndReusesDeclaredChild()
    {
        using var registration = new RegisteredAssemblyScope(typeof(EnumerationDynamicTests).Assembly);

        var instance = DiscoveredConstructorRoot.FromValue(DiscoveredConstructorValue);

        var child = Assert.IsType<DiscoveredConstructorChild>(instance);
        Assert.Same(DiscoveredConstructorChild.Known, child);
        Assert.Equal(DiscoveredConstructorName, child.Name);
        Assert.Equal(1, DiscoveredConstructorChild.ConstructorCalls);
        Assert.Same(child, DiscoveredConstructorRoot.FromValue<DiscoveredConstructorChild>(DiscoveredConstructorValue));
        Assert.Same(child, DiscoveredConstructorRoot.FromName(DiscoveredConstructorName));
    }

    [Fact]
    public void DeclaredStaticValue_RequiredConstructorIsDiscoveredWithoutDynamicConstructor()
    {
        var instance = RequiredConstructorEnum.FromValue(RequiredConstructorValue);

        Assert.Same(RequiredConstructorEnum.Known, instance);
        Assert.Equal(RequiredConstructorName, instance.Name);
        Assert.Equal("declared state", instance.Description);
        Assert.Same(instance, RequiredConstructorEnum.FromName(RequiredConstructorName));
    }

    [Fact]
    public void DeclaredReadonlyField_RequiredConstructorIsDiscoveredWithoutDynamicConstructor()
    {
        var instance = RequiredFieldEnum.FromValue(RequiredFieldValue);

        Assert.Same(RequiredFieldEnum.Known, instance);
        Assert.Equal(RequiredFieldName, instance.Name);
        Assert.Equal("declared field state", instance.Description);
        Assert.Same(instance, RequiredFieldEnum.FromName(RequiredFieldName));
    }

    [Fact]
    public void CompatibleDerivedValue_ParentLookupReusesInstanceAndWarns()
    {
        var value = UniqueValue();
        var child = CompatibleLookupRoot.FromValue<CompatibleLookupChild>(value);
        var loggerField = typeof(Enumeration<CompatibleLookupRoot>)
            .GetField("_logger", BindingFlags.NonPublic | BindingFlags.Static)!;
        var originalLogger = loggerField.GetValue(null);
        var logger = new RecordingEnumerationLogger();
        loggerField.SetValue(null, logger);

        try
        {
            Assert.Same(child, CompatibleLookupRoot.FromValue(value));
            Assert.Same(child, CompatibleLookupRoot.FromNameAndValue(value, value));
            Assert.Contains(logger.Levels, level => level == LogLevel.Warning);
            Assert.Single(CompatibleLookupRoot.List, item => item.Value == value);
        }
        finally
        {
            loggerField.SetValue(null, originalLogger);
        }
    }

    [Fact]
    public void IncompatibleChild_RejectsExistingParentWithoutCreatingAnotherInstance()
    {
        var value = UniqueValue();
        var parent = IncompatibleLookupRoot.FromValue(value);

        Assert.Throws<InvalidOperationException>(() =>
            IncompatibleLookupRoot.FromValue<IncompatibleLookupChild>(value));
        Assert.Throws<InvalidOperationException>(() =>
            IncompatibleLookupRoot.FromNameAndValue<IncompatibleLookupChild>(value, value));

        Assert.Equal(0, IncompatibleLookupChild.ConstructorCalls);
        Assert.Same(parent, IncompatibleLookupRoot.FromValue(value));
        Assert.Same(parent, IncompatibleLookupRoot.ValueCacheDictionary[value]);
        Assert.Single(IncompatibleLookupRoot.List, item => item.Value == value);
    }

    [Fact]
    public void SameValueDifferentName_ReusesCanonicalInstanceAndPreservesItsName()
    {
        var canonicalName = UniqueValue();
        var requestedName = UniqueValue();
        var value = UniqueValue();
        var canonical = IdentityContractEnum<DifferentName>.FromNameAndValue(canonicalName, value);

        var repeated = IdentityContractEnum<DifferentName>.FromNameAndValue(requestedName, value);

        Assert.Same(canonical, repeated);
        Assert.Equal(canonicalName, repeated.Name);
        Assert.Equal(value, repeated.Value);
        Assert.Single(IdentityContractEnum<DifferentName>.List, item => item.Value == value);
    }

    [Fact]
    public void SameNameDifferentValue_RejectsConflictWithoutChangingCanonicalInstance()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        var conflictingValue = UniqueValue();
        var canonical = IdentityContractEnum<DifferentValue>.FromNameAndValue(name, value);

        Assert.Throws<InvalidOperationException>(() =>
            IdentityContractEnum<DifferentValue>.FromNameAndValue(name, conflictingValue));

        Assert.Same(canonical, IdentityContractEnum<DifferentValue>.FromName(name));
        Assert.Null(IdentityContractEnum<DifferentValue>.FromExistedValue(conflictingValue, noException: true));
        Assert.Single(IdentityContractEnum<DifferentValue>.List, item => item.Name == name);

        var otherName = UniqueValue();
        var other = IdentityContractEnum<DifferentValue>.FromNameAndValue(otherName, conflictingValue);

        Assert.Throws<InvalidOperationException>(() =>
            IdentityContractEnum<DifferentValue>.FromNameAndValue(name, conflictingValue));

        Assert.Same(canonical, IdentityContractEnum<DifferentValue>.FromName(name));
        Assert.Same(canonical, IdentityContractEnum<DifferentValue>.FromValue(value));
        Assert.Same(other, IdentityContractEnum<DifferentValue>.FromName(otherName));
        Assert.Same(other, IdentityContractEnum<DifferentValue>.FromValue(conflictingValue));
        Assert.Equal(2, IdentityContractEnum<DifferentValue>.List.Count());
    }

    [Fact]
    public void RegisteredAssemblyChanges_RetainDynamicInstancesAliasesAndDiscoverReplacements()
    {
        var value = UniqueValue();
        var alias = UniqueValue();
        var dynamicInstance = AssemblyRetentionEnum.FromValue(value, alias);
        var firstValue = UniqueValue();
        var secondValue = UniqueValue();
        var firstType = CreateRegisteredEnumType(firstValue);
        var secondType = CreateRegisteredEnumType(secondValue);

        try
        {
            GeexModule.KnownModuleAssembly.Add(firstType.Assembly);
            var registeredCount = GeexModule.KnownModuleAssembly.Count;
            var first = AssemblyRetentionEnum.FromValue(firstValue);
            Assert.IsType(firstType, first);
            AssertRetained();

            GeexModule.KnownModuleAssembly.Remove(firstType.Assembly);
            GeexModule.KnownModuleAssembly.Add(secondType.Assembly);
            Assert.Equal(registeredCount, GeexModule.KnownModuleAssembly.Count);

            var second = AssemblyRetentionEnum.FromValue(secondValue);
            Assert.IsType(secondType, second);
            Assert.Same(first, AssemblyRetentionEnum.FromValue(firstValue));
            AssertRetained();

            GeexModule.KnownModuleAssembly.Remove(secondType.Assembly);
            _ = AssemblyRetentionEnum.DynamicValues;
            Assert.Same(first, AssemblyRetentionEnum.FromValue(firstValue));
            Assert.Same(second, AssemblyRetentionEnum.FromValue(secondValue));
            AssertRetained();
        }
        finally
        {
            GeexModule.KnownModuleAssembly.Remove(firstType.Assembly);
            GeexModule.KnownModuleAssembly.Remove(secondType.Assembly);
        }

        void AssertRetained()
        {
            Assert.Same(dynamicInstance, AssemblyRetentionEnum.FromValue(value));
            Assert.Same(dynamicInstance, AssemblyRetentionEnum.FromValue(alias));
            Assert.Same(dynamicInstance, AssemblyRetentionEnum.FromName(value));
            Assert.Single(AssemblyRetentionEnum.List, item => item.Value == value);
        }
    }

    [Fact]
    public async Task ConcurrentSameValue_ReturnsOneCanonicalInstance()
    {
        var value = UniqueValue();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = Enumerable.Range(0, 32).Select(async _ =>
        {
            await start.Task;
            return ConcurrentCreationEnum.FromValue(value);
        }).ToArray();

        start.SetResult();
        var instances = await Task.WhenAll(calls).WaitAsync(TimeSpan.FromSeconds(15));

        Assert.All(instances, instance => Assert.Same(instances[0], instance));
        Assert.Same(instances[0], ConcurrentCreationEnum.FromValue(value));
        Assert.Single(ConcurrentCreationEnum.List, item => item.Value == value);
        Assert.Equal(1, ConcurrentCreationEnum.ConstructorCalls);
    }

    [Fact]
    public void MixedStaticPropertyInitialization_ReusesEarlierDeclaredInstance()
    {
        var instance = MixedInitializationEnum<MixedPropertyCase>.FromValue(KnownValue<MixedPropertyCase>());
        Assert.Same(MixedInitializationEnum<MixedPropertyCase>.Known, instance);
        Assert.Same(instance, MixedInitializationEnum<MixedPropertyCase>.Alias);
        Assert.Equal(KnownName<MixedPropertyCase>(), instance.Name);
    }

    [Fact]
    public void MixedReadonlyFieldInitialization_ReusesEarlierDeclaredInstance()
    {
        var instance = MixedFieldInitializationEnum<MixedFieldCase>.FromValue(KnownValue<MixedFieldCase>());
        Assert.Same(MixedFieldInitializationEnum<MixedFieldCase>.Known, instance);
        Assert.Same(instance, MixedFieldInitializationEnum<MixedFieldCase>.Alias);
    }

    [Fact]
    public void MixedStaticInitialization_FromNamePreservesDeclaredIdentity()
    {
        var instance = MixedInitializationEnum<MixedNameCase>.FromName(KnownName<MixedNameCase>());
        Assert.Same(MixedInitializationEnum<MixedNameCase>.Known, instance);
        Assert.Same(instance, MixedInitializationEnum<MixedNameCase>.Alias);
    }

    [Fact]
    public void MixedStaticInitialization_ListContainsOneDeclaredInstance()
    {
        var instance = Assert.Single(MixedInitializationEnum<MixedListCase>.List);
        Assert.Same(MixedInitializationEnum<MixedListCase>.Known, instance);
        Assert.Same(instance, MixedInitializationEnum<MixedListCase>.Alias);
    }

    [Fact]
    public void MixedStaticInitialization_DirectMemberAccessMatchesLookup()
    {
        var instance = MixedInitializationEnum<MixedDirectCase>.Known;
        Assert.Same(instance, MixedInitializationEnum<MixedDirectCase>.Alias);
        Assert.Same(instance, MixedInitializationEnum<MixedDirectCase>.FromValue(instance.Value));
    }

    [Fact]
    public void JsonSerialization_WritesValueAndRoundtripsDifferentName()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        var options = JsonOptions();
        var instance = IdentityContractEnum<JsonRoundtripCase>.FromNameAndValue(name, value);

        var json = JsonSerializer.Serialize(instance, options);

        Assert.Equal(JsonSerializer.Serialize(value), json);
        Assert.Same(instance, JsonSerializer.Deserialize<IdentityContractEnum<JsonRoundtripCase>>(json, options));
    }

    [Fact]
    public void JsonDeserialization_AcceptsUnambiguousLegacyName()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        var instance = IdentityContractEnum<JsonLegacyCase>.FromNameAndValue(name, value);

        Assert.Same(instance, JsonSerializer.Deserialize<IdentityContractEnum<JsonLegacyCase>>(
            JsonSerializer.Serialize(name), JsonOptions()));
        Assert.Null(IdentityContractEnum<JsonLegacyCase>.FromExistedValue(name, noException: true));
    }

    [Fact]
    public void JsonDeserialization_RejectsNameValueAmbiguityWithoutChangingCache()
    {
        var sharedToken = UniqueValue();
        var named = IdentityContractEnum<JsonAmbiguousCase>.FromNameAndValue(sharedToken, UniqueValue());
        var valued = IdentityContractEnum<JsonAmbiguousCase>.FromNameAndValue(UniqueValue(), sharedToken);

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IdentityContractEnum<JsonAmbiguousCase>>(
            JsonSerializer.Serialize(sharedToken), JsonOptions()));

        Assert.Same(named, IdentityContractEnum<JsonAmbiguousCase>.FromName(sharedToken));
        Assert.Same(valued, IdentityContractEnum<JsonAmbiguousCase>.FromValue(sharedToken));
        Assert.Equal(2, IdentityContractEnum<JsonAmbiguousCase>.List.Count());
    }

    [Fact]
    public void TypedJsonBsonAndReflection_ReuseTheRootFamilyInstance()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        var child = ParameterlessRoot.FromNameAndValue<ParameterlessChild>(name, value);

        Assert.Same(child, JsonSerializer.Deserialize<ParameterlessChild>(JsonSerializer.Serialize(value), JsonOptions()));
        Assert.Same(child, DeserializeBsonString<ParameterlessChild>(value));
        Assert.Same(child, EnumerationReflectionCache.FromValue<ParameterlessChild>(value));
        Assert.Same(child, EnumerationReflectionCache.FromValue(typeof(ParameterlessChild), value));
        Assert.Same(child, ParameterlessRoot.FromValue(value));
        Assert.Single(ParameterlessRoot.List, item => item.Value == value);
    }

    [Fact]
    public void TypedConsumers_RejectAnIncompatibleCachedParent()
    {
        var value = UniqueValue();
        var parent = IncompatibleLookupRoot.FromValue(value);

        Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<IncompatibleLookupChild>(JsonSerializer.Serialize(value), JsonOptions()));
        Assert.Throws<InvalidOperationException>(() => DeserializeBsonString<IncompatibleLookupChild>(value));
        Assert.Throws<InvalidOperationException>(() => EnumerationReflectionCache.FromValue<IncompatibleLookupChild>(value));
        Assert.Same(parent, IncompatibleLookupRoot.FromValue(value));
        Assert.Single(IncompatibleLookupRoot.List, item => item.Value == value);
    }

    [Fact]
    public void DetailsEnumeration_ReflectionAndGraphqlDiscoveryUseTheRootFamily()
    {
        var values = new Geex.Gql.GeexTypeInspector().GetEnumValues(typeof(StaticDetailsEnum)).ToArray();
        var instance = Assert.IsType<StaticDetailsEnum>(Assert.Single(values));

        Assert.Same(StaticDetailsEnum.Known, instance);
        Assert.True(instance.Definition.IsComplete);
        Assert.Equal(new Details("declared", 4), instance.Definition.Details);
        Assert.Same(instance, EnumerationReflectionCache.FromValue<StaticDetailsEnum>(instance.Value));
        Assert.Same(instance, JsonSerializer.Deserialize<StaticDetailsEnum>(JsonSerializer.Serialize(instance.Value), JsonOptions()));
        Assert.Same(instance, DeserializeBsonString<StaticDetailsEnum>(instance.Value));
    }

    [Fact]
    public void AliasConflict_DoesNotLeaveEarlierAliasesFromTheFailedBatch()
    {
        var taken = UniqueValue();
        var free = UniqueValue();
        var value = UniqueValue();
        var existing = IdentityContractEnum<AliasFailureCase>.FromValue(taken);
        var canonical = IdentityContractEnum<AliasFailureCase>.FromValue(value);

        Assert.Throws<InvalidOperationException>(() =>
            IdentityContractEnum<AliasFailureCase>.FromValue(value, free, taken));

        Assert.Null(IdentityContractEnum<AliasFailureCase>.FromExistedValue(free, noException: true));
        Assert.Same(existing, IdentityContractEnum<AliasFailureCase>.FromValue(taken));
        Assert.Same(canonical, IdentityContractEnum<AliasFailureCase>.FromValue(value));
    }

    [Fact]
    public void SingleStringOptionalAndObjectConstructors_AreNotInferred()
    {
        var single = UniqueValue();
        var optional = UniqueValue();
        var objects = UniqueValue();
        Assert.Throws<InvalidOperationException>(() => SingleStringConstructorEnum.FromValue(single));
        Assert.Throws<InvalidOperationException>(() => OptionalConstructorEnum.FromValue(optional));
        Assert.Throws<InvalidOperationException>(() => ObjectConstructorEnum.FromValue(objects));
        AssertNotCached<SingleStringConstructorEnum>(single, single);
        AssertNotCached<OptionalConstructorEnum>(optional, optional);
        AssertNotCached<ObjectConstructorEnum>(objects, objects);
    }

    [Fact]
    public async Task ConcurrentEqualDefinitions_PublishOneCompleteInstance()
    {
        var value = UniqueValue();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = Enumerable.Range(0, 16).Select(async _ =>
        {
            await ready.Task;
            return DetailsEnum<ConcurrentDefinitionsCase>.Define(value, value, new Details("complete", 5));
        }).ToArray();
        ready.SetResult();
        var results = await Task.WhenAll(calls).WaitAsync(TimeSpan.FromSeconds(15));

        Assert.All(results, result => Assert.Same(results[0], result));
        Assert.True(results[0].Definition.IsComplete);
        Assert.Equal(new Details("complete", 5), results[0].Definition.Details);
    }

    [Fact]
    public async Task ConcurrentLookupThenDefine_CompletesOneSharedInstance()
    {
        var value = UniqueValue();
        var lookup = Task.Run(() => BlockedDetailsEnum<LookupFirstCase>.FromValue(value));
        Task<BlockedDetailsEnum<LookupFirstCase>>? definition = null;
        try
        {
            await BlockedDetailsEnum<LookupFirstCase>.ConstructorEntered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            definition = Task.Run(() =>
            {
                secondStarted.SetResult();
                return BlockedDetailsEnum<LookupFirstCase>.Define(value, value, new Details("complete", 6));
            });
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Null(BlockedDetailsEnum<LookupFirstCase>.FromExistedValue(value, noException: true));
        }
        finally
        {
            BlockedDetailsEnum<LookupFirstCase>.ReleaseConstructor.Set();
        }

        var results = await Task.WhenAll(lookup, definition!).WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Same(results[0], results[1]);
        Assert.True(results[0].Definition.IsComplete);
        Assert.Equal(new Details("complete", 6), results[0].Definition.Details);
        Assert.Equal(1, BlockedDetailsEnum<LookupFirstCase>.ConstructorCalls);
    }

    [Fact]
    public async Task ConcurrentDefineThenLookup_NeverPublishesAnUnknownSnapshot()
    {
        using var logging = new LoggerScope<BlockedDetailsEnum<DefinitionFirstCase>>();
        var value = UniqueValue();
        var define = Task.Run(() => BlockedDetailsEnum<DefinitionFirstCase>.Define(value, value, new Details("complete", 8)));
        Task<(BlockedDetailsEnum<DefinitionFirstCase> Instance, EnumerationDefinition<Details> Snapshot)>? lookup = null;
        try
        {
            await BlockedDetailsEnum<DefinitionFirstCase>.ConstructorEntered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lookup = Task.Run(() =>
            {
                secondStarted.SetResult();
                var instance = BlockedDetailsEnum<DefinitionFirstCase>.FromValue(value);
                return (instance, instance.Definition);
            });
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.Null(BlockedDetailsEnum<DefinitionFirstCase>.FromExistedValue(value, noException: true));
        }
        finally
        {
            BlockedDetailsEnum<DefinitionFirstCase>.ReleaseConstructor.Set();
        }

        var defined = await define.WaitAsync(TimeSpan.FromSeconds(15));
        var observed = await lookup!.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Same(defined, observed.Instance);
        Assert.True(observed.Snapshot.IsComplete);
        Assert.Equal(new Details("complete", 8), observed.Snapshot.Details);
        Assert.Equal(1, BlockedDetailsEnum<DefinitionFirstCase>.ConstructorCalls);
        Assert.Empty(logging.Logger.Levels);
    }

    [Fact]
    public async Task ConcurrentReaders_ObserveWholeUnknownOrCompleteSnapshots()
    {
        var value = UniqueValue();
        var instance = DetailsEnum<AtomicSnapshotCase>.FromValue(value);
        var snapshots = new ConcurrentBag<EnumerationDefinition<Details>> { instance.Definition };
        var reading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = Task.Run(async () =>
        {
            reading.SetResult();
            for (var index = 0; index < 1000 && !completed.Task.IsCompleted; index++)
            {
                snapshots.Add(instance.Definition);
                await Task.Yield();
            }
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(15));
            snapshots.Add(instance.Definition);
        });
        var writer = Task.Run(async () =>
        {
            await reading.Task.WaitAsync(TimeSpan.FromSeconds(15));
            try
            {
                DetailsEnum<AtomicSnapshotCase>.Define(value, value, new Details("complete", 42));
            }
            finally
            {
                completed.TrySetResult();
            }
        });

        await Task.WhenAll(reader, writer).WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Contains(snapshots, snapshot => !snapshot.IsComplete);
        Assert.Contains(snapshots, snapshot => snapshot.IsComplete);
        Assert.All(snapshots, snapshot => Assert.Equal(
            snapshot.IsComplete ? new Details("complete", 42) : UnknownDetails, snapshot.Details));
    }

    [Fact]
    public void BuiltInPermissionSubtypes_PreserveRequestedTypeAndParsedParts()
    {
        Type[] types =
        [
            typeof(Geex.Extensions.AuditLogs.AuditLogsPermission),
            typeof(Geex.Extensions.Backups.BackupsPermission),
            typeof(Geex.Extensions.Authorization.AuthorizationPermission),
            typeof(Geex.Extensions.Identity.IdentityPermission),
            typeof(Geex.Extensions.Identity.IdentityPermission.UserPermission),
            typeof(Geex.Extensions.Identity.IdentityPermission.RolePermission),
            typeof(Geex.Extensions.Identity.IdentityPermission.OrgPermission),
            typeof(Geex.Extensions.MultiTenant.Api.MultiTenantPermission),
            typeof(Geex.Extensions.MultiTenant.Api.MultiTenantPermission.TenantPermission),
            typeof(Geex.Extensions.Settings.SettingsPermission)
        ];
        foreach (var type in types)
        {
            var field = UniqueValue();
            var value = $"DynamicModule_query_{field}";
            var instance = Assert.IsAssignableFrom<AppPermission>(EnumerationReflectionCache.FromValue(type, value));
            Assert.IsType(type, instance);
            Assert.Equal(value, instance.Name);
            Assert.Equal(value, instance.Value);
            Assert.Equal("DynamicModule", instance.Mod);
            Assert.Equal("query", instance.Obj);
            Assert.Equal(field, instance.Field);
            Assert.Same(instance, AppPermission.FromValue(value));
        }
    }

    [Fact]
    public void InvalidPermissionValue_FailsWithoutCachingAPartialPermission()
    {
        var value = UniqueValue();
        Assert.Throws<InvalidOperationException>(() => AppPermission.FromValue(value));
        AssertNotCached<AppPermission>(value, value);
    }
    [Fact]
    public async Task ConcurrentConflictingDefinitions_KeepTheWinningCompleteSnapshot()
    {
        var value = UniqueValue();
        var instance = DetailsEnum<ConflictingDefinitionsCase>.FromValue(value);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Details[] candidates = [new("first", 1), new("second", 2)];
        var calls = candidates.Select(candidate => Task.Run(async () =>
        {
            await start.Task;
            try
            {
                var defined = DetailsEnum<ConflictingDefinitionsCase>.Define(value, value, candidate);
                return (Candidate: candidate, Instance: defined, Error: (InvalidOperationException?)null);
            }
            catch (InvalidOperationException error)
            {
                return (Candidate: candidate, Instance: (DetailsEnum<ConflictingDefinitionsCase>?)null, Error: error);
            }
        })).ToArray();
        start.SetResult();

        var results = await Task.WhenAll(calls).WaitAsync(TimeSpan.FromSeconds(15));
        var winner = Assert.Single(results, result => result.Error is null);
        Assert.Single(results, result => result.Error is not null);
        Assert.Same(instance, winner.Instance);
        Assert.True(instance.Definition.IsComplete);
        Assert.Equal(winner.Candidate, instance.Definition.Details);
        Assert.Same(instance, DetailsEnum<ConflictingDefinitionsCase>.FromValue(value));
    }

    [Fact]
    public void JsonValueAlias_RoundtripsAsCanonicalValue()
    {
        var value = UniqueValue();
        var alias = UniqueValue();
        var instance = IdentityContractEnum<JsonAliasCase>.FromValue(value, alias);
        var options = JsonOptions();

        var restored = JsonSerializer.Deserialize<IdentityContractEnum<JsonAliasCase>>(JsonSerializer.Serialize(alias), options);

        Assert.Same(instance, restored);
        Assert.Equal(JsonSerializer.Serialize(value), JsonSerializer.Serialize(restored, options));
    }
    [Fact]
    public void NongenericEnumeration_DoesNotImportOtherFamiliesDeclaredValues()
    {
        var first = FirstIndependentFamily.Known;
        var second = SecondIndependentFamily.Known;
        using var registration = new RegisteredAssemblyScope(typeof(EnumerationDynamicTests).Assembly);

        var plain = EnumerationReflectionCache.FromValue<Enumeration>(IndependentFamilySharedValue);
        var restored = JsonSerializer.Deserialize<Enumeration>(JsonSerializer.Serialize(IndependentFamilySharedValue), JsonOptions());

        Assert.IsType<Enumeration>(plain);
        Assert.Same(plain, restored);
        Assert.NotSame(first, plain);
        Assert.NotSame(second, plain);
        Assert.Same(first, FirstIndependentFamily.FromValue(IndependentFamilySharedValue));
        Assert.Same(second, SecondIndependentFamily.FromValue(IndependentFamilySharedValue));
        Assert.Single(Enumeration<IEnumeration>.List, item => item.Value == IndependentFamilySharedValue);
    }

    [Fact]
    public void DirectStaticInitialization_DefersComputedGettersUntilStoredMembersAreReady()
    {
        var instance = DeferredComputedEnum<DeferredComputedCase>.Known;
        Assert.Equal(0, DeferredComputedEnum<DeferredComputedCase>.ComputedReads);

        var repeated = DeferredComputedEnum<DeferredComputedCase>.FromValue(instance.Value);

        Assert.Same(instance, repeated);
        Assert.Same(instance, DeferredComputedEnum<DeferredComputedCase>.Alias);
        Assert.Same(instance, Assert.Single(DeferredComputedEnum<DeferredComputedCase>.List));
        Assert.True(DeferredComputedEnum<DeferredComputedCase>.ComputedReads > 0);
    }
    [Fact]
    public void CompletedStaticInitialization_NullStoredMembersDoNotHideComputedValues()
    {
        var instance = Assert.Single(NullableStaticStorageEnum<NullableStaticStorageCase>.List);

        Assert.Same(NullableStaticStorageEnum<NullableStaticStorageCase>.Computed, instance);
        Assert.Equal(KnownName<NullableStaticStorageCase>(), instance.Name);
        Assert.Same(instance, NullableStaticStorageEnum<NullableStaticStorageCase>.FromValue(KnownValue<NullableStaticStorageCase>()));
        Assert.Null(NullableStaticStorageEnum<NullableStaticStorageCase>.OptionalProperty);
        Assert.Null(NullableStaticStorageEnum<NullableStaticStorageCase>.OptionalField);
    }
    [Fact]
    public void CrossGenericStaticInitialization_DiscoversTheCompletedOtherTypesComputedValue()
    {
        var outer = CrossGenericInitializationEnum<CrossGenericOuterCase>.Known;
        var referenced = CrossGenericInitializationEnum<CrossGenericOuterCase>.ReferencedByInitializer;
        var inner = CrossGenericInitializationEnum<CrossGenericInnerCase>.Known;

        Assert.NotNull(referenced);
        Assert.Same(inner, referenced);
        Assert.Equal(KnownName<CrossGenericInnerCase>(), referenced.Name);
        Assert.Same(inner, CrossGenericInitializationEnum<CrossGenericInnerCase>.FromValue(KnownValue<CrossGenericInnerCase>()));
        Assert.Single(CrossGenericInitializationEnum<CrossGenericInnerCase>.List);
        Assert.Same(outer, Assert.Single(CrossGenericInitializationEnum<CrossGenericOuterCase>.List));
    }
    [Fact]
    public void DirectStaticLookup_DoesNotMarkLaterStoredMembersAsFullyDiscovered()
    {
        var first = LaterStoredMemberEnum<LaterStoredMemberCase>.First;

        var second = LaterStoredMemberEnum<LaterStoredMemberCase>.FromValue(KnownValue<LaterStoredMemberCase>());

        Assert.Equal(KnownValue<LaterStoredMemberCase>() + "-first", first.Value);
        Assert.Same(LaterStoredMemberEnum<LaterStoredMemberCase>.Second, second);
        Assert.Equal(KnownName<LaterStoredMemberCase>(), second.Name);
        Assert.Same(first, LaterStoredMemberEnum<LaterStoredMemberCase>.FromValue(first.Value));
        Assert.Equal(2, LaterStoredMemberEnum<LaterStoredMemberCase>.List.Count());
    }
    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new Geex.Json.EnumerationConverter());
        return options;
    }

    private static void AssertInvalidConstructor<TFailure>()
    {
        var name = UniqueValue();
        var value = UniqueValue();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            Assert.Throws<InvalidOperationException>(() => InvalidConstructorEnum<TFailure>.FromNameAndValue(name, value));
            AssertNotCached<InvalidConstructorEnum<TFailure>>(name, value);
            Assert.Empty(InvalidConstructorEnum<TFailure>.List);
        }
        Assert.Equal(2, InvalidConstructorEnum<TFailure>.ConstructorCalls);
        Assert.Equal(0, InvalidConstructorEnum<TFailure>.ParameterlessCalls);
    }
    private static Type CreateRegisteredEnumType(string value)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("EnumerationDynamicTests_" + UniqueValue()), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Enumerations");
        var type = module.DefineType("RegisteredEnum_" + value,
            TypeAttributes.Public | TypeAttributes.Class, typeof(AssemblyRetentionEnum));
        type.DefineDefaultConstructor(MethodAttributes.Public);
        var getter = type.DefineMethod("get_Known",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
            type, Type.EmptyTypes);
        var lookup = typeof(Enumeration<AssemblyRetentionEnum>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(AssemblyRetentionEnum.FromValue) &&
                method.IsGenericMethodDefinition && method.GetParameters().Length == 1);
        var body = getter.GetILGenerator();
        body.Emit(OpCodes.Ldstr, value);
        body.Emit(OpCodes.Call, lookup.MakeGenericMethod(type));
        body.Emit(OpCodes.Ret);
        var property = type.DefineProperty("Known", PropertyAttributes.None, type, Type.EmptyTypes);
        property.SetGetMethod(getter);
        return type.CreateType()!;
    }

    private static TEnum? DeserializeBsonString<TEnum>(string value)
        where TEnum : class, IEnumeration
    {
        using var stream = new MemoryStream(new BsonDocument("enum", value).ToBson());
        using var reader = new BsonBinaryReader(stream);
        reader.ReadStartDocument();
        Assert.Equal(BsonType.String, reader.ReadBsonType());
        Assert.Equal("enum", reader.ReadName());

        var serializer = new EnumerationSerializer<TEnum>();
        var instance = serializer.Deserialize(
            BsonDeserializationContext.CreateRoot(reader),
            new BsonDeserializationArgs { NominalType = typeof(TEnum) });

        Assert.Equal(BsonType.EndOfDocument, reader.ReadBsonType());
        reader.ReadEndDocument();
        return instance;
    }

    private static void AssertNotCached<TEnum>(string name, string value, Type? targetType = null)
        where TEnum : class, IEnumeration
    {
        Assert.Null(Enumeration<TEnum>.FromExistedName(name, noException: true));
        Assert.Null(Enumeration<TEnum>.FromExistedName(name, ignoreCase: true, noException: true));
        Assert.Null(Enumeration<TEnum>.FromExistedValue(value, noException: true));
        Assert.False(Enumeration<TEnum>.ValueCacheDictionary.ContainsKey(name));
        Assert.False(IEnumeration.ValueCacheDictionary.ContainsKey(
            $"{(targetType ?? typeof(TEnum)).Name}.{name}"));
    }

    private static string UniqueValue() => Guid.NewGuid().ToString("N");
    private static string KnownName<TMarker>() => typeof(TMarker).Name + "-name";
    private static string KnownValue<TMarker>() => typeof(TMarker).Name + "-value";

    private const string RegisteredLoginProviderValue = "EnumerationDynamicTests_RegisteredLoginProvider";
    private const string RegisteredConstructorLoginProviderName = "EnumerationConstructorLoginProvider";
    private const string RegisteredConstructorLoginProviderValue = "EnumerationDynamicTests_RegisteredConstructorLoginProvider";
    private const string DiscoveredConstructorName = "Enumeration constructor discovered child";
    private const string DiscoveredConstructorValue = "EnumerationDynamicTests_DiscoveredConstructorChild";
    private const string RequiredConstructorName = "Enumeration constructor declared value";
    private const string RequiredConstructorValue = "EnumerationDynamicTests_RequiredConstructor";
    private const string RequiredFieldName = "Enumeration constructor declared field";
    private const string RequiredFieldValue = "EnumerationDynamicTests_RequiredField";

    private sealed class RegisteredAssemblyScope : IDisposable
    {
        private readonly Assembly _assembly;
        private readonly bool _added;

        public RegisteredAssemblyScope(Assembly assembly)
        {
            _assembly = assembly;
            _added = !GeexModule.KnownModuleAssembly.Contains(assembly);
            if (_added)
            {
                GeexModule.KnownModuleAssembly.Add(assembly);
            }
        }

        public void Dispose()
        {
            if (_added)
            {
                GeexModule.KnownModuleAssembly.Remove(_assembly);
            }
        }
    }

    private sealed class RecordingEnumerationLogger : ILogger<Enumeration>
    {
        public ConcurrentQueue<LogLevel> Levels { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Levels.Enqueue(logLevel);
        }
    }
    private const string IndependentFamilySharedValue = "EnumerationDynamicTests_SharedFamilyValue";

    private sealed class FirstIndependentFamily : Enumeration<FirstIndependentFamily>
    {
        public static FirstIndependentFamily Known { get; } = new("FirstFamily", IndependentFamilySharedValue);
        static FirstIndependentFamily() { }
        private FirstIndependentFamily(string name, string value) : base(name, value) { }
    }

    private sealed class SecondIndependentFamily : Enumeration<SecondIndependentFamily>
    {
        public static SecondIndependentFamily Known { get; } = new("SecondFamily", IndependentFamilySharedValue);
        static SecondIndependentFamily() { }
        private SecondIndependentFamily(string name, string value) : base(name, value) { }
    }

    private sealed class DeferredComputedEnum<TMarker> : Enumeration<DeferredComputedEnum<TMarker>>
    {
        public static int ComputedReads;
        public static DeferredComputedEnum<TMarker> Known { get; } = new(KnownName<TMarker>(), KnownValue<TMarker>());
        public static DeferredComputedEnum<TMarker> Alias { get; } = FromValue(Known.Value);
        public static DeferredComputedEnum<TMarker> Computed
        {
            get
            {
                ComputedReads++;
                return Alias ?? throw new InvalidOperationException("The static alias is not ready.");
            }
        }

        static DeferredComputedEnum() { }
        private DeferredComputedEnum(string name, string value) : base(name, value) { }
    }
    private sealed class NullableStaticStorageEnum<TMarker> : Enumeration<NullableStaticStorageEnum<TMarker>>
    {
        public static NullableStaticStorageEnum<TMarker>? OptionalProperty { get; } = null;
        public static readonly NullableStaticStorageEnum<TMarker>? OptionalField = null;
        private static readonly NullableStaticStorageEnum<TMarker> ComputedInstance = new(KnownName<TMarker>(), KnownValue<TMarker>());
        public static NullableStaticStorageEnum<TMarker> Computed => ComputedInstance;

        static NullableStaticStorageEnum() { }
        private NullableStaticStorageEnum(string name, string value) : base(name, value) { }
    }
    private sealed class CrossGenericInitializationEnum<TMarker> : Enumeration<CrossGenericInitializationEnum<TMarker>>
    {
        private static readonly CrossGenericInitializationEnum<TMarker> DeclaredInstance = new(KnownName<TMarker>(), KnownValue<TMarker>());
        public static CrossGenericInitializationEnum<TMarker> Known => DeclaredInstance;
        public static CrossGenericInitializationEnum<CrossGenericInnerCase>? ReferencedByInitializer { get; }

        static CrossGenericInitializationEnum()
        {
            if (typeof(TMarker) == typeof(CrossGenericOuterCase))
                ReferencedByInitializer = CrossGenericInitializationEnum<CrossGenericInnerCase>.FromValue(KnownValue<CrossGenericInnerCase>());
        }

        private CrossGenericInitializationEnum(string name, string value) : base(name, value) { }
    }
    private sealed class LaterStoredMemberEnum<TMarker> : Enumeration<LaterStoredMemberEnum<TMarker>>
    {
        public static LaterStoredMemberEnum<TMarker> First { get; } = FromValue(KnownValue<TMarker>() + "-first");
        public static LaterStoredMemberEnum<TMarker> Second { get; } = new(KnownName<TMarker>(), KnownValue<TMarker>());

        static LaterStoredMemberEnum() { }
        private LaterStoredMemberEnum(string name, string value) : base(name, value) { }
    }
    private sealed class LoggerScope<TEnum> : IDisposable where TEnum : class, IEnumeration
    {
        private readonly FieldInfo _field = typeof(Enumeration<TEnum>)
            .GetField("_logger", BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly object? _original;
        public RecordingEnumerationLogger Logger { get; } = new();

        public LoggerScope()
        {
            _original = _field.GetValue(null);
            _field.SetValue(null, Logger);
        }

        public void Dispose() => _field.SetValue(null, _original);
    }

    private static readonly Details UnknownDetails = new("unknown", -1);
    private sealed record Details(string Description, int Priority);

    private class DetailsEnum<TMarker> : Enumeration<DetailsEnum<TMarker>, Details>
    {
        internal DetailsEnum(string name, string value) : base(name, value, UnknownDetails, false) { }
    }

    private sealed class ParameterlessDetailsEnum : Enumeration<ParameterlessDetailsEnum, Details>
    {
        public ParameterlessDetailsEnum() : base(UnknownDetails) { }
    }

    private sealed class DerivedDetailsEnum : Enumeration<DerivedDetailsEnum, Details>
    {
        private DerivedDetailsEnum(string name, string value)
            : base(name, value, new Details(value, value.Length), true) { }
    }

    private class DetailsRoot : Enumeration<DetailsRoot, Details>
    {
        protected DetailsRoot(string name, string value) : base(name, value, UnknownDetails, false) { }
    }

    private sealed class DetailsChild : DetailsRoot
    {
        private DetailsChild(string name, string value) : base(name, value) { }
    }

    private sealed class ValueDetailsEnum : Enumeration<ValueDetailsEnum, int>
    {
        private ValueDetailsEnum(string name, string value) : base(name, value, -1, false) { }
    }

    private sealed class StaticDetailsEnum : Enumeration<StaticDetailsEnum, Details>
    {
        public static StaticDetailsEnum Known { get; } = Define("DefinedName", "DefinedValue", new Details("declared", 4));
        static StaticDetailsEnum() { }
        private StaticDetailsEnum(string name, string value) : base(name, value, UnknownDetails, false) { }
    }

    private sealed class BlockedDetailsEnum<TMarker> : Enumeration<BlockedDetailsEnum<TMarker>, Details>
    {
        public static readonly TaskCompletionSource ConstructorEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public static readonly ManualResetEventSlim ReleaseConstructor = new(false);
        public static int ConstructorCalls;

        private BlockedDetailsEnum(string name, string value) : base(name, value, UnknownDetails, false)
        {
            Interlocked.Increment(ref ConstructorCalls);
            ConstructorEntered.TrySetResult();
            if (!ReleaseConstructor.Wait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("The test did not release the enumeration constructor.");
        }
    }

    private sealed class MixedInitializationEnum<TMarker> : Enumeration<MixedInitializationEnum<TMarker>>
    {
        public static MixedInitializationEnum<TMarker> Known { get; } = new(KnownName<TMarker>(), KnownValue<TMarker>());
        public static MixedInitializationEnum<TMarker> Alias { get; } = FromValue(KnownValue<TMarker>());
        static MixedInitializationEnum() { }
        private MixedInitializationEnum(string name, string value) : base(name, value) { }
    }

    private sealed class MixedFieldInitializationEnum<TMarker> : Enumeration<MixedFieldInitializationEnum<TMarker>>
    {
        public static readonly MixedFieldInitializationEnum<TMarker> Known = new(KnownName<TMarker>(), KnownValue<TMarker>());
        public static readonly MixedFieldInitializationEnum<TMarker> Alias = FromValue(KnownValue<TMarker>());
        static MixedFieldInitializationEnum() { }
        private MixedFieldInitializationEnum(string name, string value) : base(name, value) { }
    }

    private sealed class RegisteredLoginProviders : LoginProviderEnum
    {
        public static RegisteredLoginProviders Extra { get; } = FromValue<RegisteredLoginProviders>(RegisteredLoginProviderValue);
        static RegisteredLoginProviders() { }
        public RegisteredLoginProviders() { }
    }

    private sealed class RegisteredConstructorLoginProviders : LoginProviderEnum
    {
        public static int ConstructorCalls;
        public static RegisteredConstructorLoginProviders Extra { get; } =
            FromNameAndValue<RegisteredConstructorLoginProviders>(RegisteredConstructorLoginProviderName, RegisteredConstructorLoginProviderValue);
        public string Description { get; }
        static RegisteredConstructorLoginProviders() { }

        private RegisteredConstructorLoginProviders(string name, string value) : base(name, value)
        {
            ConstructorCalls++;
            Description = $"{name}:{value}";
        }
    }

    private class DiscoveredConstructorRoot : Enumeration<DiscoveredConstructorRoot>
    {
        public DiscoveredConstructorRoot() { }
        protected DiscoveredConstructorRoot(string name, string value) : base(name, value) { }
    }

    private sealed class DiscoveredConstructorChild : DiscoveredConstructorRoot
    {
        public static int ConstructorCalls;
        public static DiscoveredConstructorChild Known { get; } =
            FromNameAndValue<DiscoveredConstructorChild>(DiscoveredConstructorName, DiscoveredConstructorValue);
        static DiscoveredConstructorChild() { }

        private DiscoveredConstructorChild(string name, string value) : base(name, value) => ConstructorCalls++;
    }

    private sealed class RequiredConstructorEnum : Enumeration<RequiredConstructorEnum>
    {
        public static RequiredConstructorEnum Known { get; } = new(RequiredConstructorName, RequiredConstructorValue, "declared state");
        public string Description { get; }
        static RequiredConstructorEnum() { }
        private RequiredConstructorEnum(string name, string value, string description) : base(name, value) => Description = description;
    }

    private sealed class RequiredFieldEnum : Enumeration<RequiredFieldEnum>
    {
        public static readonly RequiredFieldEnum Known = new(RequiredFieldName, RequiredFieldValue, "declared field state");
        public string Description { get; }
        static RequiredFieldEnum() { }
        private RequiredFieldEnum(string name, string value, string description) : base(name, value) => Description = description;
    }

    private class CompatibleLookupRoot : Enumeration<CompatibleLookupRoot>
    {
        public CompatibleLookupRoot() { }
    }

    private sealed class CompatibleLookupChild : CompatibleLookupRoot
    {
        public CompatibleLookupChild() { }
    }

    private class IncompatibleLookupRoot : Enumeration<IncompatibleLookupRoot>
    {
        public IncompatibleLookupRoot() { }
    }

    private sealed class IncompatibleLookupChild : IncompatibleLookupRoot
    {
        public static int ConstructorCalls;
        public IncompatibleLookupChild() => ConstructorCalls++;
    }

    private sealed class IdentityContractEnum<TMarker> : Enumeration<IdentityContractEnum<TMarker>>
    {
        public IdentityContractEnum() { }
    }

    public class AssemblyRetentionEnum : Enumeration<AssemblyRetentionEnum>
    {
        public AssemblyRetentionEnum() { }
    }

    private sealed class ConcurrentCreationEnum : Enumeration<ConcurrentCreationEnum>
    {
        public static int ConstructorCalls;
        public ConcurrentCreationEnum() => Interlocked.Increment(ref ConstructorCalls);
    }

    private sealed class ParameterlessEnum : Enumeration<ParameterlessEnum>
    {
        public ParameterlessEnum() { }
    }

    private class ParameterlessRoot : Enumeration<ParameterlessRoot>
    {
        public ParameterlessRoot() { }
    }

    private sealed class ParameterlessChild : ParameterlessRoot
    {
        public ParameterlessChild() { }
    }

    private sealed class PreferredConstructorEnum : Enumeration<PreferredConstructorEnum>
    {
        public static int ParameterlessCalls;
        public static int ConstructorCalls;
        public string Description { get; } = "parameterless";
        public PreferredConstructorEnum() => ParameterlessCalls++;

        private PreferredConstructorEnum(string name, string value) : base(name, value)
        {
            ConstructorCalls++;
            Description = $"{name}:{value}";
        }
    }

    private sealed class PublicConstructorEnum : Enumeration<PublicConstructorEnum>
    {
        public PublicConstructorEnum(string name, string value) : base(name, value) { }
    }

    private class ProtectedConstructorEnum : Enumeration<ProtectedConstructorEnum>
    {
        protected ProtectedConstructorEnum(string name, string value) : base(name, value) { }
    }

    private class ConstructorRoot : Enumeration<ConstructorRoot>
    {
        public static int ConstructorCalls;
        protected ConstructorRoot(string name, string value) : base(name, value)
        {
            if (GetType() == typeof(ConstructorRoot))
                ConstructorCalls++;
        }
    }

    private sealed class ChildWithoutUsableConstructor : ConstructorRoot
    {
        public ChildWithoutUsableConstructor(string name, string value, string description) : base(name, value) { }
    }

    private class FallbackConstructorRoot : Enumeration<FallbackConstructorRoot>
    {
        public static int ConstructorCalls;
        protected FallbackConstructorRoot() { }
        private FallbackConstructorRoot(string name, string value) : base(name, value) => ConstructorCalls++;
    }

    private sealed class FallbackConstructorChild : FallbackConstructorRoot
    {
        public static int ParameterlessCalls;
        public FallbackConstructorChild() => ParameterlessCalls++;
    }

    private sealed class ChildWithConstructor : ConstructorRoot
    {
        public string Source { get; } = "child constructor";
        private ChildWithConstructor(string name, string value) : base(name, value) { }
    }

    private sealed class IndependentConstructorEnum : IEnumeration
    {
        public string Name { get; }
        public string Value { get; }
        public string Description { get; }

        private IndependentConstructorEnum(string name, string value)
        {
            Name = name;
            Value = value;
            Description = $"{name}:{value}";
        }

        public override string ToString() => Value;
    }

    private sealed class ColdEnum<TMarker> : Enumeration<ColdEnum<TMarker>>
    {
        public static int ConstructorCalls;
        public static ColdEnum<TMarker> Known { get; } = FromNameAndValue(KnownName<TMarker>(), KnownValue<TMarker>());
        static ColdEnum() { }
        private ColdEnum(string name, string value) : base(name, value) => ConstructorCalls++;
    }

    private class ColdRoot<TMarker> : Enumeration<ColdRoot<TMarker>>
    {
        protected ColdRoot(string name, string value) : base(name, value) { }
    }

    private sealed class ColdChild<TMarker> : ColdRoot<TMarker>
    {
        public static int ConstructorCalls;
        public static ColdChild<TMarker> Known { get; } =
            FromNameAndValue<ColdChild<TMarker>>(KnownName<TMarker>(), KnownValue<TMarker>());
        static ColdChild() { }
        private ColdChild(string name, string value) : base(name, value) => ConstructorCalls++;
    }

    private class NestedInitializationRoot<TMarker> : Enumeration<NestedInitializationRoot<TMarker>>
    {
        public static NestedInitializationChildB<TMarker>? ReferencedByRoot { get; }

        static NestedInitializationRoot()
        {
            if (typeof(TMarker) == typeof(NestedRootCase))
                ReferencedByRoot = FromValue<NestedInitializationChildB<TMarker>>(KnownValue<TMarker>());
        }

        public NestedInitializationRoot() { }
        protected NestedInitializationRoot(string name, string value) : base(name, value) { }
    }

    private sealed class NestedInitializationChildA<TMarker> : NestedInitializationRoot<TMarker>
    {
        public static NestedInitializationChildB<TMarker> ReferencedSibling { get; } =
            FromValue<NestedInitializationChildB<TMarker>>(KnownValue<TMarker>());
        static NestedInitializationChildA() { }
        public NestedInitializationChildA() { }
    }

    private sealed class NestedInitializationChildB<TMarker> : NestedInitializationRoot<TMarker>
    {
        public static int ConstructorCalls;
        public static NestedInitializationChildB<TMarker> Known { get; } =
            FromValue<NestedInitializationChildB<TMarker>>(KnownValue<TMarker>());
        static NestedInitializationChildB() { }
        private NestedInitializationChildB(string name, string value) : base(name, value) => ConstructorCalls++;
    }

    private sealed class ThrowingConstructorEnum : Enumeration<ThrowingConstructorEnum>
    {
        public static readonly ConstructorFailureException Failure = new();
        public static int ParameterlessCalls;
        public static int ConstructorCalls;
        public ThrowingConstructorEnum() => ParameterlessCalls++;

        private ThrowingConstructorEnum(string name, string value) : base(name, value)
        {
            ConstructorCalls++;
            throw Failure;
        }
    }

    private sealed class ConstructorFailureException : Exception { }

    private sealed class InvalidConstructorEnum<TFailure> : Enumeration<InvalidConstructorEnum<TFailure>>
    {
        public static int ConstructorCalls;
        public static int ParameterlessCalls;
        public InvalidConstructorEnum() => ParameterlessCalls++;

        private InvalidConstructorEnum(string name, string value)
            : base(typeof(TFailure) == typeof(WrongName) ? name + "-wrong" : name,
                typeof(TFailure) == typeof(WrongValue) ? value + "-wrong" : value)
        {
            ConstructorCalls++;
        }
    }

    private sealed class SingleStringConstructorEnum : Enumeration<SingleStringConstructorEnum>
    {
        public SingleStringConstructorEnum(string value) : base(value) { }
    }

    private sealed class OptionalConstructorEnum : Enumeration<OptionalConstructorEnum>
    {
        public OptionalConstructorEnum(string name, string value = "optional") : base(name, value) { }
    }

    private sealed class ObjectConstructorEnum : Enumeration<ObjectConstructorEnum>
    {
        public ObjectConstructorEnum(object name, object value) : base(name.ToString()!, value.ToString()!) { }
    }
    private sealed class UnsupportedConstructorEnum : Enumeration<UnsupportedConstructorEnum>
    {
        public static int ConstructorCalls;
        public UnsupportedConstructorEnum(string name, string value, int requiredState = 7) : base(name, value) => ConstructorCalls++;
    }

    private abstract class AbstractEnum : Enumeration<AbstractEnum> { }

    private sealed class SerializedConstructorEnum<TMarker> : Enumeration<SerializedConstructorEnum<TMarker>>
    {
        public static int ConstructorCalls;
        public string Description { get; }

        private SerializedConstructorEnum(string name, string value) : base(name, value)
        {
            ConstructorCalls++;
            Description = $"{name}:{value}";
        }
    }

    private sealed class ColdValue { }
    private sealed class ColdName { }
    private sealed class ColdPair { }
    private sealed class ColdTypedValue { }
    private sealed class ColdTypedName { }
    private sealed class ColdTypedPair { }
    private sealed class WrongName { }
    private sealed class WrongValue { }
    private sealed class JsonCase { }
    private sealed class BsonCase { }
    private sealed class DifferentName { }
    private sealed class DifferentValue { }
    private sealed class NestedRootCase { }
    private sealed class NestedChildCase { }
    private sealed class UnknownDetailsCase { }
    private sealed class DefineFirstCase { }
    private sealed class CompleteLaterCase { }
    private sealed class RepeatedDefinitionCase { }
    private sealed class NullDetailsCase { }
    private sealed class DefinitionNameConflictCase { }
    private sealed class ConcurrentDefinitionsCase { }
    private sealed class ConflictingDefinitionsCase { }
    private sealed class JsonAliasCase { }
    private sealed class LookupFirstCase { }
    private sealed class DefinitionFirstCase { }
    private sealed class AtomicSnapshotCase { }
    private sealed class MixedPropertyCase { }
    private sealed class MixedFieldCase { }
    private sealed class MixedNameCase { }
    private sealed class MixedListCase { }
    private sealed class MixedDirectCase { }
    private sealed class DeferredComputedCase { }
    private sealed class NullableStaticStorageCase { }
    private sealed class CrossGenericOuterCase { }
    private sealed class LaterStoredMemberCase { }
    private sealed class CrossGenericInnerCase { }
    private sealed class JsonRoundtripCase { }
    private sealed class JsonLegacyCase { }
    private sealed class JsonAmbiguousCase { }
    private sealed class AliasFailureCase { }
}
