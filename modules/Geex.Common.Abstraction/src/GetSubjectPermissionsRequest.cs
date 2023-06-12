using System.Collections.Generic;
using MediatR;

namespace Geex.Common.Abstraction
{
    public class GetSubjectPermissionsRequest : IRequest<IEnumerable<string>>
    {
        public string Subject { get; }

        public GetSubjectPermissionsRequest(string subject)
        {
            Subject = subject;
        }
    }
}
