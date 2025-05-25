using System.Collections.Generic;
using MediatR;

namespace Geex.Extensions.Settings.Requests
{
    public record GetInitSettingsRequest : IRequest<List<ISetting>>
    {

    }
}
