using System;
using Geex.Abstractions;

namespace Geex.Common.AuditLogs.Enums
{
    using GqlOperationType = HotChocolate.Language.OperationType;

    public class OperationType : Enumeration<OperationType>
    {
        public static OperationType Query { get; } = new OperationType(nameof(Query));
        public static OperationType Mutation { get; } = new OperationType(nameof(Mutation));
        public static OperationType Subscription { get; } = new OperationType(nameof(Subscription));

        public OperationType(string value) : base(value)
        {
        }

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
