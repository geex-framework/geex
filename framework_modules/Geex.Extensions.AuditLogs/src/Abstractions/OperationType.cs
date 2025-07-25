using System;

namespace Geex.Extensions.AuditLogs
{
    using GqlOperationType = HotChocolate.Language.OperationType;

    public class OperationType : Enumeration<OperationType>
    {
        public static OperationType Query { get; } = FromValue(nameof(Query));
        public static OperationType Mutation { get; } = FromValue(nameof(Mutation));
        public static OperationType Subscription { get; } = FromValue(nameof(Subscription));

        public static implicit operator GqlOperationType(OperationType operationType)
        {
            return Enum.Parse<GqlOperationType>(operationType.Value);
        }

        public static implicit operator OperationType (GqlOperationType operationType)
        {
            switch (operationType)
            {
                case GqlOperationType.Query:
                    return Query;
                    break;
                case GqlOperationType.Mutation:
                    return Mutation;
                    break;
                case GqlOperationType.Subscription:
                    return Subscription;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null);
            }
        }
    }
}
