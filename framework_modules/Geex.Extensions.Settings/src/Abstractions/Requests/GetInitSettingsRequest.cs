using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Settings.Requests
{
    public record GetInitSettingsRequest : IRequest<List<ISetting>>
    {

    }
}
