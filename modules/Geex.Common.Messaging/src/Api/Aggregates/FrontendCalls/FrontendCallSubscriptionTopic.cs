using Geex.Common.Messaging.Api.Aggregates.FrontendCalls;

namespace Geex.Common.Messaging.Core.Aggregates.FrontendCalls
{
    public class FrontendCallSubscriptionTopic
    {
        public string UserId { get; set; }
        public FrontendCallType FrontendCallType { get; set; }
    }
}
