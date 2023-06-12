using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Geex.Common.Abstractions;
using Geex.Common.Gql.Types;
using HotChocolate;

namespace Geex.Common.Messaging.Api.Aggregates.FrontendCalls
{
    public interface IFrontendCall
    {
        public FrontendCallType FrontendCallType { get; }
        public JsonNode? Data { get; }
    }
    public class FrontendCallType : Enumeration<FrontendCallType>
    {
        protected FrontendCallType(string name, string value) : base(name, value)
        {
        }

        public static FrontendCallType NewMessage { get; } = new(nameof(NewMessage), nameof(NewMessage));
    }

}
