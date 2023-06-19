using Microsoft.Extensions.Logging;

namespace Geex.Common.Abstractions
{
    public struct GeexboxEventId
    {
        public EventId val;

        public GeexboxEventId(string name)
        {
            this.val = new EventId(name.GetHashCode(), name);
        }

        public override string ToString()
        {
            return val.ToString();
        }

        public static implicit operator EventId(GeexboxEventId c)
        {
            return c.val;
        }

        public static readonly GeexboxEventId ApolloTracing = new(nameof(ApolloTracing));
    }
}
