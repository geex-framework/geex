namespace Geex.Extensions.Messaging
{
    public abstract class TodoType : Enumeration<TodoType>
    {
        protected TodoType(string name, string value) : base(name, value)
        {
        }
    }
}
