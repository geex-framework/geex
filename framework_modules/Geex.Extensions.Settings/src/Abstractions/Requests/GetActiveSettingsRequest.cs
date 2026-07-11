using System.Collections.Generic;
using MediatX;

namespace Geex.Extensions.Settings.Requests
{
    public record GetActiveSettingsRequest : IRequest<List<ISetting>>
    {

    }
}
