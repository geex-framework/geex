using Microsoft.Extensions.Logging;

namespace Geex.Extensions.Logging.Commom
{
    public class GeexboxEventId
    {
        public EventId val;

        public GeexboxEventId(EventId eventId)
        {
            this.val = eventId;
        }

        public override string ToString()
        {
            return val.ToString();
        }
        // .... and so on....

        // User-defined conversion from MyColor to Color
        public static implicit operator EventId(GeexboxEventId c)
        {
            return c.val;
        }
        //  User-defined conversion from Color to MyColor
        public static implicit operator GeexboxEventId(EventId c)
        {
            return new GeexboxEventId(c);
        }
    }
}
