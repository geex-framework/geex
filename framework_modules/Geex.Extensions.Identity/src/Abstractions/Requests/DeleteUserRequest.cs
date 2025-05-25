using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace Geex.Extensions.Identity.Requests
{
    public record DeleteUserRequest : IRequest<bool>
    {
        public string Id { get; set; }
    }
}
