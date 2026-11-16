namespace Geex;

public interface IEnumerationFactory<TSelf> where TSelf : class, IEnumeration
{
    static abstract TSelf CreateEnumeration(string name, string value);
}
