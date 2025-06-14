using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatX;

namespace Geex.Extensions.Identity.Requests
{
    public record DeleteUserRequest : IRequest<bool>
    {
        public string Id { get; set; }
    }
}
