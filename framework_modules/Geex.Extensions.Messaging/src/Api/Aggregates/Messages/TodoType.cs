
using Geex;
using Geex.Abstractions;

namespace KuanFang.Rms.MessageManagement.Messages
{
    public abstract class TodoType : Enumeration<TodoType>
    {
        protected TodoType(string name, string value) : base(name, value)
        {
        }
    }
}
