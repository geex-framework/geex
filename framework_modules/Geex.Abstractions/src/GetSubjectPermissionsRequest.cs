using System.Collections.Generic;
using MediatX;

namespace Geex
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
